PLUGIN.Title        = "AFK System"
PLUGIN.Description  = "Basic AFK System"
PLUGIN.Author       = "Merka"
PLUGIN.Version      = V(2, 0, 0)
PLUGIN.HasConfig    = true

function PLUGIN:Init()
  command.AddChatCommand("afk", self.Plugin, "cmdAfk")
    afkData = datafile.GetDataTable("afksystem")
    self:LoadDefaultConfig()
 end
 function PLUGIN:LoadDefaultConfig()
    self.Config.Settings = self.Config.Settings or {}
	self.Config.Settings.PluginPrefix = self.Config.Settings.PluginPrefix or "<color=cyan>**AFK**</color>"
    self.Config.Messages = self.Config.Messages or {}
    self.Config.Messages.IsAfk = self.Config.Messages.IsAfk or "{player} went AFK"
    self.Config.Messages.IsNotAfk = self.Config.Messages.IsNotAfk or "{player} is no longer AFK"
    self:SaveConfig()
end

 function PLUGIN:cmdAfk(player,  command, arg)
    local userId = rust.UserIDFromPlayer(player)
	local prefixandname = self.Config.Settings.PluginPrefix.." "..player.displayName
    if afkData[userId] then
        afkData[userId] = nil
        local message = string.gsub(self.Config.Messages.IsNotAfk, "{player}", prefixandname)
        rust.BroadcastChat(message)
        player:EndSleeping()
    else
	    afkData[userId] = true
        local message = string.gsub(self.Config.Messages.IsAfk, "{player}", prefixandname)
        rust.BroadcastChat(message)
        player:StartSleeping()
    end
    datafile.SaveDataTable("afksystem")

end
 function PLUGIN:OnPlayerSleepEnded(player)
    local userID = rust.UserIDFromPlayer(player)
	local message = string.gsub(self.Config.Messages.IsNotAfk, "{player}", self.Config.Settings.PluginPrefix.." "..player.displayName)
	if afkData[userID] then
	rust.BroadcastChat(message)
	afkData[userID] = nil
	end
end
