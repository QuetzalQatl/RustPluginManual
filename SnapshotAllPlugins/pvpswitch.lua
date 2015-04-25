PLUGIN.Title = "PVP Switch"
PLUGIN.Description = "Allows you to switch between pvp and pve"
PLUGIN.Author = "#Domestos"
PLUGIN.Version = V(1, 1, 1)
PLUGIN.HasConfig = true
PLUGIN.ResourceID = 694

function PLUGIN:Init()
    command.AddChatCommand("pvp", self.Object, "cmdSetConfig")
    self:LoadDefaultConfig()
end

local function QuoteSafe(string)
    return UnityEngine.StringExtensions.QuoteSafe(string)
end

function PLUGIN:ChatMessage(targetPlayer, chatName, msg)
    if msg then
        targetPlayer:SendConsoleCommand("chat.add "..QuoteSafe(chatName).." "..QuoteSafe(msg))
    else
        msg = chatName
        targetPlayer:SendConsoleCommand("chat.add SERVER "..QuoteSafe(msg))
    end
end
-- --------------------------------
-- admin permission check
-- --------------------------------
local function IsAdmin(player)
    if player:GetComponent("BaseNetworkable").net.connection.authLevel == 0 then
        return false
    end
    return true
end

function PLUGIN:cmdSetConfig(player)
    if not IsAdmin(player) then
        self:ChatMessage(player, "You dont have permission to use this command")
        return
    end
    if self.Config.PVP == "true" then
        self.Config.PVP = "false"
        self:ChatMessage(player, "pvp now disabled")
    else
        self.Config.PVP = "true"
        self:ChatMessage(player, "pvp now enabled")
    end
    self:SaveConfig()
end

function PLUGIN:LoadDefaultConfig()
    self.Config.PVP = self.Config.PVP or "true"
    self:SaveConfig()
end

function PLUGIN:OnPlayerAttack(attacker, hitinfo)
    if self.Config.PVP == "false" then
        if hitinfo.HitEntity then
            if hitinfo.HitEntity:ToPlayer() then
                return true
            end
        end
    end
end