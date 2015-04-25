PLUGIN.Name = "Economics"
PLUGIN.Title = "Economics"
PLUGIN.Version = V(1, 6, 0)
PLUGIN.Description = "Basic Economy System."
PLUGIN.Author = "Bombardir"
PLUGIN.HasConfig = true
PLUGIN.ResourceId = 717

----------------------------------------- LOCALS -----------------------------------------
local USERS, API, base_economy, cmds, msgs  = {}, {}, {}, {}, {}
local function SendMessage(player, msg)
	rust.SendChatMessage(player, msgs.ChatName, msg)
end
local function HasAcces(player)
	return player:GetComponent("BaseNetworkable").net.connection.authLevel >= API.Admin_Auth_LvL
end
------------------------------------------------------------------------------------------

----------------------------------------- API -----------------------------------------
function base_economy:Set(money)
	self[1] = money
	API.SaveData()
end

function base_economy:Transfer(base_eco, money)
	if self:Withdraw(money) then
		base_eco:Deposit(money)
		return true
	else
		return false 
	end
end

function base_economy:Deposit(money)
	self:Set(self[1] + money)
end

function base_economy:Withdraw(money)
	if self[1] >= money then
		self:Set(self[1] - money)
		return true
	else
		return false
	end
end

function GetEconomyAPI()
	return API
end

function PLUGIN.GetEconomyAPI()
	return API
end
 
function API.SaveData()
	datafile.SaveDataTable( "Economics" )
end

function API:GetUserDataFromPlayer(player)
	return self:GetUserData(rust.UserIDFromPlayer(player))
end

function API:GetUserData(steamid)
	local data = USERS[steamid]
	if not data then
		data = {}
		data[1] = self.StartMoney
		setmetatable(data, {__index = base_economy})
		USERS[steamid] = data
	end
	return data
end 
---------------------------------------------------------------------------------------

function PLUGIN:Init()
	USERS = datafile.GetDataTable( "Economics" ) or {}
	
	API.StartMoney = self.Config.StartMoney or 1000
	API.Admin_Auth_LvL = self.Config.Admin_Auth_LvL or 2
	API.Transfer_Fee = self.Config.Transfer_Fee or 0.01
	
	self.Config.Transfer_Fee = API.Transfer_Fee
	self.Config.StartMoney = API.StartMoney
	self.Config.Admin_Auth_LvL = API.Admin_Auth_LvL
	if self.Config.CleanBase == nil then self.Config.CleanBase = true end
	
	for k, v in pairs(USERS) do
		if self.Config.CleanBase and v[1] == API.StartMoney then -- Clean Base
			USERS[k] = nil
		else
			setmetatable(USERS[k], {__index = base_economy}) -- Bind Functions
		end 
	end
	datafile.SaveDataTable( "Economics" )
	
	cmds = self.Config.Commands or {}
	cmds.Balance = cmds.Balance or "balance"
	cmds.SetMoney = cmds.SetMoney or "setmoney"
	cmds.Transfer = cmds.Transfer or "transfer"
	cmds.Deposit = cmds.Deposit or "deposit"
	cmds.Withdraw = cmds.Withdraw or "withdraw"
	self.Config.Commands = cmds
	
	msgs = self.Config.Messages or {}
	msgs.ChatName = msgs.ChatName or "[Economy]"
	msgs.NoPermission = msgs.NoPermission or "No Permission!"
	msgs.NoPlayer = msgs.NoPlayer or "No Player Found!"
	msgs.New_Player_Balance = msgs.New_Player_Balance or "New player balance: %s"
	msgs.Syntax_Error = msgs.Syntax_Error or "Syntax Error! /%s <name/steamid> <money>"
	msgs.Withdraw_Error = msgs.Withdraw_Error or "'%s' has not enough money!"
	msgs.My_Balance = msgs.My_Balance or "Your Balance: %s"
	msgs.Balance = msgs.Balance or "Player Balance: %s"
	msgs.Transfer_Money_Error = msgs.Transfer_Money_Error or  "You do not have enough money!"
	msgs.Transfer_Negative_Error = msgs.Transfer_Negative_Error or "Money can not be negative!"
	msgs.Transfer_Error = msgs.Transfer_Error or "You can not transfer money yourself!"
	msgs.Transfer_Succes = msgs.Transfer_Succes or "You have successfully transferred money to '%s'!"
	msgs.Transfer_Succes_To = msgs.Transfer_Succes_To or "'%s' has transferred money to you! Check your balance '/balance'!"
	msgs.Save_Succes = msgs.Save_Succes or "Economics data saved!"
	msgs.Help = msgs.Help or  {"/balance - check your balance","/transfer <name> <money> - transfer [money] to [name] player for small fee"}
	self.Config.Messages = msgs
	
	self:SaveConfig()
	
	command.AddConsoleCommand( "eco.c", self.Plugin, "CC_Eco" )
	
	if cmds.Balance ~= "" then command.AddChatCommand(cmds.Balance, self.Plugin, "C_Balance") end
	if cmds.SetMoney ~= "" then command.AddChatCommand(cmds.SetMoney, self.Plugin, "C_Setmoney") end
	if cmds.Transfer ~= "" then command.AddChatCommand(cmds.Transfer, self.Plugin, "C_Transfer") end
	if cmds.Deposit ~= "" then command.AddChatCommand(cmds.Deposit, self.Plugin, "C_Deposit") end
	if cmds.Withdraw ~= "" then command.AddChatCommand(cmds.Withdraw, self.Plugin, "C_Withdraw") end
