PLUGIN.Name 		= "Airdrop Settings"
PLUGIN.Title 		= "Airdrop Settings"
PLUGIN.Description 	= "Allows to change the airdrop settings"
PLUGIN.Url 			= "http://amx-x.ru"
PLUGIN.Version 		= V(1, 0, 0)
PLUGIN.Author 		= "t0pdevice"
PLUGIN.HasConfig 	= true
PLUGIN.ResourceID 	= 785

local CanDrop = false
local Timer
local VectorZero
local Looting = {}
local AirdropLoot = {}

function PLUGIN:Init()
	command.AddChatCommand("airdrop_min", self.Plugin, "CmdMinimumPlayers")
	command.AddChatCommand("airdrop_freq", self.Plugin, "CmdDropFrequency")
	command.AddChatCommand("airdrop_stay", self.Plugin, "CmdSupplyStayTime")
	command.AddChatCommand("airdrop_reset", self.Plugin, "CmdLootReset")
	command.AddConsoleCommand("airdrop.run", self.Plugin, "CmdAirdropRun")
	command.AddConsoleCommand("airdrop.min", self.Plugin, "CmdMinimumPlayersConsole")	
	command.AddConsoleCommand("airdrop.freq", self.Plugin, "CmdDropFrequencyConsole")
	command.AddConsoleCommand("airdrop.stay", self.Plugin, "CmdSupplyStayTimeConsole")
	command.AddConsoleCommand("airdrop.reset", self.Plugin, "CmdLootResetConsole")
	
	self:CheckConfig()
	
	if (self.Config.AirdropSettings.DropFrequency > 0) then
		Timer = timer.Repeat(self.Config.AirdropSettings.DropFrequency * 60, 0, function() self:SpawnPlane() end)
	end
	
	VectorZero = new(UnityEngine.Vector3._type, nil)
	VectorZero.x = 0
	VectorZero.y = 0
	VectorZero.z = 0
	
	Looting.Container = {}
	self:LoadAirdropLoot()
end

function PLUGIN:FormatString(Text, Parameter, Value)
	return string.gsub(Text, Parameter, Value)
end

function PLUGIN:AllowDrop()
	if (self.Config.AirdropSettings.ManualRunDrop) then
		CanDrop = true
	end
end

function PLUGIN:LoadAirdropLoot()
    AirdropLoot = datafile.GetDataTable("airdrop_settings") or {}
	
	if (not AirdropLoot.Settings) then
		self:CreateDefaultLoot()
		self:SaveAirdropLoot()
	end
end

function PLUGIN:SaveAirdropLoot()
    datafile.SaveDataTable("airdrop_settings")
end

function PLUGIN:CmdAirdropRun(Args)
	if (self.Config.AirdropSettings.ManualRunDrop) then
		self:SpawnPlane()
	end
end

function PLUGIN:CmdLootResetConsole(Args)
	local Player
	
	if (not Args.connection) then
		Player = nil
	else
		Player = Args.connection.player
	end
	
	self:CmdLootReset(Player, Args.cmd, Args.Args)
end

function PLUGIN:CmdLootReset(Player, Cmd, Args)
    if (Player and Player.net.connection.authLevel < 2) then
		self:MessageToPlayer(Player, self.Config.Messages.DontHaveAccess)
        return
    end

	self:CreateDefaultLoot()
	self:SaveAirdropLoot()
	AirdropLoot = datafile.GetDataTable("airdrop_settings") or {}
	
	if (Player) then
		self:MessageToPlayer(Player, self.Config.Messages.LootReset)
	else
		self:MessageToServer(self.Config.Messages.LootReset)
	end
end

function PLUGIN:CmdMinimumPlayersConsole(Args)
	local Player
	
	if (not Args.connection) then
		Player = nil
	else
		Player = Args.connection.player
	end
	
	self:CmdMinimumPlayers(Player, Args.cmd, Args.Args)
end

