PLUGIN.Title = "Reserved"
PLUGIN.Version = V(0, 1, 6)
PLUGIN.Description = "Reserves a number of slots so that reserved players can connect."
PLUGIN.Author = "Wulfspider"
PLUGIN.Url = "http://forum.rustoxide.com/plugins/674/"
PLUGIN.ResourceId = 674
PLUGIN.HasConfig = true

local debug = false

-- TODO:
---- Add console command
---- Fix slots action not working due to args.Length check

function PLUGIN:Init()
    self:LoadDefaultConfig()
    command.AddChatCommand(self.Config.Settings.ChatCommand, self.Plugin, "cmdReserved")
    --command.AddConsoleCommand(self.Config.Settings.ConsoleCommand, self.Plugin, "ccmdReserved")
end

function PLUGIN:CanClientLogin(connection)
    local activePlayers = global.BasePlayer.activePlayerList.Count
    local maxPlayers = global.server.maxplayers
    local steamId = rust.UserIDFromConnection(connection)
    if debug then print(tostring(activePlayers) .. " players online. Max players " .. tostring(maxPlayers) .. " with " .. self.Config.Settings.ReservedSlots .. " reserved") end
    if activePlayers + tonumber(self.Config.Settings.ReservedSlots) >= maxPlayers then
        for key, value in pairs(self.Config.Settings.ReservedList) do if steamId == value then return end end
        return self.Config.Messages.Rejected
    end
end

function PLUGIN:cmdReserved(player, cmd, args)
    if player and not self:PermissionsCheck(player) then
        rust.SendChatMessage(player, self.Config.Settings.ChatName, self.Config.Messages.NoPermission)
        return
    end
    if args.Length ~= 2 then
        rust.SendChatMessage(player, self.Config.Settings.ChatName, self.Config.Messages.ChatHelp)
        return
    end
    local action = args[0]
    local argument = args[1]
    local list = self.Config.Settings.ReservedList
    local steamId
    if action == "add" or action == "remove" then
        if string.len(args[1]) == 17 and string.match(argument, "%d+") then
            steamId = argument
        else
            local targetPlayer = global.BasePlayer.Find(argument)
            if targetPlayer then
                steamId = rust.UserIDFromPlayer(targetPlayer)
            else
                rust.SendChatMessage(player, self.Config.Settings.ChatName, self.Config.Messages.InvalidTarget)
                return
            end
        end
    end
    if action == nil or action ~= "add" and action ~= "remove" and action ~= "slots" then
        rust.SendChatMessage(player, self.Config.Settings.ChatName, self.Config.Messages.UnknownAction)
        return
    end
    if action == "add" then
        local reserved
        for key, value in pairs(list) do if steamId == value then reserved = true; break end end
        if reserved ~= true then
            table.insert(list, steamId)
            local message = string.gsub(self.Config.Messages.PlayerAdded, "{player}", player.displayName .. " (" .. steamId .. ")")
            rust.SendChatMessage(player, self.Config.Settings.ChatName, message)
        else
            local message = string.gsub(self.Config.Messages.AlreadyAdded, "{player}", player.displayName .. " (" .. steamId .. ")")
            rust.SendChatMessage(player, self.Config.Settings.ChatName, message)
        end
        self:SaveConfig()
        return
    end
    if action == "remove" then
        for key, value in pairs(list) do
            if steamId == value then
                table.remove(list, key)
                self:SaveConfig()
                local message = self.Config.Messages.PlayerRemoved:gsub("{player}", player.displayName .. " (" .. steamId .. ")")
                rust.SendChatMessage(player, self.Config.Settings.ChatName, message)
                break
            else
                local message = self.Config.Messages.NotReserved:gsub("{player}", player.displayName .. " (" .. steamId .. ")")
                rust.SendChatMessage(player, self.Config.Settings.ChatName, message)
            end
        end
        return
    end
    --[[if action == "slots" then
        self.Config.Settings.ReservedSlots = argument
        rust.SendChatMessage(player, self.Config.Settings.ChatName, "Reserved slots set to " .. argument)
        self:SaveConfig()
        return
    end]]
end

function PLUGIN:ccmdReserved(args)
    -- TODO
end

function PLUGIN:PermissionsCheck(player)
    local authLevel
    if player then authLevel = player.net.connection.authLevel else authLevel = 2 end
    local neededLevel = tonumber(self.Config.Settings.AuthLevel) or 2
    if debug then print(player.displayName .. " has auth level: " .. tostring(authLevel)) end
    if authLevel and authLevel >= neededLevel then return true else return false end
end

function PLUGIN:SendHelpText(player)
    if self:PermissionsCheck(player) then rust.SendChatMessage(player, self.Config.Settings.ChatNameHelp, self.Config.Messages.ChatHelp) end
end

function PLUGIN:LoadDefaultConfig()
    self.Config.Settings = self.Config.Settings or {}
    self.Config.Settings.AuthLevel = tonumber(self.Config.Settings.AuthLevel) or 2
    self.Config.Settings.ChatCommand = self.Config.Settings.ChatCommand or "reserved"
    self.Config.Settings.ChatName = self.Config.Settings.ChatName or "RESERVED"
    self.Config.Settings.ChatNameHelp = self.Config.Settings.ChatNameHelp or self.Config.Settings.HelpChatName or "HELP"
    self.Config.Settings.ConsoleCommand = self.Config.Settings.ConsoleCommand or "server.reserved"
    self.Config.Settings.ReservedList = self.Config.Settings.ReservedList or { "76561197960634567", "76561197994144473" }
    self.Config.Settings.ReservedSlots = tonumber(self.Config.Settings.ReservedSlots) or 10
    self.Config.Messages = self.Config.Messages or {}
    self.Config.Messages.AlreadyAdded = self.Config.Messages.AlreadyAdded or "{player} is already on the reserved list!"
    self.Config.Messages.ChatHelp = self.Config.Messages.ChatHelp or self.Config.Messages.ChatHelpText or "Use '/reserved add|remove|slots player|steamid|#'"
    self.Config.Messages.InvalidTarget = self.Config.Messages.InvalidTarget or "Invalid player or SteamID! Please try again"
    self.Config.Messages.NoPermission = self.Config.Messages.NoPermission or "You do not have permission to use this command!"
    self.Config.Messages.NotReserved = self.Config.Messages.NotReserved or "{player} is not on the reserved list!"
    self.Config.Messages.PlayerAdded = self.Config.Messages.PlayerAdded or "{player} has been added to the reserved list!"
    self.Config.Messages.PlayerRemoved = self.Config.Messages.PlayerRemoved or "{player} has been removed from the reserved list!"
    self.Config.Messages.Rejected = self.Config.Messages.Rejected or "Sorry, the maximum number of players are connected!"
    self.Config.Messages.UnknownAction = self.Config.Messages.UnknownAction or "Unknown command action! Use add, remove, or slots"
    self.Config.Settings.HelpChatName = nil -- Removed in ??
    self.Config.Messages.ChatHelpText = nil -- Removed in ??
    self:SaveConfig()
end
