--[[ 
 Warp System
 
 Copyright (c) 2015 Nexus <talk@juliocesar.me>, <http://steamcommunity.com/profiles/76561197983103320/>
 
 -------------------------------------------------------------------------------------------------------------------
 This work is licensed under the Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.
 To view a copy of this license, visit http://creativecommons.org/licenses/by-nc-sa/4.0/deed.en_US.
 -----------------------------------------------------------------------------------------------------------------
 
 $Id$
 Version 0.0.9 by Nexus on 2015-03-01 10:30 AM (UTC -03:00)
]]--

PLUGIN.Name = "warp-system"
PLUGIN.Title = "Warp System"
PLUGIN.Description = "Create teleport points with a custom command"
PLUGIN.Version = V(0, 0, 9)
PLUGIN.Author = "Nexus"
PLUGIN.HasConfig = true
PLUGIN.ResourceId = 760

-- Define Warp System Class
local Warp = {}

Warp.Data = {}
Warp.PreviousLocation = {}
Warp.Timers = {}
Warp.ConfigVersion = "0.0.5"
Warp.ox = PLUGIN

-- Define Settings
Warp.Settings = {}

-- Define Messages
Warp.Messages = {}

-- General Settings:
Warp.DefaultSettings = {
  ChatName = "Warp",
  ConfigVersion = "0.0.5",
  Enabled = true,
  RequiredAuthLevel = 2,
  EnableCooldown = true,
  EnableDailyLimit = true,
  EnableDailyLimitForAdmin = false,
  EnableCoolDownForAdmin = false,
  Cooldown = 60,
  Countdown = 15,
  DailyLimit = 10,
}

-- Plugin Messages:
Warp.DefaultMessages = {
  -- Warp System:
  Remove = "You have removed the warp %s!",
  List = "The following warps are available:",
  Warped = "You Warped to %s!",
  ListEmpty = "There is no warps available!",
  Back = "You have warped back to your previous location!",
  BackSave = "Your previous location has been saved before you warped, use /warp back to teleport back!",
  Save = "You have saved the %s warp as %d, %d, %d!",
  Delete = "You have deleted the %s warp!",
  Ren = 'You have renamed the warp %s to %s!',
  AuthNeeded = 'You don\'t have the right Auth Level to use "%s!"',
  Exists = 'The warp %s already exists!',
  Cooldown = "You need wait %s to use it again.",
  LimitReached = "You have reached the daily limit of %d, You need wait until tomorrow to warp again!",
  Interrupted = "You was interrupted, Before Warp!",
  Pending = "You cannot use a Warp now, Because you are still waiting to get Warped!",
  Started = "Your request is on the wait list, It will start in %d secs.",
  NoPreviousLocationSaved  = "No previous location saved!",
  AuthLog = " %s by %s (%s)",

  -- Error Messages:
  NotFound = "Couldn't find the %s warp !",

  -- Help to admin:
  HelpAdmin = {
    "As an admin you have access to the following commands:",
    "/warp add <name> - Create a new warp at your current location.",
    "/warp add <name> <x> <y> <z> - Create a new warp to the set of coordinates.",
    "/warp del <name> - Delete a warp.",
    "/warp go <name> - Goto a warp.",
    "/warp back - Teleport you back to the location that you was before warp.",
    "/warp list - List all saved warps.",
    "/warp limits - List warp limits"
  },

  -- Help to user
  HelpUser = {
    "As an user you have access to the following commands:",
    "/warp go <name> - Goto a warp.",
    "/warp back - Teleport you back to the location that you was before warp.",
    "/warp list - List all saved warps.",
    "/warp limits - List warp limits."
  },
  
  -- Limits
  Limits = {
    "Warp System as the current settings enabled: ",
    "Time between teleports: %s",
    "Daily amount of teleports: %d"
  },

  -- Syntax Errors Warp System:
  SyntaxCommand = "A Syntax Error Occurred!"
}

-- -----------------------------------------------------------------------------------
-- Warp:UpdateConfig()
-- -----------------------------------------------------------------------------------
-- It check if the config version is outdated
-- -----------------------------------------------------------------------------------
function Warp:UpdateConfig()
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
-- Warp:IsAllowed(player)
-- -----------------------------------------------------------------------------------
-- Checks if the player is allowed to run an admin (or moderator) only command.
-- -----------------------------------------------------------------------------------
function Warp:IsAllowed(player)
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

