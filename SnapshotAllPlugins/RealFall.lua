PLUGIN.Name = "RealFall"
PLUGIN.Title = "Realistic Falling Damage"
PLUGIN.Description = "Allows you to set the maximum fall height for deaths."
PLUGIN.Version = V(1, 0, 1)
PLUGIN.Author = "M@CH!N3"
PLUGIN.HasConfig = true

function PLUGIN:Init()
	-- Load the default config and set the commands
	self:LoadDefaultConfig()
	command.AddChatCommand("maxfall", self.Plugin, "cmdMaxFall")
	command.AddChatCommand("setmaxfall", self.Plugin, "cmdSetMaxFall")
	command.AddConsoleCommand("set.maxfall", self.Plugin, "cmdSetMaxFallcon")
end

function PLUGIN:LoadDefaultConfig()
	self.Config = self.Config or {
		MaxFallHeight = "12"
	}
	self:SaveConfig()
end

function PLUGIN:OnEntityAttacked(player, hitinfo)

	if (player:GetComponent("BasePlayer")) then

		if (not tostring(hitinfo.damageTypes):find("DamageTypeList") or hitinfo.damageTypes:Total() <= 0) then
			return
		end

		local type = hitinfo.damageTypes:GetMajorityDamageType()
			if (tostring(type):find("Fall")) then
				local damage = tonumber(hitinfo.damageTypes:Total())
				local newdamage, max, health = (damage * 0.35), self.Config.MaxFallHeight, tonumber(player:Health())
				local setdamage = (health/max) * newdamage
				hitinfo.damageTypes:Set(type, setdamage)
			end
	end
end

function PLUGIN:cmdSetMaxFall(player, cmd, args)
	if player.net.connection.authLevel >= 2 then
			if cmd == "setmaxfall" then
				self.Config.MaxFallHeight = tostring(args[0])
				self:SaveConfig()
				local feet = tonumber(args[0]) * 3.3
				rust.SendChatMessage(player, "Max Fall Height is Now: "..tonumber(args[0]).."m ("..feet.."ft)")
			end
	end
end

function PLUGIN:cmdSetMaxFallcon(arg)
	command = arg.cmd.namefull
		if not arg.Args or arg.Args.Length ~= 1 then
			arg:ReplyWith("You must specify an amount. 'set.maxfall <amount>'")
		elseif arg.Args[0] then
			self.Config.MaxFallHeight = arg.Args[0]
			self:SaveConfig()
			local feet = arg.Args[0] * 3.3
			print("Max Fall Height is Now: "..arg.Args[0].."m ("..feet.."ft)")
		end
end

function PLUGIN:cmdMaxFall(player, cmd)
	if cmd == "maxfall" then
		local feet = self.Config.MaxFallHeight * 3.3
		rust.SendChatMessage(player, "Max Fall Height is : "..self.Config.MaxFallHeight.."m ("..feet.."ft)")
	end
end