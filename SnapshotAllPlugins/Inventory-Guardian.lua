--[[ 
 Inventory Guardian
 
 Copyright (c) 2015 Nexus <talk@juliocesar.me>, <http://steamcommunity.com/profiles/76561197983103320/>
 
 -------------------------------------------------------------------------------------------------------------------
 This work is licensed under the Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.
 To view a copy of this license, visit http://creativecommons.org/licenses/by-nc-sa/4.0/deed.en_US.
 -----------------------------------------------------------------------------------------------------------------
 
 $Id$
 Version 0.1.5 by Nexus on 2015-04-11 06:15 PM (UTC -03:00)
]]--

PLUGIN.Name = "Inventory-Guardian"
PLUGIN.Title = "Inventory Guardian"
PLUGIN.Description = "Keep players inventory after server wipes"
PLUGIN.Version = V(0, 1, 5)
PLUGIN.Author = "Nexus"
PLUGIN.HasConfig = true
PLUGIN.ResourceId = 773

-- Define Inventory Guardian class
local IG = {}

-- Define Inventory Data
IG.Data = {}

-- Define Player deaths table
IG.PlayerDeaths = {}

-- Define default save protocol
IG.SaveProtocol = 0

-- Get a Copy of PLUGIN Class
IG.ox = PLUGIN

-- Define Config version
IG.ConfigVersion = "0.0.8"

-- Define Settings
IG.Settings = {}
-- Define Messages
IG.Messages = {}

-- Define Local config values
IG.DefaultSettings = {
  ChatName = "Inventory Guardian",
  Enabled = true,
  RequiredAuthLevel = 2,
  ConfigVersion = "0.0.8",
  RestoreUponDeath = false,
  AutoRestore = true,
  KeepItemCondition = true
}

-- Define Plugin Messages:
IG.DefaultMessages = {
  Saved = "Your inventory was been saved!",
  Restored = "Your inventory has been restored!",
  RestoreUponDeathEnabled = "Restore Upon Death Enabled!",
  RestoreUponDeathDisabled = "Restore Upon Death Disabled!",
  RestoreEmpty = "You don't have any saved inventory, so cannot be restored!",
  DeletedInv = "Your saved inventory was deleted!",
  Enabled = "Enabled!",
  Disabled = "Disabled!",
  AutoRestoreDisabled = "Automatic Restoration has been disabled!",
  AutoRestoreEnabled = "Automatic Restoration has been enabled!",
  AuthLevelChanged = "You changed the required Auth Level to %d!",
  CantDoDisabled = "We are unable to run that command since the Inventory Guardian is disabled!",
  NotAllowed = "You cannot use that command because you don't have the required Auth Level %d!",
  InvalidAuthLevel = "You need pass a valid auth level like: admin, owner, mod, moderator, 1 or 2!",
  RestoredPlayerInventory = "Player \"%s\" inventory has been restored!",
  RestoreInit = "Initiating all players inventories restoration...",
  RestoreAll = "All players inventories has been restored!",
  SavedPlayerInventory = "Player \"%s\" inventory has been saved!",
  SaveInit = "Initiating all players inventories salvation...",
  SaveAll = "All players inventories has been saved!",
  PlayerNotFound = "The specified player couldn't be found please try again!",
  MultiplePlayersFound = "Found multiple players with that name!",
  DeletedPlayerInventory = "Player \"%s\" saved inventory has been deleted!",
  DeleteAll = "All players inventories has been deleted!",
  DeleteInit = "Initiating all players inventories deletion...",
  StripInit = "Initiating all players inventories strips...",
  StripAll = "All players inventories has been stripped!",
  SelfStriped = "Your current inventory has been cleaned!",
  PlayerStriped = "Your current inventory has been cleaned by \"%s\"",
  PlayerStripedBack = "Player \"%s\" inventory has been cleaned.",
  AutoRestoreDetected = "Map wipe was detected!",
  AutoRestoreNotDetected = "Forced map wipe not detected!",
  WipeRestoreOnce = "Restore once has been enabled to all players.",
  RestoreOnce = "Restore once has been enabled to %s!",
  KeepConditionEnabled = "Items condition restoration has been enabled!",
  KeepConditionDisabled = "Items condition restoration has been disabled!",
  PlayerSaved = "Your inventory has been saved by \"%s\".",
  PlayerDeleted = "Your saved inventory has been deleted by \"%s\"!",
  PlayerRestored = "Your inventory has been restored by \"%s\"!",
  PlayerRestoreEmpty = "Player \"%s\" don't have any saved inventory, so cannot be restored!",
  AuthLog = " %s by %s (%s)",

  Help = {
    "/ig.save - Save your inventory for later restoration!",
    "/ig.restore - Restore your saved inventory!",
    "/ig.delsaved - Delete your saved inventory!",
    "/ig.save <name> - Save player's inventory for later restoration!",
    "/ig.restore <name> - Restore player's saved inventory!",
    "/ig.delsaved <name> - Delete player's saved inventory!",
    "/ig.restoreupondeath - Toggles the Inventory restoration upon death for all players on the server!",
    "/ig.toggle - Toggle (Enable/Disable) Inventory Guardian!",
    "/ig.autorestore - Toggle (Enable/Disable) Automatic Restoration.",
    "/ig.authlevel <n/s> - Change Inventory Guardian required Auth Level.",
    "/ig.strip - Clear your current inventory.",
    "/ig.strip <name> - Clear player current inventory.",
    "/ig.keepcondition - Toggle (Enable/Disable) Items condition restoration."
  }
}

-- -----------------------------------------------------------------------------------
-- IG:UpdateConfig()
-- -----------------------------------------------------------------------------------
-- It check if the config version is outdated
-- -----------------------------------------------------------------------------------
function IG:UpdateConfig()
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