-- -----------------------------------------------------------------------------
-- Warp:Add(player, name, x, y, z)
-- -----------------------------------------------------------------------------
-- Add a new warp.
-- -----------------------------------------------------------------------------
function Warp:Add(player, name, x, y, z)
  -- Get current location
  local loc = player.transform.position

  -- Check if was sent any loc
  if x ~= 0 and y ~= 0 and z ~= 0 then
    -- Save new location
    local loc = {}
    -- Set new loc
    loc.x = math.floor(x)
    loc.y = math.floor(y)
    loc.z = math.floor(z)
  end

  -- Check if the player is allowed to run the command.
  if self:IsAllowed(player) then
    -- Check if Warp already exists
    if Warp.Data.WarpPoints[name] == nil then
      -- Add Warp at the the position
      Warp.Data.WarpPoints[name] = {x = loc.x, y = loc.y, z = loc.z}

      -- Save data
      self.ox:SaveData()

      -- Send message to player
      self:SendMessage(player, self.Messages.Save:format(name, loc.x, loc.y, loc.z) )
    else
      -- Send message to player
      self:SendMessage(player, self.Messages.Exists:format(name))
    end
  else
    -- Send message to player
    self:SendMessage(player, self.Messages.AuthNeeded:format('/warp add'))
  end
end

-- -----------------------------------------------------------------------------
-- Warp:Del(player, name)
-- -----------------------------------------------------------------------------
-- Delete a warp.
-- -----------------------------------------------------------------------------
function Warp:Del(player, name)
  -- Check if the player is allowed to run the command.
  if self:IsAllowed(player) then
    -- Check if Warp exists
    if Warp.Data.WarpPoints[name] ~= nil then
      -- Delete warp
      Warp.Data.WarpPoints[name] = nil

      -- Save data
      self.ox:SaveData()
      -- Send message to player
      self:SendMessage(player, self.Messages.Delete:format(name))
    else
      -- Send message to player
      self:SendMessage(player, self.Messages.NotFound:format(name))
    end
  else
    -- Send message to player
    self:SendMessage(player, self.Messages.AuthNeeded:format('/warp del'))
  end
end

-- -----------------------------------------------------------------------------
-- Warp:Ren(player, oldname, newname)
-- -----------------------------------------------------------------------------
-- Rename a warp.
-- -----------------------------------------------------------------------------
function Warp:Ren(player, oldname, newname)
  -- Check if the player is allowed to run the command.
  if self:IsAllowed(player) then
    -- Check if Warp exists
    if Warp.Data.WarpPoints[oldname] ~= nil then
      -- Check if Warp new exists
      if Warp.Data.WarpPoints[newname] == nil then
        -- Create a new warp
        Warp.Data.WarpPoints[newname] = Warp.Data.WarpPoints[oldname]
        -- Delete warp
        Warp.Data.WarpPoints[oldname] = nil

        -- Save data
        self.ox:SaveData()
        -- Send message to player
        self:SendMessage(player, self.Messages.Ren:format(newname, oldname))
      else
        -- Send message to player
        self:SendMessage( player, self.Messages.Exists:format(newname))
      end
    else
      -- Send message to player
      self:SendMessage(player, self.Messages.WarpNotFound:format(oldname))
    end
  else
    -- Send message to player
    self:SendMessage( player, self.Messages.AuthNeeded:format('/warp ren'))
  end
end

-- -----------------------------------------------------------------------------
-- Warp:Use(player, name)
-- -----------------------------------------------------------------------------
-- Use a Warp to teleport player to a location.
-- -----------------------------------------------------------------------------
function Warp:Use(player, name)
  -- Get PlayerID
  local playerID = rust.UserIDFromPlayer(player)
  
  -- Check if Warp exists
  if Warp.Data.WarpPoints[name] ~= nil then        
    -- Save current position
    Warp.PreviousLocation[playerID] = {x = player.transform.position.x, y = player.transform.position.y, z = player.transform.position.z}
    
    -- Teleport Player to Location
    self:Start(player, Warp.Data.WarpPoints[name].x, Warp.Data.WarpPoints[name].y, Warp.Data.WarpPoints[name].z, self.Messages.Warped:format(name), true)
  else
    -- Send message to player
    self:SendMessage(player, self.Messages.NotFound:format(name))
  end
end

