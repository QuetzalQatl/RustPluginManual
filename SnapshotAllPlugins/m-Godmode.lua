
-- -----------------------------------------------------------------------------------
-- Admin Godmode                                                         Version 1.4.3
-- -----------------------------------------------------------------------------------
-- Filename:          m-Godmode.lua
-- Last Modification: 02-27-2015
-- -----------------------------------------------------------------------------------
-- Description:
--
-- This plugin is developed for Rust servers with the Oxide Server Mod and will offer
-- server admins the option to disable all damage taken.
-- -----------------------------------------------------------------------------------


PLUGIN.Title       = "Godmode"
PLUGIN.Description = "Allows an admin to toggle Godmode."
PLUGIN.Version     = V( 1, 4, 3 )
PLUGIN.HasConfig   = true
PLUGIN.Author      = "Mughisi"
PLUGIN.ResourceId  = 673


-- -----------------------------------------------------------------------------------
-- Globals
-- -----------------------------------------------------------------------------------
-- Some globals that are used in multiple functions/hooks.
-- -----------------------------------------------------------------------------------
local AntiNoDamageSpam = {}

-- -----------------------------------------------------------------------------------
-- PLUGIN:Init()
-- -----------------------------------------------------------------------------------
-- On plugin initialisation the required in-game chat command is registered and the
-- configuration file is checked.
-- -----------------------------------------------------------------------------------
function PLUGIN:Init()
    -- Set the chat command.
    command.AddChatCommand("god", self.Object, "cmdToggleGod")

    -- Check if the config file is up to date.
    self:CheckConfig()

    -- Load the Godmode datatable file.
    self:LoadSavedData()
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:LoadDefaultConfig()
-- -----------------------------------------------------------------------------------
-- The plugin uses a configuration file to save certain settings and uses it for
-- localized messages that are send in-game to the admins. When this file doesn't
-- exist a new one will be created with these default values.
-- -----------------------------------------------------------------------------------
function PLUGIN:LoadDefaultConfig()
    -- General Setting:
    self.Config.Settings = {
        ChatName = "God",
        Version = "1.4.3"
    }

    -- Plugin Messages:
    self.Config.Messages = {
        Enabled             = "You have enabled godmode!",
        Disabled            = "You have disabled godmode!",
        HelpMessage         = "/god - This command will toggle godmode on or off.",
        NoGodDamageAttacker = "{player} is currently in godmode and can't take any damage.",
        NoGodDamagePlayer   = "{player} just tried to deal damage to you."
    }
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:CheckConfig()
-- -----------------------------------------------------------------------------------
-- This function checks if the configuration file is up to date and starts an update
-- if this is not the case.
-- -----------------------------------------------------------------------------------
function PLUGIN:CheckConfig() 
    -- Check if the current plugin version is the latest.
    if self.Config.Settings.Version ~= "1.4.3" then
        -- Different configuration version, update it now.
        self:UpdateConfig()
    end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:UpdateConfig()