-- -----------------------------------------------------------------------------------
-- IG:ClearSavedInventory(player)
-- -----------------------------------------------------------------------------------
-- Clear player's saved inventory on Data Table
-- -----------------------------------------------------------------------------------
function IG:ClearSavedInventory(player)
  -- Grab the player SteamID.
  local playerID = rust.UserIDFromPlayer(player)
  
  -- Check if playerID is valid
  if playerID ~= "0" then
    -- Reset inventory
    self.Data.GlobalInventory[playerID] = {}
    self.Data.GlobalInventory[playerID]['belt'] = {}
    self.Data.GlobalInventory[playerID]['main'] = {}
    self.Data.GlobalInventory[playerID]['wear'] = {}
    -- Save Inventory
    self.ox:SaveData()
  end
end

-- -----------------------------------------------------------------------------------
-- IG:SaveInventory(player)
-- -----------------------------------------------------------------------------------
-- Save player inventory
-- -----------------------------------------------------------------------------------
function IG:SaveInventory(player)
  -- Grab the player SteamID.
  local playerID = rust.UserIDFromPlayer(player)

  -- Get Player inventory list
  local belt = player.inventory.containerBelt
  local main = player.inventory.containerMain
  local wear = player.inventory.containerWear

  -- Enumerate inventory list
  local beltItems = belt.itemList:GetEnumerator()
  local mainItems = main.itemList:GetEnumerator()
  local wearItems = wear.itemList:GetEnumerator()
  -- Reset counts
  local beltCount = 0
  local mainCount = 0
  local wearCount = 0

  -- Reset saved inventory
  self:ClearSavedInventory(player)

  -- Loop by the Belt Items
  while beltItems:MoveNext() do
    -- Save current item to player's inventory table
    self.Data.GlobalInventory[playerID]['belt'][tostring(beltCount)] = {name = tostring(beltItems.Current.info.shortname), amount = beltItems.Current.amount, condition = beltItems.Current.condition, bp = beltItems.Current.isBlueprint}
    -- Increment the count
    beltCount = beltCount + 1
  end

  -- Loop by the Main Items
  while mainItems:MoveNext() do
    -- Save current item to player's inventory table
    self.Data.GlobalInventory[playerID]['main'][tostring(mainCount)] = {name = tostring(mainItems.Current.info.shortname), amount = mainItems.Current.amount, condition =  mainItems.Current.condition, bp = mainItems.Current.isBlueprint}
    -- Increment the count
    mainCount = mainCount + 1
  end

  -- Loop by the Wear Items
  while wearItems:MoveNext() do
    -- Save current item to player's inventory table
    self.Data.GlobalInventory[playerID]['wear'][tostring(wearCount)] = {name = tostring(wearItems.Current.info.shortname), amount = wearItems.Current.amount, condition = wearItems.Current.condition, bp = false}
    -- Increment the count
    wearCount = wearCount + 1
  end  

  -- Save inventory data
  self.ox:SaveData()
end

-- -----------------------------------------------------------------------------------
-- IG:SavedInventoryIsEmpty(player)
-- -----------------------------------------------------------------------------------
-- Check if player's saved inventory is empty
-- -----------------------------------------------------------------------------------
function IG:SavedInventoryIsEmpty(player)
  -- Grab the player SteamID.
  local playerID = rust.UserIDFromPlayer(player)
  
  -- Check if player's inventory is null
  if self.Data.GlobalInventory[playerID] == nil then
    return true
  else
    -- Check if all inventory containers are empty too
    return self:Count(self.Data.GlobalInventory [playerID] ['belt']) == 0 and self:Count(self.Data.GlobalInventory [playerID] ['main']) == 0 and self:Count(self.Data.GlobalInventory [playerID] ['wear'] )== 0
  end
end

-- -----------------------------------------------------------------------------
-- PLUGIN:Count(tbl)
-- -----------------------------------------------------------------------------
-- Counts the elements of a table.
-- -----------------------------------------------------------------------------
-- Credit: m-Teleportation
function IG:Count(tbl)
  local count = 0

  if type(tbl) == "table" then
    for _ in pairs(tbl) do
      count = count + 1
    end
  end

  return count
end

-- -----------------------------------------------------------------------------------
-- IG:RestoreInventory(player)
-- -----------------------------------------------------------------------------------
-- Restore player inventory
-- -----------------------------------------------------------------------------------
function IG:RestoreInventory(player)
  -- Grab the player his/her SteamID.
  local playerID = rust.UserIDFromPlayer(player)
  -- Clear player Inventory
  player.inventory:Strip()

  -- This fixes the incomplete restoration process
  timer.Once (1, function ()
    -- Get Player inventory list
    local belt = player.inventory.containerBelt
    local main = player.inventory.containerMain
    local wear = player.inventory.containerWear
    local Inventory = {}

    -- Set inventory
    Inventory['belt'] = belt
    Inventory['main'] = main
    Inventory['wear'] = wear

    -- Loop by player's saved inventory slots
    for slot, items in pairs(self.Data.GlobalInventory[playerID]) do
      --Loop by slots
      for i, item in pairs(items) do

        -- Create an inventory item
        local itemEntity = global.ItemManager.CreateByName(item.name, item.amount)
        
        -- Check for Blueprint field
        if item.bp then
          -- Set Item as Blueprint
          itemEntity.isBlueprint = true
        -- Check for Health field
        elseif item.condition and self.Settings.KeepItemCondition then
          -- Define item health
          itemEntity.condition = item.condition       
        end

        -- Set that created inventory item to player
        player.inventory:GiveItem(itemEntity, Inventory[slot])
      end
    end
  end)
end

-- -----------------------------------------------------------------------------
-- IG:SendMessage(param, message)
-- -----------------------------------------------------------------------------
-- Sends a chatmessage to a player/console
-- -----------------------------------------------------------------------------
function IG:SendMessage(param, message)
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
-- IG:RestoreUponDeath(player)
-- -----------------------------------------------------------------------------------
-- Toogle the config restore upon death
-- -----------------------------------------------------------------------------------
function IG:ToggleRestoreUponDeath(player)
  -- Check if Inventory Guardian is enabled
  if self.Settings.Enabled then
    -- Check if Restore Upon Death is enabled
    if self.Settings.RestoreUponDeath then
      -- Disable Restore Upon Death
      self.Settings.RestoreUponDeath = false
      -- Send Message to Player
      self:SendMessage(player, self.Messages.RestoreUponDeathDisabled)
    else
      -- Enable Restore Upon Death
      self.Settings.RestoreUponDeath = true
      -- Send Message to Player
      self:SendMessage(player, self.Messages.RestoreUponDeathEnabled)
    end

    -- Save the config.
    self.ox:SaveConfig()
  end
