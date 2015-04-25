PLUGIN.Title        = "Enhanced Ban System"
PLUGIN.Description  = "Ban system with advanced features"
PLUGIN.Author       = "#Domestos"
PLUGIN.Version      = V(2, 2, 3)
PLUGIN.HasConfig    = true
PLUGIN.ResourceID   = 693

local debugMode = false

function PLUGIN:Init()
    command.AddChatCommand("ban", self.Object, "cmdBan")
    command.AddChatCommand("unban", self.Object, "cmdUnban")
    command.AddChatCommand("kick", self.Object, "cmdKick")
    command.AddChatCommand("bancheck", self.Object, "cmdBanCheck")
    command.AddConsoleCommand("player.ban", self.Object, "ccmdBan")
    command.AddConsoleCommand("player.unban", self.Object, "ccmdUnban")
    command.AddConsoleCommand("player.kick", self.Object, "ccmdKick")
    self:LoadDataFile()
    self:LoadDefaultConfig()
end
local plugin_RustDB
function PLUGIN:OnServerInitialized()
    plugin_RustDB = plugins.Find("RustDB") or false
end

local DataFile = "ebsbanlist"
local BanData = {}
function PLUGIN:LoadDataFile()
    BanData = datafile.GetDataTable(DataFile) or {}
end
function PLUGIN:SaveDataFile()
    datafile.SaveDataTable(DataFile)
end

function PLUGIN:LoadDefaultConfig()
    self.Config.Settings = self.Config.Settings or {}
    self.Config.Settings.BroadcastBans = self.Config.Settings.BroadcastBans or "false"
    self.Config.Settings.LogToConsole = self.Config.Settings.LogToConsole or "true"
    self.Config.Settings.CheckUsableByEveryone = self.Config.Settings.CheckUsableByEveryone or "false"
    self.Config.Settings.ChatName = self.Config.Settings.ChatName or "SERVER"
    -- Messages
    self.Config.Messages = self.Config.Messages or {}
    self.Config.Messages.KickMessage = self.Config.Messages.KickMessage or "An admin kicked you for {reason}"
    self.Config.Messages.BanMessage = self.Config.Messages.BanMessage or "An admin banned you for {reason}"
    self.Config.Messages.DenyConnection = self.Config.Messages.DenyConnection or "You are banned on this server"
    self.Config.Messages.HelpText = self.Config.Messages.HelpText or "Use /bancheck to check if and for how long someone is abnned"
    self:SaveConfig()
end
-- --------------------------------
-- admin permission check
-- --------------------------------
local function IsAdmin(player)
    return player:GetComponent("BaseNetworkable").net.connection.authLevel > 0
end
-- --------------------------------
-- error and debug reporting
-- --------------------------------
local pluginTitle = PLUGIN.Title
local pluginVersion = string.match(tostring(PLUGIN.Version), "(%d+.%d+.%d+)")
local function error(msg)
    local message = "[Error] "..pluginTitle.."(v"..pluginVersion.."): "..msg
    local array = util.TableToArray({message})
    UnityEngine.Debug.LogError.methodarray[0]:Invoke(nil, array)
    print(message)
end
local function debug(msg)
    if not debugMode then return end
    local message = "[Debug] "..pluginTitle.."(v"..pluginVersion.."): "..msg
    local array = util.TableToArray({message})
    UnityEngine.Debug.LogWarning.methodarray[0]:Invoke(nil, array)
end
-- --------------------------------
-- removes expired bans
-- --------------------------------
function PLUGIN:CleanUpBanList()
    local now = time.GetUnixTimestamp()
    for key, value in pairs(BanData) do
        if BanData[key].expiration < now and BanData[key].expiration ~= 0 then
            BanData[key] = nil
            self:SaveDataFile()
        end
    end
end
-- --------------------------------
-- returns args as a table
-- --------------------------------
function PLUGIN:ArgsToTable(args, src)
    local argsTbl = {}
    if src == "chat" then
        local length = args.Length
        for i = 0, length - 1, 1 do
            argsTbl[i + 1] = args[i]
        end
        return argsTbl
    end
    if src == "console" then
        local i = 1
        while args:HasArgs(i) do
            argsTbl[i] = args:GetString(i - 1)
            i = i + 1
        end
        return argsTbl
    end
    return argsTbl
