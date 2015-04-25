PLUGIN.Title = "Country Block"
PLUGIN.Version = V(0, 1, 7)
PLUGIN.Description = "Allows or blocks players from specific countries via a whitelist or blacklist of country codes."
PLUGIN.Author = "Wulfspider"
PLUGIN.Url = "http://forum.rustoxide.com/plugins/698/"
PLUGIN.ResourceId = 698
PLUGIN.HasConfig = true

local debug = false

-- TODO:
---- Fix chat help for chat command
---- Fix "Array index is out of range" when using invalid single arg or no arg
---- Add console command function
---- Add command action to list the countries on the blacklist/whitelist

local blacklisted, whitelisted = false, false
function PLUGIN:Init()
    self:LoadDefaultConfig()
    command.AddChatCommand(self.Config.Settings.ChatCommand, self.Plugin, "cmdCountryBlock")
    --command.AddConsoleCommand(self.Config.Settings.ConsoleCommand, self.Plugin, "ccmdCountryBlock")
end

function PLUGIN:CanClientLogin(connection)
    if not debug then if self.Config.Settings.AdminExcluded ~= "false" and self:PermissionsCheck(connection) then return end end
    local country = "undefined"
    local playerIp = connection.ipaddress:match("([^:]*):")
    local listType = self.Config.Settings.ListType
    if debug then playerIp = "8.8.8.8"; print("[" .. self.Title .. "] Player's IP: " .. playerIp) end
    if playerIp ~= "127.0.0.1" then
        local url = "http://ipinfo.io/" .. playerIp .. "/country"
        webrequests.EnqueueGet(url, function(code, response)
            country = response:gsub("\n", "")
            if country == "undefined" or code ~= 200 then
                print("[" .. self.Title .. "] Checking country for " .. connection.username .. " failed!")
                self:Deport(connection, country)
                return
            end
            print("[" .. self.Title .. "] " .. connection.username .. " connected from " .. country)
            if string.lower(listType) == "blacklist" and self:ListCheck(country) then self:Deport(connection, country) end
            if string.lower(listType) == "whitelist" and not self:ListCheck(country) then self:Deport(connection, country) end
        end, self.Plugin)
    end
end

function PLUGIN:ListCheck(arg)
    local list = self.Config.Settings.CountryList
    for _, entry in pairs(list) do if arg == entry then return true end end
end

function PLUGIN:Deport(connection, country)
    Network.Net.sv:Kick(connection, self.Config.Messages.Rejected)
    local kicked = self.Config.Messages.PlayerKicked:gsub("{player}", connection.username):gsub("{country}", country)
    print("[" .. self.Title .. "] " .. kicked)
    if self.Config.Settings.Broadcast ~= "false" then rust.BroadcastChat(self.Config.Settings.ChatName, kicked) end
end

function PLUGIN:cmdCountryBlock(player, cmd, args)
    if player and not self:PermissionsCheck(player.net.connection) then
        rust.SendChatMessage(player, self.Config.Settings.ChatName, self.Config.Messages.NoPermission)
        return
    end
    local argument = string.upper(args[1])
    --[[if string.len(country) > 2 or string.len(country) < 2 then -- args.Length ~= 2, bring this back but support 1 for list action?
        rust.SendChatMessage(player, self.Config.Settings.ChatName, self.Config.Messages.ChatHelp) -- Can't do this with an array :/
        return
    end]]
    local action = args[0]
    local list = self.Config.Settings.CountryList
    if action == nil or action ~= "add" and action ~= "remove" and action ~= "type" then
        rust.SendChatMessage(player, self.Config.Settings.ChatName, self.Config.Messages.UnknownAction)
        return
    end
    if action == "add" then
        local listed
        for key, value in pairs(list) do if argument == value then listed = true; break end end
        if not listed then
            table.insert(list, argument)
            self:SaveConfig()
            local message = self.Config.Messages.CountryAdded:gsub("{country}", argument)
            rust.SendChatMessage(player, self.Config.Settings.ChatName, message)
        else
            local message = self.Config.Messages.AlreadyAdded:gsub("{country}", argument)
            rust.SendChatMessage(player, self.Config.Settings.ChatName, message)
        end
        return
    end
    if action == "remove" then
        local listed
        for key, value in pairs(list) do if argument == value then listed = true; break end end
        if listed then
            table.remove(list, key)
            self:SaveConfig()
            local message = self.Config.Messages.CountryRemoved:gsub("{country}", argument)
            rust.SendChatMessage(player, self.Config.Settings.ChatName, message)
        else
            local message = self.Config.Messages.NotListed:gsub("{country}", argument)
            rust.SendChatMessage(player, self.Config.Settings.ChatName, message)
        end
        return
    end
    if action == "type" then
        if string.lower(argument) == "blacklist" or string.lower(argument) == "whitelist" then
            self.Config.Settings.ListType = string.lower(argument)
            self:SaveConfig()
            local message = self.Config.Messages.ListTypeChanged:gsub("{listtype}", argument)
            rust.SendChatMessage(player, self.Config.Settings.ChatName, message)
            return
        else
            rust.SendChatMessage(player, self.Config.Settings.ChatName, self.Config.Messages.UnknownListType)
        end
        return
    end
    --[[if action == "list" then
        local countries = ""
        --for i = 1, #list do print(list[i]); countries = countries .. ", " .. list[i] end
        rust.SendChatMessage(player, self.Config.Settings.ChatName, countries)
    end]]
