
-- -----------------------------------------------------------------------------------
-- Increased Gather Rate                                                 Version 1.1.2
-- -----------------------------------------------------------------------------------
-- Filename:          m-GatherRate.lua
-- Last Modification: 02-10-2014
-- -----------------------------------------------------------------------------------
-- Description:
--
-- This plugin is developed for Rust servers with the Oxide Server Mod and will offer
-- server admins the option to increase the amount of items gained from gathering.
-- -----------------------------------------------------------------------------------


PLUGIN.Title = "Increased Gathering Rate"
PLUGIN.Description = "Increase the amount of resources from gathering per attack."
PLUGIN.Version = V( 1, 1, 2 )
PLUGIN.HasConfig = true
PLUGIN.Author = "Mughisi"
PLUGIN.ResourceId = 675


-- -----------------------------------------------------------------------------------
-- Globals
-- -----------------------------------------------------------------------------------
-- Some globals that are used in multiple functions.
-- -----------------------------------------------------------------------------------


-- -----------------------------------------------------------------------------------
-- PLUGIN:Init()
-- -----------------------------------------------------------------------------------
-- On plugin initialisation the required in-game chat command is registered and the
-- configuration file is checked.
-- -----------------------------------------------------------------------------------
function PLUGIN:Init()
    -- Add the chat command.
    command.AddChatCommand("gather", self.Plugin, "cmdGatherRate")

    -- Check the configuration file.
    self:CheckConfig()
    
    -- Show a message in the server console with the current values.
    print( "Resources from gathering is set to: x" .. self.Config.Settings.GatherMultiplier .. "." )
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
        Version                   = "1.2",
        ChatName                  = "Gatherer",
        GatherMultiplier          = 1
    }

    -- Plugin Messages:
    self.Config.Messages = {
        SetGatherRate = "Resources from gathering has been modified, it is now set to: x{multiplier}!",
        GatherRate = "Resources from gathering is set to: x{multiplier}!",
        Error = "The new multiplier must be a positive number!",
        HelpTextPlayer = "Use /gather to check the gathering rate!",
        HelpTextAdmin = {
                "Use '/gather' to check the gathering rate!",
                "Use '/gather <multiplier>' to increase the amount of resources gathered by <multiplier>!",
            },
        GatherResourceError = {
                "To increase the gather rate of a certain resource you need a valid resource name.",
                "These are the resources you can modify:"
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
    if self.Config.Settings.Version ~= "1.2" then
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
    print( "m-GatherRate.lua : Default Config Loaded" )
end

-- -----------------------------------------------------------------------------------
-- function PLUGIN:cmdGatherRate( player, cmd, args) 
-- -----------------------------------------------------------------------------------
-- In-game '/craft' command for server admins to be able to modify the crafting speed
-- and for players to check the crafting speed.
-- -----------------------------------------------------------------------------------
function PLUGIN:cmdGatherRate(player, cmd, args)
    -- Check if the player is an admin, if this is not the case the player will only
    -- be able to see the current rates.
    if player.net.connection.authLevel == 0 then
        -- The player is not an admin, show a message with the current multiplier and
        -- exit the function.
        self:SendMessage( player, self:Parse( self.Config.Messages["GatherRate"], { multiplier = self.Config.Settings.GatherMultiplier } ) )
        
        return 
    end
    
    -- The player is an admin, check how many arguments were supplied with the
    -- command and respond correctly.
    if args.Length == 1 then
        -- Multiple arguments were supplied, assuming the player is attempting to
        -- modify the gathering rate and perhaps the amount of resources in
        -- resource dispensers (trees, corpses, stone/ore deposits, ...)

        -- Initialise some required variables
        local newGatherMultiplier = tonumber( args[0] )

        -- Check if we have a valid value for both the new gathering multiplier and
        -- resource amount multiplier.
        if newGatherMultiplier then
            -- Check if both values are positive.
            if tonumber( newGatherMultiplier ) > 0 then
                -- Update the configuration file with the new values and save it.
                self.Config.Settings.GatherMultiplier = newGatherMultiplier
                self:SaveConfig()

                -- Send a message to the player.
                self:SendMessage( player, self:Parse( self.Config.Messages["SetGatherRate"], { multiplier = self.Config.Settings.GatherMultiplier } ) )

                return
            end
        end

        -- Something went wrong, show an error message.
        self:SendMessage( player, self.Config.Messages["Error"] )
    else
        -- Show the values to the player.
        self:SendMessage( player, self:Parse( self.Config.Messages["GatherRate"], { multiplier = self.Config.Settings.GatherMultiplier } ) )
    end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:OnPlayerAttack( player, hitinfo )
-- -----------------------------------------------------------------------------------
-- OnPlayerAttack Oxide Hook. This hook is triggered when a BasePlayer attacks another
-- entity. This hook is used to capture resource gathering.
-- -----------------------------------------------------------------------------------
function PLUGIN:OnGather( dispenser, entity, item )
    -- Increase the amount of the item gained with the modifier, this needs to be
    -- extended in the future to allow for on playerbase modifications, different
    -- modifiers per resource and different resources per dispenser.
    if entity:ToPlayer() then
        if tonumber( item.amount ) then
            item.amount = item.amount * self.Config.Settings.GatherMultiplier
        end
    end
end
-- -----------------------------------------------------------------------------------
-- PLUGIN:SendHelpText( player )
-- -----------------------------------------------------------------------------------
-- HelpText plugin support for the command /help.
-- -----------------------------------------------------------------------------------
function PLUGIN:SendHelpText( player )
    if player.net.connection.authLevel > 0 then
        self:SendMessage( player, self.Config.Messages.HelpTextAdmin )
    else
        self:SendMessage( player, self.Config.Messages.HelpTextPlayer )
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
