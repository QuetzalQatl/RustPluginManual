PLUGIN.Title        = "Player Talk"
PLUGIN.Description  = "	Talk trough players"
PLUGIN.Author       = "LaserHydra"
PLUGIN.Version      = V(0,9,0)
PLUGIN.HasConfig    = true
PLUGIN.ResourceId	= 866

function PLUGIN:Init()
	command.AddChatCommand("talk", self.Object, "cmdTalk")
	self:LoadDefaultConfig()	
end

function PLUGIN:LoadDefaultConfig()
	--self.Config.Authlevel = self.Config.Authlevel or "2"
	self.Config.NoPermission = self.Config.NoPermission or "You have no permission to use this command!"
end

function PLUGIN:cmdTalk(player, cmd, args)
	if player.net.connection.authLevel < 2 then
		rust.SendChatMessage(player, "PLAYER TALK", self.Config.NoPermission)
	else
		if args.Length == 2 then
			local TalkingPlayer = global.BasePlayer.Find(args[0])
			local Message = tostring(args[1])
			local userid = rust.UserIDFromPlayer(TalkingPlayer)
			if TalkingPlayer.net.connection.authLevel < 1 then
				rust.BroadcastChat("<color=#58ACFA>" .. TalkingPlayer.displayName, "</color><color=white>" .. Message .. "</color>", userid)
			else
				rust.BroadcastChat("<color=#ACFA58>" .. TalkingPlayer.displayName, "</color><color=white>" .. Message .. "</color>", userid)
			end
		else
			rust.SendChatMessage(player, "PLAYER TALK", "Syntax: /talk [PlayerName] ''[Message]''")		
		end
	end
end