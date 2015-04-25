PLUGIN.Title        = "FriendsAPI"
PLUGIN.Description  = "An API to manage friends"
PLUGIN.Author       = "#Domestos"
PLUGIN.Version      = V(1, 2, 0)
PLUGIN.HasConfig    = true
PLUGIN.ResourceID   = 686



function PLUGIN:Init()
    command.AddChatCommand("friend", self.Object, "cmdFriend")
    self:LoadDefaultConfig()
    self:LoadDataFile()
end

function PLUGIN:LoadDefaultConfig()
    self.Config.Settings                        = self.Config.Settings or {}
    self.Config.Settings.MaxFriends             = self.Config.Settings.MaxFriends or 30

    self.Config.Messages                        = self.Config.Messages or {}
    self.Config.Messages.List                   = self.Config.Messages.List or "Friends {count}: "
    self.Config.Messages.NoFriends              = self.Config.Messages.NoFriends or "You dont have friends :("
    self.Config.Messages.NotOnFriendlist        = self.Config.Messages.NotOnFriendlist or "{target} not found on your friendlist"
    self.Config.Messages.FriendRemoved          = self.Config.Messages.FriendRemoved or "{target} was removed from your friendlist"
    self.Config.Messages.PlayerNotFound         = self.Config.Messages.PlayerNotFound or "Player not found"
    self.Config.Messages.CantAddSelf            = self.Config.Messages.CantAddSelf or "You cant add yourself"
    self.Config.Messages.AlreadyOnList          = self.Config.Messages.AlreadyOnList or "{target} is already your friend"
    self.Config.Messages.FriendAdded            = self.Config.Messages.FriendAdded or "{target} is now your friend"
    self.Config.Messages.FriendlistFull         = self.Config.Messages.FriendlistFull or "Your friendlist is full"
    self.Config.Messages.HelpText               = self.Config.Messages.HelpText or "use /friend <add|remove|list> <name/steamID> to add/remove/list friends"
end
-- --------------------------------
-- datafile handling
-- --------------------------------
local DataFile = "friends"
local Data = {}
function PLUGIN:LoadDataFile()
    local data = datafile.GetDataTable(DataFile)
    Data = data or {}
end
function PLUGIN:SaveDataFile()
    datafile.SaveDataTable(DataFile)
end
-- --------------------------------
-- builds output messages by replacing wildcards
-- --------------------------------
local function buildOutput(str, tags, replacements)
    for i = 1, #tags do
        str = string.gsub(str, tags[i], replacements[i])
    end
    return str
