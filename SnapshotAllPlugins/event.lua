PLUGIN.Name = "Running"
PLUGIN.Title = "Running"
PLUGIN.Version = V(1, 0, 0)
PLUGIN.Description = "Бегущий человек"
PLUGIN.Author = "Mizantop"
PLUGIN.ResourceId = 777
PLUGIN.HasConfig = true

local runningman = nil
local eventstart = nil
local eventpause = nil
local time1 = nil
local time2 = nil
local API = nil

function PLUGIN:Init()
    self:LoadDefaultConfig()
	command.AddChatCommand( "eventon", self.Plugin, "cmdEvent" )
	command.AddChatCommand( "eventoff", self.Plugin, "cmdEventOff" )
	command.AddChatCommand( "run", self.Plugin, "cmdRun" )
	command.AddConsoleCommand("serv.eventon", self.Plugin, "ccmdEvent")
	command.AddConsoleCommand("serv.eventoff", self.Plugin, "cmdEventOf")
	if GetEconomyAPI then
		API = GetEconomyAPI() -- Global Function!
	else
		print("Economics not found!")
	end
	if (self.Config.Default.On == "true") then
		eventpause = timer.Once( 60 * self.Config.Default.PauseeventTime, function() self:Startevent() end )
		time1 = time.GetUnixTimestamp()
	end
end

function PLUGIN:LoadDefaultConfig()
  self.Config.Default = {}
  self.Config.Default.ChatName = "EVENT"
  self.Config.Default.authLevel = 1
  self.Config.Default.On = "true"
  self.Config.Default.Count = 5
  self.Config.Default.StarteventTime = 30
  self.Config.Default.PauseeventTime = 30
  self.Config.Reward = {}
  self.Config.Reward.Random = "true"
  self.Config.Reward.RewardFixing = "wood"
  self.Config.Reward.RewardFixingAmount = 10000
  self.Config.Reward.Reward1 = "wood"
  self.Config.Reward.Reward1Amount = 50000
  self.Config.Reward.Reward2 = "stones"
  self.Config.Reward.Reward2Amount = 50000
  self.Config.Reward.Reward3 = "metal_ore"
  self.Config.Reward.Reward3Amount = 15000
  self.Config.Reward.Reward4 = "sulfur_ore"
  self.Config.Reward.Reward4Amount = 15000
  self.Config.Reward.Reward5 = "smg_thompson"
  self.Config.Reward.Reward5Amount = 1
  self.Config.Reward.Reward5_2 = "ammo_pistol"
  self.Config.Reward.Reward5_2Amount = 150
  self.Config.Reward.RewardMoney = 50000
  self.Config.Reward.Reward6 = "wood"
  self.Config.Reward.Reward6Amount = 50000
end

function PLUGIN:Startevent()
	if (eventpause ~= nil) then
		eventpause:Destroy()
		runningman = nil
		eventpause = nil
		Runlog("timer eventpause stoped")
	end
	if (eventstart ~= nil) then
		eventstart:Destroy()
		runningman = nil
		eventstart = nil
		Runlog("timer eventstart stoped")
	end
	print(self.Config.Default.Count.." iz "..global.BasePlayer.activePlayerList.Count)
	if (global.BasePlayer.activePlayerList.Count >= self.Config.Default.Count) then
		enum = global.BasePlayer.activePlayerList:GetEnumerator()
		local i = 0
		local ArrayPlayers = {}
			while enum:MoveNext() do
		i = i+1;
			ArrayPlayers[i] = enum.Current;
		end
		local rand_i = math.random(1,global.BasePlayer.activePlayerList.Count);
		runningman = ArrayPlayers[rand_i]
		Runlog("Running man: "..runningman.displayName.."")
		global.ConsoleSystem.Broadcast("chat.add \""..self.Config.Default.ChatName.."\" \"Running man: "..runningman.displayName..",\"")
		--global.ConsoleSystem.Broadcast("chat.add \""..self.Config.Default.ChatName.."\" \"Бегущий человек: "..runningman.displayName..",\"")
		--global.ConsoleSystem.Broadcast("chat.add \""..self.Config.Default.ChatName.."\" \"Убейте его и получите награду!\"")
		global.ConsoleSystem.Broadcast("chat.add \""..self.Config.Default.ChatName.."\" \"Kill him and get the reward!\"")
		--global.ConsoleSystem.Broadcast("chat.add \""..self.Config.Default.ChatName.."\" \"Команда: /run - чтобы узнать расстояние до цели\"")
		global.ConsoleSystem.Broadcast("chat.add \""..self.Config.Default.ChatName.."\" \"Command: /run - to know the distance to the target\"")
		eventstart = timer.Once( 60 * self.Config.Default.StarteventTime, function() self:Runningstop() end )
		time1 = time.GetUnixTimestamp()
	else
		global.ConsoleSystem.Broadcast("chat.add \""..self.Config.Default.ChatName.."\" \"There aren't enough players to start the event\"")
		--global.ConsoleSystem.Broadcast("chat.add \""..self.Config.Default.ChatName.."\" \"Недостаточно игроков для запуска эвента\"")
		eventpause = timer.Once( 60 * self.Config.Default.PauseeventTime, function() self:Startevent() end )
		time1 = time.GetUnixTimestamp()
	end
