
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Logging;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Kits", "Reneb", "2.0.5")]
    class Kits : RustPlugin
    {
        private string noAccess;
        private int authlevel;
        private string itemNotFound;
        private string cantUseKit;
        private string maxKitReached;
        private string unknownKit;
        private string kitredeemed;
        private string kitsreset;

        private DateTime epoch;
        private Core.Configuration.DynamicConfigFile KitsConfig;
        private Core.Configuration.DynamicConfigFile KitsData;
        private bool Changed;
        private Dictionary<string, string> displaynameToShortname;

        void Loaded()
        {
            epoch = new System.DateTime(1970, 1, 1);
            displaynameToShortname = new Dictionary<string, string>();
            if (!permission.PermissionExists("vip")) permission.RegisterPermission("vip", this);
            LoadVariables();
            InitializeKits();
        }
        void OnServerInitialized()
        {
            InitializeTable();
        }
        double CurrentTime()
        {
            return System.DateTime.UtcNow.Subtract(epoch).TotalSeconds;
        }
        private void InitializeKits()
        {
            KitsConfig = Interface.GetMod().DataFileSystem.GetDatafile("Kits_List");
            KitsData = Interface.GetMod().DataFileSystem.GetDatafile("Kits_Data");
        }
        private void SaveKits()
        {
            Interface.GetMod().DataFileSystem.SaveDatafile("Kits_List");
        }
        private void SaveKitsData()
        {
            Interface.GetMod().DataFileSystem.SaveDatafile("Kits_Data");
        }
        private void InitializeTable()
        {
            displaynameToShortname.Clear();
            List<ItemDefinition> ItemsDefinition = ItemManager.GetItemDefinitions() as List<ItemDefinition>;
            foreach (ItemDefinition itemdef in ItemsDefinition)
            {
                displaynameToShortname.Add(itemdef.displayName.english.ToString().ToLower(), itemdef.shortname.ToString());
            }
        }
        private object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                Changed = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                Changed = true;
            }
            return value;
        }
        private void LoadVariables()
        {
            authlevel = Convert.ToInt32(GetConfig("Settings", "authLevel", 1));
            noAccess = Convert.ToString(GetConfig("Messages", "noAccess", "You are not allowed to use this command"));
            itemNotFound = Convert.ToString(GetConfig("Messages", "itemNotFound", "Item not found: "));
            cantUseKit = Convert.ToString(GetConfig("Messages", "cantUseKit", "You are not allowed to use this kit"));
            maxKitReached = Convert.ToString(GetConfig("Messages", "maxKitReached", "You've used all your tokens for this kit"));
            unknownKit = Convert.ToString(GetConfig("Messages", "unknownKit", "This kit doesn't exist"));
            kitredeemed = Convert.ToString(GetConfig("Messages", "kitredeemed", "You've redeemed a kit"));
            kitsreset = Convert.ToString(GetConfig("Messages", "kitsreset", "All kits data from players were deleted"));
            if (Changed)
            {
                SaveConfig();
                Changed = false;
            }
        }
        void LoadDefaultConfig()
        {
            Puts("Kits: Creating a new config file");
            Config.Clear();
            LoadVariables();
        }
        bool hasAccess(BasePlayer player)
        {
            if (player.net.connection.authLevel >= authlevel)
                return true;
            return false;
        }
        bool hasVip(object source)
        {
            if (!(source is BasePlayer)) return true;
            if (((BasePlayer)source).net.connection.authLevel >= authlevel) return true;
            return permission.UserHasPermission(((BasePlayer)source).userID.ToString(), "vip");
        }
        public object GiveItem(BasePlayer player, string itemname, int amount, ItemContainer pref)
        {
            itemname = itemname.ToLower();

            bool isBP = false;
            if (itemname.EndsWith(" bp"))
            {
                isBP = true;
                itemname = itemname.Substring(0, itemname.Length - 3);
            }
            if (displaynameToShortname.ContainsKey(itemname))
                itemname = displaynameToShortname[itemname];
            var definition = ItemManager.FindItemDefinition(itemname);
            if (definition == null)
                return string.Format("{0} {1}", itemNotFound, itemname);
            int giveamount = 0;
            int stack = (int)definition.stackable;
            if (isBP)
                stack = 1;
            if (stack < 1) stack = 1;
            for (var i = amount; i > 0; i = i - stack)
            {
                if (i >= stack)
                    giveamount = stack;
                else
                    giveamount = i;
                if (giveamount < 1) return true;
                player.inventory.GiveItem(ItemManager.CreateByItemID((int)definition.itemid, giveamount, isBP), pref);
            }
            return true;
        }
        private void SendTheReply(object source, string msg)
        {
            if (source is BasePlayer)
                SendReply((BasePlayer)source, msg);
            else if (source is ConsoleSystem.Arg)
                SendReply((ConsoleSystem.Arg)source, msg);
            else
                Puts(msg);
        }
        void SendList(object source)
        {
            var kitEnum = KitsConfig.GetEnumerator();
            int authlevel = GetSourceLevel(source);
            bool isvip = hasVip(source);

            while (kitEnum.MoveNext())
            {
                int kitlvl = 0;
                string kitdescription = string.Empty;
                string options = string.Empty;
                string kitname = string.Empty;
                options = string.Empty;
                kitname = kitEnum.Current.Key.ToString();
                var kitdata = kitEnum.Current.Value as Dictionary<string, object>;
                if (kitdata.ContainsKey("description"))
                    kitdescription = kitdata["description"].ToString();
                if (kitdata.ContainsKey("level"))
                {
                    kitlvl = (int)kitdata["level"];
                    options = string.Format("{0} - {1}+", options, kitlvl.ToString());

                }
                if (kitdata.ContainsKey("vip"))
                {
                    options = string.Format("{0} - {1}", options, "vip");
                    if (!isvip) continue;
                }
                if (kitdata.ContainsKey("max"))
                {
                    options = string.Format("{0} - {1} max", options, kitdata["max"].ToString());
                }
                if (kitdata.ContainsKey("cooldown"))
                {
                    options = string.Format("{0} - {1}s cooldown", options, kitdata["cooldown"].ToString());
                }
                if (kitlvl <= authlevel)
                {
                    SendTheReply(source, string.Format("{0} - {1} {2}", kitname, kitdescription, options));
                }
            }
        }
        int GetSourceLevel(object source)
        {
            if (source is BasePlayer)
            {
                return ((BasePlayer)source).net.connection.authLevel;
            }
            return 2;
        }

        void cmdAddKit(BasePlayer player, string[] args)
        {
            if (args.Length < 3)
            {
                SendTheReply(player, "/kit add \"KITNAME\" \"DESCRIPTION\" -option1 -option2 etc, Everything you have in your inventory will be used in the kit");
                SendTheReply(player, "Options avaible:");
                SendTheReply(player, "-maxXX => max times someone can use this kit. Default is infinite.");
                SendTheReply(player, "-cooldownXX => cooldown of the kit. Default is none.");
                SendTheReply(player, "-vip => Allow to give this kit only to vip & admins");
                SendTheReply(player, "-authlevelXX => Level needed to use this plugin: 0, 1 or 2. Default is 0");
                return;
            }
            string kitname = args[1].ToString();
            string desription = args[2].ToString();
            int authlevel = 0;
            int max = -1;
            bool vip = false;
            double cooldown = 0.0;
            if (KitsConfig[kitname] != null)
            {
                SendTheReply(player, string.Format("The kit {0} already exists. Delete it first or change the name.", kitname));
                return;
            }
            if (args.Length > 3)
            {
                object validoptions = VerifyOptions(args, out authlevel, out max, out cooldown, out vip);
                if (validoptions is string)
                {
                    SendTheReply(player, (string)validoptions);
                    return;
                }
            }
            Dictionary<string, object> kitsitems = GetNewKitFromPlayer(player);
            Dictionary<string, object> newkit = new Dictionary<string, object>();
            newkit.Add("items", kitsitems);
            if (authlevel > 0)
                newkit.Add("level", authlevel);
            if (max >= 0)
                newkit.Add("max", max);
            if (vip)
                newkit.Add("vip", true);
            if (cooldown > 0.0)
                newkit.Add("cooldown", cooldown);
            newkit.Add("description", desription);
            KitsConfig[kitname] = newkit;
            SaveKits();
        }
        Dictionary<string, object> GetNewKitFromPlayer(BasePlayer player)
        {
            Dictionary<string, object> kitsitems = new Dictionary<string, object>();
            List<object> wearList = new List<object>();
            List<object> mainList = new List<object>();
            List<object> beltList = new List<object>();

            ItemContainer wearcontainer = player.inventory.containerWear;
            ItemContainer maincontainer = player.inventory.containerMain;
            ItemContainer beltcontainer = player.inventory.containerBelt;

            string itemname = string.Empty;
            foreach (Item item in (List<Item>)wearcontainer.itemList)
            {
                Dictionary<string, object> newObject = new Dictionary<string, object>();


                newObject.Add(item.info.shortname.ToString(), (int)item.amount);
                wearList.Add(newObject);
            }

            foreach (Item item in (List<Item>)maincontainer.itemList)
            {
                Dictionary<string, object> newObject = new Dictionary<string, object>();
                itemname = item.info.shortname.ToString();
                if ((bool)item.isBlueprint)
                    itemname = string.Format("{0} BP", itemname);
                newObject.Add(itemname, (int)item.amount);
                mainList.Add(newObject);
            }
            foreach (Item item in (List<Item>)beltcontainer.itemList)
            {
                Dictionary<string, object> newObject = new Dictionary<string, object>();
                itemname = item.info.shortname.ToString();
                if ((bool)item.isBlueprint)
                    itemname = string.Format("{0} BP", itemname);
                newObject.Add(itemname, (int)item.amount);
                beltList.Add(newObject);
            }
            player.inventory.Strip();
            kitsitems.Add("wear", wearList);
            kitsitems.Add("main", mainList);
            kitsitems.Add("belt", beltList);
            return kitsitems;
        }
        object VerifyOptions(string[] args, out int authlevel, out int max, out double cooldown, out bool vip)
        {
            authlevel = 0;
            max = -1;
            cooldown = 0.0;
            vip = false;
            for (var i = 3; i < args.Length; i++)
            {
                int substring = 0;
                if (args[i].StartsWith("-max"))
                {
                    substring = 4;
                    if (!(int.TryParse(args[i].Substring(substring), out max)))
                        return string.Format("Wrong Number Value for : {0}", args[i].ToString());
                }
                else if (args[i].StartsWith("-cooldown"))
                {
                    substring = 9;
                    if (!(double.TryParse(args[i].Substring(substring), out cooldown)))
                        return string.Format("Wrong Number Value for : {0}", args[i].ToString());
                }
                else if (args[i].StartsWith("-vip"))
                {
                    vip = true;
                }
                else if (args[i].StartsWith("-authlevel"))
                {
                    substring = 10;
                    if (!(int.TryParse(args[i].Substring(substring), out authlevel)))
                        return string.Format("Wrong Number Value for : {0}", args[i].ToString());
                    if (authlevel > 2)
                        authlevel = 2;
                    if (authlevel < 0)
                        authlevel = 0;
                }
                else
                    return string.Format("Wrong Options: {0}", args[i].ToString());
                return true;
            }
            return true;
        }
        void cmdResetKits(BasePlayer player, string[] args)
        {
            KitsData.Clear();
            SendTheReply(player, "All kits data from players were deleted");
            SaveKitsData();
        }
        void OnPlayerRespawned(BasePlayer player)
        {
            if (KitsConfig["autokit"] == null) return;
            object thereturn = Interface.GetMod().CallHook("canRedeemKit", new object[] { player });
            if (thereturn == null)
            {
                player.inventory.Strip();
                GiveKit(player, "autokit");
            }
        }
        void cmdRemoveKit(BasePlayer player, string[] args)
        {
            if (args.Length < 2)
            {
                SendTheReply(player, "Kit must specify the name of the kit that you want to remove");
                return;
            }
            int authlevel = GetSourceLevel(player);
            int kitlvl = 0;
            string kitname = args[1].ToString();
            if (KitsConfig[kitname] == null)
            {
                SendTheReply(player, string.Format("The kit {0} doesn't exist", kitname));
                return;
            }

            var kitdata = (KitsConfig[kitname]) as Dictionary<string, object>;
            if (kitdata.ContainsKey("level"))
                kitlvl = (int)kitdata["level"];
            if (kitlvl > 2)
                kitlvl = 2;
            if (kitlvl > authlevel)
            {
                SendTheReply(player, "You don't have the level to remove this kit");
                return;
            }
            var newKits = new Dictionary<string, object>();
            var enumkits = KitsConfig.GetEnumerator();
            while (enumkits.MoveNext())
            {
                if (enumkits.Current.Key.ToString() != kitname && enumkits.Current.Value != null)
                {
                    newKits.Add(enumkits.Current.Key.ToString(), enumkits.Current.Value);
                }
            }
            KitsConfig.Clear();
            foreach (KeyValuePair<string, object> pair in newKits)
            {
                KitsConfig[pair.Key] = pair.Value;
            }
            SaveKits();
            SendTheReply(player, string.Format("The kit {0} was successfully removed", kitname));
        }
        int GetKitLeft(BasePlayer player, string kitname, int max)
        {
            if (KitsData[player.userID.ToString()] == null) return max;
            var data = KitsData[player.userID.ToString()] as Dictionary<string, object>;
            if (!(data.ContainsKey(kitname))) return max;
            var currentkit = data[kitname] as Dictionary<string, object>;
            if (!(currentkit.ContainsKey("used"))) return max;
            return (max - (int)currentkit["used"]);
        }
        double GetKitTimeleft(BasePlayer player, string kitname, double max)
        {
            if (KitsData[player.userID.ToString()] == null) return 0.0;
            var data = KitsData[player.userID.ToString()] as Dictionary<string, object>;
            if (!(data.ContainsKey(kitname))) return 0.0;
            var currentkit = data[kitname] as Dictionary<string, object>;
            if (!(currentkit.ContainsKey("cooldown"))) return 0.0;
            return ((double)currentkit["cooldown"] - CurrentTime());
        }
        void TryGiveKit(BasePlayer player, string kitname)
        {
            if (KitsConfig[kitname] == null)
            {
                SendTheReply(player, unknownKit);
                return;
            }
            object thereturn = Interface.GetMod().CallHook("canRedeemKit", new object[1] { player });
            if (thereturn != null)
            {
                if (thereturn is string)
                {
                    SendTheReply(player, (string)thereturn);
                }
                return;
            }

            Dictionary<string, object> kitdata = (KitsConfig[kitname]) as Dictionary<string, object>;
            double cooldown = 0.0;
            int kitleft = 1;
            int kitlvl = 0;

            if (kitdata.ContainsKey("level"))
                kitlvl = (int)kitdata["level"];
            if (kitlvl > player.net.connection.authLevel)
            {
                SendTheReply(player, cantUseKit);
                return;
            }
            if (kitdata.ContainsKey("max"))
                kitleft = GetKitLeft(player, kitname, (int)(kitdata["max"]));
            if (kitleft <= 0)
            {
                SendTheReply(player, maxKitReached);
                return;
            }
            if (kitdata.ContainsKey("vip"))
                if (!hasVip(player))
                {
                    SendReply(player, cantUseKit);
                    return;
                }
            if (kitdata.ContainsKey("cooldown"))
                cooldown = GetKitTimeleft(player, kitname, (double)(kitdata["cooldown"]));
            if (cooldown > 0.0)
            {
                SendTheReply(player, string.Format("You must wait {0}s before using this kit again", cooldown.ToString()));
                return;
            }
            object wasGiven = GiveKit(player, kitname);
            if ((wasGiven is bool) && !((bool)wasGiven))
            {
                Puts(string.Format("An error occurred while giving the kit {0} to {1}", kitname, player.displayName.ToString()));
                return;
            }
            proccessKitGiven(player, kitname, kitdata, kitleft);
        }

        void proccessKitGiven(BasePlayer player, string kitname, Dictionary<string, object> kitdata, int kitleft)
        {
            string userid = player.userID.ToString();
            if (KitsData[userid] == null)
            {
                (KitsData[userid]) = new Dictionary<string, object>();
            }
            var playerData = (KitsData[userid]) as Dictionary<string, object>;
            var currentKitData = new Dictionary<string, object>();
            bool write = false;
            if (kitdata.ContainsKey("max"))
            {
                currentKitData.Add("used", (((int)kitdata["max"] - kitleft) + 1));
                write = true;
            }
            if (kitdata.ContainsKey("cooldown"))
            {
                currentKitData.Add("cooldown", ((double)kitdata["cooldown"] + CurrentTime()));
                write = true;
            }
            if (write)
            {
                if (playerData.ContainsKey(kitname))
                    playerData[kitname] = currentKitData;
                else
                    playerData.Add(kitname, currentKitData);
                KitsData[userid] = playerData;
                SaveKitsData();
            }
        }
        object GiveKit(BasePlayer player, string kitname)
        {
            if (KitsConfig[kitname] == null)
            {
                SendTheReply(player, unknownKit);
                return false;
            }
            var kitdata = (KitsConfig[kitname]) as Dictionary<string, object>;
            var kitsitems = kitdata["items"] as Dictionary<string, object>;
            List<object> wearList = kitsitems["wear"] as List<object>;
            List<object> mainList = kitsitems["main"] as List<object>;
            List<object> beltList = kitsitems["belt"] as List<object>;
            ItemContainer pref = player.inventory.containerWear;

            if (wearList.Count > 0)
            {
                pref = player.inventory.containerWear;
                foreach (object items in wearList)
                {
                    foreach (KeyValuePair<string, object> pair in items as Dictionary<string, object>)
                    {
                        GiveItem(player, (string)pair.Key, (int)pair.Value, pref);
                    }
                }
            }

            if (mainList.Count > 0)
            {
                pref = player.inventory.containerMain;
                foreach (object items in mainList)
                {
                    foreach (KeyValuePair<string, object> pair in items as Dictionary<string, object>)
                    {
                        GiveItem(player, (string)pair.Key, (int)pair.Value, pref);
                    }
                }
            }
            if (beltList.Count > 0)
            {
                pref = player.inventory.containerBelt;
                foreach (object items in beltList)
                {
                    foreach (KeyValuePair<string, object> pair in items as Dictionary<string, object>)
                    {
                        GiveItem(player, (string)pair.Key, (int)pair.Value, pref);
                    }
                }
            }
            SendTheReply(player, kitredeemed);
            return true;
        }
        [ChatCommand("kit")]
        void cmdChatKits(BasePlayer player, string command, string[] args)
        {
            if (args.Length > 0 && (args[0].ToString() == "add" || args[0].ToString() == "reset" || args[0].ToString() == "remove"))
            {
                if (!hasAccess(player))
                {
                    SendReply(player, noAccess);
                    return;
                }
                if (args[0].ToString() == "add")
                    cmdAddKit(player, args);
                else if (args[0].ToString() == "reset")
                    cmdResetKits(player, args);
                else if (args[0].ToString() == "remove")
                    cmdRemoveKit(player, args);
                return;
            }
            if (args.Length == 0)
            {
                SendList(player);
                return;
            }
            TryGiveKit(player, args[0]);
        }
    }
}