-- -----------------------------------------------------------------------------
-- Warp:Back(player)
-- -----------------------------------------------------------------------------
-- Go back to a point where the player was
-- -----------------------------------------------------------------------------
function Warp:Back(player)
  -- Get PlayerID
  local playerID = rust.UserIDFromPlayer(player)
  -- Check if player already used the Warp
  if Warp.PreviousLocation[playerID] ~= nil then
    -- Teleport Player to Location
    self:Start(player, Warp.PreviousLocation[playerID].x, Warp.PreviousLocation[playerID].y, Warp.PreviousLocation[playerID].z, self.Messages.Back, false)
  else
    -- Send message to player
    self:SendMessage(player, self.Messages.NoPreviousLocationSaved)      
  end
end

-- -----------------------------------------------------------------------------
-- Warp:List(player)
-- -----------------------------------------------------------------------------
-- List all the saved warps
-- -----------------------------------------------------------------------------
function Warp:List(player)
  -- Count the Warp Points
  if self:Count(Warp.Data.WarpPoints) >= 1 then
    -- Send message to player
    self:SendMessage(player, self.Messages.List)

    -- Loop through all the saved locations and print them one by one.
    for location, coordinates in pairs(Warp.Data.WarpPoints) do
      self:SendMessage(player, location..": "..math.floor(coordinates.x).." "..math.floor(coordinates.y).." "..math.floor(coordinates.z))
    end
  else
    -- Send message to player
    self:SendMessage(player, self.Messages.ListEmpty)
  end
end

-- -----------------------------------------------------------------------------
-- Warp:Count(tbl)
-- -----------------------------------------------------------------------------
-- Counts the elements of a table.
-- -----------------------------------------------------------------------------
-- Credit: m-Teleportation
function Warp:Count(tbl)
  local count = 0

  if type(tbl) == "table" then
    for _ in pairs(tbl) do
      count = count + 1
    end
  end

  return count
end

-- -----------------------------------------------------------------------------
-- Warp:Go(player, destination)
-- -----------------------------------------------------------------------------
-- Teleports a player to a specific location.
-- -----------------------------------------------------------------------------
-- Credit: m-Teleportation
function Warp:Go(player, destination)
  -- Let the player sleep to prevent the player from falling through objects.
  player:StartSleeping()

  -- Change the player's position.
  rust.ForcePlayerPosition(player, destination.x, destination.y, destination.z)
  
  -- Set the player flag to receiving snapshots and update the player.
  player:SetPlayerFlag(global["BasePlayer+PlayerFlags"].ReceivingSnapshot, true)
  player:UpdateNetworkGroup()
  player:SendFullSnapshot()
end

-- -----------------------------------------------------------------------------
-- Warp:SendMessage(param, message)
-- -----------------------------------------------------------------------------
-- Sends a chatmessage to a player/console
-- -----------------------------------------------------------------------------
function Warp:SendMessage(param, message)
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

-- ----------------------------------------------------------------------------
-- Warp:ParseRemainingTime( time )
-- ----------------------------------------------------------------------------
-- Returns an amount of seconds as a nice time string.
-- ----------------------------------------------------------------------------
-- Credit: m-Teleportation
function Warp:ParseRemainingTime( time )
    local minutes = nil
    local seconds = nil
    local timeLeft = nil

    -- If the amount of seconds is higher than 60 we'll have minutes too, so
    -- start with grabbing the amount of minutes and then take the remainder as
    -- the seconds that are left on the timer.
    if time >= 60 then
        minutes = math.floor(time/60)
        seconds = time - (minutes*60)
    else
        seconds = time
    end

    -- Build a nice string with the remaining time.
    if minutes and seconds > 0 then
        timeLeft = minutes .. " min"..((minutes > 1) and "s" or "").." " .. seconds .. " sec"..((seconds > 1) and "s" or "")
    elseif minutes and seconds == 0 then
        timeLeft = minutes .. " min"..((minutes > 1) and "s" or "")
    else    
        timeLeft = seconds .. " sec"..((seconds > 1) and "s" or "")
    end

    -- Return the time string.
    return timeLeft        
end

