PLUGIN.Title = "MoneyForGather"
PLUGIN.Version = V(1, 2, 1)
PLUGIN.Description = "Gain money through the Economics API for gathering"
PLUGIN.Author = "Mr. Bubbles AKA BlazR"
PLUGIN.Url = "http://forum.rustoxide.com/plugins/money-for-gather.770/"
PLUGIN.ResourceId = 770
PLUGIN.HasConfig = true

local API = nil
local notified = "false"

-- Quotesafe function to help prevent unexpected output
local function QuoteSafe(string)
	return UnityEngine.StringExtensions.QuoteSafe(string)
end

function PLUGIN:Init()
	-- Load the default config and set the commands
	self:LoadDefaultConfig()
	command.AddChatCommand("setforwood", self.Plugin, "cmdSetAmount")
	command.AddChatCommand("setforores", self.Plugin, "cmdSetAmount")
	command.AddChatCommand("setforcorpses", self.Plugin, "cmdSetAmount")
	command.AddChatCommand("setforanimal", self.Plugin, "cmdSetAmount")
	-- command.AddChatCommand("gather", self.Plugin, "cmdGather")
	command.AddChatCommand("m4gtoggle", self.Plugin, "cmdToggle")
	command.AddChatCommand("m4gtogglechat", self.Plugin, "cmdToggleChat")
	command.AddChatCommand("m4gtogglewood", self.Plugin, "cmdToggleWood")
	command.AddChatCommand("m4gtoggleores", self.Plugin, "cmdToggleOres")
	command.AddChatCommand("m4gtogglecorpses", self.Plugin, "cmdToggleCorpses")
	command.AddChatCommand("m4gtoggleanimals", self.Plugin, "cmdToggleAnimals")
	command.AddChatCommand("m4ghelp", self.Plugin, "cmdHelp")
	command.AddConsoleCommand("m4g.setforwood", self.Plugin, "ccmdM4G")
	command.AddConsoleCommand("m4g.setforores", self.Plugin, "ccmdM4G")
	command.AddConsoleCommand("m4g.setforcorpses", self.Plugin, "ccmdM4G")
	command.AddConsoleCommand("m4g.setforanimal", self.Plugin, "ccmdM4G")
	command.AddConsoleCommand("m4g.toggle", self.Plugin, "ccmdM4G")
	command.AddConsoleCommand("m4g.togglechat", self.Plugin, "ccmdM4G")
	command.AddConsoleCommand("m4g.togglewood", self.Plugin, "ccmdM4G")
	command.AddConsoleCommand("m4g.toggleores", self.Plugin, "ccmdM4G")
	command.AddConsoleCommand("m4g.togglecorpses", self.Plugin, "ccmdM4G")
	command.AddConsoleCommand("m4g.toggleanimals", self.Plugin, "ccmdM4G")
end

function PLUGIN:OnServerIntialized()
	pluginsList = plugins.GetAll()
	for i = 0, tonumber(pluginsList.Length) - 1 do
		if pluginsList[i].Object.Title:match("Economics") then  
			API = GetEconomyAPI()
		end
	end
	if API == nil then
		print("Economics plugin not found. MoneyForGather plugin will not function!")
	end
end