end
-- --------------------------------
-- handles chat command /friend
-- --------------------------------
function PLUGIN:cmdFriend(player, cmd, args)
if not player then return end
    local args = self:ArgsToTable(args, "chat")
    local func, target = args[1], args[2]
    local playerSteamID = rust.UserIDFromPlayer(player)
    if not func or func ~= "add" and func ~= "remove" and func ~= "list" then
        rust.SendChatMessage(player, "Syntax: /friend <add/remove> <name/steamID> or /friend list")
        return
    end
    if func ~= "list" and not target then
        rust.SendChatMessage(player, "Syntax: /friend <add/remove> <name/steamID>")
        return
    end
    if func == "list" then
        local friendlist = self:GetFriendlist(playerSteamID)
        if friendlist then
            local i, playerCount = 1, 0
            local friendlistString = ""
            local friendlistTbl = {}
            -- build friendlist string
            for _, value in pairs(friendlist) do
                playerCount = playerCount + 1
                friendlistString = friendlistString..value..", "
                if playerCount == 8 then
                    friendlistTbl[i] = friendlistString
                    friendlistString = ""
                    playerCount = 0
                    i = i + 1
                end
            end
            -- remove comma at the end
            if string.sub(friendlistString, -2, -2) == "," then
                friendlistString = string.sub(friendlistString, 1, -3)
            end
            -- output friendlist
            if #friendlistTbl >= 1 then
                rust.SendChatMessage(player, buildOutput(self.Config.Messages.List, {"{count}"}, {"["..tostring(#friendlist).."/"..tostring(self.Config.Settings.MaxFriends).."]"}))
                for i = 1, #friendlistTbl do
                    rust.SendChatMessage(player, friendlistTbl[i])
                end
                rust.SendChatMessage(player, friendlistString)
            else
                rust.SendChatMessage(player, buildOutput(self.Config.Messages.List, {"{count}"}, {"["..tostring(#friendlist).."/"..tostring(self.Config.Settings.MaxFriends).."]"})..friendlistString)
            end
            return
        end
        rust.SendChatMessage(player, self.Config.Messages.NoFriends)
        return
    end
    local targetPlayer = global.BasePlayer.Find(target)
    if func == "remove" then
        local removed = self:removeFriend(playerSteamID, target)
        if not removed then
            rust.SendChatMessage(player, buildOutput(self.Config.Messages.NotOnFriendlist, {"{target}"}, {target}))
        else
            if targetPlayer then
                rust.SendChatMessage(player, buildOutput(self.Config.Messages.FriendRemoved, {"{target}"}, {targetPlayer.displayName}))
            else
                rust.SendChatMessage(player, buildOutput(self.Config.Messages.FriendRemoved, {"{target}"}, {target}))
            end
        end
        return
    end
    if not targetPlayer then
        rust.SendChatMessage(player, self.Config.Messages.PlayerNotFound)
        return
    end
    local targetName = targetPlayer.displayName
    local targetSteamID = rust.UserIDFromPlayer(targetPlayer)
    if func == "add" then
        if player == targetPlayer then
            rust.SendChatMessage(player, self.Config.Messages.CantAddSelf)
            return
        end
        local added = self:addFriend(player, targetSteamID, targetName)
        if added == "max" then
            rust.SendChatMessage(player, self.Config.Messages.FriendlistFull)
            return
        end
        if not added then
            rust.SendChatMessage(player, buildOutput(self.Config.Messages.AlreadyOnList, {"{target}"}, {targetName}))
        else
            rust.SendChatMessage(player, buildOutput(self.Config.Messages.FriendAdded, {"{target}"}, {targetName}))
        end
        return
    end

end
-- --------------------------------
-- returns true if player was removed
-- returns false if not (not on friendlist)
-- --------------------------------
function PLUGIN:removeFriend(playerSteamID, target)
    local playerData = self:GetPlayerData(playerSteamID)
    if not playerData then return false end
    for key, _ in pairs(playerData.Friends) do
        if playerData.Friends[key].steamID == target or playerData.Friends[key].name == target then
            table.remove(playerData.Friends, key)
            if #playerData.Friends == 0 then
                Data[playerSteamID] = nil
            end
            self:SaveDataFile()
            return true
        end
    end
    return false
end
-- --------------------------------
-- returns true if friend was added
-- returns false if not (is already on friendlist)
-- --------------------------------
function PLUGIN:addFriend(player, targetSteamID, targetName)
    local playerSteamID = rust.UserIDFromPlayer(player)
    local playerName = player.displayName
    local playerData = self:GetPlayerData(playerSteamID, playerName, true)
    if #playerData.Friends >= self.Config.Settings.MaxFriends then
        return "max"
    end
    for key, _ in pairs(playerData.Friends) do
        if playerData.Friends[key].steamID == targetSteamID then
            return false
        end
    end
    local newFriend = {["name"] = targetName, ["steamID"] = targetSteamID}
    table.insert(playerData.Friends, newFriend)
    self:SaveDataFile()
    return true
end
-- --------------------------------
-- returns true when player has target on friendlist
-- returns false if not
-- --------------------------------
function PLUGIN:HasFriend(playerSteamID, target)
    local playerData = self:GetPlayerData(playerSteamID)
    if not playerData then return false end
    for key, _ in pairs(playerData.Friends) do
        if playerData.Friends[key].steamID == target or playerData.Friends[key].name == target then
            return true
        end
    end
    return false
end
-- --------------------------------
-- returns true when player is on targets friendlist
-- returns false if hes not
-- --------------------------------
function PLUGIN:IsFriendFrom(player, targetSteamID)
    local playerData = self:GetPlayerData(targetSteamID)
    if not playerData then return false end
    for key, _ in pairs(playerData.Friends) do
        if playerData.Friends[key].steamID == player or playerData.Friends[key].name == player then
            return true
        end
    end
    return false
end
-- --------------------------------
-- returns true when player and target are friends
-- returns false when they are not
-- --------------------------------
function PLUGIN:areFriends(playerSteamID, targetSteamID)
    local hasFriend = self:HasFriend(playerSteamID, targetSteamID)
    local isFriend = self:IsFriendFrom(playerSteamID, targetSteamID)
    if hasFriend and isFriend then
        return true
    end
    return false
end
-- --------------------------------
-- returns a players friendlist as table
-- if known, the table will return the names, if not it returns steamID
-- returns false if player has no friends
-- --------------------------------
function PLUGIN:GetFriendlist(playerSteamID)
    local playerData = self:GetPlayerData(playerSteamID)
    if not playerData then return false end
    local friends = {}
    for key, _ in pairs(playerData.Friends) do
        friends[key] = playerData.Friends[key].name
    end
    return friends
end
-- --------------------------------
-- returns args as a table
-- --------------------------------
function PLUGIN:ArgsToTable(args, src)
    local argsTbl = {}
    if src == "chat" then
        local length = args.Length
        for i = 0, length - 1, 1 do
            argsTbl[i + 1] = args[i]
        end
        return argsTbl
    end
    if src == "console" then
        local i = 1
        while args:HasArgs(i) do
            argsTbl[i] = args:GetString(i - 1)
            i = i + 1
        end
        return argsTbl
    end
    return argsTbl
end
-- --------------------------------
-- returns table with player data
-- --------------------------------
function PLUGIN:GetPlayerData(playerSteamID, playerName, addNewEntry)
    local playerData = Data[playerSteamID]
    if not playerData and addNewEntry then
        playerData = {}
        playerData.SteamID = playerSteamID
        playerData.Name = playerName
        playerData.Friends = {}
        Data[playerSteamID] = playerData
        self:SaveDataFile()
    end
    return playerData
end
-- --------------------------------
-- sends the helptext
-- --------------------------------
function PLUGIN:SendHelpText(player)
    rust.SendChatMessage(player, self.Config.Messages.HelpText)
end