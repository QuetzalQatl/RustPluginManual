PLUGIN.Title = "MoneyTime"
PLUGIN.Version = V(0, 8, 5)
PLUGIN.Description = "Pays Players a specified amount for every specified seconds played."
PLUGIN.Author = "Spiritwind"
PLUGIN.HasConfig = true
PLUGIN.Url = "http://oxidemod.org/resources/moneytime-for-economics.836/"
PLUGIN.ResourceId  = 836


function PLUGIN:Init()
	if GetEconomyAPI then
		EcoAPI = GetEconomyAPI()
	else
		print("This plugin requires Economics! Please install: http://forum.rustoxide.com/plugins/economics.717/  ")
		return 
    end
	Timers = {}
	self.Config.ChatName = self.Config.ChatName or "SERVER"
	self.Config.Interval = self.Config.Interval or 600
	self.Config.PayAmount = self.Config.PayAmount or 150
	self.Config.PayMessage = self.Config.PayMessage or "You were paid $150 for playing on the server!"
	self:SaveConfig()
end

function PLUGIN:PayTime( player )
        EcoAPI:GetUserDataFromPlayer(player):Deposit(self.Config.PayAmount)
				rust.SendChatMessage(player, self.Config.ChatName, self.Config.PayMessage)
end

function PLUGIN:OnPlayerInit( player )
    local steam = rust.UserIDFromPlayer(player)
    Timers[steam] = timer.Repeat(self.Config.Interval, 0, function() self:PayTime(player) end, self.Plugin)
  print("Timer Created for MoneyTime on connection:DEBUG")
end

function PLUGIN:OnPlayerDisconnected( player )
    local steam = rust.UserIDFromPlayer(player)
	if Timers[steam] then
    Timers[steam]:Destroy()
  print("Timer destroyed for MoneyTime on disconnect:DEBUG")
    else
  print("No MoneyTime Timer found for player on disconnect:DEBUG")
    end
end