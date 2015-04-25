PLUGIN.Title = "AFK Kick"
PLUGIN.Version = V(0, 1, 3)
PLUGIN.Description = "Kicks players that are AFK (away from keyboard) for set amount of seconds."
PLUGIN.Author = "Wulfspider"
PLUGIN.Url = "http://forum.rustoxide.com/plugins/765/"
PLUGIN.ResourceId = 765
PLUGIN.HasConfig = true

local debug = false

local afkTimer = {}
function PLUGIN:Init()
    self:LoadDefaultConfig()
end

function PLUGIN:PositionCheck(player)
    local steamId = rust.UserIDFromPlayer(player)
    local junk = player.transform.position; local start = player.transform.position
    afkTimer[steamId] = timer.Repeat(self.Config.Settings.AfkLimit, 0, function()
        local current = player.transform.position
        if debug then
            print("[" .. self.Title .. "] Start position of " .. steamId .. ": " .. tostring(start))
            print("[" .. self.Title .. "] Current position of " .. steamId .. ": " .. tostring(current))
        end
        if start.x == current.x and start.y == current.y and start.z == current.z then
            local message = self.Config.Messages.YouKicked:gsub("{afklimit}", self.Config.Settings.AfkLimit)
            Network.Net.sv:Kick(player.net.connection, message)
            if self.Config.Settings.Broadcast ~= "false" then
                local message = self.Config.Messages.PlayerKicked:gsub("{player}", player.displayName)
                rust.BroadcastChat(self.Config.Settings.ChatName, message)
            end
        end
        start = current
    end)
end

function PLUGIN:OnPlayerInit(player)
    if not player then return end
    if self.Config.Settings.AdminExcluded ~= "false" and self:PermissionsCheck(player.net.connection) then return end
    self:PositionCheck(player)
end

function PLUGIN:OnPlayerDisconnected(player)
    local steamId = rust.UserIDFromPlayer(player)
    if afkTimer[steamId] then afkTimer[steamId]:Destroy(); afkTimer[steamId] = nil end
end

function PLUGIN:Unload()
    if next(afkTimer) ~= nil then afkTimer:Destroy(); afkTimer = nil end
end

function PLUGIN:PermissionsCheck(connection)
    local authLevel; if connection then authLevel = connection.authLevel else authLevel = 2 end
    local neededLevel = tonumber(self.Config.Settings.AuthLevel) or 2
    if debug then print(connection.username .. " has auth level: " .. tostring(authLevel)) end
    if authLevel and authLevel >= neededLevel then return true else return false end
end

function PLUGIN:LoadDefaultConfig()
    self.Config.Settings = self.Config.Settings or {}
    self.Config.Settings.AdminExcluded = self.Config.Settings.AdminExcluded or "true"
    self.Config.Settings.AuthLevel = tonumber(self.Config.Settings.AuthLevel) or 2
    self.Config.Settings.AfkLimit = tonumber(self.Config.Settings.AfkLimit) or 300 -- 5 minutes
    self.Config.Settings.Broadcast = self.Config.Settings.Broadcast or "true"
    self.Config.Settings.ChatName = self.Config.Settings.ChatName or "ADMIN"
    self.Config.Messages = self.Config.Messages or {}
    self.Config.Messages.PlayerKicked = self.Config.Messages.PlayerKicked or "{player} was kicked for being AFK!"
    self.Config.Messages.YouKicked = self.Config.Messages.YouKicked or "You were kicked for being AFK for {afklimit} seconds!"
    self:SaveConfig()
end