function PLUGIN:LoadDefaultConfig()
	-- Set/load the default config options
	self.Config.Settings = self.Config.Settings or {
		ChatName = "[MoneyForGather]",
		PluginEnabled = "true",
		WoodAmount = "100",
		OreAmount = "100",
		CorpseAmount = "100",
		GatherMessagesEnabled = "true",
		-- GatherEnabled = "true",
		MoneyForWoodEnabled = "true",
		MoneyForOresEnabled = "true",
		MoneyForCorpsesEnabled = "false",
		MoneyForAnimalKillsEnabled = "true",
		BearKillAmount = "200",
		WolfKillAmount = "100",
		StagKillAmount = "75",
		BoarKillAmount = "50",
		ChickenKillAmount = "25",
		AuthLevel = "1"
	}
	-- Various messages used by the plugin
	self.Config.Messages = self.Config.Messages or {
		AmountChanged = "The %s amount has been changed to %s",
		NoPermission = "You do not have permission for that command.",
		PluginStatusChanged = "MoneyForGather has been %s.",
		ReceivedMoney = "You have received %s for gathering %s.",
		GatherMessagesChanged = "MoneyForGather gather messages in chat have been %s.",
		MoneyOnGatherStateChanged = "Money for gathering %s has been %s.",
		OnAnimalKill = "You have received %s for killing a %s.",
		HelpText = "Use /m4ghelp to get a list of MoneyForGather commands.",
		HelpText1 = "/setforwood <amount> - Sets the amount of money given for gathering wood",
		HelpText2 = "/setforores <amount> - Sets the amount of money given for gathering ores",
		HelpText3 = "/setforcorpses <amount> - Sets the amount of money given for gathering from corpses",
		HelpText4 = "/setforanimal <animal> <amount> - Sets the amount of money given for killing a particular animal",
		HelpText5 = "/m4gtoggle - Toggles the MoneyForGather plugin on/off",
		HelpText6 = "/m4gtogglechat - Toggles the MoneyForGather gather messages in chat on/off",
		HelpText7 = "/m4gtogglewood - Toggles getting money for gathering wood on/off",
		HelpText8 = "/m4gtoggleores - Toggles getting money for gathering ores on/off",
		HelpText9 = "/m4gtogglecorpses - Toggles getting money for gathering corpses on/off",
		HelpText10 = "/m4gtoggleanimals - Toggles getting money for killing animlas",
		InvalidAnimal = "You have specified an invalid animal type. Valid types are bear, wolf, stag, boar, and chicken."
		-- GatherEnabled = "Gathering has been enabled.",
		-- GatherDisabled = "Gathering has been disabled."
	}
	self:SaveConfig()
end

function PLUGIN:OnGather(dispenser, player, item)
	if API ~= nil and self.Config.Settings.PluginEnabled == "true" then
		player = player:ToPlayer()
		if player then
			userdata = API:GetUserDataFromPlayer(player)
			if dispenser:GetComponentInParent(global.TreeEntity._type) and self.Config.Settings.MoneyForWoodEnabled == "true" then
				userdata:Deposit(tonumber(self.Config.Settings.WoodAmount))
				if self.Config.Settings.GatherMessagesEnabled == "true" then
					self:SendMessage(player, self.Config.Messages.ReceivedMoney:format(self.Config.Settings.WoodAmount, item.info.displayname))
				end
			elseif item.info.displayname == "Metal Ore" or item.info.displayname == "Sulfur Ore" then
				if self.Config.Settings.MoneyForOresEnabled == "true" then
					userdata:Deposit(tonumber(self.Config.Settings.OreAmount))
					if self.Config.Settings.GatherMessagesEnabled == "true" then
						self:SendMessage(player, self.Config.Messages.ReceivedMoney:format(self.Config.Settings.OreAmount, item.info.displayname))
					end
				end
			elseif dispenser:ToString():match("corpse") and self.Config.Settings.MoneyForCorpsesEnabled == "true" then
				userdata:Deposit(tonumber(self.Config.Settings.CorpseAmount))
				if self.Config.Settings.GatherMessagesEnabled == "true" then
					self:SendMessage(player, self.Config.Messages.ReceivedMoney:format(self.Config.Settings.CorpseAmount, "from a corpse"))
				end
			end
		end
	elseif API == nil and notified == "false" and self.Config.Settings.PluginEnabled == "true" then
		pluginsList = plugins.GetAll()
		for i = 0, tonumber(pluginsList.Length) - 1 do
			if pluginsList[i].Object.Title:match("Economics") then  
				API = GetEconomyAPI()
			end
		end
		if API == nil then
			print("Economics plugin not found. MoneyForGather plugin will not function!")
			notified = "true"
		end
	end
end

