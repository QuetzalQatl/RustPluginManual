PLUGIN.Name = "cleanup"
PLUGIN.Title = "Clean Up"
PLUGIN.Version = V(1, 3, 2)
PLUGIN.Description = "Clean up your server"
PLUGIN.Author = "Reneb"
PLUGIN.HasConfig = true

function PLUGIN:Init()
	command.AddChatCommand( "clean", self.Plugin, "cmdClean" )
	command.AddChatCommand( "count", self.Plugin, "cmdCount" )
	timer.Once(0.1, function() 
		nulVector3 = new(UnityEngine.Vector3._type,nil) 
	end )
end
function PLUGIN:LoadDefaultConfig()
	self.Config.authLevel = 1
end
local function ChatMessage(player,msg)
	player:SendConsoleCommand( "chat.add \"SERVER\" \"" .. msg .. "\"" );
end
local function FindBuilding(entity)
	buildingradius = 3
	local arr = util.TableToArray( { entity.transform.position , buildingradius } )
	util.ConvertAndSetOnArray(arr, 1, buildingradius, System.Single._type)
	local hits = UnityEngine.Physics.OverlapSphere["methodarray"][1]:Invoke(nil,arr)
	local it = hits:GetEnumerator()
	local buildingblock = false
	while (it:MoveNext()) do
		if(it.Current:GetComponentInParent(global.BuildingBlock._type)) then
			buildingblock =  true
			break
		end
	end
	return buildingblock
end
function PLUGIN:cmdCount( player, com, args )
	local authlevel = player:GetComponent("BaseNetworkable").net.connection.authLevel
	local neededlevel = self.Config.authLevel 
	if(authlevel >= neededlevel) then
		if(args.Length == 0) then
			ChatMessage(player,"/count bags => Gets the number of bags")
			ChatMessage(player,"/count trees => Gets the number of trees")
			ChatMessage(player,"/count resources => Gets the number of resources")
			ChatMessage(player,"/count players => Gets the number of online players")
			ChatMessage(player,"/count sleepers => Gets the number of sleepers")
			ChatMessage(player,"/count buildingblocks => Gets the number of buildingblocks")
			ChatMessage(player,"/count worlditems => Gets the number of worlditems")
			ChatMessage(player,"/count animals => Gets the number of animals")
			ChatMessage(player,"/count cupboards => Gets the number of cupboards")
			return
		end
		if(args.Length >= 1) then
			if(args[0] == "trees") then
				
				local alltrees = UnityEngine.Object.FindObjectsOfTypeAll(global.TreeEntity._type)
				local max = 0
				for i=0, alltrees.Length-1 do
					if(tostring(alltrees[i].hideFlags) == tostring(UnityEngine.HideFlags.HideInHierarchy)) then
						max = max + 1
					end
				end
				ChatMessage(player,"Trees Count: " .. max)
			elseif(args[0] == "bags") then
				ChatMessage(player,"Bags Count: " .. (UnityEngine.Object.FindObjectsOfTypeAll(global.FakePhysics._type).Length-1))
			elseif(args[0] == "resources") then
				ChatMessage(player,"Resources Count: " .. (UnityEngine.Object.FindObjectsOfTypeAll(global.BaseResource._type).Length))
			elseif(args[0] == "players") then
				local players = UnityEngine.Object.FindObjectsOfTypeAll(global.BasePlayer._type)
				local online = 0
				for i=0, players.Length - 1 do
					if(players[i]:IsConnected()) then
						online = online + 1
					end
				end
				ChatMessage(player,"Online Players Count: " .. online)
			elseif(args[0] == "sleepers") then
				local players = UnityEngine.Object.FindObjectsOfTypeAll(global.BasePlayer._type)
				local sleepers = 0
				for i=0, players.Length - 1 do
					if(not players[i]:IsConnected()) then
						sleepers = sleepers + 1
					end
				end
				ChatMessage(player,"Sleepers Players Count: " .. sleepers)
			elseif(args[0] == "buildingblocks") then
				ChatMessage(player,"Building Blocks count: " .. UnityEngine.Object.FindObjectsOfTypeAll(global.BuildingBlock._type).Length)
			elseif(args[0] == "worlditems") then
				ChatMessage(player,"World Items count: " .. UnityEngine.Object.FindObjectsOfTypeAll(global.WorldItem._type).Length)
			elseif(args[0] == "animals") then
				ChatMessage(player,"Animals count: " .. UnityEngine.Object.FindObjectsOfTypeAll(global.BaseNPC._type).Length)
 			elseif(args[0] == "cupboards") then
				ChatMessage(player,"Tool Cupboards count: " .. UnityEngine.Object.FindObjectsOfTypeAll(global.BuildPrivilegeTrigger._type).Length-1)
			end
		end
	end