end

function PLUGIN:cmdEventOff(player, cmd, args)
	if (player:GetComponent("BaseNetworkable").net.connection.authLevel >= self.Config.Default.authLevel) then
		if (eventpause ~= nil) then
			eventpause:Destroy()
			eventpause = nil
			runningman = nil
			Runlog("timer eventpause stoped")
		end
		if (eventstart ~= nil) then
			eventstart:Destroy()
			eventstart = nil
			runningman = nil
			Runlog("timer eventstart stoped")
		end
		Runlog("Running Man has stoped")
		--rust.SendChatMessage(player, self.Config.Default.ChatName, "Эвент остановлен!")
		rust.SendChatMessage(player, self.Config.Default.ChatName, "Event has stoped!")
	else
		--rust.SendChatMessage(player, self.Config.Default.ChatName, "У вас нет прав для этого!")
		rust.SendChatMessage(player, self.Config.Default.ChatName, "You have no rights to do this!")
	end
end

function PLUGIN:cmdEventOf()
	if (eventpause ~= nil) then
		eventpause:Destroy()
		eventpause = nil
		runningman = nil
		Runlog("timer eventpause stoped")
	end
	if (eventstart ~= nil) then
		eventstart:Destroy()
		eventstart = nil
		runningman = nil
		Runlog("timer eventstart stoped")
	end
	Runlog("Running Man has stoped") 
end

function PLUGIN:ccmdEvent(args)
	if (eventpause ~= nil) then
		eventpause:Destroy()
		eventpause = nil
		runningman = nil
		Runlog("timer eventpause stoped")
	end
	if (eventstart ~= nil) then
		eventstart:Destroy()
		eventstart = nil
		runningman = nil
		Runlog("timer eventstart stoped")
	end
	local enum = global.BasePlayer.activePlayerList:GetEnumerator()
	local i = 0
	local ArrayPlayers = {}
		while enum:MoveNext() do
	i = i+1;
		ArrayPlayers[i] = enum.Current;
	end
	local rand_i = math.random(1,global.BasePlayer.activePlayerList.Count);
	runningman = ArrayPlayers[rand_i]
	Runlog("Running man: "..runningman.displayName.."")
	global.ConsoleSystem.Broadcast("chat.add \""..self.Config.Default.ChatName.."\" \"Running man: "..runningman.displayName..",\"")
	--global.ConsoleSystem.Broadcast("chat.add \""..self.Config.Default.ChatName.."\" \"Бегущий человек: "..runningman.displayName..",\"")
	--global.ConsoleSystem.Broadcast("chat.add \""..self.Config.Default.ChatName.."\" \"Убейте его и получите награду!\"")
	global.ConsoleSystem.Broadcast("chat.add \""..self.Config.Default.ChatName.."\" \"Kill him and get the reward!\"")
	--global.ConsoleSystem.Broadcast("chat.add \""..self.Config.Default.ChatName.."\" \"Команда: /run - чтобы узнать расстояние до цели\"")
	global.ConsoleSystem.Broadcast("chat.add \""..self.Config.Default.ChatName.."\" \"Command: /run - to know the distance to the target\"")
	eventstart = timer.Once( 60 * self.Config.Default.StarteventTime, function() self:Runningstop() end )
	time1 = time.GetUnixTimestamp()