-- -----------------------------------------------------------------------------
-- PLUGIN:Start(player, x, y, z, sendBackSaveMSG)
-- -----------------------------------------------------------------------------
-- Teleports a player to a set of coordinates.
-- -----------------------------------------------------------------------------
-- Credit: m-Teleportation
function Warp:Start(player, x, y, z, doneMessage, sendBackSaveMSG)
  -- Get playerID          
  local playerID = rust.UserIDFromPlayer(player)        

  -- Setup variables with todays date and the current timestamp.
  local timestamp = time.GetUnixTimestamp()
  local currentDate = tostring(time.GetCurrentTime():ToString("d"))

  -- Check if there is saved teleport data available for the
  -- player.
  if Warp.Data.Usage[playerID] then
    if Warp.Data.Usage[playerID].date ~= currentDate then
        Warp.Data.Usage[playerID] = nil
    end
  end

  -- Grab the user his/her teleport data.
  Warp.Data.Usage[playerID] = Warp.Data.Usage[playerID] or {}
  Warp.Data.Usage[playerID].amount = Warp.Data.Usage[playerID].amount or 0
  Warp.Data.Usage[playerID].date = currentDate
  Warp.Data.Usage[playerID].timestamp = Warp.Data.Usage[playerID].timestamp or 0

  -- Check if the cooldown option is enabled and if it is make
  -- sure that the cooldown time has passed.
  if self.Settings.EnableCooldown and (timestamp-Warp.Data.Usage[playerID].timestamp) < self.Settings.Cooldown and (not self:IsAllowed(player) and not self.Settings.EnableCooldownForAdmin) then
    -- Get the remaining time.
    local remainingTime = self:ParseRemainingTime(self.Settings.Cooldown-(timestamp-Warp.Data.Usage[playerID].timestamp))
    -- Teleport is on cooldown, show a message to the player.
    self:SendMessage(player, self.Messages.Cooldown:format(remainingTime))
  
    return
  end
  
  -- Check if the teleports daily limit is enabled and make sure
  -- that the player has not yet reached the limit.
  if self.Settings.EnableDailyLimit and Warp.Data.Usage[playerID].amount >= self.Settings.DailyLimit and (not self:IsAllowed(player) and not self.Settings.EnableDailyLimitForAdmin) then
    -- The player has reached the limit, show a message to the
    -- player.
    self:SendMessage(player, self.Messages.LimitReached:format(self.Settings.DailyLimit))
  
    return
  end

  -- Check if the player already has a teleport pending.
  if Warp.Timers[playerID] then
    -- Send a message to the player.
    self:SendMessage(player, self.Messages.Pending)

    return
  end
  
  -- no limits were reached so we ca
  -- teleport the player after a short delay.
  Warp.Timers[playerID] = timer.Once(self.Settings.Countdown, function()
    -- set the destination for the player.
    local destination = new(UnityEngine.Vector3._type, nil)
    destination.x = x
    destination.y = y
    destination.z = z

    -- Teleport the player to the destination.
    self:Go(player, destination)
    
    -- Modify the teleport amount and last teleport
    -- timestamp.
    Warp.Data.Usage[playerID].amount = Warp.Data.Usage[playerID].amount + 1
    Warp.Data.Usage[playerID].timestamp = timestamp
    -- Save data
    self.ox:SaveData()
    
    -- Show a message to the player.
    self:SendMessage(player, doneMessage)
    
    -- Check if we need send a "Back" message
    if sendBackSaveMSG then
      -- Send message to player
      self:SendMessage(player, self.Messages.BackSave)
    else
      -- Remove previous location 
      Warp.PreviousLocation[playerID] = nil
    end    
    
    -- Remove the pending timer info.
    Warp.Timers[playerID] = nil
    
    -- Update time
    timestamp = time.GetUnixTimestamp()
    currentDate = tostring(time.GetCurrentTime():ToString("d"))
    
    -- Update timer
    Warp.Data.Usage[playerID].date = currentDate
    Warp.Data.Usage[playerID].timestamp = Warp.Data.Usage[playerID].timestamp or 0
  end)
  
  -- Send message to player
  self:SendMessage(player, self.Messages.Started:format(self.Settings.Countdown))
end

-- -----------------------------------------------------------------------------------
-- Warp:Log(message)
-- -----------------------------------------------------------------------------------
-- Log normal
-- -----------------------------------------------------------------------------------
-- Credit: HooksTest
-- -----------------------------------------------------------------------------------
function Warp:Log(message)
  UnityEngine.Debug.Log.methodarray[0]:Invoke(nil, util.TableToArray({message}))
end