end

function PLUGIN:SendHelpText(player)
    for i=1,#msgs.Help do
		SendMessage(player, msgs.Help[i])
	end
end

function PLUGIN:C_Transfer(player, cmd, args)
	if args.Length > 1 then
		local money = tonumber(args[1]) 
		if money then
			if money > 0 then
				local target   = global.BasePlayer.Find(args[0])
				if target then
					if target ~= player then
						if API:GetUserDataFromPlayer(player):Withdraw(money) then
							API:GetUserDataFromPlayer(target):Deposit(money*(1-API.Transfer_Fee))
							SendMessage(player, msgs.Transfer_Succes:format(target.displayName))
							SendMessage(target, msgs.Transfer_Succes_To:format(player.displayName))
						else
							SendMessage(player, msgs.Transfer_Money_Error)
						end
					else
						SendMessage(player, msgs.Transfer_Error)
					end
				else
					SendMessage(player, msgs.NoPlayer)
				end
			else
				SendMessage(player, msgs.Transfer_Negative_Error)
			end
		else
			SendMessage(player, msgs.Syntax_Error:format(cmds.Transfer))
		end
	else
		SendMessage(player, msgs.Syntax_Error:format(cmds.Transfer))
	end
end

function PLUGIN:C_Balance(player, cmd, args)
	if args.Length > 0 then
		if HasAcces(player) then
			local target   = global.BasePlayer.Find(args[0])
			if target then
				SendMessage(player, msgs.Balance:format( API:GetUserDataFromPlayer(target)[1] )) 
			else
				SendMessage(player, msgs.NoPlayer)
			end
		else
			SendMessage(player, msgs.NoPermission)
		end
	else
		SendMessage(player, msgs.My_Balance:format(API:GetUserDataFromPlayer(player)[1]))
	end
end

function PLUGIN:C_Setmoney(player, cmd, args) 
	if HasAcces(player) then
		if args.Length > 1 then
			local money = tonumber(args[1])
			if money then
				local target   = global.BasePlayer.Find(args[0])
				if target then
					local data = API:GetUserDataFromPlayer(target)
					data:Set(money)
					SendMessage(player, msgs.New_Player_Balance:format( data[1] )) 
				else
					SendMessage(player, msgs.NoPlayer)
				end
			else
				SendMessage(player, msgs.Syntax_Error:format(cmds.SetMoney))
			end
		else
			SendMessage(player, msgs.Syntax_Error:format(cmds.SetMoney))
		end
	else
		SendMessage(player, msgs.NoPermission)
	end