end

function PLUGIN:cmdEvent(player, cmd, args)
	if (player:GetComponent("BaseNetworkable").net.connection.authLevel >= self.Config.Default.authLevel) then
		if (eventpause ~= nil) then
			eventpause:Destroy()
			eventpause = nil
			runningman = nil
			Runlog("timer eventpause stoped")
		end
		if (eventstart ~= nil) then
			eventstart:Destroy()
			eventstart = nil
			runningman = nil
			Runlog("timer eventstart stoped")
		end
		local enum = global.BasePlayer.activePlayerList:GetEnumerator()
		local i = 0
		local ArrayPlayers = {}
			while enum:MoveNext() do
		i = i+1;
			ArrayPlayers[i] = enum.Current;
		end
		local rand_i = math.random(1,global.BasePlayer.activePlayerList.Count);
		runningman = ArrayPlayers[rand_i]
		Runlog("Running man: "..runningman.displayName.."")
		global.ConsoleSystem.Broadcast("chat.add \""..self.Config.Default.ChatName.."\" \"Running man: "..runningman.displayName..",\"")
		--global.ConsoleSystem.Broadcast("chat.add \""..self.Config.Default.ChatName.."\" \"Бегущий человек: "..runningman.displayName..",\"")
		--global.ConsoleSystem.Broadcast("chat.add \""..self.Config.Default.ChatName.."\" \"Убейте его и получите награду!\"")
		global.ConsoleSystem.Broadcast("chat.add \""..self.Config.Default.ChatName.."\" \"Kill him and get the reward!\"")
		--global.ConsoleSystem.Broadcast("chat.add \""..self.Config.Default.ChatName.."\" \"Команда: /run - чтобы узнать расстояние до цели\"")
		global.ConsoleSystem.Broadcast("chat.add \""..self.Config.Default.ChatName.."\" \"Command: /run - to know the distance to the target\"")
		eventstart = timer.Once( 60 * self.Config.Default.StarteventTime, function() self:Runningstop() end )
		time1 = time.GetUnixTimestamp()
	else
		rust.SendChatMessage(player, self.Config.Default.ChatName, "У вас нет прав для этого!")
		--rust.SendChatMessage(player, self.Config.Default.ChatName, "You have no rights to do this!")
	end
end

function PLUGIN:SendHelpText(player)
    --player:ChatMessage("Пиши \"/run\" чтобы узнать инфу о бегущем человеке")
	player:ChatMessage("Use \"/run\" to find out information about the running man")
	local authlevel = player:GetComponent("BaseNetworkable").net.connection.authLevel
	if(authlevel >= self.Config.Default.authLevel) then
		--player:ChatMessage("Пиши \"/eventon\" чтобы запустить бегущего человека")
		--player:ChatMessage("Пиши \"/eventon\" чтобы остановить бегущего человека")
		player:ChatMessage("Use \"/eventon\" for start event Running Man")
		player:ChatMessage("Use \"/eventoff\" for stop event Running Man")
	end
end

function PLUGIN:cmdRun(player, cmd, args)
	--eventstart:Destroy()
	if (not player) then return end
	if (runningman ~= nil) then
		local xr = string.format("%.0f", runningman.transform.position.x)
		local zr = string.format("%.0f", runningman.transform.position.z)
		local xk = string.format("%.0f", player.transform.position.x)
		local zk = string.format("%.0f", player.transform.position.z)
		local dist = math.floor(math.sqrt(math.pow(xr - xk,2) + math.pow(zr - zk,2)))
		--rust.SendChatMessage(player, self.Config.Default.ChatName, "Бегущий человек "..runningman.displayName..",")
		rust.SendChatMessage(player, self.Config.Default.ChatName, "Running man "..runningman.displayName..",")
		--rust.SendChatMessage(player, self.Config.Default.ChatName, "на расстоянии "..dist.."м")
		rust.SendChatMessage(player, self.Config.Default.ChatName, "is at a distance of "..dist.."м")
		--rust.SendChatMessage(player, self.Config.Default.ChatName, "Убейте его и получите награду!")
		rust.SendChatMessage(player, self.Config.Default.ChatName, "Kill him and get the reward!")
		time2 = time.GetUnixTimestamp()
		local time3 = time2 - time1
		time3 = eventstart.Delay - time3
		time3 = math.floor(time3 / 60)
		--rust.SendChatMessage(player, self.Config.Default.ChatName, "До конца эвента осталось: "..time3.." минут")
		rust.SendChatMessage(player, self.Config.Default.ChatName, "Until the end of event left: "..time3.." minutes")
	else
		time2 = time.GetUnixTimestamp()
		local time3 = time2 - time1
		time3 = eventpause.Delay - time3
		time3 = math.floor(time3 / 60)
		rust.SendChatMessage(player, self.Config.Default.ChatName, "At the moment the event is not running")
		rust.SendChatMessage(player, self.Config.Default.ChatName, "Before the start of the event remained: "..time3.." minutes")
		--rust.SendChatMessage(player, self.Config.Default.ChatName, "В данный момент эвент не запущен")
		--rust.SendChatMessage(player, self.Config.Default.ChatName, "До начала эвента осталось : "..time3.." минут")
	end