end

function PLUGIN:ccmdCountryBlock(args)
    -- TODO
end

function PLUGIN:PermissionsCheck(connection)
    local authLevel; if connection then authLevel = connection.authLevel else authLevel = 2 end
    local neededLevel = tonumber(self.Config.Settings.AuthLevel) or 2
    if debug then print(connection.username .. " has auth level: " .. tostring(authLevel)) end
    if authLevel and authLevel >= neededLevel then return true else return false end
end

function PLUGIN:SendHelpText(player)
    if self:PermissionsCheck(player.net.connection) then
        for i = 1, #self.Config.Messages.ChatHelp do
            rust.SendChatMessage(player, self.Config.Settings.ChatNameHelp, self.Config.Messages.ChatHelp[i])
        end
    end
end

function PLUGIN:LoadDefaultConfig()
    self.Config.Settings = self.Config.Settings or {}
    self.Config.Settings.AdminExcluded = self.Config.Settings.AdminExcluded or "true"
    self.Config.Settings.AuthLevel = tonumber(self.Config.Settings.AuthLevel) or 2
    self.Config.Settings.Broadcast = self.Config.Settings.Broadcast or "true"
    self.Config.Settings.ChatCommand = self.Config.Settings.ChatCommand or "country"
    self.Config.Settings.ChatName = self.Config.Settings.ChatName or "SERVER"
    self.Config.Settings.ChatNameHelp = self.Config.Settings.ChatNameHelp or "HELP"
    self.Config.Settings.ConsoleCommand = self.Config.Settings.ConsoleCommand or "country.block"
    self.Config.Settings.CountryList = self.Config.Settings.CountryList or { "UK", "US" }
    self.Config.Settings.ListType = self.Config.Settings.ListType or "whitelist"
    self.Config.Messages = self.Config.Messages or {}
    self.Config.Messages.AlreadyAdded = self.Config.Messages.AlreadyAdded or "{country} is already on the country list!"
    self.Config.Messages.ChatHelp = self.Config.Messages.ChatHelp or {
        "Use /country add countrycode to add a country to the list",
        "Use /country remove countrycode to remove a country from the list",
        "Use /country list to list all the countries on the list"
    }
    self.Config.Messages.CountryAdded = self.Config.Messages.CountryAdded or "{country} has been added to the country list!"
    self.Config.Messages.CountryRemoved = self.Config.Messages.CountryRemoved or "{country} has been removed from the country list!"
    self.Config.Messages.NoPermission = self.Config.Messages.NoPermission or "You do not have permission to use this command!"
    self.Config.Messages.NotListed = self.Config.Messages.NotListed or "{country} is not on the country list!"
    self.Config.Messages.ListTypeChanged = self.Config.Messages.ListTypeChanged or "Country list type changed to {listtype}"
    self.Config.Messages.PlayerKicked = self.Config.Messages.PlayerKicked or "{player} was kicked as their country ({country}) is blocked!"
    self.Config.Messages.Rejected = self.Config.Messages.Rejected or "Sorry, this server doesn't allow players from your country!"
    self.Config.Messages.UnknownAction = self.Config.Messages.UnknownAction or "Unknown command action! Use add, remove, list, or type"
    self.Config.Messages.UnknownListType = self.Config.Messages.UnknownListType or "Unknown list type! Use blacklist or whitelist"
    self:SaveConfig()
end
