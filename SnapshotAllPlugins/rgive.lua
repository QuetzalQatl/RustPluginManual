PLUGIN.Title        = "Random Item Give"
PLUGIN.Description  = "Give players a random item"
PLUGIN.Author       = "LaserHydra"
PLUGIN.Version      = V(1,0,5)
PLUGIN.HasConfig    = false
PLUGIN.ResourceId	= 929

function PLUGIN:Init()
    self:LoadDefaultConfig()
	command.AddChatCommand("rgive", self.Object, "cmdGive")
end

function PLUGIN:LoadDefaultConfig()
	self.Config.Sent = self.Config.Sent or "You have sent a random item!"
	self.Config.Recieved = self.Config.Recieved or "You have recieved a random item!"
	self.Config.NoPermissionMsg = self.Config.NoPermissionMsg or "You have no permission to use this command!"
	self.Config.Items = self.Config.Items or {
	"bow_hunting",
	"knife_bone",
	"pistol_eoka",
	"pistol_revolver",
	"rifle_ak",
	"rifle_bolt",
	"shotgun_pump",
	"shotgun_waterpipe",
	"smg_thompson",
	"spear_stone",
	"spear_wooden",
	"cupboard.tool",
	"lock.code",
	"lock.key",
	"box_wooden",
	"box_wooden_large",
	"campfire",
	"furnace",
	"lantern",
	"sleepingbag",
	"bucket_helmet",
	"burlap_gloves",
	"burlap_shirt",
	"burlap_shoes",
	"burlap_trousers",
	"coffeecan_helmet",
	"hazmat_boots",
	"hazmat_gloves",
	"hazmat_helmet",
	"hazmat_jacket",
	"hazmat_pants",
	"jacket_snow",
	"jacket_snow2",
	"jacket_snow3",
	"longsleeve_tshirt",
	"metal_facemask",
	"metal_plate_torso",
	"urban_boots",
	"urban_jacket",
	"urban_pants",
	"urban_shirt",
	"vagabond_jacket",
	"axe_salvaged",
	"explosive.time",
	"hammer",
	"hammer_salvaged",
	"hatchet",
	"icepick_salvaged",
	"pickaxe",
	"stonehatchet",
	"torch",
	"antiradpills",
	"bandage",
	"largemedkit",
	"syringe_medical",
	"trap_bear",
	"F1 Grenade",
	"Acoustic Guitar",
	"Camera"
	}
end

function PLUGIN:cmdGive(player, cmd, args)
	if	player.net.connection.authLevel > 0 then
		if args.Length == 1 then
			local targetPlayer = global.BasePlayer.Find(args[0])
			local item = self.Config.Items[math.random(1, #self.Config.Items)]
			rust.RunServerCommand("inv.giveplayer " .. targetPlayer.displayName .. " " .. item)
			print("<color=orange>[rGIVE] </color>" .. item ..  " given to "  .. targetPlayer.displayName)
			rust.SendChatMessage(player, "rGIVE", "" .. self.Config.Sent)
			rust.SendChatMessage(targetPlayer, "rGIVE", "" .. self.Config.Recieved)
		else
		rust.SendChatMessage(player, "rGIVE", "Syntax: /rgive [NAME]")
		end
	else
		rust.SendChatMessage(player, "rGIVE", "" .. self.Config.NoPermissionMsg)
	end
end