-- -----------------------------------------------------------------------------------
-- This function updates the configuration file.
-- -----------------------------------------------------------------------------------
function PLUGIN:UpdateConfig()
    -- Check and update the global plugin settings.
    self.Config.Settings.ChatName         = self.Config.Settings.ChatName or "God"
    self.Config.Settings.ShowHitIndicator = nil
    self.Config.Settings.Version          = "1.4.3"

    -- Check and update the plugin messages.
    self.Config.Messages.Enabled             = self.Config.Messages.Enabled or "You have enabled godmode!"
    self.Config.Messages.Disabled            = self.Config.Messages.Disabled or "You have disabled godmode!"
    self.Config.Messages.HelpMessage         = self.Config.Messages.HelpMessage or "/god - This command will toggle godmode on or off."
    self.Config.Messages.NoGodDamageAttacker = self.Config.Messages.NoGodDamageAttacker or "{player} is currently in godmode and can't take any damage."
    self.Config.Messages.NoGodDamagePlayer   = self.Config.Messages.NoGodDamagePlayer or "{player} just tried to deal damage to you."

    -- Save the updated configuration file.
    self:SaveConfig()
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:LoadSavedData()
-- -----------------------------------------------------------------------------------
-- Load the DataTable file into a table or create a new table when the file doesn't
-- exist yet.
-- -----------------------------------------------------------------------------------
function PLUGIN:LoadSavedData()
    -- Open the datafile if it exists, otherwise we'll create a new one.
    Gods = datafile.GetDataTable( "m-Godmode" )
    Gods = Gods or {}
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:SaveData()
-- -----------------------------------------------------------------------------------
-- Saves the table with all the teleportdata to a DataTable file.
-- -----------------------------------------------------------------------------------
function PLUGIN:SaveData()  
    -- Save the DataTable
    datafile.SaveDataTable( "m-Godmode" )
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:cmdToggleGod( player, cmd, args )                              Admin Command
-- -----------------------------------------------------------------------------------
-- In-game '/god' command for server admins to be able to toggle Godmode on and off.
-- -----------------------------------------------------------------------------------
function PLUGIN:cmdToggleGod(player, cmd, args)
    -- Check if the player is an admin.
    if player.net.connection.authLevel == 0 then return end

    -- Grab the player is Steam ID.
    local userID = rust.UserIDFromPlayer(player)

    -- Check if the player is turning Godmode on or off.
    if Gods[userID] then
        -- Godmode is currently enabled, disable it and send the player a message.
        Gods[userID] = nil
        self:SendMessage(player, self.Config.Messages["Disabled"])
    else
        -- Godmode is currently disabled, enable it and send the player a message.
        Gods[userID] = true
        self:SendMessage(player, self.Config.Messages["Enabled"])
    end

    self:SaveData()
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:OnEntityAttacked( entity, hitinfo )
-- -----------------------------------------------------------------------------------
-- OnEntityAttacked Oxide Hook. This hook is triggered when an entity (BasePlayer or 
-- BaseAnimals) is attacked. This hook is used to check if an admin is taking damage
-- and if he/she is, it will prevent the damage from being dealt.
-- -----------------------------------------------------------------------------------
function PLUGIN:OnEntityAttacked( entity, hitinfo )
    -- Check if a player is taking the damage.
    if entity:ToPlayer() then
        -- Grab the Steam ID of the player.
        local userID = rust.UserIDFromPlayer(entity)

        -- Check if the player has Godmode enabled.
        if Gods[userID] then
            -- The player has Godmode enabled, set the damage dealt to 0 for every
            -- damage type as some weapons deal multiple types of damage.
            while hitinfo.damageTypes:Total() > 0 do
                hitinfo.damageTypes:Set( hitinfo.damageTypes:GetMajorityDamageType(), 0 )
            end

            -- Set the material getting hit to 0 to prevent warning messages to 
            -- be shown in console regarding missing hit effects.
            hitinfo.HitMaterial = 0

            -- Check if the damage is dealt by a player, if it's from a player then
            -- inform him that he's hitting an admin with godmode enabled.

            if hitinfo.Initiator:ToPlayer() and hitinfo.Initiator ~= entity then
                -- Get the BasePlayer of the attacker.
                local attacker   = hitinfo.Initiator
                local attackerID = rust.UserIDFromPlayer( attacker )

                -- Get the current timestamp
                local timestamp = time.GetUnixTimestamp()

                -- Check the anti-spam value for the attacker, we don't want to send
                -- multiple messages if he'd empty an entire SMG clip on the admin.
                AntiNoDamageSpam[attackerID] = AntiNoDamageSpam[attackerID] or 0

                -- Check if it has been more than 30 seconds that we've send a message to
                -- the attacking player.
                if ( timestamp - AntiNoDamageSpam[attackerID] ) > 30 then
                    -- It has been more than 30 seconds after the last message, send the
                    -- player a new message.
                    self:SendMessage( attacker, self:Parse( self.Config.Messages.NoGodDamageAttacker, { player = entity.displayName } ) )
                    self:SendMessage( entity, self:Parse( self.Config.Messages.NoGodDamagePlayer, { player = attacker.displayName } ) )
 
                    -- Update the timestamp
                    AntiNoDamageSpam[attackerID] = timestamp
                end
            end
        end
    end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:OnRunPlayerMetabolism( metabolism )