end

	function Runlog(text)
		print('[EVENT] +--------------- RUNNING MAN -----------------');
		print('[EVENT] | '..text); -- Пишем в консоль что бы видно было.
		print('[EVENT] +---------------------------------------------');
	end

function PLUGIN:OnEntityDeath(entity, hitinfo)
	if(entity:ToPlayer()) then
		self:PlayerKilled(entity,hitinfo)
	end
end	
	
function PLUGIN:PlayerKilled(victim,hitinfo)
	if (hitinfo.Initiator:ToPlayer()) then
		local attacker = hitinfo.Initiator:ToPlayer()
		if(attacker ~= victim) then
			if (victim == runningman) then
				Runlog("Running man - "..attacker.displayName.." kill "..runningman.displayName.." and received as a reward!")
				--global.ConsoleSystem.Broadcast("chat.add \""..self.Config.Default.ChatName.."\" \"Игрок - "..attacker.displayName.." убил "..runningman.displayName.." и получил награду!\"")
				global.ConsoleSystem.Broadcast("chat.add \""..self.Config.Default.ChatName.."\" \"Player - "..attacker.displayName.." kill "..runningman.displayName.." and received as a reward!\"")
				local inv = attacker.inventory
				if (self.Config.Reward.Random == "true") then
					Runlog("random")
					local rand = math.random(1,6)
					Runlog(rand)
					if (rand == 1) then
						Runlog("wood")
						inv:GiveItem(global.ItemManager.CreateByName(self.Config.Reward.Reward1, self.Config.Reward.Reward1Amount), inv.containerMain)
					end
					if (rand == 2) then
						Runlog("stones")
						inv:GiveItem(global.ItemManager.CreateByName(self.Config.Reward.Reward2, self.Config.Reward.Reward2Amount), inv.containerMain)
					end
					if (rand == 3) then
						Runlog("metal_ore")
						inv:GiveItem(global.ItemManager.CreateByName(self.Config.Reward.Reward3, self.Config.Reward.Reward3Amount), inv.containerMain)
					end
					if (rand == 4) then
						Runlog("sulfur_ore")
						inv:GiveItem(global.ItemManager.CreateByName(self.Config.Reward.Reward4, self.Config.Reward.Reward4Amount), inv.containerMain)
					end
					if (rand == 5) then
						Runlog("smg_thompson")
						inv:GiveItem(global.ItemManager.CreateByName(self.Config.Reward.Reward5, self.Config.Reward.Reward5Amount), inv.containerMain)
						inv:GiveItem(global.ItemManager.CreateByName(self.Config.Reward.Reward5_2, self.Config.Reward.Reward5_2Amount), inv.containerMain)
					end
					if (rand == 6) then
						if (API ~= nil) then
							Runlog("money")
							local userdata = API:GetUserDataFromPlayer(attacker)
							userdata:Deposit(self.Config.Reward.RewardMoney)
							Runlog(userdata[1])
							API.SaveData()
						else
							Runlog("Economics not found!")
							inv:GiveItem(global.ItemManager.CreateByName(self.Config.Reward.Reward6, self.Config.Reward.Reward6Amount), inv.containerMain)
						end
					end
				else
					Runlog("reward")
					inv:GiveItem(global.ItemManager.CreateByName(self.Config.Reward.RewardFixing, self.Config.Reward.RewardFixingAmount), inv.containerMain)
				end
				eventstart:Destroy()
				eventstart = nil
				runningman = nil
				Runlog("timer eventstart stoped")
				eventpause = timer.Once( 60 * self.Config.Default.PauseeventTime, function() self:Startevent() end )
				time1 = time.GetUnixTimestamp()
			end
		end
	end