end
function PLUGIN:cmdClean( player, com, args )
	local authlevel = player:GetComponent("BaseNetworkable").net.connection.authLevel
	local neededlevel = self.Config.authLevel 
	if(authlevel >= neededlevel) then
		if(args.Length == 0) then
			ChatMessage(player,"Clean Tool")
			ChatMessage(player,"/clean bags => clean all world bags that are not connected to a building")
			ChatMessage(player,"/clean bags all => clean all world bags")
			ChatMessage(player,"/clean cupboards => clean all Tool Cupboards that are not connected to a building")
			ChatMessage(player,"/clean cupboards all => clean all Tool Cupboards bags")
			ChatMessage(player,"/clean trees => cleans 50% of the trees")
			ChatMessage(player,"/clean trees all => clean 100% of the trees")
			ChatMessage(player,"/clean trees 0-1 => cleans 0% to 100% of the trees")
			ChatMessage(player,"/clean animals => cleans 50% of the animals")
			ChatMessage(player,"/clean animals all => clean 100% of the animals")
			ChatMessage(player,"/clean animals 0-1 => cleans 0% to 100% of the animals")
			return
		end
		local ttype = 0
		if(args.Length >= 2 and args[1] and args[1] == "all") then
			ttype = 1
		end
		-------- Get all items that are outside boxes, laying in the open world --------
		if(args.Length >= 1 and args[0] == "bags") then
			local fakephysics = UnityEngine.Object.FindObjectsOfTypeAll(global.FakePhysics._type)
			local max = 0
			local removed = 0
			for i=(fakephysics.Length-1), 0, -1 do
				local worlditem = fakephysics[i]:GetComponent("WorldItem")
				if(worlditem) then
					max = max + 1
					if(ttype == 1) then
						if(worlditem:GetItem()) then -- this is used to check that you are not removing the world_generic
							worlditem:GetItem():SetWorldEntity(nil)
							worlditem:Kill(ProtoBuf.Mode.None,0,0,nulVector3)
							removed = removed + 1
						end
					else
						if(worlditem:GetItem()) then -- this is used to check that you are not removing the world_generic
							if(not FindBuilding(worlditem)) then -- check if the item in on a building or not
								worlditem:GetItem():SetWorldEntity(nil)
								worlditem:Kill(ProtoBuf.Mode.None,0,0,nulVector3)
								removed = removed + 1
							end
						end
					end
				end
			end
			if(ttype == 1) then
				ChatMessage(player,"Cleaned up " .. removed .. " bags")
			else
				ChatMessage(player,"Cleaned up " .. removed .. " bags out of " .. max .. " total bags")
			end
		elseif(args.Length >= 1 and args[0] == "cupboards") then
			local allTriggerBase = UnityEngine.Object.FindObjectsOfTypeAll(global.TriggerBase._type)
			local max = 0
			local removed = 0
			for i=(allTriggerBase.Length-1), 0, -1 do
				if(allTriggerBase[i]:GetComponent("BuildPrivilegeTrigger")) then
					if(allTriggerBase[i].privlidgeEntity and allTriggerBase[i].privlidgeEntity:GetComponentInChildren(UnityEngine.Rigidbody._type) ~= nil) then -- check if not going to destroy the source tool cupboard that would make everything bug if deleted 
						max = max + 1
						if(ttype == 1) then
							allTriggerBase[i].privlidgeEntity:GetComponent("BaseEntity"):Kill(ProtoBuf.Mode.None,0,0,nulVector3)
							removed = removed + 1
						else
							if(not FindBuilding(allTriggerBase[i].privlidgeEntity:GetComponent("BaseEntity"))) then -- check if the item in on a building or not
								allTriggerBase[i].privlidgeEntity:GetComponent("BaseEntity"):Kill(ProtoBuf.Mode.None,0,0,nulVector3)
								removed = removed + 1
							end
						end
					end
				end
			end
			if(ttype == 1) then
				ChatMessage(player,"Cleaned up " .. removed .. " Tool Cupboards")
			else
				ChatMessage(player,"Cleaned up " .. removed .. " Tool Cupboards out of " .. max .. " total cupboards")
			end
		elseif(args.Length >= 1 and args[0] == "trees") then
			local percent = 50
			if(args.Length >= 2 and args[1] and tonumber(args[1]) ~= nil) then
				if(tonumber(args[1]) > 1) then
					ChatMessage(player,"Invalid range: 0 to 1 (0.5 = 50%)")
					return
				end
				ttype = 0
				percent = tonumber(args[1]) * 100
			end
			local trees = UnityEngine.Object.FindObjectsOfTypeAll(global.TreeEntity._type)
			local max = 0
			local removed = 0
			for i=(trees.Length-1), 0, -1 do
				if(trees[i]:GetComponentInChildren(global.Spawnable._type)) then
					max = max + 1
					if(ttype == 1) then
						trees[i]:Kill(ProtoBuf.Mode.None,0,0,nulVector3)
						removed = removed + 1
					else
						if(math.random(0,100) <= percent) then
							trees[i]:Kill(ProtoBuf.Mode.None,0,0,nulVector3)
							removed = removed + 1
						end
					end
				end
			end
			ChatMessage(player,"Cleaned up " .. removed .. " trees (" .. (removed/max)*100 .. "%)" )
		elseif(args.Length >= 1 and args[0] == "animals") then
			local percent = 50
			if(args.Length >= 2 and args[1] and tonumber(args[1]) ~= nil) then
				if(tonumber(args[1]) > 1) then
					ChatMessage(player,"Invalid range: 0 to 1 (0.5 = 50%)")
					return
				end
				ttype = 0
				percent = tonumber(args[1]) * 100
			end
			local animals = UnityEngine.Object.FindObjectsOfTypeAll(global.BaseNPC._type)
			local max = 0
			local removed = 0
			for i=(animals.Length-1), 0, -1 do
				if(animals[i]:GetComponentInChildren(global.Spawnable._type)) then
					max = max + 1
					if(ttype == 1) then
						animals[i]:Kill(ProtoBuf.Mode.None,0,0,nulVector3)
						removed = removed + 1
					else
						if(math.random(0,100) <= percent) then
								animals[i]:Kill(ProtoBuf.Mode.None,0,0,nulVector3)
								removed = removed + 1
						end
					end
				end
			end
			ChatMessage(player,"Cleaned up " .. removed .. " Animals (" .. (removed/max)*100 .. "%)" )
		end
	end
end