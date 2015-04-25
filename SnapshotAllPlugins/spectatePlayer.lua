PLUGIN.Name = "Spectate Admin Handler"
PLUGIN.Title = "Spectate Admin Handler"
PLUGIN.Version = V(1, 0, 3)
PLUGIN.Description = "Choose who to spectate, hopefully a temporary plugin"
PLUGIN.Author = "Reneb"
PLUGIN.HasConfig = true

function PLUGIN:Init()
	command.AddConsoleCommand("spectate.player", self.Plugin, "ccmdSpectatePlayer")
	print(tostring(global.Type.Dead))
end

function PLUGIN:LoadDefaultConfig()
	self.Config.authLevel = 1
end

function PLUGIN:ccmdSpectatePlayer(arg)
	if not arg.connection then
        arg:ReplyWith("You must be a player to use this command.")
        return
    end
    sourcePlayer = arg.connection.player
    if (sourcePlayer:GetComponent("BaseNetworkable").net.connection.authLevel < self.Config.authLevel)  then
        arg:ReplyWith("You are not allowed to use this command.")
        return
    end
    if(arg.Args.Length == 0) then
    	arg:ReplyWith("You must specify a player name/steamid to spectate.")
    	return
    end
    targetPlayer, err = self:FindPlayer(arg.Args[0])
    if(not targetPlayer) then
    	arg:ReplyWith(err)
    	return
    end
    if(not sourcePlayer:IsSpectating() and not sourcePlayer:IsDead()) then
    	sourcePlayer:Hurt(1000,Rust.DamageType.Suicide,nil)
    end
    --[[print(tostring(global.Type.Spectating))
    sourcePlayer:ChangePlayerState(global.Type.Spectating,false)
    sourcePlayer:GetComponentInParent(global.BaseEntity._type):CancelInvoke("MetabolismUpdate")
    sourcePlayer:GetComponentInParent(global.BaseEntity._type):CancelInvoke("InventoryUpdate")]]
    timer.Once(0.1, function()
		if( not sourcePlayer:IsSpectating() ) then sourcePlayer:StartSpectating() end
		rust.SendChatMessage(sourcePlayer,"Spectating: " .. targetPlayer.displayName .. " - " .. rust.UserIDFromPlayer(targetPlayer))
		sourcePlayer:SetParent(targetPlayer)
    end)
end


function PLUGIN:FindPlayer( target )
	local steamid = false
	if(tonumber(target) ~= nil and string.len(target) == 17) then
		steamid = target
	end
	local targetplayer = false
	local allBasePlayer = UnityEngine.Object.FindObjectsOfTypeAll(global.BasePlayer._type)
	for i = 0, tonumber(allBasePlayer.Length - 1) do
		local currentplayer = allBasePlayer[ i ];
		if(steamid) then
			if(steamid == rust.UserIDFromPlayer(currentplayer)) then
				return currentplayer
			end
		else
			if(currentplayer.displayName == target) then
				return currentplayer
			elseif(string.find(currentplayer.displayName,target)) then
				if(targetplayer) then
					return false, "Multiple players found."
				end
				targetplayer = currentplayer
			end
		end
	end
	if(not targetplayer) then return false, "No players found." end
	return targetplayer
end