function PLUGIN:CmdMinimumPlayers(Player, Cmd, Args)
    if (Player and Player.net.connection.authLevel == 0) then
		self:MessageToPlayer(Player, self.Config.Messages.DontHaveAccess)
        return
    end

    if (not Args or Args.Length == 0) then
		if (Player) then
			self:MessageToPlayer(Player, self.Config.Messages.MinNumberExplain)
		else
			self:MessageToServer(self.Config.Messages.MinNumberExplain)
		end
        return
	end
	
    local MinPlayers = tonumber(Args[0])
	
	if (not MinPlayers) then
		return
	end

    self.Config.AirdropSettings.MinimumPlayers = MinPlayers
    self:SaveConfig()
	
	if (Player) then
		self:MessageToPlayer(Player, self:FormatString(self.Config.Messages.MinNumberChanged, "{number}", MinPlayers))
	else
		self:MessageToServer(self:FormatString(self.Config.Messages.MinNumberChanged, "{number}", MinPlayers))
	end
end

function PLUGIN:CmdSupplyStayTimeConsole(Args)
	local Player
	
	if (not Args.connection) then
		Player = nil
	else
		Player = Args.connection.player
	end
	
	self:CmdSupplyStayTime(Player, Args.cmd, Args.Args)
end

function PLUGIN:CmdSupplyStayTime(Player, Cmd, Args)
    if (Player and Player.net.connection.authLevel == 0) then
		self:MessageToPlayer(Player, self.Config.Messages.DontHaveAccess)
        return
    end

    if (not Args or Args.Length == 0) then
		if (Player) then
			self:MessageToPlayer(Player, self.Config.Messages.SupplyRemoveExplain)
		else
			self:MessageToServer(self.Config.Messages.SupplyRemoveExplain)
		end
        return
	end
	
    local Time = tonumber(Args[0])
	
	if (not Time) then
		return
	end

    self.Config.AirdropSettings.SupplyStayTime = Time
    self:SaveConfig()
	
	if (Player) then
		self:MessageToPlayer(Player, self:FormatString(self.Config.Messages.SupplyRemoveChanged, "{number}", Time))
	else
		self:MessageToServer(self:FormatString(self.Config.Messages.SupplyRemoveChanged, "{number}", Time))
	end
end

function PLUGIN:CmdDropFrequencyConsole(Args)
	local Player
	
	if (not Args.connection) then
		Player = nil
	else
		Player = Args.connection.player
	end
	
	self:CmdDropFrequency(Player, Args.cmd, Args.Args)
end

function PLUGIN:CmdDropFrequency(Player, Cmd, Args)
    if (Player and Player.net.connection.authLevel == 0) then
		self:MessageToPlayer(Player, self.Config.Messages.DontHaveAccess)
       return
    end
	
    if (not Args or Args.Length == 0) then
		if (Player) then
			self:MessageToPlayer(Player, self.Config.Messages.DropFrequencyExplain)
		else
			self:MessageToServer(self.Config.Messages.DropFrequencyExplain)
		end
        return
	end
	
    local Frequency = tonumber(Args[0])

	if (not Frequency) then
		return
	end
	
    self.Config.AirdropSettings.DropFrequency = Frequency
    self:SaveConfig()
	
	if (Player) then
		self:MessageToPlayer(Player, self:FormatString(self.Config.Messages.DropFrequencyChanged, "{number}", Frequency))
	else
		self:MessageToServer(self:FormatString(self.Config.Messages.DropFrequencyChanged, "{number}", Frequency))
	end
end

function PLUGIN:MessageToPlayer(Player, Message)
	rust.SendChatMessage(Player, self.Config.PluginSettings.Title, tostring(Message))
end

function PLUGIN:MessageToAll(Message)
	rust.BroadcastChat(self.Config.PluginSettings.Title, tostring(Message))
end

function PLUGIN:MessageToServer(Message)
	print("[" .. self.Config.PluginSettings.Title .. "] " .. tostring(Message))
end

function PLUGIN:SpawnPlane()
	CanDrop = true
	
	local Entity = global.GameManager.server:CreateEntity("events/cargo_plane", new(UnityEngine.Vector3._type, nil), new(UnityEngine.Quaternion._type, nil))
	
	if (Entity) then
		self:MessageToServer(self.Config.Messages.PlaneSpawned)
		Entity:Spawn(true)
	end
end