end

-- -----------------------------------------------------------------------------------
-- IG:ToogleKeepCondition()
-- -----------------------------------------------------------------------------------
-- Toogle the config KeepItemCondition
-- -----------------------------------------------------------------------------------
function IG:ToogleKeepCondition(player)
  -- Check if Inventory Guardian is enabled
  if self.Settings.Enabled then
    -- Check if Keep Items Condition is enabled
    if self.Settings.KeepItemCondition then
      -- Disable Keep Items Condition
      self.Settings.KeepItemCondition = false
      -- Send Message to Player
      self:SendMessage(player, self.Messages.KeepConditionDisabled)
    else
      -- Enable Restore Upon Death
      self.Settings.KeepItemCondition = true
      -- Send Message to Player
      self:SendMessage(player, self.Messages.KeepConditionEnabled)
    end

    -- Save the config.
    self.ox:SaveConfig()
  end
end

-- -----------------------------------------------------------------------------------
-- IG:ToggleInventoryGuardian(player)
-- -----------------------------------------------------------------------------------
-- Enable/Disable Inventory Guardian
-- -----------------------------------------------------------------------------------
function IG:ToggleInventoryGuardian(player)
  -- Check if Inventory Guardian is enabled
  if self.Settings.Enabled then
    -- Disable Inventory Guardian
    self.Settings.Enabled = false
    -- Send Message to Player
    self:SendMessage(player, self.Messages.Disabled)
  else
    -- Enable Inventory Guardian
    self.Settings.Enabled = true
    -- Send Message to Player
    self:SendMessage(player, self.Messages.Enabled)
  end

  -- Save the config.
  self.ox:SaveConfig()
end

-- -----------------------------------------------------------------------------------
-- IG:ToggleAutoRestore(player)
-- -----------------------------------------------------------------------------------
-- Enable/Disable Automatic restoration
-- -----------------------------------------------------------------------------------
function IG:ToggleAutoRestore(player)
  -- Check if Inventory Guardian is enabled
  if self.Settings.AutoRestore then
    -- Disable Inventory Guardian's Auto restore
    self.Settings.AutoRestore = false
    -- Send Message to Player
    self:SendMessage(player, self.Messages.AutoRestoreDisabled)
  else
    -- Enable Inventory Guardian's Auto restore
    self.Settings.AutoRestore = true
    -- Send Message to Player
    self:SendMessage(player, self.Messages.AutoRestoreEnabled)
  end

  -- Save the config.
  self.ox:SaveConfig()
end

-- -----------------------------------------------------------------------------------
-- IG:ChangeAuthLevel(player, authLevel)
-- -----------------------------------------------------------------------------------
-- Change Auth Level required to use Inventory Guardian
-- -----------------------------------------------------------------------------------
function IG:ChangeAuthLevel(player, authLevel)
  -- Check if Inventory Guardian is enabled
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
    else
      -- Send message to player
      self:SendMessage(player, self.Messages.InvalidAuthLevel)
    end

    -- Save the config.
    self.ox:SaveConfig()
  end
end

-- -----------------------------------------------------------------------------------
-- IG:IsAllowed(player)
-- -----------------------------------------------------------------------------------
-- Checks if the player is allowed to run an admin (or moderator or user) only command.
-- -----------------------------------------------------------------------------------
function IG:IsAllowed(player)
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
-- IG:Check(player)
-- -----------------------------------------------------------------------------------
-- Checks if the player is allowed to run and save Inventory
-- -----------------------------------------------------------------------------------
function IG:Check(player)
  -- Check if Inventory Guardian is enabled
  if not self.Settings.Enabled then
    -- Send message to player
    self:SendMessage(player, self.Messages.CantDoDisabled)

    return false
      -- Check if player is allowed and Inventory Guardian is enabled
  elseif not self:IsAllowed(player) then
    -- Send message to player
    self:SendMessage(player, self.Messages.NotAllowed:format(self.Settings.RequiredAuthLevel))

    return false
  else
    return true
  end
end

-- -----------------------------------------------------------------------------------
-- IG:PlayerIsConnected(player)
-- -----------------------------------------------------------------------------------
-- Checks if the player is connected
-- -----------------------------------------------------------------------------------
function IG:PlayerIsConnected(player)
  -- Check if player is not null
  if player ~= nil then
    return player:GetComponent("BaseNetworkable").net.connection ~= nil
  else
    return nil
  end
end

-- -----------------------------------------------------------------------------------
-- IG:RestoreAll(param)
-- -----------------------------------------------------------------------------------
-- Restore all players inventories
-- -----------------------------------------------------------------------------------
function IG:RestoreAll(param)
  -- Send message
  self:SendMessage(param, self.Messages.RestoreInit)

  -- Get all players
  local players = UnityEngine.Object.FindObjectsOfTypeAll(global.BasePlayer._type)
  local player = nil
  local playerID = 0

  -- Loop by all players
  for i = 0, (players.Length-1) do
    -- Get current player
    player = players[i]
    
    -- Get PlayerID
    playerID = rust.UserIDFromPlayer(player)
    
    -- Check if player have a valid Player ID
    if playerID ~= "0" then
      -- Check if player have a saved inventory
      if not self:SavedInventoryIsEmpty(player) then
        -- Restore Inventory
        self:RestoreInventory(player)
        -- Send message to player
        self:SendMessage(param, self.Messages.RestoredPlayerInventory:format(player.displayName))
        
        -- Check if player is connected
        if self:PlayerIsConnected(player) then
          -- Send message to Oplayer
          IG:SendMessage(player, self.Messages.PlayerSaved:format("admin"))
        end
      end
    end
    
    -- Add timer
    timer.Once(1, function ()
      -- Check if loop is done
      if (players.Length-1) == i then
        -- Send message
        self:SendMessage(param, self.Messages.RestoreAll)
      end
    end)
  end
end

