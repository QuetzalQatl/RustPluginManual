PLUGIN.Title        = "Airdrops"
PLUGIN.Description  = "Custom Airdrop Commands"
PLUGIN.Author       = "LaserHydra"
PLUGIN.Version      = V(1,0,2)
PLUGIN.HasConfig    = false
PLUGIN.ResourceId 	= 860

local debug = false
function PLUGIN:Init()
    self:LoadDefaultConfig()
	command.AddChatCommand("massdrop", self.Object, "cmdMassdrop")
    command.AddChatCommand("airdrop", self.Object, "cmdAirdrop")
end

--								WILL MAYBE IMPLEMENT THIS
function PLUGIN:LoadDefaultConfig()
	self.Config.AIRDROP_CMD = self.Config.AIRDROP_CMD or {"massdrop"}
	self.Config.MASSDROP_CMD = self.Config.MASSDROP_CMD or {"massdrop"}
	self.Config.CONSOLECMD_AIRDROP_TOPLAYER =  self.Config.CONSOLECMD_AIRDROP_TOPLAYER or {"airdrop.toplayer"}
	self.Config.CONSOLECMD_AIRDROP_TOPOS = self.Config.CONSOLECMD_AIRDROP_TOPOS or {"airdrop.topos"}
	self.Config.CONSOLECMD_AIRDROP_MASSDROP = self.Config.CONSOLECMD_AIRDROP_MASSDROP or {"airdrop.massdrop"}
end

--				MASSDROP
function PLUGIN:cmdMassdrop(player, cmd, args)
	if player.net.connection.authLevel == 0 then return end
	if args.Length == 1 then
		local dropAmount = tonumber(args[0])
		if dropAmount < 2 then return end
		rust.BroadcastChat("AIRDROP", "" .. dropAmount .. " Airdrops summoned by " .. player.displayName)
		rust.RunServerCommand("airdrop.massdrop " .. dropAmount)
	else
		rust.SendChatMessage(player, "AIRDROP", "Syntax: /massdrop [AMOUNT]")
	end
end

--			AIRDROP / TOPLAYER
function PLUGIN:cmdAirdrop(player, cmd, args)
    if player.net.connection.authLevel == 0 then return end
	if args.Length == 1 then
        local targetPlayer = global.BasePlayer.Find(args[0])
        rust.BroadcastChat("AIRDROP", "Airdrop for a specific player summoned by "  .. player.displayName)
        rust.RunServerCommand("airdrop.toplayer " .. targetPlayer.displayName)
    else
        rust.BroadcastChat("AIRDROP", "Airdrop summoned by " .. player.displayName)
		rust.RunServerCommand("event.run")
    end
end