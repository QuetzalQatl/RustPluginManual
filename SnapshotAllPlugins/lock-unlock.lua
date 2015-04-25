PLUGIN.Name = "lock-unlock"
PLUGIN.Title = "Door Unlocker"
PLUGIN.Version = V(1, 1, 0)
PLUGIN.Description = "Manually unlock doors"
PLUGIN.Author = "Reneb"
PLUGIN.HasConfig = true


function PLUGIN:Init()
	command.AddChatCommand( "unlock", self.Object, "cmdUnlock" )
	command.AddChatCommand( "lock", self.Object, "cmdLock" )
	local pluginList = plugins.GetAll()
    for i = 0, pluginList.Length - 1 do
        local pluginTitle = pluginList[i].Object.Title
        if pluginTitle == "FriendsAPI" then
            friendsAPI = pluginList[i].Object
            break
        end
    end
	local pluginList = plugins.GetAll()
    for i = 0, pluginList.Length - 1 do
        local pluginTitle = pluginList[i].Object.Title
        if pluginTitle == "Building Owners" then
            buildingowners = pluginList[i].Object
            break
        end
    end
end
function PLUGIN:Unload()
end
function PLUGIN:LoadDefaultConfig()
	self.Config.authLevelToUsePlugin = 0
	self.Config.authLevelToForceAccess = 1
end

function PLUGIN:cmdUnlock( player, com, args )
	local authlevel = player:GetComponent("BaseNetworkable").net.connection.authLevel
	local neededlevel = self.Config.authLevelToUsePlugin
	if(authlevel and neededlevel and authlevel >= neededlevel) then
		local arr = util.TableToArray( { player.eyes:Ray()  } )
		local hits = UnityEngine.Physics.RaycastAll["methodarray"][1]:Invoke(nil,arr)
		local it = hits:GetEnumerator()
		while (it:MoveNext()) do
            if(it.Current.collider:GetComponentInParent(global.Door._type)) then
				local door = it.Current.collider:GetComponentInParent(global.BuildingBlock._type)
				if(self:CanLock(authlevel,door,player)) then
					local lock = door:GetComponent("BaseEntity"):GetSlot(global.Slot.Lock)
					if(lock) then
						lock:SetFlag(global.Flags.Locked,false)
						lock:SendNetworkUpdate()
					end
				else
					player:ChatMessage("You dont have any access to this door")
					return
				end
			end
		end
		player:ChatMessage("You have successfully unlocked the door")
	else
		player:ChatMessage("You are not allowed to use this command")
	end
end
function PLUGIN:cmdLock( player, com, args )
	local authlevel = player:GetComponent("BaseNetworkable").net.connection.authLevel
	local neededlevel = self.Config.authLevelToUsePlugin
	if(authlevel and neededlevel and authlevel >= neededlevel) then
		local arr = util.TableToArray( { player.eyes:Ray()  } )
		local hits = UnityEngine.Physics.RaycastAll["methodarray"][1]:Invoke(nil,arr)
		local it = hits:GetEnumerator()
		while (it:MoveNext()) do
            if(it.Current.collider:GetComponentInParent(global.Door._type)) then
				local door = it.Current.collider:GetComponentInParent(global.BuildingBlock._type)
				if(self:CanLock(authlevel,door,player)) then
					local lock = door:GetComponent("BaseEntity"):GetSlot(global.Slot.Lock)
					if(lock) then
						lock:SetFlag(global.Flags.Locked,true)
						lock:SendNetworkUpdate()
					end
				else
					player:ChatMessage("You dont have any access to this door")
					return
				end
			end
		end
		player:ChatMessage("You have successfully locked the door")
	else
		player:ChatMessage("You are not allowed to use this command")
	end
end
function PLUGIN:CanLock(authlevel,blockbuilding,baseplayer)
	if(authlevel >= self.Config.authLevelToForceAccess) then
		return true
	end
	if(buildingowners) then
		local ownerid = buildingowners:FindBlockData(blockbuilding)
		if(ownerid and ownerid == rust.UserIDFromPlayer(baseplayer)) then
			return true
		end
		if(friendsAPI and friendsAPI:HasFriend(ownerid,rust.UserIDFromPlayer(baseplayer))) then
			return true
		end
	end
	return false
end