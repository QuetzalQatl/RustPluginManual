PLUGIN.Title        = "Supply Signals"
PLUGIN.Description  = "Supply Signals just like in legacy"
PLUGIN.Author       = "LaserHydra"
PLUGIN.Version      = V(1,0,5)
PLUGIN.HasConfig    = true
PLUGIN.ResourceId     = 933
 
function PLUGIN:Init()
command.AddChatCommand("sstoggle", self.Object, "cmdToggle")
command.AddConsoleCommand("ss.toggle", self.Object, "ccmdToggle")
self:LoadDefaultConfig()
print("#####################################")
print("[Supply Signal]: Plugin by LaserHydra")
print("[Supply Signal]: Loaded!")
print("#####################################")
delay = 3.2
end

function PLUGIN:Unload()
print("#####################################")
print("[Supply Signal]: Unloaded!")
print("#####################################")
end

function PLUGIN:LoadDefaultConfig()
	self.Config.IsEnabled = self.Config.IsEnabled or "true"
	self.Config.NoPermissionMsg = self.Config.NoPermissionMsg or "You have no permission to use this command!"
end

function PLUGIN:cmdToggle(player)
	if player.net.connection.authLevel == 2 then
		if self.Config.IsEnabled == "false" then
			self.Config.IsEnabled = "true"
			self:SaveConfig()
			rust.RunServerCommand("oxide.reload supplysignals")
			rust.SendChatMessage(player, "<color=orange>SUPPLY SIGNALS:</color>", "Plugin enabled!")
			print("********************************************")
			print("[Supply Signal]: Supply Signals enabled!")
			print("********************************************")
		else
			self.Config.IsEnabled = "false"
			self:SaveConfig()
			rust.RunServerCommand("oxide.reload supplysignals")
			rust.SendChatMessage(player, "<color=orange>SUPPLY SIGNALS:</color>", "Plugin disabled!")
			print("********************************************")
			print("[Supply Signal]: Supply Signals disabled!")
			print("********************************************")
		end
	else
		rust.SendChatMessage(player, "<color=orange>SUPPLY SIGNALS:</color>", "" .. self.Config.NoPermissionMsg)
	end
end

function PLUGIN:ccmdToggle()
	if self.Config.IsEnabled == "false" then
		self.Config.IsEnabled = "true"
		self:SaveConfig()
		rust.RunServerCommand("oxide.reload supplysignals")
	print("********************************************")
	print("[Supply Signal]: Supply Signals enabled!")
	print("********************************************")
	else
		self.Config.IsEnabled = "false"
		self:SaveConfig()
		rust.RunServerCommand("oxide.reload supplysignals")
	print("********************************************")
	print("[Supply Signal]: Supply Signals disabled!")
	print("********************************************")
	end
end

function PLUGIN:OnEntitySpawned(entity)
    if self.Config.IsEnabled == "true" then
		if entity.name == "grenade.f1.deployed" or entity.name == "grenade.f1.deployed(Clone)" then
			timer.Once( delay, function() spawnAirdrop() end )
			function spawnAirdrop()
				------------ getting Grenade position ---------------
				local position = (entity:GetEstimatedWorldPosition())
				local pos = {}
				pos.x = tostring(position.x)
				pos.y = tostring(position.y)
				pos.z = tostring(position.z)
				------ Height Fix -----
				pos.y = pos.y + 150
				------- Rounding ------
				pos.x = math.ceil(pos.x)
				pos.y = math.ceil(pos.y)
				pos.z = math.ceil(pos.z)
				---- to one String ----
				dropPos = pos.x .. " " .. pos.y .. " " .. pos.z
				----------- sending Airdrop + Msg ------------
				print("[Supply Signal]: Supply Grenade at " .. dropPos)
				rust.RunServerCommand("airdrop.topos " .. dropPos)
				entity:KillMessage()
			end
		else	
		end
	end
end 