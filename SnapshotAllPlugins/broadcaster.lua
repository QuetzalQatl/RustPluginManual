PLUGIN.Title        = "Broadcaster"
PLUGIN.Description  = "Easy Broadcasting"
PLUGIN.Author       = "LaserHydra"
PLUGIN.Version      = V(1,0,1)
PLUGIN.HasConfig    = true
PLUGIN.ResourceId 	= 863

function PLUGIN:Init()
command.AddChatCommand("bcast", self.Object, "cmdBroadcast")
self:LoadDefaultConfig()
end

function PLUGIN:LoadDefaultConfig()
	self.Config.NoPermissionMsg = self.Config.NoPermissionMsg or "You have no permission to use this command!"
	self:SaveConfig()
end

function PLUGIN:cmdBroadcast(player, cmd, args)
	if player.net.connection.authLevel > 0 then
		if args.Length >= 1 then
			local BroadcastText = tostring(args[0])
			rust.BroadcastChat("BROADCAST", BroadcastText)
		else
			rust.SendChatMessage(player, "BROADCAST", "Syntax: /bcast ''TEXT''")
		end
	else
		rust.SendChatMessage(player, "BROADCAST", self.Config.NoPermissionMsg)
	end
end