end
-- --------------------------------
-- handles chat command /ban
-- --------------------------------
function PLUGIN:cmdBan(player, cmd, args)
    local args = self:ArgsToTable(args, "chat")
    local target, reason, duration = args[1], args[2], args[3]
    if not IsAdmin(player) then
        rust.SendChatMessage(player, "You dont have permission to use this command")
        return
    end
    if not reason then
        rust.SendChatMessage(player, "Syntax: \"/ban <name|steamID|ip> <reason> <time[m|h|d] (optional)>\"")
        return
    end
    local targetPlayer = global.BasePlayer.Find(target)
    if not targetPlayer then
        rust.SendChatMessage(player, "Player not found")
        return
    end
    self:Ban(player, targetPlayer, reason, duration)
end
-- --------------------------------
-- handles console command player.ban
-- --------------------------------
function PLUGIN:ccmdBan(arg)
    local player
    if arg.connection then
        player = arg.connection.player
    end
    if player and not IsAdmin(player) then
        arg:ReplyWith("You dont have permission to use this command")
        return true
    end
    local args = self:ArgsToTable(arg, "console")
    local target, reason, duration = args[1], args[2], args[3]
    if player and not IsAdmin(player) then
        rust.SendChatMessage(player, "You dont have permission to use this command")
        return
    end
    if not reason then
        print("Syntax: \"player.ban <name|steamID|ip> <reason> <time[m|h|d] (optional)>\"")
        return
    end
    local targetPlayer = global.BasePlayer.Find(target)
    if not targetPlayer then
        print("Player not found")
        return
    end
    self:Ban(player, targetPlayer, reason, duration)
end
-- --------------------------------
-- handles chat command /unban
-- --------------------------------
function PLUGIN:cmdUnban(player, cmd, args)
    local args = self:ArgsToTable(args, "chat")
    local target = args[1]
    if not IsAdmin(player) then
        rust.SendChatMessage(player, "You dont have permission to use this command")
        return
    end
    if not target then
        rust.SendChatMessage(player, "Syntax: \"/unban <name|steamID|ip>\"")
        return
    end
    self:UnBan(player, target)
end
-- --------------------------------
-- handles console command player.unban
-- --------------------------------
function PLUGIN:ccmdUnban(arg)
    local player
    if arg.connection then
        player = arg.connection.player
    end
    if player and not IsAdmin(player) then
        arg:ReplyWith("You dont have permission to use this command")
        return true
    end
    local args = self:ArgsToTable(arg, "console")
    local target = args[1]
    if not target then
        print("Syntax: \"player.unban <name|steamID|ip>\"")
        return
    end
    self:UnBan(player, target)
end
-- --------------------------------
-- handles chat command /kick
-- --------------------------------
function PLUGIN:cmdKick(player, cmd, args)
    local args = self:ArgsToTable(args, "chat")
    local target, reason = args[1], args[2]
    if not IsAdmin(player) then
        rust.SendChatMessage(player, "You dont have permission to use this command")
        return
    end
    if not reason then
        rust.SendChatMessage(player, "Syntax: \"/kick <name|steamID|ip> <reason>\"")
        return
    end
    local targetPlayer = global.BasePlayer.Find(target)
    if not targetPlayer then
        rust.SendChatMessage(player, "Player not found")
        return
    end
    self:Kick(player, targetPlayer, reason)
end
-- --------------------------------
-- handles console command player.kick
-- --------------------------------
function PLUGIN:ccmdKick(arg)
    local player
    if arg.connection then
        player = arg.connection.player
    end
    if player and not IsAdmin(player) then
        arg:ReplyWith("You dont have permission to use this command")
        return true
    end
    local args = self:ArgsToTable(arg, "console")
    local target, reason = args[1], args[2]
    if not reason then
        print("Syntax: \"player.kick <name|steamID|ip> <reason>\"")
        return
    end
    local targetPlayer = global.BasePlayer.Find(target)
    if not targetPlayer then
        print("Player not found")
        return
    end
    self:Kick(player, targetPlayer, reason)
end
-- --------------------------------
-- handles chat command /bancheck
-- --------------------------------
function PLUGIN:cmdBanCheck(player, cmd, args)
    local args = self:ArgsToTable(args, "chat")
    local target = args[1]
    if not IsAdmin(player) and self.Config.Settings.CheckUsableByEveryone == "false" then
        rust.SendChatMessage(player, "You dont have permission to use this command")
        return
    end
    if not target then
        rust.SendChatMessage(player, "Syntax: \"/bancheck <name|steamID|ip>\"")
        return
    end
    local now = time.GetUnixTimestamp()
    for key, value in pairs(BanData) do
        if BanData[key].name == target or BanData[key].steamID == target or BanData[key].IP == target then
            if BanData[key].expiration > now or BanData[key].expiration == 0 then
                if BanData[key].expiration == 0 then
                    rust.SendChatMessage(player, target.." is permanently banned")
                    return
                else
                    local expiration = BanData[key].expiration
                    local bantime = expiration - now
                    local days = string.format("%02.f", math.floor(bantime / 86400))
                    local hours = string.format("%02.f", math.floor(bantime / 3600 - (days * 24)))
                    local minutes = string.format("%02.f", math.floor(bantime / 60 - (days * 1440) - (hours * 60)))
                    local seconds = string.format("%02.f", math.floor(bantime - (days * 86400) - (hours * 3600) - (minutes * 60)))
                    rust.SendChatMessage(player, target.." is banned for "..tostring(days).." days "..tostring(hours).." hours "..tostring(minutes).." minutes "..tostring(seconds).." seconds")
                    return
                end
            end
        end
    end
    rust.SendChatMessage(player, target.." is not banned")
