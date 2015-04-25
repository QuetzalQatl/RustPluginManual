PLUGIN.Name = "RotAG-Roubo"
PLUGIN.Title = "RotAG-Roubo"
PLUGIN.Version = V(1, 1, 2)
PLUGIN.Description = "Robbery plugin to use with Economics plugin from Bombardir"
PLUGIN.Author = "TheRotAG"
PLUGIN.HasConfig = true
PLUGIN.ResourceId  = 736

local function SendMessage(player, msg)
	player:SendConsoleCommand("chat.add \"".. msgs.ChatName.."\" \"".. msg .."\"")
end

function PLUGIN:Init()
	if GetEconomyAPI then
		EcoAPI = GetEconomyAPI()
	else
		print("This plugin requires Economics! Please install: http://forum.rustoxide.com/plugins/economics.717/  ")
		return 
	end 

	self.Config.Settings = self.Config.Settings or {}
	self.Config.Settings.PercWake = self.Config.Settings.PercWake or 100
	self.Config.Settings.PercSlpng = self.Config.Settings.PercSlpng or 100
	
	self.Config.Messages = self.Config.Messages or {}
	self.Config.Messages.RobResult = self.Config.Messages.RobResult or "You stole $%i from %s!"
	self.Config.Messages.ChatName = self.Config.Messages.ChatName or "[SERVER]"
	
	sets = self.Config.Settings
	msgs = self.Config.Messages
	self:SaveConfig()
end

function PLUGIN:OnEntityDeath(entity, hitinfo)
	if(hitinfo == nil) then
		return
	else
		if(entity:ToPlayer()) then
			self:PlayerDeath(entity,hitinfo)
		end
	end
end

function PLUGIN:PlayerDeath(victim,hitinfo)
	if string.sub(tostring(hitinfo.damageTypes:GetMajorityDamageType()), 1, 7) ~= "Suicide" then
		if(hitinfo.Initiator:ToPlayer()) then
			local attacker = hitinfo.Initiator:ToPlayer()
			local killed = victim.displayName
			local attackerWallet = EcoAPI:GetUserDataFromPlayer(attacker)
			local victimWalletData = EcoAPI:GetUserDataFromPlayer(victim)[1]
			local victimWallet = EcoAPI:GetUserDataFromPlayer(victim)
			if(victim:IsSleeping()) then
				local cash = math.floor(victimWalletData * (sets.PercSlpng / 100))
				local cashMath = cash
				victimWallet:Transfer(attackerWallet, cash)
				SendMessage(attacker, msgs.RobResult:format(cashMath, killed))
			else
				local cash = math.floor(victimWalletData * (sets.PercWake / 100))
				local cashMath = cash
				victimWallet:Transfer(attackerWallet, cash)
				SendMessage(attacker, msgs.RobResult:format(cashMath, killed))
			end
		end
	end
end