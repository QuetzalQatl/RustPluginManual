PLUGIN.Title = "Respawner"
PLUGIN.Version = V(0, 1, 7)
PLUGIN.Description = "Automatically respawns players after they die."
PLUGIN.Author = "Wulfspider"
PLUGIN.Url = "http://oxidemod.org/resources/669/"
PLUGIN.ResourceId = 669
PLUGIN.HasConfig = true

function PLUGIN:LoadDefaultConfig()
    self.Config.AutoWakeUp = self.Config.AutoWakeUp or "true"
    self.Config.SameLocation = self.Config.SameLocation or "false"
    self.Config.SleepingBags = self.Config.SleepingBags or "true"
    self.Config.Settings = nil -- Removed in 0.1.7
    self:SaveConfig()
end

function PLUGIN:Init() self:LoadDefaultConfig() end

local FindForPlayer = global.SleepingBag.FindForPlayer.methodarray[0]
local function FindSleepingBags(steamId)
    param = util.TableToArray({ steamId, true })
    util.ConvertAndSetOnArray(param, 0, steamId, System.UInt64._type)
    return FindForPlayer:Invoke(nil, param)
end

function PLUGIN:RespawnPlayer(player)
    local steamId = rust.UserIDFromPlayer(player)
    local spawnTimer = {}
    spawnTimer[steamId] = timer.Once(1, function()
        if self.Config.SleepingBags == "true" then
            local sleepingBags = FindSleepingBags(steamId)
            if player and sleepingBags.Length > 0 then
                local sleepingBag = sleepingBags[math.random(1, sleepingBags.Length - 1)]
                player.transform.position = sleepingBag.transform.position
                player.transform.rotation = sleepingBag.transform.rotation
                player:Respawn(false)
                if self.Config.AutoWakeUp == "true" then player:EndSleeping() end
                return
            end
        end
        if self.Config.SameLocation == "true" then
            player.transform.position = player.transform.position
            player.transform.rotation = player.transform.rotation
            player:Respawn(false)
            if self.Config.AutoWakeUp == "true" then player:EndSleeping() end
            return
        end
        player:Respawn(true)
        if self.Config.AutoWakeUp == "true" then player:EndSleeping() end
    end, self.Plugin)
end

function PLUGIN:OnEntityDeath(entity)
    local player = entity:ToPlayer()
    if player and player:IsConnected() then self:RespawnPlayer(player) end
end
