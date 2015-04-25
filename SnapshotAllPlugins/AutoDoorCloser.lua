PLUGIN.Title        = "AutoDoorCloser"
PLUGIN.Description  = "Automaticly close the doors"
PLUGIN.Author       = "Bombardir"
PLUGIN.Version      = V(1, 3, 1)
PLUGIN.HasConfig = true
PLUGIN.ResourceId = 800

local Timers, Data, msgs, UpdateLayerMethod, settings = {}

local function SendChatMessage(player, msg)
	player:SendConsoleCommand("chat.add", (msgs.ChatPlayerIcon and rust.UserIDFromPlayer(player)) or 0, msgs.ChatFormat:format(msg))
end
  
function PLUGIN:Init()
	settings = self.Config.Settings or {} 
	settings.DataFile = settings.DataFile or "AutoDoorCloserData"
	settings.DefaultTime = settings.DefaultTime or 3
	settings.MinTime = settings.MinTime or 1
	if settings.MinTime <= 0 then settings.MinTime = 0.1 end
	settings.MaxTime = settings.MaxTime or 10
	if settings.MaxTime < settings.MinTime then settings.MaxTime = settings.MinTime + 0.1 end
	settings.Command = settings.Command or "ad"
	self.Config.Settings = settings
	   
	msgs = self.Config.Message or {}
	msgs.ChatFormat = msgs.ChatFormat or "<color=#af5>[AutoDoor]</color> %s" 
	if msgs.ChatPlayerIcon == nil then msgs.ChatPlayerIcon = true end
	msgs.SuccesOn = msgs.SuccesOn or "Your doors will close automatically after %s sec."
	msgs.SuccesOff = msgs.SuccesOff or "You turn off the automatic closing doors!"
	msgs.ErrorMin = (msgs.ErrorMin or "The minimum value of time: %s"):format(settings.MinTime)
	msgs.ErrorMax = (msgs.ErrorMax or "The maximum value of time: %s"):format(settings.MaxTime)
	msgs.Syntax = (msgs.Syntax or "/%s [off/<time>]"):format(settings.Command)
	msgs.Help = (msgs.Help or "/%s [off/<time>] -- set/del time for automatic closing doors"):format(settings.Command)
	self.Config.Message = msgs
	
	Data = datafile.GetDataTable( settings.DataFile )
	
	if settings.Command ~= "" then command.AddChatCommand(settings.Command, self.Plugin, "C_AD") end
	self:SaveConfig()
	
	UpdateLayerMethod = global.BuildingBlock._type:GetMethod("UpdateLayer", rust.PrivateBindingFlag() )
end

function PLUGIN:SendHelpText(player)
	SendChatMessage(player, msgs.Help)
end   

function PLUGIN:CanOpenDoor( player, lock )
	local door = lock:GetParentEntity()
	door = door and door:GetComponent("Door")
	if door and not door:IsOpen() then 
		if Timers[door] then Timers[door]:Destroy() Timers[door] = nil end
		local ADtime = Data[rust.UserIDFromPlayer(player)]
		if ADtime == nil then ADtime = settings.DefaultTime end
		if ADtime then
			Timers[door] = timer.Once(ADtime, function() 
				if door and door:IsOpen() then
					door:SetFlag(global.BaseEntity.Flags.Open, false) 
					UpdateLayerMethod:Invoke(door, nil)
					door:SendNetworkUpdateImmediate(false)
				end
				Timers[door] = nil
			end) 
		end 
	end
end

function PLUGIN:C_AD(player, _, args)
	if args.Length > 0 then
		local ADtime = tonumber(args[0])
		local steam = rust.UserIDFromPlayer(player)
		if ADtime then
			if ADtime >= settings.MinTime then
				if ADtime <= settings.MaxTime then
					Data[steam] = ADtime
					SendChatMessage(player, msgs.SuccesOn:format(ADtime))
				else
					SendChatMessage(player, msgs.ErrorMax)
				end
			else
				SendChatMessage(player, msgs.ErrorMin)
			end
		else
			Data[steam] = false
			SendChatMessage(player, msgs.SuccesOff)
		end
		datafile.SaveDataTable( settings.DataFile )
	else
		SendChatMessage(player, msgs.Syntax)
	end
end