-- -----------------------------------------------------------------------------------
-- IG:SaveAll(param)
-- -----------------------------------------------------------------------------------
-- Save all players inventories
-- -----------------------------------------------------------------------------------
function IG:SaveAll(param)
  -- Send message
  self:SendMessage(param, self.Messages.SaveInit)

  -- Get all players
  local players = UnityEngine.Object.FindObjectsOfTypeAll(global.BasePlayer._type)
  local player = nil
  local playerID = 0

  -- Loop by all players
  for i = 0, (players.Length-1) do
    -- Get current player
    player = players[i]
    
    -- Get PlayerID
    playerID = rust.UserIDFromPlayer(player)
    
    -- Check if player have a valid Player ID
    if playerID ~= "0" then
      -- Save Inventory
      self:SaveInventory(player)

      -- Send message to console
      self:SendMessage(param, self.Messages.SavedPlayerInventory:format(player.displayName))
      
      -- Check if player is connected
      if self:PlayerIsConnected(player) then
        -- Send message to Oplayer
        self:SendMessage(player, self.Messages.PlayerSaved:format("admin"))
      end
    end
    
    -- Add timer
    timer.Once(1, function ()
      -- Check if loop is done
      if (players.Length-1) == i then
        -- Send message
        self:SendMessage(param, self.Messages.SaveAll)
      end
    end)
  end
end

-- -----------------------------------------------------------------------------------
-- IG:DeleteAll(param)
-- -----------------------------------------------------------------------------------
-- Delete all players inventories
-- -----------------------------------------------------------------------------------
function IG:DeleteAll(param)
  -- Send message
  self:SendMessage(param, self.Messages.DeleteInit)

  -- Get all players
  local players = UnityEngine.Object.FindObjectsOfTypeAll(global.BasePlayer._type)
  local player = nil
  local playerID = 0
  local i = 0

  -- Loop by all players
  for i = 0, (players.Length-1) do
    -- Get current player
    player = players[i]

    -- Get PlayerID
    playerID = rust.UserIDFromPlayer(player)
    -- Check if player have a valid Player ID
    if playerID ~= "0" then
      -- Delete player Inventory
      self:ClearSavedInventory(player)
      -- Send message to player
      self:SendMessage(param, self.Messages.DeletedPlayerInventory:format(player.displayName))
      
      -- Check if player is connected
      if self:PlayerIsConnected(player) then
        -- Send message to Oplayer
        IG:SendMessage(player, self.Messages.PlayerDeleted:format("admin"))
      end
    end

    -- Add timer
    timer.Once(1, function ()
      -- Check if loop is done
      if (players.Length-1) == i then
        -- Send message
        self:SendMessage(param, self.Messages.DeleteAll)
      end
    end)
  end
end

-- -----------------------------------------------------------------------------------
-- IG:StripAll(param)
-- -----------------------------------------------------------------------------------
-- Strip all players inventories
-- -----------------------------------------------------------------------------------
function IG:StripAll(param)
  -- Send message
  self:SendMessage(param, self.Messages.StripInit)

  -- Get all players
  local players = UnityEngine.Object.FindObjectsOfTypeAll(global.BasePlayer._type)
  local player = nil
  local playerID = 0
  local i = 0

  -- Loop by all players
  for i = 0, (players.Length-1) do
    -- Get current player
    player = players[i]

    -- Get PlayerID
    playerID = rust.UserIDFromPlayer(player)
    
    -- Check if player have a valid Player ID
    if playerID ~= "0" then
      -- Clear player Inventory
      player.inventory:Strip()
      
      -- Send message to player
      self:SendMessage(param, self.Messages.PlayerStripedBack:format(player.displayName))
      
      -- Check if player is connected
      if self:PlayerIsConnected(player) then
        -- Send message to Oplayer
        IG:SendMessage(player, self.Messages.PlayerStriped:format("admin"))
      end
    end

    -- Add timer
    timer.Once(1, function ()
      -- Check if loop is done
      if (players.Length-1) == i then
        -- Send message
        self:SendMessage(param, self.Messages.StripAll)
      end
    end)
  end
end

-- -----------------------------------------------------------------------------
-- IG:FindPlayersByName(playerName)
-- -----------------------------------------------------------------------------
-- Searches the online players for a specific name.
-- -----------------------------------------------------------------------------
function IG:FindPlayersByName(playerName)
  -- Check if a player name was supplied.
  if not playerName then return end

  -- Set the player name to lowercase to be able to search case insensitive.
  playerName = string.lower(playerName)

  -- Setup some variables to save the matching BasePlayers with that partial
  -- name.
  local matches = {}
  -- Get all players (Sleeping/Online)
  local PlayerList = UnityEngine.Object.FindObjectsOfTypeAll(global.BasePlayer._type)
  -- Enumarate list
  PlayerList = PlayerList:GetEnumerator()

  -- Iterate through the online player list and check for a match.
  while PlayerList:MoveNext() do
    -- Get the player his/her display name and set it to lowercase.
    local displayName = string.lower(PlayerList.Current.displayName)

    -- Look for a match.
    if string.find(displayName, playerName, 1, true) then
      -- Match found, add the player to the list.
      table.insert(matches, PlayerList.Current)
    end
  end

  -- Return all the matching players.
  return matches
end

-- -----------------------------------------------------------------------------
-- IG:FindPlayerByName(oPlayer, playerName)
-- -----------------------------------------------------------------------------
-- Searches the online players for a specific name.
-- -----------------------------------------------------------------------------
function IG:FindPlayerByName(oPlayer, playerName)
  -- Get a list of matched players
  local players = self:FindPlayersByName(playerName)
  local player = nil

  -- Check if we found the targetted player.
  if self:Count(players) == 0 then
    -- The targetted player couldn't be found, send a message to the player.
    self:SendMessage(oPlayer, self.Messages.PlayerNotFound)

    return player
  end

  -- Check if we found multiple players with that partial name.
  if self:Count(players) > 1 then
    -- Multiple players were found, send a message to the player.
    self:SendMessage(oPlayer, self.Messages.MultiplePlayersFound)

    return player
  else
    -- Only one player was found, modify the targetPlayer variable value.
    player = players[1]
  end

  return player