end

function PLUGIN:OnPlayerDisconnected(player)
	if (player == runningman) then
		Runlog("Player "..runningman.displayName.." got scared and ran away!")
		--global.ConsoleSystem.Broadcast("chat.add \""..self.Config.Default.ChatName.."\" \"Игрок "..runningman.displayName.." испугался и сбежал!\"")
		global.ConsoleSystem.Broadcast("chat.add \""..self.Config.Default.ChatName.."\" \"Player "..runningman.displayName.." got scared and ran away!\"")
		eventstart:Destroy()
		runningman = nil
		eventstart = nil
		Runlog("timer eventstart stoped")
		eventpause = timer.Once( 60 * self.Config.Default.PauseeventTime, function() self:Startevent() end )
		time1 = time.GetUnixTimestamp()
	end
end

function PLUGIN:Runningstop()
	Runlog("Running man - "..runningman.displayName.." ran away from the chase and received as a reward!")
	--global.ConsoleSystem.Broadcast("chat.add \""..self.Config.Default.ChatName.."\" \"Бегущий человек - "..runningman.displayName.." сбежал от погони и получил награду!\"")
	global.ConsoleSystem.Broadcast("chat.add \""..self.Config.Default.ChatName.."\" \"Running man - "..runningman.displayName.." ran away from the chase and received as a reward!\"")
	local inv = runningman.inventory
	if (self.Config.Reward.Random == "true") then
		Runlog("random")
		local rand = math.random(1,6)
		if (rand == 1) then
			Runlog("wood")
			inv:GiveItem(global.ItemManager.CreateByName(self.Config.Reward.Reward1, self.Config.Reward.Reward1Amount), inv.containerMain)
		end
		if (rand == 2) then
			Runlog("stones")
			inv:GiveItem(global.ItemManager.CreateByName(self.Config.Reward.Reward2, self.Config.Reward.Reward2Amount), inv.containerMain)
		end
		if (rand == 3) then
			Runlog("metal_ore")
			inv:GiveItem(global.ItemManager.CreateByName(self.Config.Reward.Reward3, self.Config.Reward.Reward3Amount), inv.containerMain)
		end
		if (rand == 4) then
			Runlog("sulfur_ore")
			inv:GiveItem(global.ItemManager.CreateByName(self.Config.Reward.Reward4, self.Config.Reward.Reward4Amount), inv.containerMain)
		end
		if (rand == 5) then
			Runlog("smg_thompson")
			inv:GiveItem(global.ItemManager.CreateByName(self.Config.Reward.Reward5, self.Config.Reward.Reward5Amount), inv.containerMain)
			inv:GiveItem(global.ItemManager.CreateByName(self.Config.Reward.Reward5_2, self.Config.Reward.Reward5_2Amount), inv.containerMain)
		end
		if (rand == 6) then
			if (API ~= nil) then
				Runlog("money")
				local userdata = API:GetUserDataFromPlayer(runningman)
				userdata:Deposit(self.Config.Reward.RewardMoney)
				Runlog(userdata[1])
				API.SaveData()
			else
				Runlog("Economics not found!")
				inv:GiveItem(global.ItemManager.CreateByName(self.Config.Reward.Reward6, self.Config.Reward.Reward6Amount), inv.containerMain)
			end
		end
	else
		Runlog("reward")
		inv:GiveItem(global.ItemManager.CreateByName(self.Config.Reward.RewardFixing, self.Config.Reward.RewardFixingAmount), inv.containerMain)
	end
	eventstart:Destroy()
	eventstart = nil
	runningman = nil
	Runlog("timer eventstart stoped")
	eventpause = timer.Once( 60 * self.Config.Default.PauseeventTime, function() self:Startevent() end )
	time1 = time.GetUnixTimestamp()
end

