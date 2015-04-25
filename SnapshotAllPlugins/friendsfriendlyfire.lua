PLUGIN.Title = "Friends Friendly Fire"
PLUGIN.Version = V(1, 3, 3)
PLUGIN.Description = "Enable/disable friend fire for friends server-wide."
PLUGIN.Author = "Wulfspider"
PLUGIN.ResourceId = 687
PLUGIN.HasConfig = true

local debug = false

function PLUGIN:Init()
    self:LoadDefaultConfig()
    command.AddChatCommand(self.Config.Settings.ChatCommand, self.Plugin, "cmdFriendlyFire")
end

function PLUGIN:LoadDefaultConfig()
    self.Config.Settings = self.Config.Settings or {}
    self.Config.Settings.CmdAuthLevel = tonumber(self.Config.Settings.CmdAuthLevel) or 2
    self.Config.Settings.ChatCommand = self.Config.Settings.ChatCommand or "fff"
    self.Config.Settings.FriendlyFire = self.Config.Settings.FriendlyFire or self.Config.FriendlyFire or "true"
    self.Config.Messages = self.Config.Messages or {}
    self.Config.Messages.CantHurtFriend = self.Config.Messages.CantHurtFriend or "You can't hurt your friend!"
    self.Config.FriendlyFire = nil -- Removed in 0.3.2
    self:SaveConfig()
end

local friendsAPI
function PLUGIN:OnServerInitialized()
    friendsAPI = plugins.Find("0friendsAPI") or false
    if not friendsAPI then
        print("[" .. self.Title .. "] Friends API not found!")
        print("[" .. self.Title .. "] Get it here: http://forum.rustoxide.com/resources/686/")
        return
    end
end

function PLUGIN:OnPlayerAttack(attacker, hitinfo)
    if self.Config.FriendlyFire == "false" then
        if debug then print("[" .. self.Title .. "] HitEntity: " .. tostring(hitinfo.HitEntity)) end
        if hitinfo.HitEntity then
            if string.match(tostring(hitinfo.HitEntity), "BasePlayer") then
                local targetPlayer = hitinfo.HitEntity
                local targetSteamId = rust.UserIDFromPlayer(targetPlayer)
                local attackerSteamId = rust.UserIDFromPlayer(attacker)
                local hasFriend = friendsAPI.CallHook("HasFriend", attackerSteamId, targetSteamId)
                if debug then print("[" .. self.Title .. "] hasFriend: " .. tostring(hasFriend)) end
                if hasFriend then
                    rust.SendChatMessage(attacker, self.Config.Messages.CantHurtFriend)
                    hitinfo.damageTypes = new(Rust.DamageTypeList._type, nil)
                    hitinfo.HitMaterial = 0
                    return true
                end
            end
        end
    end
end

function PLUGIN:HasPermission(connection)
    local authLevel; if connection then authLevel = connection.authLevel else authLevel = 2 end
    local neededLevel = tonumber(self.Config.Settings.AuthLevel) or 2
    if debug then print(connection.username .. " has auth level: " .. tostring(authLevel)) end
    if authLevel and authLevel >= neededLevel then return true else return false end
end

function PLUGIN:cmdFriendlyFire(player)
    if not self:HasPermission(player.net.connection) then return false end
    if self.Config.Settings.FriendlyFire == "false" then
        self.Config.Settings.FriendlyFire = "true"
        rust.SendChatMessage(player, "Friendly Fire on")
    else
        self.Config.Settings.FriendlyFire = "false"
        rust.SendChatMessage(player, "Friendly Fire off")
    end
    self:SaveConfig()
end