end

-- -----------------------------------------------------------------------------------
-- IG:AutomaticRestoration()
-- -----------------------------------------------------------------------------------
-- Detect and restore inventories
-- -----------------------------------------------------------------------------------
function IG:AutomaticRestoration()
  -- Check if protocols are detected
  if self.Settings.AutoRestore then
    -- Check if the current Save Protocol is different then the last saved
    if self.SaveProtocol ~= self.Data.SaveProtocol and self.Data.SaveProtocol ~= 0 then
      -- Wipe Restore Once list
      self.Data.RestoreOnce = {}
      -- Send message to console
      self:LogWarning(self.Settings.ChatName..": "..self.Messages.AutoRestoreDetected)
    end
  end

  -- Set data save protocol
  self.Data.SaveProtocol = self.SaveProtocol
  
  -- Save SaveProtocol
  self.ox:SaveData()
end

-- -----------------------------------------------------------------------------------
-- IG:Log(message)
-- -----------------------------------------------------------------------------------
-- Log normal
-- -----------------------------------------------------------------------------------
function IG:Log(message)
  UnityEngine.Debug.Log.methodarray[0]:Invoke(nil, util.TableToArray({message}))
end

-- -----------------------------------------------------------------------------------
-- IG:LogWarning (message)
-- -----------------------------------------------------------------------------------
-- Log Warning
-- -----------------------------------------------------------------------------------
function IG:LogWarning(message)
  UnityEngine.Debug.LogWarning.methodarray[0]:Invoke(nil, util.TableToArray({message}))
end

-- -----------------------------------------------------------------------------------
-- IG:LogError(message)
-- -----------------------------------------------------------------------------------
-- Log Error
-- -----------------------------------------------------------------------------------
function IG:LogError(message)
  UnityEngine.Debug.LogError.methodarray[0]:Invoke(nil, util.TableToArray({message}))
end

-- -----------------------------------------------------------------------------------
-- IG:ClearRestoreOnce(player, param)
-- -----------------------------------------------------------------------------------
-- Clear restore once table
function IG:ClearRestoreOnce(player, param)
  -- Set original player
  local oPlayer = player
  
  -- Check if player want to clear all
  if param == "all" or param == "*" then
    -- Wipe the whole Restore Once table
    self.Data.RestoreOnce = {} 
    -- Send message to player
    self:SendMessage(player, self.Messages.WipeRestoreOnce)
  elseif param ~= "" or param ~= " " then
      -- Find player by Name
      player = self:FindPlayerByName(player, param)
      
      -- Check if a player was found
      if player ~= nil then
        -- Get player ID
        local playerID = rust.UserIDFromPlayer(player)
        -- Delete playerID from Restore Once table
        self.Data.RestoreOnce[playerID] = nil
        -- Send message to player
        self:SendMessage(player, self.Messages.RestoreOnce:format(player.displayName))
      end  
  else
    -- Get player ID
    local playerID = rust.UserIDFromPlayer(player)
    -- Delete playerID from Restore Once table
    self.Data.RestoreOnce[playerID] = nil
    -- Send message to player
    self:SendMessage(player, self.Messages.RestoreOnce:format(player.displayName))
  end
  -- Save data table
  self.ox:SaveData()
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:OnServerInitialized()
-- -----------------------------------------------------------------------------------
-- On server initialisation finished the required in-game chat commands are registered and data
-- from the DataTable file is loaded.
-- -----------------------------------------------------------------------------------
function PLUGIN:OnServerInitialized()
  -- Add the current save protocol
  IG.SaveProtocol = Rust.Protocol.save

  -- Add chat commands
  command.AddChatCommand("ig.save", self.Plugin, "cmdSaveInventory")
  command.AddChatCommand("ig.restore", self.Plugin, "cmdRestoreInventory")
  command.AddChatCommand("ig.restoreupondeath", self.Plugin, "cmdToggleRestoreUponDeath")
  command.AddChatCommand("ig.delsaved", self.Plugin, "cmdDeleteInventory")
  command.AddChatCommand("ig.toggle", self.Plugin, "cmdToggleInventoryGuardian")
  command.AddChatCommand("ig.autorestore", self.Plugin, "cmdToggleAutoRestore")
  command.AddChatCommand("ig.authlevel", self.Plugin, "cmdChangeAuthLevel")
  command.AddChatCommand("ig.strip", self.Plugin, "cmdStripInv")
  command.AddChatCommand("ig.restoreonce", self.Plugin, "cmdClearRestoreOnce")
  command.AddChatCommand("ig.keepcondition", self.Plugin, "cmdToogleKeepCondition")

  -- Add console commands
  command.AddConsoleCommand("ig.authlevel", self.Plugin, "ccmdChangeAuthLevel")
  command.AddConsoleCommand("ig.toggle", self.Plugin, "ccmdToggleInventoryGuardian")
  command.AddConsoleCommand("ig.restoreupondeath", self.Plugin, "ccmdToggleRestoreUponDeath")
  command.AddConsoleCommand("ig.autorestore", self.Plugin, "ccmdToggleAutoRestore")
  command.AddConsoleCommand("ig.restoreall", self.Plugin, "ccmdRestoreAll")
  command.AddConsoleCommand("ig.saveall", self.Plugin, "ccmdSaveAll")
  command.AddConsoleCommand("ig.deleteall", self.Plugin, "ccmdDeleteAll")
  command.AddConsoleCommand("ig.restoreonce", self.Plugin, "ccmdClearRestoreOnce")
  command.AddConsoleCommand("ig.keepcondition", self.Plugin, "ccmdToogleKeepCondition")
  command.AddConsoleCommand("ig.strip", self.Plugin, "ccmdStripInv")
  command.AddConsoleCommand("ig.delsaved", self.Plugin, "ccmdDeleteInventory")
  command.AddConsoleCommand("ig.save", self.Plugin, "ccmdSaveInventory")
  command.AddConsoleCommand("ig.restore", self.Plugin, "ccmdRestoreInventory")
  command.AddConsoleCommand("ig.stripall", self.Plugin, "ccmdStripAll")

  -- Load default saved data
  self:LoadSavedData()

  -- Update config version
  IG:UpdateConfig()

  -- Run automatic restoration
  IG:AutomaticRestoration()
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:LoadDefaultConfig()
-- -----------------------------------------------------------------------------------
-- The plugin uses a configuration file to save certain settings and uses it for
-- localized messages that are send in-game to the players. When this file doesn't
-- exist a new one will be created with these default values.
-- -----------------------------------------------------------------------------------
function PLUGIN:LoadDefaultConfig()
  self.Config.Settings = IG.DefaultSettings
  self.Config.Messages = IG.DefaultMessages
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:OnPlayerDisconnected(player)
-- -----------------------------------------------------------------------------------
-- Run on Player Disconnect
-- -----------------------------------------------------------------------------------
function PLUGIN:OnPlayerDisconnected(player)
  -- Save player inventory
  IG:SaveInventory(player)
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:OnEntityDeath(entity)
-- -----------------------------------------------------------------------------------
-- When an entity dies
-- -----------------------------------------------------------------------------------
function PLUGIN:OnEntityDeath(entity)
  -- Convert entity to player
  local player = entity:ToPlayer()

  -- Check if entity is a player
  if player then
    -- Grab the player his/her SteamID.
    local playerID = rust.UserIDFromPlayer(player)
    
    -- Add playerID to player death list
    IG.PlayerDeaths[playerID] = true

    -- Check if the Restore upon death is enabled
    if self.Config.Settings.RestoreUponDeath then
      -- Save player inventory
      IG:SaveInventory(player)
    else
      -- Reset saved inventory
      IG:ClearSavedInventory(player)
    end
  end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:OnPlayerRespawned(player)
