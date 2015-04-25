
-- -----------------------------------------------------------------------------------
-- Disappearing Corpses                                                  Version 1.0.3
-- -----------------------------------------------------------------------------------
-- Filename:          m-DisappearingCorpses.lua
-- Last Modification: 01-21-2015
-- -----------------------------------------------------------------------------------
-- Description:
--
-- This plugin is developed for Rust servers with the Oxide Server Mod and will
-- allow server administrators modify the time that a player's corpse remains active
-- in the world.
-- -----------------------------------------------------------------------------------

PLUGIN.Title       = "Disappearing Corpses"
PLUGIN.Description = "Modify the duration of which a player's corpse remains active."
PLUGIN.Version     = V( 1, 0, 3 )
PLUGIN.HasConfig   = true
PLUGIN.Author      = "Mughisi"
PLUGIN.ResourceId  = 778

-- -----------------------------------------------------------------------------------
-- PLUGIN:Init()
-- -----------------------------------------------------------------------------------
-- On plugin initialisation the required in-game chat command is registered.
-- -----------------------------------------------------------------------------------
function PLUGIN:Init()
    -- Add the chat commands:
    command.AddChatCommand( "corpsetime", self.Object, "cmdCorpseDuration" )

    -- Check the config.
    self:CheckConfig()
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:LoadDefaultConfig()
-- -----------------------------------------------------------------------------------
-- The plugin uses a configuration file to save certain settings and uses it for
-- localized messages that are send in-game to the admins. When this file doesn't
-- exist a new one will be created with these default values.
-- -----------------------------------------------------------------------------------
function PLUGIN:LoadDefaultConfig()
    -- General Settings:
    self.Config.Settings = {
        ChatName           = "Corpses",
        Duration           = 120,
        Version            = "1.0"
    }

    -- Plugin Messages:
    self.Config.Messages = {
        -- Messages involving /settime and env.time
        CorpseTimeSuccess       = "Modified the length of which a player's corpse remains active in the world to {length} minutes.",
        SyntaxCommandCorpseTime = {
            "A Syntax Error Occurred!",
            "You can only use the /corpsetime command as follows:",
            "/corpsetime <minutes> - Keeps the player's corpse active in the world for <minutes>.",
            "The time in minutes must be atleast 1 and should not exceed 60."
        }
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
    if self.Config.Settings.Version ~= "1.0" then
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
    self:LoadDefaultConfig()
    self:SaveConfig()
    print( "m-DisappearingCorpses.lua : Default Config Loaded" )
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:cmdCorpseDuration( player, cmd, args )                         Admin Command
-- -----------------------------------------------------------------------------------
-- In-game '/corpsetime' command that allows the server admins to modify the length of
-- which the corpse of a player remains active in the world.
-- -----------------------------------------------------------------------------------
function PLUGIN:cmdCorpseDuration( player, cmd, args )
    -- Check if the player is an admin, if this is not the case exit the function.
    if player.net.connection.authLevel == 0 then
        return
    end
    
    -- Check if the player specified an argument.
    if args.Length == 1 then
        -- The player specified an argument, checking if this is a number.
        local newDuration = tonumber( args[0] )

        -- Checking if the new duration is valid.
        if newDuration >= 1 and newDuration <= 60 then
            -- The new hour is valid, modify the duration and inform the player.
            self.Config.Settings.Duration = newDuration * 60
            self:SaveConfig()

            self:SendMessage( player, self:Parse( self.Config.Messages.CorpseTimeSuccess, { length = newDuration } ) )

            return
        end
    end

    -- Something went wrong, show a syntax error to the player.
    self:SendMessage( player, self.Config.Messages.SyntaxCommandCorpseTime )
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:OnEntitySpawn( entity )
-- -----------------------------------------------------------------------------------
-- OnEntitySpawn Oxide Hook. This hook is triggered when an entity is spawned into the
-- world. This hook is used to modify the length of which the corpse of a player
-- remains active in the world when the corpse is first spawned.
-- -----------------------------------------------------------------------------------
function PLUGIN:OnEntitySpawn( entity )
    -- Check if a corpse is spawned in the world.
    if entity:GetComponent( "BaseCorpse" ) then
        -- Check if a valid parent entity is available.
        if not entity:GetComponent( "BaseCorpse" ).parentEnt then return end

        -- Check if the corpse is from a player and not an animal.
        if entity.parentEnt:ToPlayer() then
            -- A corpse is spawned in the world, modify the duration that it remains
            -- active.
            entity:ResetRemovalTime( self.Config.Settings.Duration )
        end
    end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:OnGather( dispenser, entity, item )
-- -----------------------------------------------------------------------------------
-- OnGather Oxide Hook. This hook is triggered when an entity gathers a 
-- ResourceDispenser. This hook is used to reset the timer that a player's corpse 
-- remains alive otherwise it would reset it to the standard duration of 2 minutes.
-- -----------------------------------------------------------------------------------
function PLUGIN:OnGather( dispenser, entity, item )
    if dispenser:GetComponent( "BaseCorpse" ) then
        local corpse = dispenser:GetComponent( "BaseCorpse" )
        
        -- Check if a valid parent entity is available.
        if not corpse.parentEnt then return end
        if not corpse.parentEnt:ToString():find("player/player", 1, true) then return end

        -- Check if the corpse is from a player and not an animal.
        if a then
            -- A corpse is spawned in the world, modify the duration that it remains active.
           corpse:ResetRemovalTime( self.Config.Settings.Duration )
        end
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
    for k, v in pairs( values ) do
        -- Replace the variable in the message with the specified value.
        tostring(v):gsub("(%%)", "%%%%") 
        message = message:gsub( "{" .. k .. "}", v)
    end

    return message
end