function PLUGIN:OnEntityDeath(entity, hitinfo)	
	if API ~= nil and self.Config.Settings.MoneyForAnimalKillsEnabled == "true" and self.Config.Settings.PluginEnabled == "true" then
		if(entity:GetComponent("BaseNPC")) then
			if(hitinfo.Initiator:ToPlayer()) then
				player = hitinfo.Initiator:ToPlayer()
				userdata = API:GetUserDataFromPlayer(player)
				animal = entity.corpseEntity
				print("Animal: " .. animal)
				if animal:lower():match("bear") then
					userdata:Deposit(tonumber(self.Config.Settings.BearKillAmount))
					if self.Config.Settings.GatherMessagesEnabled == "true" then
						self:SendMessage(player, self.Config.Messages.OnAnimalKill:format(self.Config.Settings.BearKillAmount, "bear"))
					end
				elseif animal:lower():match("wolf") then
					userdata:Deposit(tonumber(self.Config.Settings.WolfKillAmount))
					if self.Config.Settings.GatherMessagesEnabled == "true" then
						self:SendMessage(player, self.Config.Messages.OnAnimalKill:format(self.Config.Settings.WolfKillAmount, "wolf"))
					end
				elseif animal:lower():match("stag") then
					userdata:Deposit(tonumber(self.Config.Settings.StagKillAmount))
					if self.Config.Settings.GatherMessagesEnabled == "true" then
						self:SendMessage(player, self.Config.Messages.OnAnimalKill:format(self.Config.Settings.StagKillAmount, "stag"))
					end
				elseif animal:lower():match("boar") then
					userdata:Deposit(tonumber(self.Config.Settings.BoarKillAmount))
					if self.Config.Settings.GatherMessagesEnabled == "true" then
						self:SendMessage(player, self.Config.Messages.OnAnimalKill:format(self.Config.Settings.BoarKillAmount, "boar"))
					end
				elseif animal:lower():match("chicken") then
					userdata:Deposit(tonumber(self.Config.Settings.ChickenKillAmount))
					if self.Config.Settings.GatherMessagesEnabled == "true" then
						self:SendMessage(player, self.Config.Messages.OnAnimalKill:format(self.Config.Settings.ChickenKillAmount, "chicken"))
					end
				end
			end
		end
	elseif API == nil and notified == "false" and self.Config.Settings.PluginEnabled == "true"then
		pluginsList = plugins.GetAll()
		for i = 0, tonumber(pluginsList.Length) - 1 do
			if pluginsList[i].Object.Title:match("Economics") then  
				API = GetEconomyAPI()
			end
		end
		if API == nil then
			print("Economics plugin not found. MoneyForGather plugin will not function!")
			notified = "true"
		end
	end
end

function PLUGIN:cmdSetAmount(player, cmd, args)
	if player.net.connection.authLevel >= tonumber(self.Config.Settings.AuthLevel) then
		if args.Length == 1 then
			if cmd == "setforwood" then
				self.Config.Settings.WoodAmount = tostring(args[0])
				self:SaveConfig()
				self:SendMessage(player, self.Config.Messages.AmountChanged:format("Wood", tostring(args[0])))
			elseif cmd == "setforores" then
				self.Config.Settings.OreAmount = tostring(args[0])
				self:SaveConfig()
				self:SendMessage(player, self.Config.Messages.AmountChanged:format("Ores", tostring(args[0])))
			elseif cmd == "setforcorpses" then
				self.Config.Settings.CorpseAmount = tostring(args[0])
				self:SaveConfig()
				self:SendMessage(player, self.Config.Messages.AmountChanged:format("corpses", tostring(args[0])))
			elseif cmd == "setforanimal" then
				self:SendMessage(player, self.Config.Messages.HelpText4)
			end
		elseif args.Length == 2 then
			if cmd == "setforwood" then
				self:SendMessage(player, self.Config.Messages.HelpText1)
			elseif cmd == "setforores" then
				self:SendMessage(player, self.Config.Messages.HelpText2)
			elseif cmd == "setforcorpses" then
				self:SendMessage(player, self.Config.Messages.HelpText3)
			elseif cmd == "setforanimal" then
				if args[0]:lower() == "bear" then
					self.Config.Settings.BearKillAmount = tostring(args[1])
					self:SendMessage(player, self.Config.Messages.AmountChanged:format("bears", tostring(args[1])))
				elseif args[0]:lower() == "wolf" then
					self.Config.Settings.WolfKillAmount = tostring(args[1])
					self:SendMessage(player, self.Config.Messages.AmountChanged:format("wolves", tostring(args[1])))
				elseif args[0]:lower() == "stag" then
					self.Config.Settings.StagKillAmount = tostring(args[1])
					self:SendMessage(player, self.Config.Messages.AmountChanged:format("stags", tostring(args[1])))
				elseif args[0]:lower() == "boar" then
					self.Config.Settings.BoarKillAmount = tostring(args[1])
					self:SendMessage(player, self.Config.Messages.AmountChanged:format("boars", tostring(args[1])))
				elseif args[0]:lower() == "chicken" then
					self.Config.Settings.ChickenKillAmount = tostring(args[1])
					self:SendMessage(player, self.Config.Messages.AmountChanged:format("chickens", tostring(args[1])))
				else
					self:SendMessage(player, self.Config.Messages.InvalidAnimal)
				end
			end
		else
			if cmd == "setforwood" then
				self:SendMessage(player, self.Config.Messages.HelpText1)
			elseif cmd == "setforores" then
				self:SendMessage(player, self.Config.Messages.HelpText2)
			elseif cmd == "setforcorpses" then
				self:SendMessage(player, self.Config.Messages.HelpText3)
			elseif cmd == "setforanimal" then
				self:SendMessage(player, self.Config.Messages.HelpText4)
			end
		end
	else
		self:SendMessage(player, self.Config.Messages.NoPermission)
	end