function PLUGIN:OnPlayerLoot(Inventory, Entity)	
	if (not Entity) then
		return
	end
	
	if (not string.find(tostring(Entity.name), "supply_drop")) then
		return
	end
	
	local Container = Entity:GetComponent("StorageContainer").inventory
	
	if (not Container) then
		return
	end

	local Player = Inventory:GetComponent("BasePlayer")
	
	if (not Player) then
		return
	end
	
	local UserID = rust.UserIDFromPlayer(Player)
	Looting[UserID] = Entity
	
	if (self.Config.AirdropSettings.CustomSupplyLoot) then
	
		if (Looting.Container) then
			for i, Loot in pairs(Looting.Container) do
				if (Loot[1] == Entity) then
					Entity:GetComponent("StorageContainer").inventory = Looting.Container[i][2]
					return
				end
			end
		end
		
		self:BuildLoot(Entity, Container)
	end
end

function PLUGIN:BuildLoot(Entity, Container)
	Container.itemList:Clear()
	Container.capacity = AirdropLoot.Settings.Capacity
	
	self:AddRandomItem(AirdropLoot.Blueprint, AirdropLoot.Count.Blueprint, Container, true)
	self:AddRandomItem(AirdropLoot.Weapon, AirdropLoot.Count.Weapon, Container, false)
	self:AddRandomItem(AirdropLoot.Item, AirdropLoot.Count.Item, Container, false)
	self:AddRandomItem(AirdropLoot.Outerwear, AirdropLoot.Count.Outerwear, Container, false)
	self:AddRandomItem(AirdropLoot.Underwear, AirdropLoot.Count.Underwear, Container, false)
	self:AddRandomItem(AirdropLoot.Gloves, AirdropLoot.Count.Gloves, Container, false)
	self:AddRandomItem(AirdropLoot.Boots, AirdropLoot.Count.Boots, Container, false)
	self:AddRandomItem(AirdropLoot.Helmet, AirdropLoot.Count.Helmet, Container, false)
	self:AddRandomItem(AirdropLoot.Tool, AirdropLoot.Count.Tool, Container, false)
	self:AddRandomItem(AirdropLoot.Medical, AirdropLoot.Count.Medical, Container, false)
	self:AddRandomItem(AirdropLoot.Food, AirdropLoot.Count.Food, Container, false)
	self:AddRandomItem(AirdropLoot.Ammo, AirdropLoot.Count.Ammo, Container, false)
	self:AddRandomItem(AirdropLoot.Resource, AirdropLoot.Count.Resource, Container, false)
	
	local Info = { Entity, Container }
	table.insert(Looting.Container, Info)	
end

