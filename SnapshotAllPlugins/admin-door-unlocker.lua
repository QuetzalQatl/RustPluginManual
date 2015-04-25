--[[
 Admin door unlocker
 
 Copyright (c) 2015 Nexus <talk@juliocesar.me>, <http://steamcommunity.com/profiles/76561197983103320>
 
 -------------------------------------------------------------------------------------------------------------------
 This work is licensed under the Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.
 To view a copy of this license, visit http://creativecommons.org/licenses/by-nc-sa/4.0/deed.en_US.
 -----------------------------------------------------------------------------------------------------------------
 
 $Id$
 Version 0.1.2 by Nexus on 2015-04-11 06:05 PM (UTC -03:00)
]]--

PLUGIN.Name = "admin-door-unlocker"
PLUGIN.Title = "Admin door Unlocker"
PLUGIN.Description = "Unlock any door/box for Admins"
PLUGIN.Version = V(0, 1, 2)
PLUGIN.Author = "Nexus"
PLUGIN.HasConfig = true
PLUGIN.ResourceId = 756

-- Define A.D.U class
local ADU = {}

-- Define Config version
ADU.ConfigVersion = "0.0.4"

-- Get a Copy of PLUGIN Class
ADU.ox = PLUGIN

-- Define Settings
ADU.Settings = {}
-- Define Messages
ADU.Messages = {}

-- General Settings:
ADU.DefaultSettings = {
  ChatName = "A.D.U",
  Enabled = true,
  RequiredAuthLevel = 2,
  ConfigVersion = "0.0.4"
}

-- Plugin Messages:
ADU.DefaultMessages = {
  Enabled = "Enabled!",
  Disabled = "Disabled!",
  AuthLevelChanged = "You changed the required Auth Level to %d!",
  InvalidAuthLevel = "You need pass a valid auth level like: admin, owner, mod, moderator, user, player, 0 or 1 or 2!",
  NotAllowed = "You cannot use that command because you don't have the required Auth Level %d!",
  AuthLog = " %s by %s (%s)",

  Help = {
    "/adu.toggle - Toggle (Enable/Disable) A.D.U!",
    "/adu.authlevel <n/s> - Change the required auth level to open locked doors."
  }
}

-- -----------------------------------------------------------------------------------
-- ADU:UpdateConfig()
-- -----------------------------------------------------------------------------------
-- It check if the config version is outdated
-- -----------------------------------------------------------------------------------
function ADU:UpdateConfig()
  -- Check if the current config version differs from the saved
  if self.ox.Config.Settings.ConfigVersion ~= self.ConfigVersion then
    -- Reset the whole table
    self.ox.Config.Settings = {}
    self.ox.Config.Messages = {}
    
    -- Load the default
    self.ox:LoadDefaultConfig()
    -- Save config
    self.ox:SaveConfig()
  end
  
  -- Copy Tables
  self.Settings = self.ox.Config.Settings
  self.Messages = self.ox.Config.Messages
end

-- -----------------------------------------------------------------------------
-- ADU:SendMessage(param, message)
-- -----------------------------------------------------------------------------
-- Sends a chatmessage to a player/console
-- -----------------------------------------------------------------------------
function ADU:SendMessage(param, message)
  -- Check if the message is a table with multiple messages.
  if type(message) == "table" then
    -- Loop by table of messages and send them one by one
    for i, message in pairs(message) do
      -- Loop back
      self:SendMessage(param, message)
    end
  else
    -- Check if param is not null
    if param ~= nil then
      -- Check if call came from user's chat or console
      if type(param.net) == 'userdata' then
          -- Send the message to the targetted player.
         rust.SendChatMessage(param, self.Settings.ChatName, message, rust.UserIDFromPlayer(param))
      elseif type(param.net) == 'string' then    
        -- Check if was passed by client's console 
        if param.connection then
          -- Reply back to player's console
          param:ReplyWith(self.Settings.ChatName..": "..message) 
          -- Send message with authLog to console
          self:SendMessage(nil, self.Messages.AuthLog:format(message, param.connection.player.displayName, rust.UserIDFromPlayer(param.connection.player)))
        else
          -- Send message to console
          self:SendMessage(nil, message)
        end
      end
    else
      -- Log
      self:Log(self.Settings.ChatName..": "..message) 
    end
  end
end

