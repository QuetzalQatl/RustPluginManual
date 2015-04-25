PLUGIN.Title = "Ping"
PLUGIN.Version = V(0, 2, 6)
PLUGIN.Description = "Player ping checking and with optional high ping rejection on join."
PLUGIN.Author = "Wulfspider"
PLUGIN.Url = "http://oxidemod.org/resources/656/"
PLUGIN.ResourceId = 656
PLUGIN.HasConfig = true

local debug = false

-- TODO:
---- Add command to change max ping, with permissions
------ permission.RegisterPermission("ping", self.Plugin)
------ UserHasPermission(player, "ping.max")

function PLUGIN:LoadDefaultConfig()
    self.Config.Settings = self.Config.Settings or {}
    self.Config.Settings.AuthLevel = tonumber(self.Config.Settings.AuthLevel) or 2
    self.Config.Settings.ChatCommand = self.Config.Settings.ChatCommand or "ping"
    self.Config.Settings.ConsoleCommand = self.Config.Settings.ConsoleCommand or "global.ping"
    self.Config.Settings.MaxPing = tonumber(self.Config.Settings.MaxPing) or 200 -- Milliseconds
    self.Config.Settings.PingKick = self.Config.Settings.PingKick or "true"
    self.Config.Settings.ShowKick = self.Config.Settings.ShowKick or "true"

    self.Config.Messages = self.Config.Messages or {}
    self.Config.Messages.ChatHelp = self.Config.Messages.ChatHelp or "Use /ping player to check target player's ping"
    self.Config.Messages.ConsoleHelp = self.Config.Messages.ConsoleHelp or "Use player.ping player to check target player's ping"
    self.Config.Messages.InvalidTarget = self.Config.Messages.InvalidTarget or "Invalid target player! Please try again"
    self.Config.Messages.NoPermission = self.Config.Messages.NoPermission or "You do not have permission to use this command!"
    self.Config.Messages.PlayerCheck = self.Config.Messages.PlayerCheck or self.Config.Messages.PingCheck or "{player} has a ping of {ping}ms"
    self.Config.Messages.SelfCheck = self.Config.Messages.SelfCheck or "You have a ping of {ping}ms"
    self.Config.Messages.PlayerConnected = self.Config.Messages.PlayerConnected  or "{player} ({steamid}) connected with {ping}ms ping"
    self.Config.Messages.PlayerKicked = self.Config.Messages.PlayerKicked or "{player} was kicked for high ping ({ping}ms)"
    self.Config.Messages.Rejected = self.Config.Messages.Rejected or "Your ping is too high for this server!"

    self.Config.Settings.ChatName = nil -- Removed in 0.2.5
    self.Config.Settings.ChatNameHelp = nil -- Removed in 0.2.5
    self.Config.Messages.PingCheck = nil -- Removed in 0.2.5

    self:SaveConfig()
end

function PLUGIN:Init()
    self:LoadDefaultConfig()
    command.AddChatCommand(self.Config.Settings.ChatCommand, self.Plugin, "ChatCommand")
    command.AddConsoleCommand(self.Config.Settings.ConsoleCommand, self.Plugin, "ConsoleCommand")
end

local function ParseMessage(message, values)
    for key, value in pairs(values) do 
        message = message:gsub("{" .. key .. "}", value)
    end
    return message
end

local function HasPermission(self, connection)
    local authLevel
    if connection then
        authLevel = connection.authLevel
    else
        authLevel = 2
    end
    if debug then print(connection.username .. " has auth level: " .. tostring(authLevel)) end
    local neededLevel = tonumber(self.Config.Settings.AuthLevel) or 2
    if authLevel and authLevel >= neededLevel then
        return true
    else
        return false
    end
end

local function FindPlayer(self, player, target)
    local targetPlayer = global.BasePlayer.Find(target)
    if not targetPlayer then
        if not player then
            print("[" .. self.Title .. "] " .. self.Config.Messages.InvalidTarget)
        else
            rust.SendChatMessage(player, self.Config.Messages.InvalidTarget)
        end
        return
    end
    return targetPlayer
end

local function Kick(connection, message) Network.Net.sv:Kick(connection, message) end

local function Ping(connection) return Network.Net.sv:GetAveragePing(connection) end

function PLUGIN:PingKick(connection)
    local ping = Ping(connection)
    if self.Config.Settings.PingKick == "true" then
        if ping >= self.Config.Settings.MaxPing then
            if self.Config.Settings.ShowKick ~= "false" then
                local message = ParseMessage(self.Config.Messages.PlayerKicked, { player = connection.username, ping = ping })
                rust.BroadcastChat(message)
            end
            Kick(connection, self.Config.Messages.Rejected)
        end
    end
    return ping
end

function PLUGIN:OnPlayerConnected(packet)
    if not packet then return end
    if not packet.connection then return end
    local connection = packet.connection
    local steamId = rust.UserIDFromConnection(connection)
    local ping = self:PingKick(connection)
    local message = ParseMessage(self.Config.Messages.PlayerConnected, { player = connection.username, steamid = steamId, ping = ping })
    print("[" .. self.Title .. "] " .. message)
end

function PLUGIN:ChatCommand(player, cmd, args)
    if args.Length > 1 then
        rust.SendChatMessage(player, self.Config.Messages.ChatHelp)
        return
    end
    if args.Length == 1 then
        if player and not HasPermission(self, player.net.connection) then
            rust.SendChatMessage(player, self.Config.Messages.NoPermission)
            return
        end
        local targetPlayer = FindPlayer(self, player, args[0])
        if targetPlayer then
            local ping = self:PingKick(targetPlayer.net.connection)
            local message = ParseMessage(self.Config.Messages.PlayerCheck, { player = targetPlayer.displayName, ping = ping })
            rust.SendChatMessage(player, message)
        end
    else
        local ping = Ping(player.net.connection)
        local message = ParseMessage(self.Config.Messages.SelfCheck, { player = player.displayName, ping = ping })
        rust.SendChatMessage(player, message)
    end
end

function PLUGIN:ConsoleCommand(args)
    local player
    if args.connection then
        player = args.connection.player
    end
    if player and not HasPermission(self, args.connection) then
        args:ReplyWith(self.Config.Messages.NoPermission)
        return
    end
    if not args:HasArgs(1) then
        args:ReplyWith(self.Config.Messages.ConsoleHelp)
        return
    end
    local targetPlayer = FindPlayer(self, player, args:GetString(0))
    if targetPlayer then
        local ping = self:PingKick(targetPlayer.net.connection)
        local message = ParseMessage(self.Config.Messages.PlayerCheck, { player = targetPlayer.displayName, ping = ping })
        args:ReplyWith(message)
    end
end

function PLUGIN:SendHelpText(player)
    rust.SendChatMessage(player, self.Config.Messages.ChatHelp)
end
