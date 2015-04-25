using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Oxide.Plugins
{

    [Info("Crafting Controller", "Mughisi", "2.2.1", ResourceId = 695)]
    class CraftingController : RustPlugin
    {

        #region Configuration Data
        // Do not modify these values, to configure this plugin edit
        // 'CraftingController.json' in your server's config folder.
        // <drive>:\...\server\<server identity>\oxide\config\

        private bool configChanged = false;

        // Plugin settings
        string defaultChatPrefix = "Crafting Controller";
        string defaultChatPrefixColor = "#008000ff";

        string chatPrefix;
        string chatPrefixColor;

        // Plugin options
        float defaultCraftingRate = 1;
        bool defaultAdminInstantCraft = true;
        bool defaultModeratorInstantCraft = false;
        bool defaultCompleteCurrentCraftingOnShutdown = false;
        int defaultKeyLShift = 5;
        int defaultKeyLCtrl = 10;

        float craftingRate;
        bool adminInstantCraft;
        bool moderatorInstantCraft;
        bool completeCurrentCrafting;
        bool cancelAllCrafting;
        int keyLShift;
        int keyLCtrl;

        // Plugin options - blocked items
        List<object> defaultBlockedItems = new List<object>();

        List<string> blockedItems = new List<string>();

        // Plugin messages
        string defaultCurrentCraftingRate = "The crafting rate is set to {0}.";
        string defaultModifyCraftingRate = "The crafting rate is now set to {0}.";
        string defaultModifyError = "The new crafting rate must be a number between 0 and 2 where 0 is instant craft, 1 is normal and 2 is double!";
        string defaultCraftBlockedItem = "{0} is blocked and can not be crafted!";
        string defaultNoItemSpecified = "You need to specify an item to block/unblock.";
        string defaultInvalidItem = "{0} is not a valid item. Please use the name of the item as it appears in the item list. Ex: Camp Fire";
        string defaultBlockedItem = "{0} has already been blocked!";
        string defaultBlockSucces = "{0} has been blocked from crafting.";
        string defaultUnblockItem = "{0} is not blocked!";
        string defaultUnblockSucces = "{0} is no longer blocked from crafting.";
        string defaultNoPermission = "You don't have permission to use this command.";
        string defaultShowBlockedItems = "The following items are blocked: ";
        string defaultNoBlockedItems = "No items have been blocked.";

        string currentCraftingRate;
        string modifyCraftingRate;
        string modifyError;
        string craftBlockedItem;
        string noItemSpecified;
        string invalidItem;
        string blockedItem;
        string blockSucces;
        string unblockItem;
        string unblockSucces;
        string noPermission;
        string possibleItems;
        string showBlockedItems;
        string noBlockedItems;

        #endregion

        List<ItemBlueprint> blueprintDefinitions = new List<ItemBlueprint>();
        Dictionary<string, float> blueprints = new Dictionary<string, float>();

        List<ItemDefinition> itemDefinitions = new List<ItemDefinition>();
        List<string> items = new List<string>();

        private MethodInfo FinishCraftingTask = typeof(ItemCrafter).GetMethod("FinishCrafting", BindingFlags.NonPublic | BindingFlags.Instance);

        private readonly MethodInfo CollectIngredients = typeof(ItemCrafter).GetMethod("CollectIngredients", BindingFlags.NonPublic | BindingFlags.Instance);

        private readonly FieldInfo serverInputField = typeof(BasePlayer).GetField("serverInput", BindingFlags.NonPublic | BindingFlags.Instance);

        void Loaded() => LoadConfigValues();

        void OnServerInitialized()
        {
            blueprintDefinitions.Clear();
            itemDefinitions.Clear();
            blueprintDefinitions = Resources.LoadAll<ItemBlueprint>("items/").ToList();
            itemDefinitions = Resources.LoadAll<ItemDefinition>("items/").ToList();
            foreach (ItemBlueprint bp in blueprintDefinitions)
                blueprints.Add(bp.targetItem.shortname, bp.time);
            foreach (ItemDefinition itemdef in itemDefinitions)
                items.Add(itemdef.displayName.english);
            UpdateCraftingRate();
        }

        void Unloaded()
        {
            foreach (ItemBlueprint bp in blueprintDefinitions)
                bp.time = blueprints[bp.targetItem.shortname];
        }

        protected override void LoadDefaultConfig() => Log("New configuration file created.");

        void LoadConfigValues()
        {
            // Plugin settings
            chatPrefix = Convert.ToString(GetConfigValue("Settings", "ChatPrefix", defaultChatPrefix));
            chatPrefixColor = Convert.ToString(GetConfigValue("Settings", "ChatPrefixColor", defaultChatPrefixColor));

            // Plugin options
            adminInstantCraft = Convert.ToBoolean(GetConfigValue("Options", "InstantCraftForAdmins", defaultAdminInstantCraft));
            moderatorInstantCraft = Convert.ToBoolean(GetConfigValue("Options", "InstantCraftForModerators", defaultModeratorInstantCraft));
            craftingRate = float.Parse(Convert.ToString(GetConfigValue("Options", "CraftingRate", defaultCraftingRate)), System.Globalization.CultureInfo.InvariantCulture);
            completeCurrentCrafting = Convert.ToBoolean(GetConfigValue("Options", "CompleteCurrentCraftingOnShutdown", defaultCompleteCurrentCraftingOnShutdown));
            keyLShift = int.Parse(Convert.ToString(GetConfigValue("Options", "LShiftAmount", defaultKeyLShift)));
            keyLCtrl = int.Parse(Convert.ToString(GetConfigValue("Options", "LCtrlAmount", defaultKeyLCtrl)));

            // Plugin options - blocked items
            var list = GetConfigValue("Options", "BlockedItems", defaultBlockedItems);

            blockedItems.Clear();
            foreach (object item in list as List<object>)
                blockedItems.Add(item.ToString());

            // Plugin messages
            currentCraftingRate = Convert.ToString(GetConfigValue("Messages", "CurrentCraftingRate", defaultCurrentCraftingRate));
            modifyCraftingRate = Convert.ToString(GetConfigValue("Messages", "ModifyCraftingRate", defaultModifyCraftingRate));
            modifyError = Convert.ToString(GetConfigValue("Messages", "ModifyError", defaultModifyError));
            craftBlockedItem = Convert.ToString(GetConfigValue("Messages", "CraftBlockedItem", defaultCraftBlockedItem));
            noItemSpecified = Convert.ToString(GetConfigValue("Messages", "NoItemSpecified", defaultNoItemSpecified));
            invalidItem = Convert.ToString(GetConfigValue("Messages", "InvalidItem", defaultInvalidItem));
            blockedItem = Convert.ToString(GetConfigValue("Messages", "BlockedItem", defaultBlockedItem));
            blockSucces = Convert.ToString(GetConfigValue("Messages", "BlockSucces", defaultBlockSucces));
            unblockItem = Convert.ToString(GetConfigValue("Messages", "UnblockItem", defaultUnblockItem));
            unblockSucces = Convert.ToString(GetConfigValue("Messages", "UnblockSucces", defaultUnblockSucces));
            noPermission = Convert.ToString(GetConfigValue("Messages", "NoPermission", defaultNoPermission));
            showBlockedItems = Convert.ToString(GetConfigValue("Messages", "ShowBlockedItems", defaultShowBlockedItems));
            noBlockedItems = Convert.ToString(GetConfigValue("Messages", "NoBlockedItems", defaultNoBlockedItems));

            if (configChanged)
            {
                Log("Configuration file updated.");
                SaveConfig();
            }
        }

        [ChatCommand("craft")]
        void ChangeCraftingRate(BasePlayer player, string command, string[] args)
        {
            if (player.net.connection.authLevel == 2 && args.Length == 1)
            {
                try
                {
                    craftingRate = float.Parse(args[0], System.Globalization.CultureInfo.InvariantCulture);
                }
                catch
                {
                    SendChatMessage(player, modifyError);
                    return;
                }
                if (craftingRate >= 0f && craftingRate <= 2f)
                {
                    SetConfigValue("Options", "CraftingRate", craftingRate);
                    SendChatMessage(player, modifyCraftingRate, craftingRate.ToString());
                    UpdateCraftingRate();
                    return;
                }
                SendChatMessage(player, modifyError);
                return;
            }
            SendChatMessage(player, currentCraftingRate, craftingRate.ToString());
        }

        [ChatCommand("block")]
        void BlockItemCraft(BasePlayer player, string command, string[] args)
        {
            if (player.net.connection.authLevel == 2)
            {
                if (args.Length == 0)
                {
                    SendChatMessage(player, noItemSpecified);
                    return;
                }
                string item = string.Join(" ", args);
                if (!items.Contains(item))
                {
                    SendChatMessage(player, invalidItem, item);
                    return;
                }
                if (blockedItems.Contains(item))
                {
                    SendChatMessage(player, blockedItem, item);
                    return;
                }
                blockedItems.Add(item);
                SetConfigValue("Options", "BlockedItems", blockedItems);
                SendChatMessage(player, blockSucces, item);
                return;
            }
            SendChatMessage(player, noPermission);
        }

        [ChatCommand("unblock")]
        void UnblockItemCraft(BasePlayer player, string command, string[] args)
        {
            if (player.net.connection.authLevel == 2)
            {
                if (args.Length == 0)
                {
                    SendChatMessage(player, noItemSpecified);
                    return;
                }
                string item = string.Join(" ", args);
                if (!items.Contains(item))
                {
                    SendChatMessage(player, invalidItem, item);
                    return;
                }
                if (!blockedItems.Contains(item))
                {
                    SendChatMessage(player, unblockItem, item);
                    return;
                }
                blockedItems.Remove(item);
                SetConfigValue("Options", "BlockedItems", blockedItems);
                SendChatMessage(player, unblockSucces, item);
                return;
            }
            SendChatMessage(player, noPermission);
        }
        [ChatCommand("blocked")]
        void BlockedItems(BasePlayer player, string command, string[] args)
        {
            if (blockedItems.Count == 0)
                SendChatMessage(player, noBlockedItems);
            else
            {
                SendChatMessage(player, showBlockedItems);
                foreach (string item in blockedItems)
                    SendChatMessage(player, item);
            }
        }

        void OnServerQuit()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (completeCurrentCrafting)
                    CompleteCrafting(player);

                CancelAllCrafting(player);
            }
        }

        void CompleteCrafting(BasePlayer player)
        {
            ItemCrafter crafter = player.inventory.crafting;
            if (crafter.queue.Count == 0) return;
            ItemCraftTask task = crafter.queue.First<ItemCraftTask>();
            FinishCraftingTask.Invoke(crafter, new object[] { task });
            crafter.queue.Dequeue();
        }

        void CancelAllCrafting(BasePlayer player)
        {
            ItemCrafter crafter = player.inventory.crafting;
            foreach (ItemCraftTask task in crafter.queue)
                crafter.CancelTask(task.taskUID, true);
        }

        void UpdateCraftingRate()
        {
            foreach (ItemBlueprint bp in blueprintDefinitions)
                bp.time = blueprints[bp.targetItem.shortname] * craftingRate;
        }

        void OnItemCraft(ItemCraftTask item)
        {
            BasePlayer crafter = item.owner;
            string itemname = item.blueprint.targetItem.displayName.english;
            if (adminInstantCraft && item.owner.net.connection.authLevel == 2) item.endTime = 1f;
            if (moderatorInstantCraft && item.owner.net.connection.authLevel == 1) item.endTime = 1f;
            if (blockedItems.Contains(itemname))
            {
                item.cancelled = true;
                SendChatMessage(crafter, craftBlockedItem, itemname);
                foreach (ItemAmount amount in item.blueprint.ingredients)
                    crafter.inventory.GiveItem(amount.itemid, (int)amount.amount, false);
            }

            if (craftingRate == 0f) item.endTime = 1f;
            var input = (InputState)serverInputField.GetValue(crafter);
            if (input.IsDown(BUTTON.SPRINT))
                BulkCraft(crafter, item, keyLShift);
            if (input.IsDown(BUTTON.DUCK))
                BulkCraft(crafter, item, keyLCtrl);
        }

        void BulkCraft(BasePlayer player, ItemCraftTask task, int amount)
        {
            ItemCrafter crafter = player.inventory.crafting;
            if (crafter.queue.ToArray()[0] == task)
                amount--;

            for (int i = 1; i <= amount; i++)
            {
                if (!crafter.CanCraft(task.blueprint, 1))
                    break;

                crafter.taskUID++;
                ItemCraftTask item = new ItemCraftTask {
                    blueprint = task.blueprint
                };

                CollectIngredients.Invoke(crafter, new object[] { item.blueprint, item.ingredients });
                if (craftingRate == 0) item.endTime = 1f;
                else item.endTime = 0f;
                item.taskUID = crafter.taskUID;
                item.owner = player;
                item.instanceData = null;
                crafter.queue.Enqueue(item);

                if (item.owner != null)
                {
                    object[] args = new object[] { item.taskUID, item.blueprint.targetItem.itemid };
                    item.owner.Command("note.craft_add", args);
                }
            }
        }

        #region Helper methods
        void Log(string message) => Puts("{0} : {1}", Title, message);

        void SendChatMessage(BasePlayer player, string message, string arguments = null)
        {
            string chatMessage = $"<color={chatPrefixColor}>{chatPrefix}</color>: {message}";
            player?.SendConsoleCommand("chat.add", -1, string.Format(chatMessage, arguments), 1.0);
        }

        object GetConfigValue(string category, string setting, object defaultValue)
        {
            var data = Config[category] as Dictionary<string, object>;
            object value;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[category] = data;
                configChanged = true;
            }
            if (!data.TryGetValue(setting, out value))
            {
                value = defaultValue;
                data[setting] = value;
                configChanged = true;
            }
            return value;
        }

        void SetConfigValue(string category, string setting, object newValue)
        {
            var data = Config[category] as Dictionary<string, object>;
            object value;
            if (data.TryGetValue(setting, out value))
            {
                value = newValue;
                data[setting] = value;
                configChanged = true;
            }
            SaveConfig();
        }
        #endregion
    }

}