-- -----------------------------------------------------------------------------------
-- ADU:Toggle(player)
-- -----------------------------------------------------------------------------------
-- Enable/Disable Admin Door Unlocker
-- -----------------------------------------------------------------------------------
function ADU:Toggle(player)
  -- Check if Admin Door Unlocker is enabled
  if self.Settings.Enabled then
    -- Disable Admin Door Unlocker
    self.Settings.Enabled = false
    -- Send Message to Player
    self:SendMessage(player, self.Messages.Disabled)
  else
    -- Enable Admin Door Unlocker
    self.Settings.Enabled = true
    -- Send Message to Player
    self:SendMessage(player, self.Messages.Enabled)
  end

  -- Save the config.
  self.ox:SaveConfig()
end

-- -----------------------------------------------------------------------------------
-- ADU:ChangeAuthLevel(player, authLevel)
-- -----------------------------------------------------------------------------------
-- Change Auth Level required to use Admin Door Unlocker
-- -----------------------------------------------------------------------------------
function ADU:ChangeAuthLevel(player, authLevel)
  -- Check if Admin Door Unlocker is enabled
  if self.Settings.Enabled then
    -- Check for Admin
    if authLevel == "admin" or authLevel == "owner" or authLevel == "2" then
      -- Set required auth level to admin
      self.Settings.RequiredAuthLevel = 2
      -- Send message to player
      self:SendMessage(player, self.Messages.AuthLevelChanged:format(2))
      -- Check for Mod
    elseif authLevel == "mod" or authLevel == "moderator" or authLevel == "1" then
      -- Set required auth level to moderator
      self.Settings.RequiredAuthLevel = 1
      -- Send message to player
      self:SendMessage(player, self.Messages.AuthLevelChanged:format(1))
      -- Check for Mod
    elseif authLevel == "user" or authLevel == "player" or authLevel == "0" then
      -- Set required auth level to moderator
      self.Settings.RequiredAuthLevel = 0
      -- Send message to player
      self:SendMessage(player, self.Messages.AuthLevelChanged:format(0))
    else
      -- Send message to player
      self:SendMessage(player, self.Messages.InvalidAuthLevel)
    end

    -- Save the config.
    self.ox:SaveConfig()
  end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:IsAllowed(player)
-- -----------------------------------------------------------------------------------
-- Checks if the player is allowed to run an admin (or moderator or user) only command.
-- -----------------------------------------------------------------------------------
function ADU:IsAllowed(player)
  -- Check if player is valid
  if player ~= nil then
    -- Check if is connected
    if player:GetComponent("BaseNetworkable").net.connection ~= nil then
      -- Compare the Player's AuthLevel with the required AuthLevel, if it's higher or equal
      return player:GetComponent("BaseNetworkable").net.connection.authLevel >= self.Settings.RequiredAuthLevel
    end
  end

  return false
end

-- -----------------------------------------------------------------------------------
-- ADU:Log(message)
-- -----------------------------------------------------------------------------------
-- Log normal
-- -----------------------------------------------------------------------------------
-- Credit: HooksTest
-- -----------------------------------------------------------------------------------
function ADU:Log(message)
  UnityEngine.Debug.Log.methodarray[0]:Invoke(nil, util.TableToArray({message}))
end

-- -----------------------------------------------------------------------------------
-- ADU:LogWarning(message)
-- -----------------------------------------------------------------------------------
-- Log Warning
-- -----------------------------------------------------------------------------------
-- Credit: HooksTest
-- -----------------------------------------------------------------------------------
function ADU:LogWarning(message)
  UnityEngine.Debug.LogWarning.methodarray[0]:Invoke(nil, util.TableToArray({message}))
end

-- -----------------------------------------------------------------------------------
-- ADU:LogError(message)
-- -----------------------------------------------------------------------------------
-- Log Error
-- -----------------------------------------------------------------------------------
-- Credit: HooksTest
-- -----------------------------------------------------------------------------------
function ADU:LogError(message)
  UnityEngine.Debug.LogError.methodarray[0]:Invoke(nil, util.TableToArray({message}))
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:Init()
-- -----------------------------------------------------------------------------------
-- On plugin initialisation the required in-game chat commands are registered and data
-- from the DataTable file is loaded.
-- -----------------------------------------------------------------------------------
function PLUGIN:Init ()
  -- Add chat commands
  command.AddChatCommand("adu.authlevel", self.Plugin, "cmdChangeAuthLevel")
  command.AddChatCommand("adu.toggle", self.Plugin, "cmdToggleADU")
  
  -- Add console commands
  command.AddConsoleCommand("adu.authlevel", self.Plugin, "ccmdChangeAuthLevel")
  command.AddConsoleCommand("adu.toggle", self.Plugin, "ccmdToggleADU")
  
  -- Update config version
  ADU:UpdateConfig()
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:LoadDefaultConfig()
-- -----------------------------------------------------------------------------------
-- The plugin uses a configuration file to save certain settings and uses it for
-- localized messages that are send in-game to the players. When this file doesn't
-- exist a new one will be created with these default values.
-- -----------------------------------------------------------------------------------
function PLUGIN:LoadDefaultConfig ()
  self.Config.Settings = ADU.DefaultSettings
  self.Config.Messages = ADU.DefaultMessages
