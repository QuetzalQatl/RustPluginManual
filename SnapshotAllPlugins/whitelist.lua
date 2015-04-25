PLUGIN.Title = "Whitelist"
PLUGIN.Version = V(0, 2, 4)
PLUGIN.Description = "Restricts access to your server, automatically rejecting users whose SteamID is not whitelisted."
PLUGIN.Author = "Wulfspider"
PLUGIN.Url = "http://forum.rustoxide.com/plugins/654/"
PLUGIN.ResourceId = 654
PLUGIN.HasConfig = true

local debug = false

-- TODO:
---- Add console command

function PLUGIN:Init()
    self:LoadDefaultConfig()
    command.AddChatCommand(self.Config.Settings.ChatCommand, self.Plugin, "cmdWhitelist")
    --command.AddConsoleCommand(self.Config.Settings.ConsoleCommand, self.Plugin, "ccmdWhitelist")
end

function PLUGIN:CanClientLogin(connection)
    local steamId = rust.UserIDFromConnection(connection)
    if debug then print(connection.username .. " (" .. steamId .. ") connected") end
    for key, value in pairs(self.Config.Settings.Whitelist) do if steamId == value then return end end
    return self.Config.Messages.Rejected
end

function PLUGIN:cmdWhitelist(player, cmd, args)
    if not self:PermissionsCheck(player) then rust.SendChatMessage(player, self.Config.Settings.ChatName, self.Config.Messages.NoPermission) return end
    if args.Length ~= 2 then rust.SendChatMessage(player, self.Config.Settings.ChatName, self.Config.Messages.ChatHelp) return end
    local action, argument = args[0], args[1]
    local whitelist = self.Config.Settings.Whitelist
    local steamId
    if string.len(args[1]) == 17 and argument:match("%d+") then
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
    if action == nil or action ~= "add" and action ~= "remove" then
        rust.SendChatMessage(player, self.Config.Settings.ChatName, self.Config.Messages.UnknownAction)
        return
    elseif action == "add" then
        local whitelisted
        for key, value in pairs(whitelist) do if steamId == value then whitelisted = true; break end end
        if whitelisted ~= true then
            table.insert(whitelist, steamId)
            self:SaveConfig()
            local message = self.Config.Messages.PlayerAdded:gsub("{player}", player.displayName .. " (" .. steamId .. ")")
            rust.SendChatMessage(player, self.Config.Settings.ChatName, message)
        else
            local message = self.Config.Messages.AlreadyAdded:gsub("{player}", player.displayName .. " (" .. steamId .. ")")
            rust.SendChatMessage(player, self.Config.Settings.ChatName, message)
        end
    elseif action == "remove" then
        for key, value in pairs(whitelist) do
            if steamId == value then
                table.remove(whitelist, key)
                self:SaveConfig()
                local message = self.Config.Messages.PlayerRemoved:gsub("{player}", player.displayName .. " (" .. steamId .. ")")
                rust.SendChatMessage(player, self.Config.Settings.ChatName, message)
                break
            else
                local message = self.Config.Messages.NotWhitelisted:gsub("{player}", player.displayName .. " (" .. steamId .. ")")
                rust.SendChatMessage(player, self.Config.Settings.ChatName, message)
            end
        end
    end
end

function PLUGIN:ccmdWhitelist(args)
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
    self.Config.Settings.ChatCommand = self.Config.Settings.ChatCommand or "whitelist"
    self.Config.Settings.ChatName = self.Config.Settings.ChatName or "WHITELIST"
    self.Config.Settings.ChatNameHelp = self.Config.Settings.ChatNameHelp or self.Config.Settings.HelpChatName or "HELP"
    self.Config.Settings.ConsoleCommand = self.Config.Settings.ConsoleCommand or "server.whitelist"
    self.Config.Settings.Whitelist = self.Config.Settings.Whitelist or { "76561197960634567", "76561197994144473" }
    self.Config.Messages = self.Config.Messages or {}
    self.Config.Messages.AlreadyAdded = self.Config.Messages.AlreadyAdded or "{player} is already whitelisted!"
    self.Config.Messages.ChatHelp = self.Config.Messages.ChatHelp or self.Config.Messages.ChatHelpText or "Use '/whitelist add|remove player|steamid'"
    self.Config.Messages.InvalidTarget = self.Config.Messages.InvalidTarget or "Invalid player or SteamID! Please try again"
    self.Config.Messages.NoPermission = self.Config.Messages.NoPermission or "You do not have permission to use this command!"
    self.Config.Messages.NotWhitelisted = self.Config.Messages.NotWhitelisted or "{player} is not whitelisted!"
    self.Config.Messages.PlayerAdded = self.Config.Messages.PlayerAdded or "{player} has been added to the whitelist!"
    self.Config.Messages.PlayerRemoved = self.Config.Messages.PlayerRemoved or "{player} has been removed from the whitelist!"
    self.Config.Messages.Rejected = self.Config.Messages.Rejected or "Sorry, you are not whitelisted!"
    self.Config.Messages.UnknownAction = self.Config.Messages.UnknownAction or "Unknown command action! Use add or remove"
    self.Config.Settings.HelpChatName = nil -- Removed in 0.2.3
    self.Config.Messages.ChatHelpText = nil -- Removed in 0.2.3
    self:SaveConfig()
end