end

-- function PLUGIN:cmdGather(player, cmd, args)
	-- Add code to toggle config.Settings.GatherEnabled here
-- end

function PLUGIN:cmdToggle(player, cmd, args)
	if player.net.connection.authLevel >= tonumber(self.Config.Settings.AuthLevel) then
		if self.Config.Settings.PluginEnabled == "true" then
			self.Config.Settings.PluginEnabled = "false"
			self:SaveConfig()
			self:SendMessage(player, self.Config.Messages.PluginStatusChanged:format("disabled"))
		else
			self.Config.Settings.PluginEnabled = "true"
			self:SaveConfig()
			self:SendMessage(player, self.Config.Messages.PluginStatusChanged:format("enabled"))
		end
	else
		self:SendMessage(player, self.Config.Messages.NoPermission)
	end
end

function PLUGIN:cmdToggleChat(player, cmd, args)
	if player.net.connection.authLevel >= tonumber(self.Config.Settings.AuthLevel) then
		if self.Config.Settings.GatherMessagesEnabled == "true" then
			self.Config.Settings.GatherMessagesEnabled = "false"
			self:SaveConfig()
			self:SendMessage(player, self.Config.Messages.GatherMessagesChanged:format("disabled"))
		else
			self.Config.Settings.GatherMessagesEnabled = "true"
			self:SaveConfig()
			self:SendMessage(player, self.Config.Messages.GatherMessagesChanged:format("enabled"))
		end
	else
		self:SendMessage(player, self.Config.Messages.NoPermission)
	end
end

function PLUGIN:cmdToggleWood(player, cmd, args)
	if player.net.connection.authLevel >= tonumber(self.Config.Settings.AuthLevel) then
		if self.Config.Settings.MoneyForWoodEnabled == "true" then
			self.Config.Settings.MoneyForWoodEnabled = "false"
			self:SaveConfig()
			self:SendMessage(player, self.Config.Messages.MoneyOnGatherStateChanged:format("Wood", "disabled"))
		else
			self.Config.Settings.MoneyForWoodEnabled = "true"
			self:SaveConfig()
			self:SendMessage(player, self.Config.Messages.MoneyOnGatherStateChanged:format("Wood", "enabled"))
		end
	else
		self:SendMessage(player, self.Config.Messages.NoPermission)
	end
end

function PLUGIN:cmdToggleOres(player, cmd, args)
	if player.net.connection.authLevel >= tonumber(self.Config.Settings.AuthLevel) then
		if self.Config.Settings.MoneyForOresEnabled == "true" then
			self.Config.Settings.MoneyForOresEnabled = "false"
			self:SaveConfig()
			self:SendMessage(player, self.Config.Messages.MoneyOnGatherStateChanged:format("Ore", "disabled"))
		else
			self.Config.Settings.MoneyForOresEnabled = "true"
			self:SaveConfig()
			self:SendMessage(player, self.Config.Messages.MoneyOnGatherStateChanged:format("Ore", "enabled"))
		end
	else
		self:SendMessage(player, self.Config.Messages.NoPermission)
	end
end

function PLUGIN:cmdToggleCorpses(player, cmd, args)
	if player.net.connection.authLevel >= tonumber(self.Config.Settings.AuthLevel) then
		if self.Config.Settings.MoneyForCorpsesEnabled == "true" then
			self.Config.Settings.MoneyForCorpsesEnabled = "false"
			self:SaveConfig()
			self:SendMessage(player, self.Config.Messages.MoneyOnGatherStateChanged:format("corpses", "disabled"))
		else
			self.Config.Settings.MoneyForCorpsesEnabled = "true"
			self:SaveConfig()
			self:SendMessage(player, self.Config.Messages.MoneyOnGatherStateChanged:format("corpses", "enabled"))
		end
	else
		self:SendMessage(player, self.Config.Messages.NoPermission)
	end
