PLUGIN.Title        = "Custom Name Colors"
PLUGIN.Description  = "change chat colors"
PLUGIN.Author       = "LaserHydra"
PLUGIN.Version      = V(1,0,2)
PLUGIN.HasConfig    = true
PLUGIN.ResourceId     = 979

function PLUGIN:Init()
	command.AddChatCommand("color", self.Object, "cmdColor")
	self:LoadDefaultConfig()
end

function PLUGIN:LoadDefaultConfig()
	self.Config.Colors = self.Config.Colors or {}
	
	self.Config.Colors.AuthLevel2 = self.Config.Colors.AuthLevel2 or "orange"
	self.Config.Colors.AuthLevel1 = self.Config.Colors.AuthLevel1 or "yellow"
	self.Config.Colors.AuthLevel0 = self.Config.Colors.AuthLevel0 or "lime"
	
	self:SaveConfig()
end

function PLUGIN:OnPlayerChat(arg)
	local message = arg:GetString(0, "text")
	local player = arg.connection.player
	local userid = rust.UserIDFromPlayer(player)
	
	if player.net.connection.authLevel == 0 then
		rust.BroadcastChat("<color=" .. self.Config.Colors.AuthLevel0 .. ">" .. player.displayName .. "</color><color=white>", "</color>" ..message, userid)
	elseif player.net.connection.authLevel == 1 then
		rust.BroadcastChat("<color=" .. self.Config.Colors.AuthLevel1 .. ">" .. player.displayName .. "</color><color=white>", "</color>" .. message, userid)
	elseif player.net.connection.authLevel == 2 then
		rust.BroadcastChat("<color=" .. self.Config.Colors.AuthLevel2 .. ">" .. player.displayName .. "</color><color=white>", "</color>" .. message, userid)
	else
	end
	return ""
end