end
-- --------------------------------
-- kick player
-- --------------------------------
function PLUGIN:Kick(player, targetPlayer, reason)
    local targetName = targetPlayer.displayName
    local targetSteamID = rust.UserIDFromPlayer(targetPlayer)
    local targetInfo = targetName.." ("..targetSteamID..")"
    -- Kick player
    local kickMsg = string.gsub(self.Config.Messages.KickMessage, "{reason}", reason)
    Network.Net.sv:Kick(targetPlayer.net.connection, kickMsg)
    -- Output the bans
    if self.Config.Settings.BroadcastBans == "true" then
        rust.BroadcastChat(self.Config.Settings.ChatName, targetName.." has been kicked for "..reason)
    else
        if player then
            rust.SendChatMessage(player, targetName.." has been kicked for "..reason)
        else
            print(targetName.." has been kicked for "..reason)
        end
    end
    if self.Config.Settings.LogToConsole == "true" then
        if player then
            print(self.Title..": "..player.displayName.." kicked "..targetInfo)
        else
            print(self.Title..": Admin kicked "..targetInfo)
        end
    end
end
-- --------------------------------
-- unban player
-- --------------------------------
function PLUGIN:UnBan(player, target)
    debug("unban target: "..target)
    for key, _ in pairs(BanData) do
        if BanData[key].name == target or BanData[key].steamID == target or BanData[key].IP == target then
            debug("ban found")
            -- Send unban request to RustDB
            if plugin_RustDB then
                plugin_RustDB:RustDBUnban(BanData[key].steamID)
            end
            -- remove from banlist
            BanData[key].name = nil
            BanData[key].steamID = nil
            BanData[key].IP = nil
            BanData[key].expiration = nil
            BanData[key].reason = nil
            BanData[key] = nil
            debug("bandata nil: "..tostring(BanData[key] == nil))
            self:SaveDataFile()
            -- Output the bans
            if self.Config.Settings.BroadcastBans == "true" then
                rust.BroadcastChat(self.Config.Settings.ChatName, target.." has been unbanned")
            else
                if player then
                    rust.SendChatMessage(player, target.." has been unbanned")
                else
                    print(target.." has been unbanned")
                end
            end
            if self.Config.Settings.LogToConsole == "true" then
                if player then
                    print(self.Title..": "..player.displayName.." unbanned "..target)
                else
                    print(self.Title..": Admin unbanned "..target)
                end
            end
            return
        end
    end
    if player then
        rust.SendChatMessage(player, target.." not found in banlist")
    else
        print(target.." not found in banlist")
    end