end

function PLUGIN:C_Deposit(player, cmd, args) 
	if HasAcces(player) then
		if args.Length > 1 then
			local money = tonumber(args[1])
			if money then
				local target   = global.BasePlayer.Find(args[0])
				if target then
					local data = API:GetUserDataFromPlayer(target)
					data:Deposit(money)
					SendMessage(player, msgs.New_Player_Balance:format( data[1] )) 
				else
					SendMessage(player, msgs.NoPlayer)
				end
			else
				SendMessage(player, msgs.Syntax_Error:format(cmds.Deposit))
			end
		else
			SendMessage(player, msgs.Syntax_Error:format(cmds.Deposit))
		end
	else
		SendMessage(player, msgs.NoPermission)
	end
end

function PLUGIN:C_Withdraw(player, cmd, args) 
	if HasAcces(player) then
		if args.Length > 1 then
			local money = tonumber(args[1])
			if money then
				local target   = global.BasePlayer.Find(args[0])
				if target then
					local data = API:GetUserDataFromPlayer(target)
					if data:Withdraw(money) then
						SendMessage(player, msgs.New_Player_Balance:format( data[1] ))
					else
						SendMessage(player, msgs.Withdraw_Error:format( target.displayName ))
					end
				else
					SendMessage(player, msgs.NoPlayer)
				end
			else
				SendMessage(player, msgs.Syntax_Error:format(cmds.Withdraw))
			end
		else
			SendMessage(player, msgs.Syntax_Error:format(cmds.Withdraw))
		end
	else
		SendMessage(player, msgs.NoPermission)
	end
end

function PLUGIN:CC_Eco(arg)
	local reply = ""
	local player
	if arg.connection then
		player = arg.connection.player
	end
	if not player or HasAcces(player) then
		local cmd = arg:GetString( 0, "" )
		if cmd == "save" then
			API.SaveData()
			reply = "Economics data saved!"
		elseif cmd == "deposit" or cmd == "balance" or cmd == "withdraw" or cmd == "setmoney" then
			local steam  = arg:GetString( 1, "" )
			local target = global.BasePlayer.Find(steam)
			local userdata
			if target then
				userdata = API:GetUserDataFromPlayer( target )
				steam = target.displayName
			elseif steam:match("%d%d%d%d%d%d%d%d%d%d%d%d%d%d%d%d%d") then
				userdata = API:GetUserData(steam)
			end
			if userdata then
				if cmd == "balance" then
					reply =  "Balance(" .. steam .. ") = " .. tostring(userdata[1]) 
				else
					local money = tonumber(arg:GetString( 2, "" ))
					if money then
						if cmd == "setmoney" then
							userdata:Set(money)
							reply = "(SetMoney) New '" .. steam .. "' balance: " .. tostring(userdata[1])
						elseif cmd == "deposit" then
							userdata:Deposit(money)
							reply = "(Deposit) New '" .. steam .. "' balance: " .. tostring(userdata[1])
						elseif userdata:Withdraw(money) then
							reply = "(Withdraw) New '" .. steam .. "' balance: " .. tostring(userdata[1])
						else
							reply = "This user haven't enough money!"
						end
					else
						reply =  "Syntax Error! (eco.c " .. cmd .. " <steam/name> <money>)"
					end
				end
			else
				reply = "No user with steam/name: '" .. steam .. "' !"
			end
		else
			reply = "Economy Commands: 'eco.c deposit', 'eco.c save','eco.c balance', 'eco.c withdraw', 'eco.c setmoney'"  
		end
	else
		reply = "No permission!"
	end
	arg:ReplyWith(reply)
	return true
end