end

function PLUGIN:cmdToggleAnimals(player, cmd, args)
	if player.net.connection.authLevel >= tonumber(self.Config.Settings.AuthLevel) then
		if self.Config.Settings.MoneyForAnimalKillsEnabled  == "true" then
			self.Config.Settings.MoneyForAnimalKillsEnabled  = "false"
			self:SaveConfig()
			self:SendMessage(player, self.Config.Messages.MoneyOnGatherStateChanged:format("animal kills", "disabled"))
		else
			self.Config.Settings.MoneyForAnimalKillsEnabled  = "true"
			self:SaveConfig()
			self:SendMessage(player, self.Config.Messages.MoneyOnGatherStateChanged:format("animal kills", "enabled"))
		end
	else
		self:SendMessage(player, self.Config.Messages.NoPermission)
	end
end

function PLUGIN:cmdHelp(player, cmd, args)
	if player.net.connection.authLevel >= tonumber(self.Config.Settings.AuthLevel) then
		self:SendMessage(player, self.Config.Messages.HelpText1)
		self:SendMessage(player, self.Config.Messages.HelpText2)
		self:SendMessage(player, self.Config.Messages.HelpText3)
		self:SendMessage(player, self.Config.Messages.HelpText4)
		self:SendMessage(player, self.Config.Messages.HelpText5)
		self:SendMessage(player, self.Config.Messages.HelpText6)
		self:SendMessage(player, self.Config.Messages.HelpText7)
		self:SendMessage(player, self.Config.Messages.HelpText8)
	else
		self:SendMessage(player, self.Config.Messages.NoPermission)
	end
end