-- -----------------------------------------------------------------------------------
-- When a player respawn
-- -----------------------------------------------------------------------------------
function PLUGIN:OnPlayerRespawned(player)
  -- Grab the player his/her SteamID.
  local playerID = rust.UserIDFromPlayer(player)

  -- Check if saved inventory is empty
  if not IG:SavedInventoryIsEmpty(player) then
    -- Check if Once Restoration is enabled and if player never got once restored or if Once Restoration is disabled or if the Restore upon death is enabled and if player just died or If player never died = First spawn
    if IG.Data.RestoreOnce [playerID] == nil or (self.Config.Settings.RestoreUponDeath and IG.PlayerDeaths[playerID] == true) or IG.PlayerDeaths[playerID] == nil then
      -- Restore player inventory
      IG:RestoreInventory(player)
      -- Send message to user
      IG:SendMessage(player, self.Config.Messages.Restored)
      -- Add Player ID to Once Restorated List
      IG.Data.RestoreOnce [playerID] = true
      -- Reset saved inventory
      timer.Once(3, function() IG:ClearSavedInventory(player) end)
    end
  end

  -- Remove PlayerID from player deaths list
  IG.PlayerDeaths[playerID] = nil
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:SendHelpText(player)
-- -----------------------------------------------------------------------------------
-- HelpText plugin support for the command /help.
-- -----------------------------------------------------------------------------------
function PLUGIN:SendHelpText(player)
  -- Check if user is admin
  if IG:IsAllowed(player) then
    -- Send message to player
    IG:SendMessage(player, self.Config.Messages.Help)
  end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:cmdSaveInventory(player, _, args)
-- -----------------------------------------------------------------------------------
-- Checks if the player is allowed to run and save Inventory
-- -----------------------------------------------------------------------------------
function PLUGIN:cmdSaveInventory(player, _, args)
  -- Make a copy of the player what ran the command
  local oPlayer = player
  local tPlayer = nil

  -- Check if Inventory Guardian is enabled and If player is allowed
  if IG:Check(player) then
    -- Check if any arg was passed
    if args.Length == 1 then
      -- Check if arg is not empty
      if args[0] ~= "" or args[0] ~= " " then
        -- Find a player by name
        tPlayer = IG:FindPlayerByName(oPlayer, args[0])

        -- Check if player is valid
        if tPlayer ~= nil then
          -- Set player as the founded player
          player = tPlayer
        else
          return nil
        end
      end
    end

    -- Save player inventory
    IG:SaveInventory(player)

    -- Check if oPlayer is the same then player
    if player ~= oPlayer then
      -- Send message to oPlayer
      IG:SendMessage(oPlayer, self.Config.Messages.SavedPlayerInventory:format(player.displayName))
      -- Send message to player
      IG:SendMessage(player, self.Config.Messages.PlayerSaved:format(oPlayer.displayName))
    else
      -- Send message to user
      IG:SendMessage(player, self.Config.Messages.Saved)
    end
  end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:cmdRestoreInventory(player, _, args)
-- -----------------------------------------------------------------------------------
-- Checks if the player is allowed to run and restore Inventory
-- -----------------------------------------------------------------------------------
function PLUGIN:cmdRestoreInventory(player, _, args)
  -- Make a copy of the player what ran the command
  local oPlayer = player
  local tPlayer = nil

  -- Check if Inventory Guardian is enabled and If player is allowed
  if IG:Check(player) then
    -- Check if any arg was passed
    if args.Length == 1 then
      -- Check if arg is not empty
      if args[0] ~= "" or args[0] ~= " " then
        -- Find a player by name
        tPlayer = IG:FindPlayerByName(oPlayer, args[0])

        -- Check if player is valid
        if tPlayer ~= nil then
          -- Set player as the founded player
          player = tPlayer
        else
          return nil
        end
      end
    end

    -- Check if saved inventory is empty
    if IG:SavedInventoryIsEmpty (player) then
      -- Send message
      IG:SendMessage(oPlayer, self.Config.Messages.RestoreEmpty)
    else
      -- Restore Inventory
      IG:RestoreInventory(player)
      -- Check if oPlayer is the same then player
      if player ~= oPlayer then
        -- Send message to oPlayer
        IG:SendMessage(oPlayer, self.Config.Messages.RestoredPlayerInventory:format(player.displayName))
        -- Send message to player
        IG:SendMessage(player, self.Config.Messages.PlayerRestored:format(oPlayer.displayName))
      else      
        -- Send message to user
        IG:SendMessage(player, self.Config.Messages.Restored)
      end
    end
  end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:cmdDeleteInventory(player, _, args)