end

-- -----------------------------------------------------------------------------
-- PLUGIN:Count(tbl)
-- -----------------------------------------------------------------------------
-- Counts the elements of a table.
-- -----------------------------------------------------------------------------
-- Credit: m-Teleportation
function PLUGIN:Count(tbl)
  local count = 0

  if type(tbl) == "table" then
    for _ in pairs(tbl) do
      count = count + 1
    end
  end

  return count
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:SendHelpText(player)
-- -----------------------------------------------------------------------------------
-- HelpText plugin support for the command /help.
-- -----------------------------------------------------------------------------------
function PLUGIN:SendHelpText(player)
  -- Check if user is admin
  if ADU:IsAllowed(player) then
    -- Send message to player
    ADU:SendMessage(player, self.Config.Messages.Help)
  end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:cmdChangeAuthLevel(player, _, args)
-- -----------------------------------------------------------------------------------
-- Change required Auth Level
-- -----------------------------------------------------------------------------------
function PLUGIN:cmdChangeAuthLevel(player, _, args)
  -- Check if Admin Door Unlocker is enabled
  if ADU:IsAllowed(player) and self.Config.Settings.Enabled then
    -- Check for passed args
    if args.Length == 1 then
      -- Change required Auth level
      ADU:ChangeAuthLevel(player, args[0])
    elseif args.Length == 0 then
      -- Send message to player
      ADU:SendMessage(player, self.Config.Messages.InvalidAuthLevel)
    end
  -- Check if player is not allowed and A.D.U is enabled
  elseif not ADU:IsAllowed(player) and self.Config.Settings.Enabled then
    -- Send message to player
    ADU:SendMessage(player, self.Config.Messages.NotAllowed:format(self.Config.Settings.RequiredAuthLevel))  
  end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:cmdToggleADU(player)
-- -----------------------------------------------------------------------------------
-- Enable/Disable Inventory Guardian
-- -----------------------------------------------------------------------------------
function PLUGIN:cmdToggleADU(player)
  -- Check if Inventory Guardian is enabled and If player is allowed
  if ADU:IsAllowed(player) then
    -- Restore Player inventory
    ADU:Toggle(player)
  else
    -- Send message to player
    ADU:SendMessage(player, self.Config.Messages.NotAllowed:format(self.Config.Settings.RequiredAuthLevel))
  end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:CanUseDoor()
-- -----------------------------------------------------------------------------------
-- When any player try to open any door this function is trigged
-- -----------------------------------------------------------------------------------
function PLUGIN:CanUseDoor(player, door)
  -- Check if player is admin
  if ADU:IsAllowed(player) and self.Config.Settings.Enabled then
    -- Unlock the door
    door:SetFlag(global["BaseEntity+Flags"].Locked, false)
    -- Lock the door
    timer.Once(0.1, function() door:SetFlag(global["BaseEntity+Flags"].Locked, true) end)
  end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:ccmdChangeAuthLevel(arg)
-- -----------------------------------------------------------------------------------
-- Change required Auth Level
-- -----------------------------------------------------------------------------------
function PLUGIN:ccmdChangeAuthLevel(arg)
  -- Check for passed args
  if arg:HasArgs(1) then
    -- Change required Auth level
    ADU:ChangeAuthLevel(arg, arg.Args[0])
  else
    -- Send message to player
    ADU:SendMessage(arg, self.Config.Messages.InvalidAuthLevel)
  end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:ccmdToggleInventoryGuardian(arg)
-- -----------------------------------------------------------------------------------
-- Enable/Disable Inventory Guardian
-- -----------------------------------------------------------------------------------
function PLUGIN:ccmdToggleADU(arg)
  -- Restore Player inventory
  ADU:Toggle(arg)
end