end
-- --------------------------------
-- ban player
-- --------------------------------
function PLUGIN:Ban(player, targetPlayer, reason, duration)
    local targetName = targetPlayer.displayName
    local targetIP = targetPlayer.net.connection.ipaddress:match("([^:]*):")
    local targetSteamID = rust.UserIDFromPlayer(targetPlayer)
    -- Check if player is already banned
    local now = time.GetUnixTimestamp()
    for key, value in pairs(BanData) do
        if BanData[key].steamID == targetSteamID then
            if BanData[key].expiration > now or BanData[key].expiration == 0 then
                if player then
                    rust.SendChatMessage(player, targetName.." is already banned!")
                else
                    print(targetName.." is already banned!")
                end
                return
            else
                self:CleanUpBanList()
            end
        end
    end
    if not duration then -- If no time is given ban permanently
        local expiration = 0
        -- Insert data into the banlist
        BanData[targetSteamID] = {}
        BanData[targetSteamID].steamID = targetSteamID
        BanData[targetSteamID].name = targetName
        BanData[targetSteamID].expiration = expiration
        BanData[targetSteamID].IP = targetIP
        BanData[targetSteamID].reason = reason
        table.insert(BanData, BanData[targetSteamID])
        self:SaveDataFile()
        -- Send ban to RustDB
        if plugin_RustDB then
            plugin_RustDB:RustDBBan(player, targetName, targetSteamID, reason)
        end
        -- Kick target from server
        debug("kicked: "..targetName)
        local BanMsg = string.gsub(self.Config.Messages.BanMessage, "{reason}", reason)
        Network.Net.sv:Kick(targetPlayer.net.connection, BanMsg)
        -- Output bans
        if self.Config.Settings.BroadcastBans == "true" then
            rust.BroadcastChat(self.Config.Settings.ChatName, targetName.." has been permanently banned")
        else
            if player then
                rust.SendChatMessage(player, targetName.." has been permanently banned")
            else
                print(targetName.." has been permanently banned")
            end
        end
        if self.Config.Settings.LogToConsole == "true" then
            if player then
                print(self.Title..": "..player.displayName.." permanently banned "..targetName.." for "..reason)
            else
                print(self.Title..": Admin permanently banned "..targetName.." for "..reason)
            end
        end
    else -- if time is given, ban for time
        -- Check if time input is a valid format
        if string.len(duration) < 2 or not string.match(duration, "^%d*[mhd]$") then
            if player then
                rust.SendChatMessage(player, "Invalid time format")
            else
                print("Invalid time format")
            end
            return
        end
        -- Build time format
        local now = time.GetUnixTimestamp()
        local banTime = tonumber(string.sub(duration, 1, -2))
        local timeUnit = string.sub(duration, -1)
        local timeMult, timeUnitLong
        if timeUnit == "m" then
            timeMult = 60
            timeUnitLong = "minutes"
        elseif timeUnit == "h" then
            timeMult = 3600
            timeUnitLong = "hours"
        elseif timeUnit == "d" then
            timeMult = 86400
            timeUnitLong = "days"
        end
        local expiration = now + (banTime * timeMult)
        -- Insert data into the banlist
        BanData[targetSteamID] = {}
        BanData[targetSteamID].steamID = targetSteamID
        BanData[targetSteamID].name = targetName
        BanData[targetSteamID].expiration = expiration
        BanData[targetSteamID].IP = targetIP
        BanData[targetSteamID].reason = reason
        table.insert(BanData, BanData[targetSteamID])
        self:SaveDataFile()
        -- Kick target from server
        debug("kicked: "..targetName)
        local BanMsg = string.gsub(self.Config.Messages.BanMessage, "{reason}", reason)
        Network.Net.sv:Kick(targetPlayer.net.connection, BanMsg)
        -- Output bans
        if self.Config.Settings.BroadcastBans == "true" then
            rust.BroadcastChat(self.Config.Settings.ChatName, targetName.." has been banned for "..banTime.." "..timeUnitLong)
        else
            if player then
                rust.SendChatMessage(player, targetName.." has been banned for "..banTime.." "..timeUnitLong)
            else
                print(targetName.." has been banned for "..banTime.." "..timeUnitLong)
            end
        end
        if self.Config.Settings.LogToConsole == "true" then
            if player then
                print(self.Title..": "..player.displayName.." banned "..targetName.." for "..banTime.." "..timeUnitLong.." for "..reason)
            else
                print(self.Title..": Admin banned "..targetName.." for "..banTime.." "..timeUnitLong.." for "..reason)
            end
        end
    end
end
-- --------------------------------
-- checks for ban on player connects
-- --------------------------------
function PLUGIN:CanClientLogin(connection)
    local steamID = rust.UserIDFromConnection(connection)
    local ip = connection.ipaddress:match("([^:]*):")
    local name = connection.username
    local userInfo = name.." ("..steamID..")"
    local now = time.GetUnixTimestamp()
    for key, value in pairs(BanData) do
        if BanData[key].steamID == steamID or BanData[key].IP == ip then
            if BanData[key].expiration < now and BanData[key].expiration ~= 0 then
                self:CleanUpBanList()
                return
            else
                debug(userInfo.." connection denied")
                if self.Config.Settings.LogToConsole == "true" then
                    print(self.Title..": "..userInfo.." connection denied")
                end
                return self.Config.Messages.DenyConnection
            end
        else
            if BanData[key].name == name then
                print(self.Title..": Warning! the name from "..userInfo.." has been banned but is using another steam account now!")
                print(self.Title..": It might be the same person with another account or just someone else with the same name. Judge it by yourself")
            end
        end
    end
end
-- --------------------------------
-- sends helptext when /help is used
-- --------------------------------
function PLUGIN:SendHelpText(player)
    if self.Config.Settings.CheckUsableByEveryone == "true" then
        rust.SendChatMessage(player, self.Config.Messages.HelpText)
    end
end