function PLUGIN:AddRandomItem(Loot, MaxCount, Container, IsBlueprint)
	local Name, MinAmount, MaxAmount, Chance, Amount, Random, Item, IsStacked, IsFinded, i
	local Count = 0
	
	if (not Loot) then
		return
	end
	
	if (MaxCount == 0) then
		return
	end
	
	repeat
		if (Container.capacity == Container.itemList.Count) then
			return
		end
			
		i = math.random(1, #Loot)	
		
		Name = Loot[i]["name"]
		MinAmount = Loot[i]["min_amount"]
		MaxAmount = Loot[i]["max_amount"]
		Chance = Loot[i]["chance"]
		
		if (Chance > 0) then				
			if (MinAmount and MaxAmount) then
				Amount = math.random(MinAmount, MaxAmount)
				IsStacked = true
			else
				Amount = 1
				IsStacked = false
			end
			
			if (Amount == 0) then
				Amount = 1
			end
			
			Random = math.random(1, 100)
			
			if (Random <= Chance) then
				Item = global.ItemManager.CreateByName(Name, Amount)
				
				if Item then
					if (IsBlueprint) then
						Item.isBlueprint = true
					end
					
					if (IsStacked) then
						IsFinded = Container:FindItemByItemID(Item.info.itemid)
					end
					
					if (not IsFinded) then
						Count = Count + 1	
						Item:MoveToContainer(Container, -1, IsStacked)
					end
				end
			else
				if (not AirdropLoot.Settings.FillCount) then
					Count = Count + 1
				end
			end
		end
	until (Count == MaxCount)
end

function PLUGIN:OnEntitySpawn(Entity)
	if (not Entity) then
		return
	end
	
	if (Entity:GetComponentInParent(global.SupplyDrop._type)) then
		timer.Once(1, function() self:CheckSupplyLanded(Entity) end)
	end
	
	if (not Entity:GetComponentInParent(global.CargoPlane._type)) then
		return
	end
	
	if (global.BasePlayer.activePlayerList.Count < self.Config.AirdropSettings.MinimumPlayers) then
		self:MessageToServer(self:FormatString(self.Config.Messages.PlaneRemovedMin, "{number}", self.Config.AirdropSettings.MinimumPlayers ))
		self:RemoveEntity(Entity)
		return
	end
	
	if (not CanDrop) then
		self:MessageToServer(self.Config.Messages.PlaneRemovedTrigger)
		self:RemoveEntity(Entity)
		return	
	end
	
	if (self.Config.AirdropSettings.NotifyAirdropStart) then
		self:MessageToAll(self.Config.Messages.PlaneLaunched)
	end	
	
	CanDrop = false
end

function PLUGIN:CheckSupplyLanded(SupplyDrop)
	if (SupplyDrop) then
		local ParachuteField = global.SupplyDrop._type:GetField("parachute", rust.PrivateBindingFlag()) 
		
		if (ParachuteField) then
			local Parachute = ParachuteField:GetValue(SupplyDrop)
			
			if (Parachute) then
				timer.Once(1, function() self:CheckSupplyLanded(SupplyDrop) end)
			else
				if (self.Config.AirdropSettings.SupplyStayTime > 0) then
					timer.Once(self.Config.AirdropSettings.SupplyStayTime * 60, function() self:RemoveEntity(SupplyDrop) end)
				end
				
				if (self.Config.AirdropSettings.NotifySupplyLanded) then
					local Message
					local x = string.format("%.2f", SupplyDrop.transform.position.x)
					local y = string.format("%.2f", SupplyDrop.transform.position.y)
					local z = string.format("%.2f", SupplyDrop.transform.position.z)
					
					Message = self:FormatString(self.Config.Messages.SupplyLanded, "{x}", x)
					Message = self:FormatString(Message, "{y}", y)
					Message = self:FormatString(Message, "{z}", z)

					self:MessageToAll(Message)
				end

				if (self.Config.AirdropSettings.ArrowEnabled) then
					local StartPos = new(UnityEngine.Vector3._type, nil)
					StartPos.x = SupplyDrop.transform.position.x
					StartPos.y = SupplyDrop.transform.position.y + 5 + self.Config.AirdropSettings.ArrowLength
					StartPos.z = SupplyDrop.transform.position.z
					
					local EndPos = new(UnityEngine.Vector3._type, nil)
					EndPos.x = SupplyDrop.transform.position.x
					EndPos.y = SupplyDrop.transform.position.y + 5
					EndPos.z = SupplyDrop.transform.position.z
	
					local ArrowParams = util.TableToArray({ self.Config.AirdropSettings.ArrowTime, System.ConsoleColor.White, StartPos, EndPos, self.Config.AirdropSettings.ArrowSize })
					global.ConsoleSystem.Broadcast("ddraw.arrow", ArrowParams)
				end				
			end
		end
	end
end

function PLUGIN:RemoveEntity(Entity)
	if (Entity) then
		if (Looting.Container) then
			for i, Loot in pairs(Looting.Container) do
				if (Loot[1] == Entity) then
					Looting.Container[i] = {}
				end
			end
		end
	
		Entity:KillMessage()
	end
end

function PLUGIN:LoadDefaultConfig()
    self.Config.PluginSettings =
	{
        Title = "Airdrop Settings",
        Version = "1.0.0"
    }

	self.Config.AirdropSettings =
	{
		MinimumPlayers = 3,
		DropFrequency = 60,
		SupplyStayTime = 60,
		ManualRunDrop = true,
		CustomSupplyLoot = true,
		NotifyAirdropStart = true,
		NotifySupplyLanded = true,
		ArrowEnabled = true,
		ArrowLength = 15,
		ArrowSize = 4,
		ArrowTime = 60
	}
	
	self.Config.Messages =
	{
		DontHaveAccess = "You don't have access!",
		LootReset = "Custom loot successfully reset to the default settings.",
		MinNumberExplain = "Specify the minimum number of players. Example: /airdrop_min 3",
		MinNumberChanged = "Minimum number of players successfully changed to {number}.",
		SupplyRemoveExplain = "Time in minutes after which the supply is removed. Example: /airdrop_stay 30",
		SupplyRemoveChanged = "Time after which the supply is removed changed to {number}.",
		DropFrequencyExplain = "Specify the frequency drop supplies in minutes. Example: /airdrop_freq 60",
		DropFrequencyChanged = "Frequency drop supplies successfully changed to {number}.",
		PlaneSpawned = "Cargo Plane has spawned.",
		PlaneRemovedMin = "Cargo Plane has removed. Minimum number of players: {number}.",
		PlaneRemovedTrigger = "Cargo Plane has removed. The event was triggered not by timer.",
		PlaneLaunched = "Cargo Plane has launched!",
		SupplyLanded = "Supply has landed at coordinates X: {x} Y: {y} Z: {z}"
	}
end

function PLUGIN:CheckConfig() 
    if self.Config.PluginSettings.Version ~= "1.0.0" then
        self:UpdateConfig()
    end
end

function PLUGIN:UpdateConfig()
    self:LoadDefaultConfig()
    self:SaveConfig()
end

function PLUGIN:Unload()
	if (Timer) then
		Timer:Destroy()
	end
end

function PLUGIN:CreateDefaultLoot()	
	AirdropLoot.Settings =
	{
		Capacity = 24,
		FillCount = true
	}
	
	AirdropLoot.Count = 
	{
		Blueprint = 2,
		Weapon = 2,
		Item = 1,
		Outerwear = 1,
		Underwear = 1,
		Gloves = 1,
		Boots = 1,
		Helmet = 1,
		Tool = 2,
		Medical = 3,
		Food = 3,
		Ammo = 2,
		Resource = 4
	}
	
	AirdropLoot.Blueprint =
	{
		{
			["name"] = "bow_hunting",
			["chance"] = 80
		},
		{
			["name"] = "knife_bone",
			["chance"] = 0
		},
		{
			["name"] = "pistol_eoka",
			["chance"] = 70
		},
		{
			["name"] = "pistol_revolver",
			["chance"] = 60
		},
		{
			["name"] = "rifle_ak",
			["chance"] = 20
		},
		{
			["name"] = "rifle_bolt",
			["chance"] = 30
		},
		{
			["name"] = "shotgun_waterpipe",
			["chance"] = 40
		},
		{
			["name"] = "smg_thompson",
			["chance"] = 40
		},
		{
			["name"] = "spear_stone",
			["chance"] = 80
		},
		{
			["name"] = "spear_wooden",
			["chance"] = 0
		},
		{
			["name"] = "building_planner",
			["chance"] = 0
		},
		{
			["name"] = "cupboard.tool",
			["chance"] = 0
		},
		{
			["name"] = "lock.code",
			["chance"] = 50
		},
		{
			["name"] = "lock.key",
			["chance"] = 0
		},
		{
			["name"] = "box_wooden",
			["chance"] = 0
		},
		{
			["name"] = "box_wooden_large",
			["chance"] = 70
		},
		{
			["name"] = "campfire",
			["chance"] = 0
		},
		{
			["name"] = "furnace",
			["chance"] = 0
		},
		{
			["name"] = "lantern",
			["chance"] = 80
		},
		{
			["name"] = "sleepingbag",
			["chance"] = 0
		},
		{
			["name"] = "gunpowder",
			["chance"] = 50
		},
		{
			["name"] = "lowgradefuel",
			["chance"] = 0
		},
		{
			["name"] = "paper",
			["chance"] = 0
		},
		{
			["name"] = "bucket_helmet",
			["chance"] = 40
		},
		{
			["name"] = "burlap_gloves",
			["chance"] = 50
		},
		{
			["name"] = "burlap_shirt",
			["chance"] = 0
		},
		{
			["name"] = "burlap_shoes",
			["chance"] = 0
		},
		{
			["name"] = "burlap_trousers",
			["chance"] = 0
		},
		{
			["name"] = "coffeecan_helmet",
			["chance"] = 40
		},
		{
			["name"] = "hazmat_boots",
			["chance"] = 50
		},
		{
			["name"] = "hazmat_gloves",
			["chance"] = 50
		},
		{
			["name"] = "hazmat_helmet",
			["chance"] = 40
		},
		{
			["name"] = "hazmat_jacket",
			["chance"] = 40
		},
		{
			["name"] = "hazmat_pants",
			["chance"] = 40
		},
		{
			["name"] = "jacket_snow",
			["chance"] = 30
		},
		{
			["name"] = "jacket_snow2",
			["chance"] = 30
		},
		{
			["name"] = "jacket_snow3",
			["chance"] = 30
		},
		{
			["name"] = "metal_facemask",
			["chance"] = 30
		},
		{
			["name"] = "metal_plate_torso",
			["chance"] = 20
		},
		{
			["name"] = "urban_boots",
			["chance"] = 0
		},
		{
			["name"] = "urban_jacket",
			["chance"] = 50
		},
		{
			["name"] = "urban_pants",
			["chance"] = 0
		},
		{
			["name"] = "urban_shirt",
			["chance"] = 50
		},
		{
			["name"] = "vagabond_jacket",
			["chance"] = 30
		},
		{
			["name"] = "axe_salvaged",
			["chance"] = 30
		},
		{
			["name"] = "hammer",
			["chance"] = 0
		},
		{
			["name"] = "hammer_salvaged",
			["chance"] = 30
		},
		{
			["name"] = "hatchet",
			["chance"] = 60
		},
		{
			["name"] = "icepick_salvaged",
			["chance"] = 30
		},
		{
			["name"] = "pickaxe",
			["chance"] = 50
		},
		{
			["name"] = "stonehatchet",
			["chance"] = 0
		},
		{
			["name"] = "torch",
			["chance"] = 0
		},
		{
			["name"] = "bandage",
			["chance"] = 0
		},
		{
			["name"] = "largemedkit",
			["chance"] = 30
		},
		{
			["name"] = "syringe_medical",
			["chance"] = 40
		},
		{
			["name"] = "ammo_pistol",
			["chance"] = 40
		},
		{
			["name"] = "ammo_rifle",
			["chance"] = 20
		},
		{
			["name"] = "ammo_shotgun",
			["chance"] = 30
		},
		{
			["name"] = "arrow_wooden",
			["chance"] = 80
		},
		{
			["name"] = "trap_bear",
			["chance"] = 70
		},
		{
			["name"] = "door_key",
			["chance"] = 70
		},
		{
			["name"] = "longsleeve_tshirt_blue",
			["chance"] = 40
		},
		{
			["name"] = "explosives",
			["chance"] = 30
		},
		{
			["name"] = "explosive.timed",
			["chance"] = 10
		},
		{
			["name"] = "shotgun_pump",
			["chance"] = 40
		}
	}
	
	AirdropLoot.Weapon =
	{
		{
			["name"] = "bow_hunting",
			["chance"] = 90
		},
		{
			["name"] = "knife_bone",
			["chance"] = 80
		},
		{
			["name"] = "pistol_eoka",
			["chance"] = 70
		},
		{
			["name"] = "pistol_revolver",
			["chance"] = 60
		},
		{
			["name"] = "rifle_ak",
			["chance"] = 30
		},
		{
			["name"] = "rifle_bolt",
			["chance"] = 50
		},
		{
			["name"] = "shotgun_waterpipe",
			["chance"] = 60
		},
		{
			["name"] = "smg_thompson",
			["chance"] = 50
		},
		{
			["name"] = "spear_stone",
			["chance"] = 80
		},
		{
			["name"] = "spear_wooden",
			["chance"] = 80
		},
		{
			["name"] = "trap_bear",
			["chance"] = 90
		},
		{
			["name"] = "explosive.timed",
			["chance"] = 30
		},
		{
			["name"] = "shotgun_pump",
			["chance"] = 50
		}
	}
	
	AirdropLoot.Item =
	{
		{
			["name"] = "bed",
			["chance"] = 0
		},
		{
			["name"] = "box_wooden",
			["chance"] = 90
		},
		{
			["name"] = "box_wooden_large",
			["chance"] = 80
		},
		{
			["name"] = "campfire",
			["chance"] = 0
		},
		{
			["name"] = "furnace",
			["chance"] = 50
		},
		{
			["name"] = "lantern",
			["chance"] = 60
		},
		{
			["name"] = "sleepingbag",
			["chance"] = 0
		},
		{
			["name"] = "flare",
			["chance"] = 0
		}
	}
	
	AirdropLoot.Outerwear =
	{
		{
			["name"] = "burlap_shirt",
			["chance"] = 80
		},
		{
			["name"] = "hazmat_jacket",
			["chance"] = 70
		},
		{
			["name"] = "jacket_snow",
			["chance"] = 60
		},
		{
			["name"] = "jacket_snow2",
			["chance"] = 60
		},
		{
			["name"] = "jacket_snow3",
			["chance"] = 60
		},
		{
			["name"] = "urban_jacket",
			["chance"] = 80
		},
		{
			["name"] = "urban_shirt",
			["chance"] = 80
		},
		{
			["name"] = "vagabond_jacket",
			["chance"] = 40
		},
		{
			["name"] = "metal_plate_torso",
			["chance"] = 40
		},
		{
			["name"] = "longsleeve_tshirt_blue",
			["chance"] = 30
		}
	}
	
	AirdropLoot.Underwear =
	{
		{
			["name"] = "burlap_trousers",
			["chance"] = 80
		},
		{
			["name"] = "hazmat_pants",
			["chance"] = 60
		},
		{
			["name"] = "urban_pants",
			["chance"] = 80
		}
	}
	
	AirdropLoot.Gloves =
	{
		{
			["name"] = "burlap_gloves",
			["chance"] = 70
		},
		{
			["name"] = "hazmat_gloves",
			["chance"] = 60
		}
	}
	
	AirdropLoot.Boots =
	{
		{
			["name"] = "burlap_shoes",
			["chance"] = 80
		},
		{
			["name"] = "hazmat_boots",
			["chance"] = 60
		},
		{
			["name"] = "urban_boots",
			["chance"] = 80
		}
	}
	
	AirdropLoot.Helmet =
	{
		{
			["name"] = "bucket_helmet",
			["chance"] = 70
		},
		{
			["name"] = "coffeecan_helmet",
			["chance"] = 60
		},
		{
			["name"] = "hazmat_helmet",
			["chance"] = 60
		},
		{
			["name"] = "metal_facemask",
			["chance"] = 50
		}
	}
	
	AirdropLoot.Tool =
	{
		{
			["name"] = "axe_salvaged",
			["chance"] = 40
		},
		{
			["name"] = "hammer",
			["chance"] = 80
		},
		{
			["name"] = "hammer_salvaged",
			["chance"] = 40
		},
		{
			["name"] = "hatchet",
			["chance"] = 80
		},
		{
			["name"] = "icepick_salvaged",
			["chance"] = 40
		},
		{
			["name"] = "pickaxe",
			["chance"] = 50
		},
		{
			["name"] = "rock",
			["chance"] = 0
		},
		{
			["name"] = "stonehatchet",
			["chance"] = 90
		},
		{
			["name"] = "torch",
			["chance"] = 0
		}
	}
	
	AirdropLoot.Medical =
	{
		{
			["name"] = "antiradpills",
			["chance"] = 70
		},
		{
			["name"] = "bandage",
			["chance"] = 90
		},
		{
			["name"] = "blood",
			["chance"] = 60
		},
		{
			["name"] = "largemedkit",
			["chance"] = 40
		},
		{
			["name"] = "syringe_medical",
			["chance"] = 60
		}
	}
	
	AirdropLoot.Food =
	{
		{
			["name"] = "apple",
			["chance"] = 90
		},
		{
			["name"] = "apple_spoiled",
			["chance"] = 0
		},
		{
			["name"] = "bearmeat",
			["chance"] = 0
		},
		{
			["name"] = "black",
			["chance"] = 0
		},
		{
			["name"] = "blueberries",
			["chance"] = 80
		},
		{
			["name"] = "can_beans",
			["chance"] = 70
		},
		{
			["name"] = "can_tuna",
			["chance"] = 70
		},
		{
			["name"] = "chicken_burned",
			["chance"] = 0
		},
		{
			["name"] = "chicken_cooked",
			["chance"] = 0
		},
		{
			["name"] = "chicken_raw",
			["chance"] = 0
		},
		{
			["name"] = "chicken_spoiled",
			["chance"] = 0
		},
		{
			["name"] = "chocholate",
			["chance"] = 70
		},
		{
			["name"] = "granolabar",
			["chance"] = 60
		},
		{
			["name"] = "humanmeat_burned",
			["chance"] = 0
		},
		{
			["name"] = "humanmeat_cooked",
			["chance"] = 0
		},
		{
			["name"] = "humanmeat_raw",
			["chance"] = 0
		},
		{
			["name"] = "humanmeat_spoiled",
			["chance"] = 0
		},
		{
			["name"] = "smallwaterbottle",
			["chance"] = 50
		},
		{
			["name"] = "wolfmeat_burned",
			["chance"] = 0
		},
		{
			["name"] = "wolfmeat_cooked",
			["chance"] = 0
		},
		{
			["name"] = "wolfmeat_raw",
			["chance"] = 0
		},
		{
			["name"] = "wolfmeat_spoiled",
			["chance"] = 0
		},
		{
			["name"] = "black raspberries",
			["chance"] = 50
		}
	}
	
	AirdropLoot.Ammo =
	{
		{
			["name"] = "ammo_pistol",
			["min_amount"] = 50,
			["max_amount"] = 100,
			["chance"] = 60
		},
		{
			["name"] = "ammo_rifle",
			["min_amount"] = 50,
			["max_amount"] = 100,
			["chance"] = 50
		},
		{
			["name"] = "ammo_shotgun",
			["min_amount"] = 50,
			["max_amount"] = 100,
			["chance"] = 70
		},
		{
			["name"] = "arrow_wooden",
			["min_amount"] = 50,
			["max_amount"] = 100,
			["chance"] = 90
		}
	}
	
	AirdropLoot.Resource =
	{
		{
			["name"] = "battery_small",
			["chance"] = 0
		},
		{
			["name"] = "bone_fragments",
			["min_amount"] = 100,
			["max_amount"] = 200,
			["chance"] = 80
		},
		{
			["name"] = "charcoal",
			["min_amount"] = 200,
			["max_amount"] = 800,
			["chance"] = 70
		},
		{
			["name"] = "cloth",
			["min_amount"] = 100,
			["max_amount"] = 200,
			["chance"] = 50
		},
		{
			["name"] = "fat_animal",
			["min_amount"] = 100,
			["max_amount"] = 200,
			["chance"] = 50
		},
		{
			["name"] = "gunpowder",
			["min_amount"] = 100,
			["max_amount"] = 300,
			["chance"] = 50
		},
		{
			["name"] = "lowgradefuel",
			["min_amount"] = 50,
			["max_amount"] = 100,
			["chance"] = 50
		},
		{
			["name"] = "metal_fragments",
			["min_amount"] = 500,
			["max_amount"] = 1000,
			["chance"] = 40
		},
		{
			["name"] = "metal_ore",
			["min_amount"] = 500,
			["max_amount"] = 1000,
			["chance"] = 60
		},
		{
			["name"] = "metal_refined",
			["min_amount"] = 500,
			["max_amount"] = 1000,
			["chance"] = 60
		},
		{
			["name"] = "paper",
			["chance"] = 80
		},
		{
			["name"] = "skull_human",
			["min_amount"] = 50,
			["max_amount"] = 100,
			["chance"] = 80
		},
		{
			["name"] = "skull_wolf",
			["min_amount"] = 50,
			["max_amount"] = 100,
			["chance"] = 80
		},
		{
			["name"] = "stones",
			["min_amount"] = 500,
			["max_amount"] = 1000,
			["chance"] = 90
		},
		{
			["name"] = "sulfur",
			["min_amount"] = 500,
			["max_amount"] = 1000,
			["chance"] = 70
		},
		{
			["name"] = "sulfur_ore",
			["min_amount"] = 500,
			["max_amount"] = 1000,
			["chance"] = 70
		},
		{
			["name"] = "wood",
			["min_amount"] = 1000,
			["max_amount"] = 5000,
			["chance"] = 90
		},
		{
			["name"] = "explosives",
			["min_amount"] = 10,
			["max_amount"] = 100,
			["chance"] = 30
		}
	}
end