-- -----------------------------------------------------------------------------------
-- OnEntityAttack Oxide Hook. This hook is triggered when the metabolism is updated. 
-- This hook is used to disable damage taken from drowning and radiation when Godmode
-- is enabled for the player.
-- -----------------------------------------------------------------------------------
function PLUGIN:OnRunPlayerMetabolism( metabolism )
    -- Grab the Steam ID of the player.
    local player = metabolism:GetComponent("BasePlayer")
    local userID = rust.UserIDFromPlayer( player )

    -- Check if the player has Godmode enable.
    if Gods[userID] then
        -- The player has Godmode enabled, change the metabolism values.
        player:InitializeHealth( 100, 100 )
        metabolism.oxygen:Add( metabolism.oxygen.max )
        metabolism.wetness:Add( -metabolism.wetness.max )
        metabolism.radiation_level:Add( -metabolism.radiation_level.max )
        metabolism.radiation_poison:Add( -metabolism.radiation_poison.max )
        metabolism.temperature:Reset()
        metabolism.hydration:Add( metabolism.hydration.max )
        metabolism.calories:Add( metabolism.calories.max )
        metabolism.bleeding:Reset()
    end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:SendHelpText( player )
-- -----------------------------------------------------------------------------------
-- HelpText plugin support for the command /help.
-- -----------------------------------------------------------------------------------
function PLUGIN:SendHelpText( player )
    if player.net.connection.authLevel > 0 then
        self:SendMessage( player, self.Config.Messages.HelpMessage )
    end
end

-- -----------------------------------------------------------------------------
-- PLUGIN:SendMessage( target, message )
-- -----------------------------------------------------------------------------
-- Sends a chatmessage to a player.
-- -----------------------------------------------------------------------------
function PLUGIN:SendMessage( target, message )
    -- Check if we have an existing target to send the message to.
    if not target then return end
    if not target:IsConnected() then return end

    -- Check if the message is a table with multiple messages.
    if type( message ) == "table" then
        -- The message is a table with multiple messages, send them one by one.
        for _, message in pairs( message ) do
            self:SendMessage( target, message )
        end

        return
    end

    -- "Build" the message to be able to show it correctly.
    message = UnityEngine.StringExtensions.QuoteSafe( message )

    -- Send the message to the targetted player.
    target:SendConsoleCommand( "chat.add \"" .. self.Config.Settings.ChatName .. "\""  .. message );
end

-- -----------------------------------------------------------------------------
-- PLUGIN:Parse( message, values )
-- -----------------------------------------------------------------------------
-- Replaces the parameters in a message with the corresponding values.
-- -----------------------------------------------------------------------------
function PLUGIN:Parse( message, values )
    if type( message ) == "table" then
        local returnTable = {}

        for _, msg in pairs( message ) do
            for k, v in pairs( values ) do
                -- Replace the variable in the message with the specified value.
                tostring(v):gsub("(%%)", "%%%%") 
                msg = msg:gsub( "{" .. k .. "}", v)
                table.insert( returnTable, msg )
            end
        end

        return returnTable
    else
        for k, v in pairs( values ) do
            -- Replace the variable in the message with the specified value.
            tostring(v):gsub("(%%)", "%%%%") 
            message = message:gsub( "{" .. k .. "}", v)
        end

        return message
    end
end