-- -----------------------------------------------------------------------------------
-- Checks if the player is allowed to run and delete Inventory
-- -----------------------------------------------------------------------------------
function PLUGIN:cmdDeleteInventory(player, _, args)
  -- Make a copy of the player what ran the command
  local oPlayer = player
  local tPlayer = nil

  -- Check if Inventory Guardian is enabled and If player is allowed
  if IG:Check(player) then
    -- Check if any arg was passed
    if args.Length == 1 then
      -- Check if arg is not empty
      if args[0] ~= "" or args[0] ~= " " then
        -- Find a player by name
        tPlayer = IG:FindPlayerByName(oPlayer, args[0])

        -- Check if player is valid
        if tPlayer ~= nil then
          -- Set player as the founded player
          player = tPlayer
        else
          return nil
        end
      end
    end

    -- Restore player Inventory
    IG:ClearSavedInventory(player)

    -- Check if oPlayer is the same then player
    if player ~= oPlayer then
      -- Send message to oPlayer
      IG:SendMessage(oPlayer, self.Config.Messages.DeletedPlayerInventory:format(player.displayName))
      -- Send message to player
      IG:SendMessage(player, self.Config.Messages.PlayerDeleted:format(oPlayer.displayName))
    else
      -- Send message to user
      IG:SendMessage(player, self.Config.Messages.DeletedInv)
    end
  end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:cmdToggleInventoryGuardian(player)
-- -----------------------------------------------------------------------------------
-- Enable/Disable Inventory Guardian
-- -----------------------------------------------------------------------------------
function PLUGIN:cmdToggleInventoryGuardian(player)
  -- Check if Inventory Guardian is enabled and If player is allowed
  if IG:IsAllowed(player) then
    -- Restore Player inventory
    IG:ToggleInventoryGuardian(player)
  else
    -- Send message to player
    IG:SendMessage(player, self.Config.Messages.NotAllowed:format(self.Config.Settings.RequiredAuthLevel))
  end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:cmdToggleRestoreOnce(player)
-- -----------------------------------------------------------------------------------
-- Enable/Disable Automatic restoration
-- -----------------------------------------------------------------------------------
function PLUGIN:cmdToggleAutoRestore(player)
  -- Check if Inventory Guardian is enabled and If player is allowed
  if IG:Check(player) then
    -- Toggle Automatic Restoration
    IG:ToggleAutoRestore(player)
  end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:cmdChangeAuthLevel(player, _, args)
-- -----------------------------------------------------------------------------------
-- Change required Auth Level
-- -----------------------------------------------------------------------------------
function PLUGIN:cmdChangeAuthLevel(player, _, args)
  -- Check if Inventory Guardian is enabled
  if IG:Check(player) then
    -- Check for passed args
    if args.Length == 1 then
      -- Change required Auth level
      IG:ChangeAuthLevel(player, args[0])
    end
  end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:cmdToggleRestoreUponDeath(player)
-- -----------------------------------------------------------------------------------
-- Enable/Disable Restoration upon death
-- -----------------------------------------------------------------------------------
function PLUGIN:cmdToggleRestoreUponDeath(player)
  -- Check if Inventory Guardian is enabled
  if IG:Check(player) then
    -- Toggle restore upon death
    IG:ToggleRestoreUponDeath(player)
  end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:cmdToogleKeepCondition(player)
-- -----------------------------------------------------------------------------------
-- Enable/Disable Keep Items Condition
-- -----------------------------------------------------------------------------------
function PLUGIN:cmdToogleKeepCondition(player)
  -- Check if Inventory Guardian is enabled
  if IG:Check(player) then
    -- Toggle keep items condition
    IG:ToogleKeepCondition(player)
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
    IG:ChangeAuthLevel(arg, arg.Args[0])
  end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:ccmdToggleInventoryGuardian()
