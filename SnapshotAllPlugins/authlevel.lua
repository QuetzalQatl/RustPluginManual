PLUGIN.Title = "Auth Level"
PLUGIN.Version = V(0, 1, 8)
PLUGIN.Description = "Add or remove players as owner/moderator/player via command."
PLUGIN.Author = "Wulfspider"
PLUGIN.Url = "http://forum.rustoxide.com/resources/702/"
PLUGIN.ResourceId = 702
PLUGIN.HasConfig = true

local debug = false

-- TODO:
---- Figure out a method to set player auth level instantly without having to restart client

function PLUGIN:Init()
    self:LoadDefaultConfig()
    command.AddChatCommand(self.Config.Settings.ChatCommand, self.Plugin, "cmdAuthLevel")
    command.AddConsoleCommand(self.Config.Settings.ConsoleCommand, self.Plugin, "ccmdAuthLevel")
end

function PLUGIN:cmdAuthLevel(player, cmd, args)
    if player and not self:PermissionsCheck(player) then
        rust.SendChatMessage(player, self.Config.Settings.ChatName, self.Config.Messages.NoPermission)
        return
    end
    if args.Length ~= 2 then
        local message = self.Config.Messages.ChatHelp:gsub("{command}", self.Config.Settings.ChatCommand)
        rust.SendChatMessage(player, self.Config.Settings.ChatNameHelp, message)
        return
    end
    local targetPlayer = global.BasePlayer.Find(args[0])
    if not targetPlayer then
        rust.SendChatMessage(player, self.Config.Settings.ChatName, self.Config.Messages.InvalidTarget)
        return
    end
    local authLevel = string.lower(args[1])
    if authLevel ~= "2" and authLevel ~= "1" and authLevel ~= "0" and authLevel ~= "admin" and authLevel ~= "owner"
            and authLevel ~= "mod" and authLevel ~= "moderator" and authLevel ~= "guest" and authLevel ~= "player" then
        rust.SendChatMessage(player, self.Config.Settings.ChatName, self.Config.Messages.InvalidAuthLevel)
        return
    end
    local steamId = rust.UserIDFromPlayer(targetPlayer)
    if authLevel == "2" or authLevel == "admin" or authLevel == "owner" then
        rust.RunServerCommand("ownerid", steamId, targetPlayer.displayName)
    end
    if authLevel == "1" or authLevel == "mod" or authLevel == "moderator" then
        rust.RunServerCommand("moderatorid", steamId, targetPlayer.displayName)
    end
    if authLevel == "0" or authLevel == "guest" or authLevel == "player" then
        rust.RunServerCommand("removeowner", steamId)
        rust.RunServerCommand("removemoderator", steamId)
    end
    rust.RunServerCommand("server.writecfg")
    local message = self.Config.Messages.AuthLevelSet:gsub("{level}", authLevel):gsub("{player}", targetPlayer.displayName)
    rust.SendChatMessage(player, self.Config.Settings.ChatName, message)
end

function PLUGIN:ccmdAuthLevel(args)
    local player = nil
    if args.connection then player = args.connection.player end
    if player and not self:PermissionsCheck(player) then args:ReplyWith(self.Config.Messages.NoPermission); return end
    if not args:HasArgs(2) then
        local message = self.Config.Messages.ConsoleHelp:gsub("{command}", self.Config.Settings.ConsoleCommand)
        if player then args:ReplyWith(message) else print(message) end
        return
    end
    local targetPlayer = global.BasePlayer.Find(args:GetString(0))
    if not targetPlayer then args:ReplyWith(self.Config.Messages.InvalidTarget); return end
    local authLevel = args:GetString(1)
    if authLevel ~= "2" and authLevel ~= "1" and authLevel ~= "0" and authLevel ~= "admin" and authLevel ~= "owner"
            and authLevel ~= "mod" and authLevel ~= "moderator" and authLevel ~= "guest" and authLevel ~= "player" then
        if player then args:ReplyWith(self.Config.Messages.InvalidAuthLevel) else print(self.Config.Messages.InvalidAuthLevel) end
        return
    end
    local steamId = rust.UserIDFromPlayer(targetPlayer)
    if authLevel == "2" or authLevel == "admin" or authLevel == "owner" then
        rust.RunServerCommand("ownerid", steamId, targetPlayer.displayName)
    end
    if authLevel == "1" or authLevel == "mod" or authLevel == "moderator" then
        rust.RunServerCommand("moderatorid", steamId, targetPlayer.displayName)
    end
    if authLevel == "0" or authLevel == "guest" or authLevel == "player" then
        rust.RunServerCommand("removeowner", steamId)
        rust.RunServerCommand("removemoderator", steamId)
     end
    rust.RunServerCommand("server.writecfg")
    local message = self.Config.Messages.AuthLevelSet:gsub("{level}", authLevel):gsub("{player}", targetPlayer.displayName)
    if player then args:ReplyWith(message) else print(message) end
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
    self.Config.Settings.ChatCommand = self.Config.Settings.ChatCommand or "authlevel"
    self.Config.Settings.ChatName = self.Config.Settings.ChatName or "ADMIN"
    self.Config.Settings.ChatNameHelp = self.Config.Settings.ChatNameHelp or "HELP"
    self.Config.Settings.ConsoleCommand = self.Config.Settings.ConsoleCommand or "global.authlevel"
    self.Config.Messages = self.Config.Messages or {}
    self.Config.Messages.AuthLevelSet = self.Config.Messages.AuthLevelSet or "Auth level set to {level} for {player}!"
    self.Config.Messages.ChatHelp = self.Config.Messages.ChatHelp or "Use /authlevel player authlevel to set the auth level for player"
    self.Config.Messages.ConsoleHelp = self.Config.Messages.ConsoleHelp or "Use authlevel player authlevel to set the auth level for player"
    self.Config.Messages.InvalidAuthLevel = self.Config.Messages.InvalidAuthLevel or "Invalid auth level! Valid levels are 0 (player), 1 (moderator), and 2 (owner)"
    self.Config.Messages.InvalidTarget = self.Config.Messages.InvalidTarget or "Invalid player name! Please try again"
    self.Config.Messages.NoPermission = self.Config.Messages.NoPermission or "You do not have permission to use this command!"
    self:SaveConfig()
end