-- -----------------------------------------------------------------------------------
-- Warp:LogWarning(message)
-- -----------------------------------------------------------------------------------
-- Log Warning
-- -----------------------------------------------------------------------------------
-- Credit: HooksTest
-- -----------------------------------------------------------------------------------
function Warp:LogWarning(message)
  UnityEngine.Debug.LogWarning.methodarray[0]:Invoke(nil, util.TableToArray({message}))
end

-- -----------------------------------------------------------------------------------
-- Warp:LogError(message)
-- -----------------------------------------------------------------------------------
-- Log Error
-- -----------------------------------------------------------------------------------
-- Credit: HooksTest
-- -----------------------------------------------------------------------------------
function Warp:LogError(message)
  UnityEngine.Debug.LogError.methodarray[0]:Invoke(nil, util.TableToArray({message}))
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:Init()
-- -----------------------------------------------------------------------------------
-- On plugin initialisation the required in-game chat commands are registered and data
-- from the DataTable file is loaded.
-- -----------------------------------------------------------------------------------
function PLUGIN:Init ()
  -- Load default saved data
  self:LoadSavedData()

  -- Update config version
  Warp:UpdateConfig()
  
  -- Add chat command
  command.AddChatCommand("warp", self.Plugin, "cmdWarp")
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:LoadSavedData()
-- -----------------------------------------------------------------------------------
-- Load the DataTable file into a table or create a new table when the file doesn't
-- exist yet.
-- -----------------------------------------------------------------------------------
function PLUGIN:LoadSavedData()
  Warp.Data = datafile.GetDataTable("warp-system")
  Warp.Data = Warp.Data or {}
  Warp.Data.WarpPoints =  Warp.Data.WarpPoints or {}
  Warp.Data.Usage = Warp.Data.Usage or {}
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:SaveData()
-- -----------------------------------------------------------------------------------
-- Saves the table with all the warpdata to a DataTable file.
-- -----------------------------------------------------------------------------------
function PLUGIN:SaveData()
  -- Save the DataTable
  datafile.SaveDataTable("warp-system")
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:LoadDefaultConfig()
-- -----------------------------------------------------------------------------------
-- The plugin uses a configuration file to save certain settings and uses it for
-- localized messages that are send in-game to the players. When this file doesn't
-- exist a new one will be created with these default values.
-- -----------------------------------------------------------------------------------
function PLUGIN:LoadDefaultConfig()
  self.Config.Settings = Warp.DefaultSettings
  self.Config.Messages = Warp.DefaultMessages
end

-- -----------------------------------------------------------------------------
-- PLUGIN:OnRunCommand(args)
-- -----------------------------------------------------------------------------
-- Triggerd when any player send a chat message.
-- -----------------------------------------------------------------------------
function PLUGIN:OnRunCommand(arg)
  if not arg.connection then return end
  if not arg.cmd then return end
  local cmd = arg.cmd.namefull
  local chat = arg:GetString(0, "text")
  local player = arg.connection.player

  if cmd == "chat.say" and string.sub(chat, 1, 1) == "/" then
    -- Loop through all the saved locations and print them one by one.
    for location, _ in pairs(Warp.Data.WarpPoints) do
      -- Check for a Warp Location
      if chat == '/'..location then
        -- Use Warp
        Warp:Use(player, location)
      end
    end
  end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:cmdWarp(player, cmd, args)