-- -----------------------------------------------------------------------------------
-- Enable/Disable Inventory Guardian
-- -----------------------------------------------------------------------------------
function PLUGIN:ccmdToggleInventoryGuardian(arg)
  -- Restore Player inventory
  IG:ToggleInventoryGuardian(arg)
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:ccmdToogleKeepCondition(arg)
-- -----------------------------------------------------------------------------------
-- Enable/Disable Keep Items Condition
-- -----------------------------------------------------------------------------------
function PLUGIN:ccmdToogleKeepCondition(arg)
  -- Toggle keep items condition
  IG:ToogleKeepCondition(arg)
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:ccmdToggleRestoreOnce(arg)
-- -----------------------------------------------------------------------------------
-- Enable/Disable Automatic restoration
-- -----------------------------------------------------------------------------------
function PLUGIN:ccmdToggleAutoRestore(arg)
  -- Toggle automatic restoration
  IG:ToggleAutoRestore(arg)
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:ccmdToggleRestoreUponDeath(arg)
-- -----------------------------------------------------------------------------------
-- Enable/Disable Restoration upon death
-- -----------------------------------------------------------------------------------
function PLUGIN:ccmdToggleRestoreUponDeath(arg)
  -- Toggle restore upon death
  IG:ToggleRestoreUponDeath(arg)
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:ccmdRestoreAll()
-- -----------------------------------------------------------------------------------
-- Restore All players inventories
-- -----------------------------------------------------------------------------------
function PLUGIN:ccmdRestoreAll(arg)
  -- Restore all players inventories
  IG:RestoreAll(arg)
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:ccmdSaveAll()
-- -----------------------------------------------------------------------------------
-- Save All players inventories
-- -----------------------------------------------------------------------------------
function PLUGIN:ccmdSaveAll(arg)
  -- Save all players inventories
  IG:SaveAll(arg)
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:ccmdDeleteAll()
-- -----------------------------------------------------------------------------------
-- Delete All players inventories
-- -----------------------------------------------------------------------------------
function PLUGIN:ccmdDeleteAll(arg)
  -- Delete all players inventories
  IG:DeleteAll(arg)
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:ccmdStripAll()
-- -----------------------------------------------------------------------------------
-- Delete All players inventories
-- -----------------------------------------------------------------------------------
function PLUGIN:ccmdStripAll(arg)
  -- Strip all players inventories
  IG:StripAll(arg)
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:cmdStripInv(player, _, args)
-- -----------------------------------------------------------------------------------
-- Strip player inventory
-- ----------------------------------------------------------------------------------
function PLUGIN:cmdStripInv(player, _, args)
  -- Copy origin player
  local oPlayer = player
  local tPlayer = nil

    -- Check if any arg was passed
    if args.Length == 1 then
      -- Check if arg is not empty
      if args[0] ~= "" or args[0] ~= " " then
        -- Find a player by name
        tPlayer = IG:FindPlayerByName(oPlayer, args[0])
    
        -- Check if player is valid
        if tPlayer ~= nil then
          -- Set player as the founded player
          player = tPlayer
        else
          return nil
        end
    end
  end

  -- Check if player is valid
  if player ~= nil then
    -- Clear player Inventory
    player.inventory:Strip()
  end

  -- Check if player is not oPlayer
  if player ~= oPlayer then
    -- Send message to target player
    IG:SendMessage(tPlayer, self.Config.Messages.PlayerStriped:format(oPlayer.displayName))
    -- Send message back to oPlayer
    IG:SendMessage(oPlayer, self.Config.Messages.PlayerStripedBack:format(player.displayName))
  else
    -- Send message to oPlayer
    IG:SendMessage(oPlayer, self.Config.Messages.SelfStriped)
  end
  
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:ccmdStripInv(arg)
-- -----------------------------------------------------------------------------------
-- Strip player inventory
-- ----------------------------------------------------------------------------------
function PLUGIN:ccmdStripInv(arg)
    -- Define player
    local player = nil
    -- Check if any arg was passed
    if arg:HasArgs(1) then
      -- Check if arg is not empty
      if arg.Args[0] ~= "" or arg.Args[0] ~= " " then
        -- Find a player by name
        player = IG:FindPlayerByName(arg, arg.Args[0])
    end
  end

  -- Check if player is valid
  if player ~= nil then
    -- Clear player Inventory
    player.inventory:Strip()
          
    -- Send message back to console
    IG:SendMessage(arg, self.Config.Messages.PlayerStripedBack:format(player.displayName))    
    -- Send message 
    IG:SendMessage(player, self.Config.Messages.PlayerStriped:format("admin"))
  end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:cmdDeleteInventory(arg)
-- -----------------------------------------------------------------------------------
-- Checks if the player is allowed to run and delete Inventory
-- -----------------------------------------------------------------------------------
function PLUGIN:ccmdDeleteInventory(arg)
  -- Define player
  local player = nil
  
  -- Check if any arg was passed
  if arg:HasArgs(1) then
    -- Find a player by name
    player = IG:FindPlayerByName(arg, arg.Args[0])
  end

  -- Check if is valid
  if player ~= nil then
    -- Restore player Inventory
    IG:ClearSavedInventory(player)
    
    -- Send message back to player
    IG:SendMessage(arg, self.Config.Messages.DeletedPlayerInventory:format(player.displayName))
    -- Send message
    IG:SendMessage(player, self.Config.Messages.PlayerDeleted:format("admin"))
  end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:ccmdSaveInventory(arg)
-- -----------------------------------------------------------------------------------
-- Checks if the player is allowed to run and save Inventory
-- -----------------------------------------------------------------------------------
function PLUGIN:ccmdSaveInventory(arg)
  -- Make a copy of the player what ran the command
  local player = nil

  -- Check if any arg was passed
  if arg:HasArgs(1) then
    -- Find a player by name
    player = IG:FindPlayerByName(arg, arg.Args[0])
  end

  -- Check if player is valid
  if player ~= nil then
    -- Save player inventory
    IG:SaveInventory(player)
    
    -- Send message to oPlayer
    IG:SendMessage(arg, self.Config.Messages.SavedPlayerInventory:format(player.displayName))
    -- Send message to player
    IG:SendMessage(player, self.Config.Messages.PlayerSaved:format("admin"))
  end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:ccmdRestoreInventory(arg)
-- -----------------------------------------------------------------------------------
-- Checks if the player is allowed to run and restore Inventory
-- -----------------------------------------------------------------------------------
function PLUGIN:ccmdRestoreInventory(arg)
  -- Define player
  local player = nil
  -- Check if any arg was passed
  if arg:HasArgs(1) then
    -- Find a player by name
    player = IG:FindPlayerByName(arg, arg.Args[0])
  end

  -- Check if is valid
  if player ~= nil then
    -- Check if saved inventory is empty
    if IG:SavedInventoryIsEmpty (player) then
      -- Send message
      IG:SendMessage(arg, self.Config.Messages.PlayerRestoreEmpty:format(player.displayName))
    else    
      -- Restore Inventory
      IG:RestoreInventory(player)
  
      -- Send message to oPlayer
      IG:SendMessage(arg, self.Config.Messages.RestoredPlayerInventory:format(player.displayName))
      -- Send message to player
      IG:SendMessage(player, self.Config.Messages.PlayerRestored:format("admin"))
    end  
  end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:SaveData()
-- -----------------------------------------------------------------------------------
-- Saves the table with all the warpdata to a DataTable file.
-- -----------------------------------------------------------------------------------
function PLUGIN:SaveData()
  -- Save the DataTable
  datafile.SaveDataTable("Inventory-Guardian")
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:LoadSavedData()
-- -----------------------------------------------------------------------------------
-- Load the DataTable file into a table or create a new table when the file doesn't
-- exist yet.
-- -----------------------------------------------------------------------------------
function PLUGIN:LoadSavedData()
  IG.Data = datafile.GetDataTable("Inventory-Guardian")
  IG.Data = IG.Data or {}
  IG.Data.GlobalInventory = IG.Data.GlobalInventory or {}
  IG.Data.RestoreOnce = IG.Data.RestoreOnce or {}
  IG.Data.SaveProtocol = IG.Data.SaveProtocol or IG.SaveProtocol
end