function PLUGIN:ccmdM4G(arg)
	command = arg.cmd.namefull
	if command == "m4g.setforwood" then
		if not arg.Args or arg.Args.Length ~= 1 then
			arg:ReplyWith("You must specify an amount. 'm4g.setforwood <amount>'")
		elseif arg.Args[0] then
			self.Config.Settings.WoodAmount = tostring(arg.Args[0])
			self:SaveConfig()
			arg:ReplyWith(self.Config.Messages.AmountChanged:format("Wood", tostring(arg.Args[0])))
		end
	elseif command == "m4g.setforores" then
		if not arg.Args or arg.Args.Length ~= 1 then
			arg:ReplyWith("You must specify an amount. 'm4g.setforores <amount>'")
		elseif arg.Args[0] then
			self.Config.Settings.OreAmount = tostring(arg.Args[0])
			self:SaveConfig()
			arg:ReplyWith(self.Config.Messages.AmountChanged:format("Ores", tostring(arg.Args[0])))
		end
	elseif command == "m4g.setforcorpses" then
		if not arg.Args or arg.Args.Length ~= 1 then
			arg:ReplyWith("You must specify an amount. 'm4g.setforcorpses <amount>'")
		elseif arg.Args[0] then
			self.Config.Settings.CorpseAmount = tostring(arg.Args[0])
			self:SaveConfig()
			arg:ReplyWith(self.Config.Messages.AmountChanged:format("corpses", tostring(arg.Args[0])))
		end
	elseif command == "m4g.setforanimal" then
		if not arg.Args or arg.Args.Length ~= 2 then
			arg:ReplyWith("You must specify an animal type and amount. 'm4g.setforanimals <animal> <amount>'")
		elseif arg.Args[0] and arg.Args[1] then
			if arg.Args[0]:lower() == "bear" then
					self.Config.Settings.BearKillAmount = tostring(arg.Args[1])
					arg:ReplyWith(self.Config.Messages.AmountChanged:format("bears", tostring(arg.Args[1])))
				elseif arg.Args[0]:lower() == "wolf" then
					self.Config.Settings.WolfKillAmount = tostring(arg.Args[1])
					arg:ReplyWith(self.Config.Messages.AmountChanged:format("wolves", tostring(arg.Args[1])))
				elseif arg.Args[0]:lower() == "stag" then
					self.Config.Settings.StagKillAmount = tostring(arg.Args[1])
					arg:ReplyWith(self.Config.Messages.AmountChanged:format("stags", tostring(arg.Args[1])))
				elseif arg.Args[0]:lower() == "boar" then
					self.Config.Settings.BoarKillAmount = tostring(arg.Args[1])
					arg:ReplyWith(self.Config.Messages.AmountChanged:format("boars", tostring(arg.Args[1])))
				elseif arg.Args[0]:lower() == "chicken" then
					self.Config.Settings.ChickenKillAmount = tostring(arg.Args[1])
					arg:ReplyWith(self.Config.Messages.AmountChanged:format("chickens", tostring(arg.Args[1])))
				else
					arg:ReplyWith(self.Config.Messages.InvalidAnimal)
				end
			self:SaveConfig()
		end
	elseif command == "m4g.toggle" then
		if self.Config.Settings.PluginEnabled == "true" then
			self.Config.Settings.PluginEnabled = "false"
			self:SaveConfig()
			arg:ReplyWith(self.Config.Messages.PluginDisabled)
		else
			self.Config.Settings.PluginEnabled = "true"
			self:SaveConfig()
			arg:ReplyWith(self.Config.Messages.PluginEnabled)
		end
	elseif command == "m4g.togglechat" then
		if self.Config.Settings.GatherMessagesEnabled == "true" then
			self.Config.Settings.GatherMessagesEnabled = "false"
			self:SaveConfig()
			arg:ReplyWith(self.Config.Messages.GatherMessagesChanged:format("disabled"))
		else
			self.Config.Settings.GatherMessagesEnabled= "true"
			self:SaveConfig()
			arg:ReplyWith(self.Config.Messages.GatherMessagesChanged:format("enabled"))
		end
	elseif command == "m4g.togglewood" then
		if self.Config.Settings.MoneyForWoodEnabled == "true" then
			self.Config.Settings.MoneyForWoodEnabled = "false"
			self:SaveConfig()
			arg:ReplyWith(self.Config.Messages.MoneyOnGatherStateChanged:format("Wood", "disabled"))
		else
			self.Config.Settings.MoneyForWoodEnabled = "true"
			self:SaveConfig()
			arg:ReplyWith(self.Config.Messages.MoneyOnGatherStateChanged:format("Wood", "enabled"))
		end
	elseif command == "m4g.toggleores" then
		if self.Config.Settings.MoneyForOresEnabled == "true" then
			self.Config.Settings.MoneyForOresEnabled = "false"
			self:SaveConfig()
			arg:ReplyWith(self.Config.Messages.MoneyOnGatherStateChanged:format("Ores", "disabled"))
		else
			self.Config.Settings.MoneyForOresEnabled = "true"
			self:SaveConfig()
			arg:ReplyWith(self.Config.Messages.MoneyOnGatherStateChanged:format("Ores", "enabled"))
		end
	elseif command == "m4g.togglecorpses" then
		if self.Config.Settings.MoneyForCorpsesEnabled == "true" then
			self.Config.Settings.MoneyForCorpsesEnabled = "false"
			self:SaveConfig()
			arg:ReplyWith(self.Config.Messages.MoneyOnGatherStateChanged:format("corpses", "disabled"))
		else
			self.Config.Settings.CorpsesEnabled = "true"
			self:SaveConfig()
			arg:ReplyWith(self.Config.Messages.MoneyOnGatherStateChanged:format("corpses", "enabled"))
		end
	elseif command == "m4g.toggleanimals" then
		if self.Config.Settings.MoneyForAnimalKillsEnabled == "true" then
			self.Config.Settings.MoneyForAnimalKillsEnabled = "false"
			self:SaveConfig()
			arg:ReplyWith(self.Config.Messages.MoneyOnGatherStateChanged:format("animal kills", "disabled"))
		else
			self.Config.Settings.MoneyForAnimalKillsEnabled = "true"
			self:SaveConfig()
			arg:ReplyWith(self.Config.Messages.MoneyOnGatherStateChanged:format("animal kills", "enabled"))
		end
	end
	return
end

function PLUGIN:SendHelpText(player)
	if player.net.connection.authLevel >= tonumber(self.Config.Settings.AuthLevel) then
		self:SendMessage(player, self.Config.Messages.HelpText)
	end
end

function PLUGIN:SendMessage(player, message)
	player:SendConsoleCommand("chat.add " .. QuoteSafe(self.Config.Settings.ChatName) .. " " .. QuoteSafe(message))
end