-- -----------------------------------------------------------------------------------
-- In-game '/warp' command for server admins to be able to manage warps.
-- -----------------------------------------------------------------------------------
function PLUGIN:cmdWarp(player, _, args)
  -- Check if the Warp System is enabled.
  if not self.Config.Settings.Enabled then return end

  -- Setup default vars
  local cmd = ''
  local param = ''
  local x = 0
  local y = 0
  local z = 0

  -- Check and setup args
  if args.Length == 1 then
    cmd = args[0]
  elseif args.Length == 2 then
    cmd = args[0]
    param = args[1]
  elseif args.Length == 5 then
    cmd = args[0]
    param = args[1]
    x = args[2]
    y = args[3]
    z = args[4]
  end

  -- Check if the command is to add a new warp
  if cmd == 'add' then
    -- Check if the warp is at a current location
    if args.Length >= 2 then
      -- Test for empty strings
      if param ~= '' or param ~= ' ' then
        -- Add a new warp
        Warp:Add(player, param, x, y, z)
      end
    else
      -- Send message to player
      Warp:SendMessage(player, self.Config.Messages.SyntaxCommand)
      -- List all commands
      self:SendHelpText(player)
    end
    -- Check if the command is to delete a warp
  elseif cmd == 'del' then
    -- Check if param is valid
    if param ~= '' and param ~= ' ' then
      -- Delete a warp
      Warp:Del(player, param)
    else
      -- Send message to player
      Warp:SendMessage(player, self.Config.Messages.SyntaxCommand)
      -- List all commands
      self:SendHelpText(player)
    end
    -- Check if the command is to use a warp
  elseif cmd == 'go' then
    -- Check if param is valid
    if param ~= '' and param ~= ' ' then
      -- Use a Warp
      Warp:Use(player, param)
    else
      -- Send message to player
      Warp:SendMessage(player, self.Config.Messages.SyntaxCommand)
      -- List all commands
      self:SendHelpText(player)
    end
    -- Check if the command is to go back before warp
  elseif cmd == 'back' then
    -- Go Back to the Previous location to Warp
    Warp:Back(player)
  -- Check if the command is to list warps
  elseif cmd == 'list' then
    -- List Warps
    Warp:List(player)
  -- Check if the command is to list limits
  elseif cmd == 'limits' then      
      -- Send messages to player
      Warp:SendMessage(player, self.Config.Messages.Limits[1])
      Warp:SendMessage(player, self.Config.Messages.Limits[2]:format(Warp:ParseRemainingTime(self.Config.Settings.Cooldown)))
      Warp:SendMessage(player, self.Config.Messages.Limits[3]:format(self.Config.Settings.DailyLimit))  
  else
    -- Send message to player
    Warp:SendMessage(player, 'Command '..cmd..' is not valid!' )    
    -- List all commands
    self:SendHelpText(player)
  end
end

-- -----------------------------------------------------------------------------------
-- PLUGIN:SendHelpText(player)
-- -----------------------------------------------------------------------------------
-- HelpText plugin support for the command /help.
-- -----------------------------------------------------------------------------------
function PLUGIN:SendHelpText(player)
  -- Check if player is allowed
  if Warp:IsAllowed(player) then
    -- Send message to player
    Warp:SendMessage(player, self.Config.Messages.HelpAdmin)
  else
    -- Send message to player
    Warp:SendMessage(player, self.Config.Messages.HelpUser)
  end
end

-- ----------------------------------------------------------------------------
-- PLUGIN:OnEntityAttacked(entity, hitinfo)
-- ----------------------------------------------------------------------------
-- OnEntityAttacked Oxide Hook. This hook is triggered when an entity
-- (BasePlayer or BaseNPC) is attacked. This hook is used to interrupt
-- a teleport when a player takes damage.
-- ----------------------------------------------------------------------------
-- Credit: m-Teleportation
function PLUGIN:OnEntityAttacked(entity, hitinfo)
    -- Check if the entity taking damage is a player.
    if entity:ToPlayer() then
        -- The entity taking damage is a player, grab his/her Steam ID.
        local playerID = rust.UserIDFromPlayer( entity )

        -- Check if the player has a teleport pending.
        if Warp.Timers[playerID] ~= nil then
            -- Send a message to the players or to both players.
            Warp:SendMessage(entity, self.Config.Messages.Interrupted)

            -- Destroy the timer.
            Warp.Timers[playerID]:Destroy()

            -- Remove the table entry.
            Warp.Timers[playerID] = nil
        end

    end
end

-- ----------------------------------------------------------------------------
-- PLUGIN:OnPlayerDisconnected(player)
-- ----------------------------------------------------------------------------
-- OnPlayerDisconnected Oxide Hook. This hook is triggered when a player leaves
-- the server. This hook is used to cancel pending the teleport requests and
-- pending teleports for the disconnecting player.
-- ----------------------------------------------------------------------------
-- Credit: m-Teleportation
function PLUGIN:OnPlayerDisconnected(player)
    -- Grab the player his/her Steam ID.
    local playerID = rust.UserIDFromPlayer( player )

    -- Check if the player has a teleport in progress.
    if Warp.Timers[playerID] ~= nil then
        -- The player is about to be teleported, cancel the teleport and remove
        -- the table entry.
        Warp.Timers[playerID]:Destroy()
        Warp.Timers[playerID] = nil
    end
end
