// Reference: Oxide.Ext.Rust
// Reference: Newtonsoft.Json
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Hunt.RPG;
using Hunt.RPG.Keys;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Linq;
using System.Reflection;
using System.Text;
using Oxide.Core.Libraries;
using Oxide.Plugins;
using Rust;
using Random=System.Random;
using Time=UnityEngine.Time;
using System.Collections;

namespace Oxide.Plugins
{

    [Info("Hunt RPG", "PedraozauM / SW", "1.2.10", ResourceId = 841)]
    public class HuntPlugin : RustPlugin
    {
        [PluginReference] private Plugin Pets;
        [PluginReference] private Plugin BuildingOwners;
        private readonly HuntRPG HuntRPGInstance;
        private bool ServerInitialized;
        private bool UpdateConfig;
        private bool UpdatePlayerData;
        private DynamicConfigFile HuntDataFile;
        private VersionNumber DataVersion;

        public HuntPlugin()
        {
            HasConfig = true;
            HuntRPGInstance = new HuntRPG(this);
            DataVersion = new VersionNumber(0,9,0);
        }

        public void GiveTamePermission(string playerid, string perm)
        {
            if (permission.UserHasPermission(playerid, perm)) return;
            permission.GrantUserPermission(playerid, perm, Pets);
        }

        public void RevokeTamePermission(string playerid, string perm)
        {
            if (!permission.UserHasPermission(playerid, perm)) return;
            permission.RevokeUserPermission(playerid, perm);
        }

        protected override void LoadDefaultConfig()
        {
            UpdateConfig = true;
            DefaultConfig();
            UpdateData();
        }

        private void DefaultConfig()
        {
            if (!ServerInitialized && UpdateConfig)
            {
                //this will only be called if there is not a config file, or it needs updating
                Config[HK.ConfigVersion] = Version;
                Config[HK.DataVersion] = DataVersion;
                Config[HK.XPTable] = HuntTablesGenerator.GenerateXPTable(HK.MaxLevel, HK.BaseXP, HK.LevelMultiplier, HK.LevelModule, HK.ModuleReducer);
                Config[HK.MaxStatsTable] = HuntTablesGenerator.GenerateMaxStatsTable();
                Config[HK.SkillTable] = HuntTablesGenerator.GenerateSkillTable();
                Config[HK.ResearchSkillTable] = HuntTablesGenerator.GenerateResearchTable();
                Config[HK.UpgradeBuildTable] = HuntTablesGenerator.GenerateUpgradeBuildingTable();
                Config[HK.MessagesTable] = HuntTablesGenerator.GenerateMessageTable();
                Config[HK.TameTable] = HuntTablesGenerator.GenerateTameTable();
                SaveConfig();
            }
            else
            {
                var itemTable = ReadFromConfig<Dictionary<string, ItemInfo>>(HK.ItemTable);
                if (itemTable == null || UpdatePlayerData)
                {
                    //this will be called only on serverinit if the config needs updating
                    LogToConsole("Generating item table.");
                    Config[HK.ItemTable] = HuntTablesGenerator.GenerateItemTable();
                    SaveConfig();
                }
            }
        }

        private void UpdateData()
        {
            if (!UpdatePlayerData) return;
            // this will only be called if this version requires a data wipe and the config is outdated.
            LogToConsole("This version needs a wipe to data file.");
            LogToConsole("Dont worry levels will be kept! =]");
            LogToConsole("Doing that now...");
            LoadRPG(false);
            var profiles = new Dictionary<string, RPGInfo>(ReadFromData<Dictionary<string, RPGInfo>>(HK.Profile));
            var rpgInfos = new Dictionary<string, RPGInfo>();
            foreach (var profile in profiles)
            {
                var steamId = profile.Key;
                var player = BasePlayer.FindByID(Convert.ToUInt64(steamId)) ??
                             BasePlayer.FindSleeping(Convert.ToUInt64(steamId));
                var rpgInfo = new RPGInfo(player.displayName);
                rpgInfos.Add(steamId, rpgInfo);
                HuntRPGInstance.LevelUpPlayer(rpgInfo, profile.Value.Level);
            }
            LogToConsole("Data file updated!");
            Config[HK.DataVersion] = DataVersion;
            SaveConfig();
            SaveRPG(rpgInfos, new Dictionary<string, string>());
            UpdatePlayerData = false;
        }

        private void LoadRPG(bool showMsgs = true)
        {
            LoadConfig();
            if (showMsgs)
                LogToConsole("Loading plugin data and config...");
            HuntDataFile = Interface.GetMod().DataFileSystem.GetDatafile(HK.DataFileName);
            var rpgConfig = ReadFromData<Dictionary<string, RPGInfo>>(HK.Profile) ?? new Dictionary<string, RPGInfo>();
            if (showMsgs)
                LogToConsole(String.Format("{0} profiles loaded", rpgConfig.Count));
            var playerFurnaces = ReadFromData<Dictionary<string, string>>(HK.Furnaces) ?? new Dictionary<string, string>();
            if (showMsgs)
                LogToConsole(String.Format("{0} furnaces loaded", playerFurnaces.Count));
            var xpTable = ReadFromConfig<Dictionary<int, long>>(HK.XPTable);
            var messagesTable = ReadFromConfig<PluginMessagesConfig>(HK.MessagesTable);
            var skillTable = ReadFromConfig<Dictionary<string, Skill>>(HK.SkillTable);
            var maxStatsTable = ReadFromConfig<Dictionary<string, float>>(HK.MaxStatsTable);
            var itemTable = ReadFromConfig<Dictionary<string, ItemInfo>>(HK.ItemTable);
            var researchSkillTable = ReadFromConfig<Dictionary<string, int>>(HK.ResearchSkillTable);
            var upgradeBuildTable = ReadFromConfig<Dictionary<BuildingGrade.Enum, float>>(HK.UpgradeBuildTable);
            var tameTable = ReadFromConfig<Dictionary<int, string>>(HK.TameTable);
            HuntRPGInstance.ConfigRPG(messagesTable, xpTable, maxStatsTable, upgradeBuildTable, skillTable, researchSkillTable, itemTable,tameTable, rpgConfig, playerFurnaces);
            if (showMsgs)
                LogToConsole("Data and config loaded!");
            
            if (Pets == null)
            {
                LogToConsole("Pets plugin was not found, disabling taming skill");
                skillTable[HRK.Tamer].Enabled = false;
            }
            if (BuildingOwners == null)
            {
                LogToConsole("Building Owners plugin was not found, disabling blink to arrow skill");
                skillTable[HRK.BlinkArrow].Enabled = false;
            }

        }

        public T ReadFromConfig<T>(string configKey)
        {
            string serializeObject = JsonConvert.SerializeObject(Config[configKey]);
            return JsonConvert.DeserializeObject<T>(serializeObject);
        }

        public T ReadFromData<T>(string dataKey)
        {
            string serializeObject = JsonConvert.SerializeObject(HuntDataFile[dataKey]);
            return JsonConvert.DeserializeObject<T>(serializeObject);
        }

        public void SaveRPG(Dictionary<string, RPGInfo> rpgConfig, Dictionary<string, string> playersFurnaces, bool showMsgs = true)
        {
            if (showMsgs)
                LogToConsole("Data being saved...");
            HuntDataFile[HK.Profile] = rpgConfig;
            HuntDataFile[HK.Furnaces] = playersFurnaces;
            Interface.GetMod().DataFileSystem.SaveDatafile(HK.DataFileName);
            if (!showMsgs) return;
            LogToConsole(String.Format("{0} profiles saved", rpgConfig.Count));
            LogToConsole(String.Format("{0} furnaces saved", playersFurnaces.Count));
            LogToConsole("Data was saved successfully!");
        }

        [HookMethod("Init")]
        void Init()
        {
            LogToConsole(HuntRPGInstance == null ? "Problem initializating RPG Instance!" : "Hunt RPG initialized!");
        }

        [HookMethod("OnServerInitialized")]
        void OnServerInitialized()
        {
            ServerInitialized = true;
            DefaultConfig();
            LoadRPG();
        }

        [HookMethod("Unload")]
        void Unload()
        {
            HuntRPGInstance.SaveRPG();
        }

        [HookMethod("Loaded")]
        private void Loaded()
        {
            Interface.GetMod().DataFileSystem.GetDatafile(HK.DataFileName);
            var configVersion = new VersionNumber();
            if (Config[HK.ConfigVersion] != null)
                configVersion = ReadFromConfig<VersionNumber>(HK.ConfigVersion);
            var dataVersion = new VersionNumber();
            if (Config[HK.DataVersion] != null)
                dataVersion = ReadFromConfig<VersionNumber>(HK.DataVersion);
            var needDataUpdate = !DataVersion.Equals(dataVersion);
            var needConfigUpdate = !Version.Equals(configVersion);
            if (!needConfigUpdate && !needDataUpdate)
            {
                PrintToChat("<color=lightblue>Hunt</color>: RPG Loaded!");
                PrintToChat("<color=lightblue>Hunt</color>: To see the Hunt RPG help type \"/hunt\" or \"/h\"");
                return;
            }
            if (needConfigUpdate)
            {
                LogToConsole("Your config needs updating...Doing it now.");
                Config.Clear();
                UpdateConfig = true;
                DefaultConfig();
                LogToConsole("Config updated!");
            }
            UpdatePlayerData = needDataUpdate;
            if (needDataUpdate)
            {
                var wasUpdated = UpdatePlayerData;
                UpdateData();
                foreach (var player in BasePlayer.activePlayerList)
                    HuntRPGInstance.PlayerInit(player, wasUpdated);
            }
        }

        [HookMethod("OnPlayerInit")]
        void OnPlayerInit(BasePlayer player)
        {
            HuntRPGInstance.PlayerInit(player, UpdatePlayerData);
        }

        //[HookMethod("OnItemAddedToContainer")]
        //void OnItemAddedToContainer(ItemContainer itemContainer, Item item)
        //{
        //    HuntRPGInstance.OnItemAddedToContainer(itemContainer, item);
        //}

        [HookMethod("OnEntityTakeDamage")]
        object OnEntityTakeDamage(MonoBehaviour entity, HitInfo hitInfo)
        {
            var player = entity as BasePlayer;
            if (player == null) return null;
            if (!HuntRPGInstance.OnAttacked(player, hitInfo)) return null;
            hitInfo = new HitInfo();
            return hitInfo;
        }

        [HookMethod("OnPlayerAttack")]
        object OnPlayerAttack(BasePlayer player, HitInfo hitInfo)
        {
            return HuntRPGInstance.OnPlayerAttack(player, hitInfo) ? true as object : null;
        }

        [HookMethod("OnEntityDeath")]
        void OnEntityDeath(MonoBehaviour entity, HitInfo hitinfo)
        {
            var player = entity as BasePlayer;
            if (player == null) return;
            HuntRPGInstance.OnDeath(player);
        }

        [HookMethod("OnItemCraft")]
        ItemCraftTask OnItemCraft(ItemCraftTask item)
        {
            return HuntRPGInstance.OnItemCraft(item);
        }

        [HookMethod("OnGather")]
        void OnGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            HuntRPGInstance.OnGather(dispenser, entity, item);
        }

        [HookMethod("OnItemDeployed")]
        void OnItemDeployed(Deployer deployer, BaseEntity baseEntity)
        {
            HuntRPGInstance.OnDeployItem(deployer, baseEntity);
        }

        [HookMethod("OnConsumeFuel")]
        void OnConsumeFuel(BaseOven oven,Item fuel, ItemModBurnable burnable)
        {
            HuntRPGInstance.OnConsumeFuel(oven, fuel, burnable);
        }

        [HookMethod("OnBuildingBlockDoUpgradeToGrade")]
        object OnBuildingBlockUpgrade(BuildingBlock buildingBlock, BaseEntity.RPCMessage message, BuildingGrade.Enum grade)
        {
            HuntRPGInstance.OnBuildingBlockUpgrade(message.player, buildingBlock, grade);
            return null;
        }


        [ChatCommand("h")]
        void cmdHuntShortcut(BasePlayer player, string command, string[] args)
        {
            cmdHunt(player, command, args);
        }

        [ChatCommand("hunt")]
        void cmdHunt(BasePlayer player, string command, string[] args)
        {
            HuntRPGInstance.HandleChatCommand(player, args);
        }

        [ConsoleCommand("hunt.saverpg")]
        private void cmdSaveRPG(ConsoleSystem.Arg arg)
        {
            if (!arg.CheckPermissions()) return;
            HuntRPGInstance.SaveRPG();
        }

        [ConsoleCommand("hunt.resetrpg")]
        private void cmdResetRPG(ConsoleSystem.Arg arg)
        {
            if (!arg.CheckPermissions()) return;
            HuntRPGInstance.ResetRPG();
        }

        [ConsoleCommand("hunt.genxptable")]
        private void cmdGenerateXPTable(ConsoleSystem.Arg arg)
        {
            if (!arg.CheckPermissions()) return;
            arg.ReplyWith("Gerando Tabela");
            var levelMultiplier = HK.LevelMultiplier;
            var baseXP = HK.BaseXP;
            var levelModule = HK.LevelModule;
            var moduleReducer = HK.ModuleReducer;
            if (arg.HasArgs())
                baseXP = arg.GetInt(0);
            if (arg.HasArgs(2))
                levelMultiplier = arg.GetFloat(1);
            if (arg.HasArgs(3))
                levelModule = arg.GetInt(2);
            if (arg.HasArgs(4))
                moduleReducer = arg.GetFloat(3);
                Config[HK.XPTable] = HuntTablesGenerator.GenerateXPTable(HK.MaxLevel, baseXP, levelMultiplier, levelModule, moduleReducer);
            SaveConfig();
            arg.ReplyWith("Tabela Gerada");
        }

        [HookMethod("OnServerSave")]
        void OnServerSave()
        {
            HuntRPGInstance.SaveRPG();
        }

        public void TeleportPlayerTo(BasePlayer player, Vector3 position)
        {
            ForcePlayerPosition(player, position);
        }

        public Vector3? GetGround(Vector3 position)
        {
            var direction = Vector3.forward;
            var raycastHits = Physics.RaycastAll(position, direction, 25f).GetEnumerator();
            float nearestDistance = 9999f;
            Vector3? nearestPoint = null;
            while (raycastHits.MoveNext())
            {
                var hit = (raycastHits.Current);
                if (hit != null)
                {
                    RaycastHit raycastHit = (RaycastHit)hit;
                    if (raycastHit.distance < nearestDistance)
                    {
                        nearestDistance = raycastHit.distance;
                        nearestPoint = raycastHit.point;
                    }
                }
            }
            return nearestPoint;
        }

        public void LogToConsole(string message)
        {
            Puts(String.Format("Hunt: {0}",message));
        }

        public bool IsOwner(object buildingBlock, BasePlayer player)
        {
            if (BuildingOwners == null) return false;
            string owner = (string) BuildingOwners.Call("FindBlockData", new[] {buildingBlock});
            return owner == RPGHelper.SteamId(player);
        }
    }
}

namespace Hunt.RPG
{

    class HuntRPG
    {
        private Dictionary<string, RPGInfo> RPGConfig;
        private PluginMessagesConfig MessagesTable;
        private Dictionary<string, Skill> SkillTable;
        private Dictionary<int, long> XPTable;
        private Dictionary<int, string> TameTable;
        private Dictionary<string, ItemInfo> ItemTable;
        private Dictionary<string, int> ResearchTable;
        private Dictionary<string, string> PlayersFurnaces;
        private readonly Dictionary<string, float> PlayerLastPercentChange;
        private readonly Dictionary<string, Dictionary<string,float>> SkillsCooldowns;
        private Dictionary<BuildingGrade.Enum, float> UpgradeBuildingTable;
        private Dictionary<string, float> MaxStatsTable;
        private readonly HuntPlugin PluginInstance;
        readonly Random RandomGenerator = new Random();

        public HuntRPG(HuntPlugin pluginInstance)
        {
            PluginInstance = pluginInstance;
            SkillsCooldowns = new Dictionary<string, Dictionary<string, float>>();
            PlayerLastPercentChange = new Dictionary<string, float>();
        }

        public void ConfigRPG(PluginMessagesConfig messagesTable, Dictionary<int, long> xpTable, Dictionary<string, float> maxStatsTable, Dictionary<BuildingGrade.Enum, float> upgradeBuildTable, Dictionary<string, Skill> skillTable, Dictionary<string, int> researchSkillTable, Dictionary<string, ItemInfo> itemTable, Dictionary<int, string> tameTable, Dictionary<string, RPGInfo> rpgConfig, Dictionary<string, string> playerFurnaces)
        {
            MessagesTable = messagesTable;
            XPTable = xpTable;
            SkillTable = skillTable;
            MaxStatsTable = maxStatsTable;
            ItemTable = itemTable;
            TameTable = tameTable;
            RPGConfig = rpgConfig;
            ResearchTable = researchSkillTable;
            UpgradeBuildingTable = upgradeBuildTable;
            PlayersFurnaces = playerFurnaces;
        }

        private RPGInfo RPGInfo(BasePlayer player)
        {
            var steamId = RPGHelper.SteamId(player);
            if (RPGConfig.ContainsKey(steamId)) return RPGConfig[steamId];
            RPGConfig[steamId] = new RPGInfo(player.displayName);
            PluginInstance.SaveRPG(RPGConfig, PlayersFurnaces);
            return RPGConfig[steamId];
        }

        private RPGInfo RPGInfo(string steamId)
        {
            return RPGConfig.ContainsKey(steamId) ? RPGConfig[steamId] : null;
        }

        public void HandleChatCommand(BasePlayer player, string[] args)
        {
            if (args.Length == 0)
            {
                ChatMessage(player, MessagesTable.GetMessage(HMK.Help));
                return;
            }
            var rpgInfo = RPGInfo(player);

            switch (args[0].ToLower())
            {
                case "about":
                    ChatMessage(player, MessagesTable.GetMessage(HMK.About));
                    break;
                case "shortcuts":
                    ChatMessage(player, MessagesTable.GetMessage(HMK.Shortcuts));
                    break;
                case "p":
                case "profile":
                    DisplayProfile(player);
                    break;
                case "pp":
                case "profilepreferences":
                    ChatMessage(player, MessagesTable.GetMessage(HMK.ProfilePreferences));
                    break;
                case "sts":
                case "statset":
                    SetStatsCommand(player, args, rpgInfo);
                    break;
                case "sks":
                case "skillset":
                    SetSkillsCommand(player, args, rpgInfo);
                    break;
                case "skill":
                    DisplaySkillCommand(player, args);
                    break;
                case "skilllist":
                    ListSkills(player);
                    break;
                case "lvlup":
                    LevelUpChatHandler(player, args, rpgInfo);
                    break;
                case "research":
                    ReserachItem(player, args, rpgInfo);
                    break;
                case "xp":
                    ChatMessage(player, XPProgression(rpgInfo));
                    break;
                case "xp%":
                    ChangePlayerXPMessagePreference(player, args, rpgInfo);
                    break;
                case "craftmsg":
                    ToogleCraftMessage(player, rpgInfo);
                    break;
                case "ba":
                    ToogleBlinkArrow(player, rpgInfo);
                    break;
                case "aba":
                    ToogleAutoBlinkArrow(player, rpgInfo);
                    break;
                default:
                    ChatMessage(player, MessagesTable.GetMessage(HMK.InvalidCommand, new[]{args[0]}));
                    break;
            }
        }

        private void ToogleAutoBlinkArrow(BasePlayer player, RPGInfo rpgInfo)
        {
            rpgInfo.Preferences.AutoToggleBlinkArrow = !rpgInfo.Preferences.AutoToggleBlinkArrow;
            var toggleBlinkArrowStatus = rpgInfo.Preferences.AutoToggleBlinkArrow ? "On" : "Off";
            ChatMessage(player, String.Format("Auto Toggle Blink Arrow is now {0}", toggleBlinkArrowStatus));
        }

        private void ToogleBlinkArrow(BasePlayer player, RPGInfo rpgInfo)
        {
            rpgInfo.Preferences.UseBlinkArrow = !rpgInfo.Preferences.UseBlinkArrow;
            var blinkArrowStatus = rpgInfo.Preferences.UseBlinkArrow ? "On" : "Off";
            ChatMessage(player, String.Format("Blink Arrow is now {0}", blinkArrowStatus));
        }

        private void ToogleCraftMessage(BasePlayer player, RPGInfo rpgInfo)
        {
            rpgInfo.Preferences.ShowCraftMessage = !rpgInfo.Preferences.ShowCraftMessage;
            var craftMessageStatus = rpgInfo.Preferences.ShowCraftMessage ? "On" : "Off";
            ChatMessage(player, String.Format("Craft message is now {0}",craftMessageStatus));
        }

        public bool IsNight()
        {
            var dateTime = TOD_Sky.Instance.Cycle.DateTime;
            return dateTime.Hour >= 19 || dateTime.Hour <= 5;
        }

        private void ChangePlayerXPMessagePreference(BasePlayer player, string[] args, RPGInfo rpgInfo)
        {
            var commandArgs = args.Length - 1;
            if (commandArgs != 1)
            {
                InvalidCommand(player, args);
                return;
            }
            float xpPercent;
            if (!Single.TryParse(args[1], out xpPercent))
            {
                InvalidCommand(player, args);
                return;
            }
            rpgInfo.Preferences.ShowXPMessagePercent = xpPercent/100;
            ChatMessage(player, String.Format("XP will be shown at every {0:P} change", rpgInfo.Preferences.ShowXPMessagePercent));
        }

        private void DisplaySkillCommand(BasePlayer player, string[] args)
        {
            int commandArgs = args.Length - 1;
            if (commandArgs != 1)
            {
                InvalidCommand(player, args);
                return;
            }
            var skillName = args[1];
            if (!SkillTable.ContainsKey(skillName))
            {
                ChatMessage(player, HMK.InvalidSkillName);
                return;
            }
            var sb = new StringBuilder();
            RPGHelper.SkillInfo(sb, SkillTable[skillName]);
            ChatMessage(player, sb.ToString());
        }

        private void ListSkills(BasePlayer player)
        {
            var sb = new StringBuilder();
            sb.AppendLine("==================");
            sb.AppendLine("Availabel Skills:");
            foreach (var skill in SkillTable)
                RPGHelper.SkillInfo(sb, skill.Value);
            sb.AppendLine("==================");
            ChatMessage(player, sb.ToString());
        }

        public bool OnAttacked(BasePlayer player, HitInfo hitInfo)
        {
            var baseNpc = hitInfo.Initiator as BaseNPC;
            var basePlayer = hitInfo.Initiator as BasePlayer;
            bool canEvade = baseNpc != null || basePlayer != null && player.userID != basePlayer.userID;
            if (!canEvade) return false;
            var randomFloat = Random(0, 1);
            RPGInfo rpgInfo = RPGInfo(player);
            var evasion = RPGHelper.GetEvasion(rpgInfo, MaxStatsTable[HRK.AgiEvasionGain]);
            bool evaded = randomFloat <= evasion;
            if (evaded)
            {
                ChatMessage(player, "Dodged!");
                return true;
            }
            var blockPercent = RPGHelper.GetBlock(rpgInfo, MaxStatsTable[HRK.StrBlockGain]);
            //var total = hitInfo.damageTypes.Total();
            float[] array = hitInfo.damageTypes.types;
            for (int index = 0; index < array.Length; index++)
            {
                var damage = array[index];
                damage = damage - (damage * blockPercent);
                hitInfo.damageTypes.Set((DamageType)index, damage);
            }
            //var blocked = hitInfo.damageTypes.Total();
            //ChatMessage(player, String.Format("Blocked {0:F1}/{1}", total-blocked, total));
            return false;
        }

        double Random(double a, double b)
        {
            return a + RandomGenerator.NextDouble() * (b - a);
        }

        public ItemCraftTask OnItemCraft(ItemCraftTask item)
        {
            BasePlayer player = item.owner;
            var itemName = item.blueprint.targetItem.displayName.translated.ToLower();
            if (!ItemTable.ContainsKey(itemName))
                return null;
            var blueprintTime = ItemTable[itemName].BlueprintTime;
            
            var rpgInfo = RPGInfo(player);
            float craftingTime = blueprintTime;
            float craftingReducer = RPGHelper.GetCraftingReducer(rpgInfo, MaxStatsTable[HRK.IntCraftingReducer]);
            var amountToReduce = (craftingTime*craftingReducer);    
            float reducedCraftingTime = craftingTime - amountToReduce;
            item.blueprint.time = reducedCraftingTime;
            if(rpgInfo.Preferences.ShowCraftMessage)
                ChatMessage(player, String.Format("Crafting will end in {0:F} seconds. Reduced in {1:F} seconds", reducedCraftingTime, amountToReduce));
            return item;
        }

        public void OnGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity.ToPlayer();
            if (player == null) return;
            var gatherType = dispenser.gatherType;
            RPGInfo rpgInfo = RPGInfo(player);
            int experience = item.amount;
            if (rpgInfo == null) return;
            if (gatherType == ResourceDispenser.GatherType.Tree)
            {
                if (rpgInfo.Skills.ContainsKey(HRK.LumberJack))
                {
                    var modifier = SkillTable[HRK.LumberJack].Modifiers[HRK.GatherModifier];
                    int newAmount = SkillMethods.GatherModifier(rpgInfo.Skills[HRK.LumberJack], Convert.ToInt32(modifier.Args[0]), item.amount);
                    item.amount = newAmount;
                }
                experience = item.amount;
            }
            if (gatherType == ResourceDispenser.GatherType.Ore)
            {
                if (rpgInfo.Skills.ContainsKey(HRK.Miner))
                {
                    var modifier = SkillTable[HRK.Miner].Modifiers[HRK.GatherModifier];
                    int newAmount = SkillMethods.GatherModifier(rpgInfo.Skills[HRK.Miner], Convert.ToInt32(modifier.Args[0]), item.amount);
                    item.amount = newAmount;
                }
                experience = (int) ((float)item.amount/3);
            }
            if (gatherType == ResourceDispenser.GatherType.Flesh)
            {
                if (rpgInfo.Skills.ContainsKey(HRK.Hunter))
                {
                    var modifier = SkillTable[HRK.Hunter].Modifiers[HRK.GatherModifier];
                    int newAmount = SkillMethods.GatherModifier(rpgInfo.Skills[HRK.Hunter], Convert.ToInt32(modifier.Args[0]), item.amount);
                    item.amount = newAmount;
                }
                experience = item.amount * 5;
            }
            ExpGain(rpgInfo, experience, player);
        }

        private void ExpGain(RPGInfo rpgInfo, int experience, BasePlayer player)
        {
            var steamId = RPGHelper.SteamId(player);
            if (IsNight())
                experience *= 2;
            if (rpgInfo.AddExperience(experience, RequiredExperience(rpgInfo.Level)))
            {
                NotifyLevelUp(player, rpgInfo);
                PlayerLastPercentChange[steamId] = 0;
            }
            else
            {
                var currentPercent = CurrentPercent(rpgInfo);
                if (!PlayerLastPercentChange.ContainsKey(steamId))
                    PlayerLastPercentChange.Add(steamId, currentPercent);
                var lastPercent = PlayerLastPercentChange[steamId];
                var requiredPercentChange = rpgInfo.Preferences.ShowXPMessagePercent;
                float percentChange = currentPercent - lastPercent;
                if (percentChange < requiredPercentChange) return;
                ChatMessage(player, XPProgression(rpgInfo));
                PlayerLastPercentChange[steamId] = currentPercent;
            }
                
        }

        private void NotifyLevelUp(BasePlayer player, RPGInfo rpgInfo)
        {
            ChatMessage(player, String.Format("<color=yellow>Level Up! You are now level {0}</color>", rpgInfo.Level));
            DisplayProfile(player);
            PluginInstance.SaveRPG(RPGConfig, PlayersFurnaces, false);
        }

        private long RequiredExperience(int level)
        {
            return XPTable[level];
        }

        public string XPProgression(RPGInfo rpgInfo)
        {
            var percent = CurrentPercent(rpgInfo);
            string nightBonus = "";
            if (IsNight())
                nightBonus = "Bonus Night Exp On";
            return String.Format("Current XP: {0:P}. {1}", percent, nightBonus);
        }

        private float CurrentPercent(RPGInfo rpgInfo)
        {
            return rpgInfo.Experience/(float) (RequiredExperience(rpgInfo.Level));
        }

        public string Profile(RPGInfo rpgInfo, BasePlayer player)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine(String.Format("========{0}========", rpgInfo.SteamName));
            sb.AppendLine(String.Format("Level: {0}", rpgInfo.Level));
            sb.AppendLine(String.Format("Damage Block: {0:P}", RPGHelper.GetBlock(rpgInfo, MaxStatsTable[HRK.StrBlockGain])));
            sb.AppendLine(String.Format("Evasion Chance: {0:P}", RPGHelper.GetEvasion(rpgInfo, MaxStatsTable[HRK.AgiEvasionGain])));
            sb.AppendLine(String.Format("Crafting Reducer: {0:P}", RPGHelper.GetCraftingReducer(rpgInfo, MaxStatsTable[HRK.IntCraftingReducer])));
            sb.AppendLine(XPProgression(rpgInfo));
            sb.Append(String.Format("<color={0}>Agi: {1}</color> | ","green", rpgInfo.Agility));
            sb.Append(String.Format("<color={0}>Str: {1}</color> | ", "red", rpgInfo.Strength));
            sb.Append(String.Format("<color={0}>Int: {1}</color>", "blue", rpgInfo.Intelligence));
            sb.AppendLine();
            sb.AppendLine(String.Format("Stats Points: {0}", rpgInfo.StatsPoints));
            sb.AppendLine(String.Format("Skill Points: {0}", rpgInfo.SkillPoints));
            sb.AppendLine(String.Format("========<color={0}>Skills</color>========", "purple"));
            foreach (var skill in rpgInfo.Skills)
                sb.AppendLine(String.Format("{0}: {1}/{2}", skill.Key, skill.Value, SkillTable[skill.Key].MaxPoints));
            sb.AppendLine("====================");
            return sb.ToString();
        }

        private void ChatMessage(BasePlayer player, IEnumerable<string> messages)
        {
            foreach (string message in messages)
                ChatMessage(player, message);
        }

        private void ChatMessage(BasePlayer player, string message)
        {
            player.ChatMessage(string.Format("<color={0}>{1}</color>: {2}", MessagesTable.ChatPrefixColor, MessagesTable.ChatPrefix, message));
        }

        public bool ReserachItem(BasePlayer player, string[] args, RPGInfo rpgInfo)
        {
            int commandArgs = args.Length - 1;
            if (commandArgs != 1)
            {
                InvalidCommand(player, args);
                return false;
            }
            
            if (!PlayerHaveSkill(player, rpgInfo, HRK.Researcher)) return false;
            var playerResearchPoints = rpgInfo.Skills[HRK.Researcher];
            var itemname = args[1];
            itemname = itemname.ToLower();
            if (!ItemTable.ContainsKey(itemname))
            {
                ChatMessage(player, MessagesTable.GetMessage(HMK.ItemNotFound, new[] { itemname }));
                return false;
            }

            var itemInfo = ItemTable[itemname];
            itemname = itemInfo.Shortname;
            var definition = ItemManager.FindItemDefinition(itemname);
            if (definition == null)
            {
                ChatMessage(player, MessagesTable.GetMessage(HMK.ItemNotFound, new[] { itemname }));
                return false;
            }
            if (!itemInfo.CanResearch)
            {
                ChatMessage(player, MessagesTable.GetMessage(HMK.ResearchBlocked, new []{ itemname }));
                return false;
            }
            var playerContainer = player.inventory.containerMain;
            var hasItem = player.inventory.AllItems().Any(item => item.info.shortname.Equals(itemname));
            if (!hasItem)
            {
                ChatMessage(player, String.Format("In order to research an item you must have it on your inventory"));
                return false;
            }
            if (!ResearchTable.ContainsKey(itemInfo.ItemCategory))
            {
                ChatMessage(player, "You can research itens of this type");
                return false;
            }
            var requiredSkillPoints = ResearchTable[itemInfo.ItemCategory];
            if (playerResearchPoints < requiredSkillPoints)
            {
                ChatMessage(player, String.Format("Your research skills are not hight enought. Required {0}", requiredSkillPoints));
                return false;
            }

            var steamId = RPGHelper.SteamId(player);
            float availableAt = 0;
            var time = Time.realtimeSinceStartup;
            var playerCooldowns = PlayerCooldowns(steamId);
            var isReady = RPGHelper.IsSkillReady(playerCooldowns, ref availableAt, time, HRK.Researcher);
            if (isReady)
            {
                var random = Random(0, 1);
                if (random > 0.6)
                {
                    ChatMessage(player, String.Format("You managed to reverse enginier the {0}. The blueprint its on your inventory", definition.displayName.translated));
                    player.inventory.GiveItem(ItemManager.CreateByItemID(definition.itemid, 1, true), playerContainer);
                    NoticeArea.ItemPickUp(definition,1, true);
                }
                else
                {
                    ChatMessage(player, String.Format("OPS! While you were trying to research the {0} you accidently broke it.", definition.displayName.translated));
                    var itemInstance = player.inventory.FindItemID(definition.itemid);
                    player.inventory.Take(new List<Item> { itemInstance }, definition.itemid, 1);
                }
                SetCooldown(rpgInfo, time, playerCooldowns, HRK.Researcher);
            }
            else
            {
                var formatedTimeLeft = RPGHelper.TimeLeft(availableAt, time);
                ChatMessage(player, String.Format("You have tried this moments ago, give it a rest. Time left to research again: {0}",formatedTimeLeft));
            }
            return true;
        }

        private bool PlayerHaveSkill(BasePlayer player, RPGInfo rpgInfo, string skillKey,bool sendMsg = true)
        {
            if (rpgInfo.Skills.ContainsKey(skillKey)) return true;
            if (sendMsg)
                ChatMessage(player, MessagesTable.GetMessage(HMK.SkillNotLearned));
            return false;
        }

        private void SetCooldown(RPGInfo rpgInfo, float time, Dictionary<string, float> playerCooldowns, string skillKey)
        {
            var modifier = SkillTable[skillKey].Modifiers[HRK.CooldownModifier];
            var availableAt = SkillMethods.CooldownModifier(rpgInfo.Skills[skillKey], Convert.ToInt32(modifier.Args[0]),
                Convert.ToInt32(modifier.Args[1]), time);
            playerCooldowns[skillKey] = availableAt;
        }

        private Dictionary<string, float> PlayerCooldowns(string steamId)
        {
            if (!SkillsCooldowns.ContainsKey(steamId))
                SkillsCooldowns.Add(steamId, new Dictionary<string, float>());
            Dictionary<string, float> playerCooldowns = SkillsCooldowns[steamId];
            return playerCooldowns;
        }

        private void SetSkillsCommand(BasePlayer player, string[] args, RPGInfo rpgInfo)
        {
            int commandArgs = args.Length - 1;
            if (args.Length < 3 || (commandArgs%2) != 0)
            {
                InvalidCommand(player, args);
                return;
            }
            var pointsSpent = new List<string>();
            int pairs = (commandArgs / 2) + 1;
            for (int i = 1; i < pairs; i++)
            {
                int index = i * 2 - 1;
                string skillKey = args[index];
                int points;
                if (!Int32.TryParse(args[index + 1], out points))
                {
                    InvalidCommand(player, args);
                    continue;
                }

                if (SkillTable.ContainsKey(skillKey))
                {
                    var skill = SkillTable[skillKey];
                    if (!skill.Enabled)
                    {
                        pointsSpent.AddRange(MessagesTable.GetMessage(HMK.SkillDisabled));
                        continue;
                    }
                    string reason;
                    var pointsAdded = rpgInfo.AddSkill(skill, points, out reason);
                    if (pointsAdded > 0)
                    {
                        pointsSpent.Add(String.Format("<color={0}>{1}: +{2}</color>", "purple", skillKey,
                            pointsAdded));
                        if (!skill.Name.Equals(HRK.Tamer)) continue;
                        var tamerSkill = rpgInfo.Skills[HRK.Tamer];
                        PluginInstance.GiveTamePermission(RPGHelper.SteamId(player), HPK.CanTame);
                        for (int j = 1; j <= tamerSkill; j++)
                            PluginInstance.GiveTamePermission(RPGHelper.SteamId(player), TameTable[j]);
                    }
                        
                    else
                    {
                        pointsSpent.AddRange(MessagesTable.GetMessage(reason));
                        pointsSpent.AddRange(MessagesTable.GetMessage(HMK.SkillInfo));
                    }
                            
                }
                else
                    pointsSpent.AddRange(MessagesTable.GetMessage(HMK.InvalidSkillName));
            }
            ChatMessage(player, pointsSpent);
        }

        private void SetStatsCommand(BasePlayer player, string[] args, RPGInfo rpgInfo)
        {
            int commandArgs = args.Length - 1;
            if (args.Length < 3 || (commandArgs%2) != 0)
                InvalidCommand(player, args);
            else
            {
                var pointsSpent = new List<string>();
                int pairs = (commandArgs/2) + 1;
                for (int i = 1; i < pairs; i++)
                {
                    int index = i*2 - 1;
                    int points;
                    if (!Int32.TryParse(args[index + 1], out points))
                    {
                        InvalidCommand(player, args);
                        continue;
                    }

                    switch (args[index].ToLower())
                    {
                        case "agi":
                            if (rpgInfo.AddAgi(points))
                                pointsSpent.Add(String.Format("<color={0}>Agi: +{1}</color>", "green", points));
                            else
                                pointsSpent.AddRange(MessagesTable.GetMessage(HMK.NotEnoughtPoints));
                            break;
                        case "str":
                            if (rpgInfo.AddStr(points))
                            {
                                pointsSpent.Add(String.Format("<color={0}>Str: +{1}</color>", "red", points));
                            }
                            else
                                pointsSpent.AddRange(MessagesTable.GetMessage(HMK.NotEnoughtPoints));
                            break;
                        case "int":
                            if (rpgInfo.AddInt(points))
                                pointsSpent.Add(String.Format("<color={0}>Int: +{1}</color>", "blue", points));
                            else
                                pointsSpent.AddRange(MessagesTable.GetMessage(HMK.NotEnoughtPoints));
                            break;
                        default:
                            InvalidCommand(player, args);
                            break;
                    }
                }
                ChatMessage(player, pointsSpent);
            }
        }

        private void LevelUpChatHandler(BasePlayer player, string[] args, RPGInfo rpgInfo)
        {
            if (!player.IsAdmin()) return;
            var callerPlayer = player;
            int commandArgs = args.Length - 1;
            if (commandArgs > 2 && commandArgs < 1)
                InvalidCommand(player, args);
            else
            {
                int levelIndex = 1;
                if (commandArgs  == 2)
                {
                    levelIndex = 2;
                    string playerToSearch = args[1].ToLower();
                    var activePlayerList = BasePlayer.activePlayerList;
                    var playersFound = (from basePlayer in activePlayerList let displayName = basePlayer.displayName.ToLower() where displayName.Equals(playerToSearch) select basePlayer).ToDictionary(basePlayer => basePlayer.displayName);
                    if (playersFound.Count > 1)
                    {
                        var playerFoundNames = String.Join(",", playersFound.Select(basePlayer => basePlayer.Key).ToArray());
                        ChatMessage(callerPlayer, String.Format("Multiple players found. {0}", playerFoundNames));
                        return;
                    }
                    if (playersFound.Count == 0)
                    {
                        ChatMessage(callerPlayer, "No player found with that name!");
                        return;
                    }
                    player = playersFound.First().Value;
                    rpgInfo = RPGInfo(player);
                }
                int desiredLevel;
                if (!Int32.TryParse(args[levelIndex], out desiredLevel))
                {
                    InvalidCommand(callerPlayer, args);
                    return;
                }
                if (desiredLevel <= rpgInfo.Level) return;
                LevelUpPlayer(rpgInfo, desiredLevel);
                NotifyLevelUp(player, rpgInfo);
                if (callerPlayer != player)
                    ChatMessage(callerPlayer, String.Format("Player {0} lvlup to {1}", player, desiredLevel));
            }
        }

        public void LevelUpPlayer(RPGInfo rpgInfo, int desiredLevel)
        {
            var levelsToUp = desiredLevel - rpgInfo.Level;
            for (int i = 0; i < levelsToUp; i++)
            {
                long requiredXP = RequiredExperience(rpgInfo.Level);
                rpgInfo.AddExperience(requiredXP, requiredXP);
            }
        }

        private void InvalidCommand(BasePlayer player, string[] args)
        {
            ChatMessage(player, MessagesTable.GetMessage(HMK.InvalidCommand, new[] {args[0]}));
        }

        public void ResetRPG()
        {
            foreach (var rpgInfoPair in RPGConfig)
            {
                var rpgInfo = rpgInfoPair.Value;
                if (!rpgInfo.Skills.ContainsKey(HRK.Tamer))
                    continue;
                PluginInstance.RevokeTamePermission(rpgInfoPair.Key, HPK.CanTame);
                PluginInstance.RevokeTamePermission(rpgInfoPair.Key, HPK.CanTameWolf);
                PluginInstance.RevokeTamePermission(rpgInfoPair.Key, HPK.CanTameBear);
            }
            RPGConfig.Clear();
            PlayersFurnaces.Clear();
            PluginInstance.SaveRPG(RPGConfig, PlayersFurnaces);
        }

        public void SaveRPG()
        {
            PluginInstance.SaveRPG(RPGConfig, PlayersFurnaces);
        }

        public void DisplayProfile(BasePlayer player)
        {
            ChatMessage(player, Profile(RPGInfo(player), player));
        }

        public void OnDeath(BasePlayer player)
        {
            RPGInfo(player).Died();
            ChatMessage(player, String.Format("Oh no man! You just died! You lost {0:P} of XP because of this....", HK.DeathReducer));
        }

        public void PlayerInit(BasePlayer player, bool dataWasUpdated)
        {
            if(dataWasUpdated)
                ChatMessage(player, MessagesTable.GetMessage(HMK.DataUpdated));
            DisplayProfile(player);
            var steamId = RPGHelper.SteamId(player);
            if(!PlayerLastPercentChange.ContainsKey(steamId))
                PlayerLastPercentChange.Add(steamId, CurrentPercent(RPGInfo(player)));
        }

        public void OnBuildingBlockUpgrade(BasePlayer player, BuildingBlock buildingBlock, BuildingGrade.Enum grade)
        {
            var items = buildingBlock.blockDefinition.grades[(int) grade].costToBuild;
            int total = items.Sum(item => (int) item.amount);
            int experience = (int) Math.Ceiling(UpgradeBuildingTable[grade]*total);
            ExpGain(RPGInfo(player), experience, player);
        }

        public void OnDeployItem(Deployer deployer, BaseEntity baseEntity)
        {
            var player = deployer.ownerPlayer;
            var item = deployer.GetItem();
            var itemDef = item.info;
            var type = baseEntity.GetType();
            if (type != typeof (BaseOven) || !itemDef.displayName.translated.ToLower().Equals("furnace")) return;
            var baseOven = (BaseOven)baseEntity;
            var instanceId = RPGHelper.OvenId(baseOven);
            if (PlayersFurnaces.ContainsKey(instanceId))
            {
                ChatMessage(player, "Contact the developer, tell him wrong Id usage for furnace.");
                return;
            }
            PlayersFurnaces.Add(instanceId, RPGHelper.SteamId(player));
        }

        public void OnConsumeFuel(BaseOven oven, Item fuel, ItemModBurnable burnable)
        {
            var instanceId = RPGHelper.OvenId(oven);
            if (!PlayersFurnaces.ContainsKey(instanceId))
                return;
            var steamId = Convert.ToUInt64(PlayersFurnaces[instanceId]);
            var player = BasePlayer.FindByID(steamId) ?? BasePlayer.FindSleeping(steamId);
            var rpgInfo = player == null ? RPGInfo(PlayersFurnaces[instanceId]) : RPGInfo(player);
            if (rpgInfo == null)
                return;
            if (!rpgInfo.Skills.ContainsKey(HRK.Blacksmith))
                return;
            var skillPoints = rpgInfo.Skills[HRK.Blacksmith];
            double random = Random(0, 1);
            float skillChance = (float)skillPoints/7;
            float maybeGiveAmount = (float)skillPoints/2;
            int amountToGive = (int) Math.Ceiling(maybeGiveAmount);
            if (random > skillChance)
                return;
            var itemList = oven.inventory.itemList;
            var itensCanMelt = (from item in itemList let itemModCookable = item.info.GetComponent<ItemModCookable>() where itemModCookable != null select item).ToList();
            foreach (var item in itensCanMelt)
            {
                var itemModCookable = item.info.GetComponent<ItemModCookable>();
                oven.inventory.Take(null, item.info.itemid, amountToGive);
                var itemToGive = ItemManager.Create(itemModCookable.becomeOnCooked, amountToGive);
                if (!itemToGive.MoveToContainer(oven.inventory))
                    itemToGive.Drop(oven.inventory.dropPosition, oven.inventory.dropVelocity);
            }
        }

        public bool OnPlayerAttack(BasePlayer player, HitInfo hitInfo)
        {
            var weapon = hitInfo.Weapon.GetOwnerItemDefinition().displayName.translated.ToLower();
            if (SkillTable.ContainsKey(HRK.BlinkArrow))
                if (!SkillTable[HRK.BlinkArrow].Enabled)
                    return false;
            if (!weapon.Equals("hunting bow"))
                return false;
            var steamId = RPGHelper.SteamId(player);
            float availableAt = 0;
            var time = Time.realtimeSinceStartup;
            var rpgInfo = RPGInfo(player);
            if (!PlayerHaveSkill(player, rpgInfo, HRK.BlinkArrow, false)) return false;
            var playerCooldowns = PlayerCooldowns(steamId);
            var isReady = RPGHelper.IsSkillReady(playerCooldowns, ref availableAt, time, HRK.BlinkArrow);
            if (isReady)
            {
                if (rpgInfo.Preferences.AutoToggleBlinkArrow)
                    rpgInfo.Preferences.UseBlinkArrow = true;
                if (!rpgInfo.Preferences.UseBlinkArrow) return false;
                var newPos = PluginInstance.GetGround(hitInfo.HitPositionWorld);
                if (newPos == null)
                {
                    ChatMessage(player, "Can't blink there!");
                    return false;
                }
                var position = (Vector3)newPos;
                var buildingBlock = GetBuildingBlock(position);
                if (buildingBlock != null)
                {
                    if (!PluginInstance.IsOwner(buildingBlock, player))
                    {
                        ChatMessage(player, "Can't blink to other player house!");
                        return false;
                    }
                }
                PluginInstance.TeleportPlayerTo(player, position);
                SetCooldown(rpgInfo, time, playerCooldowns, HRK.BlinkArrow);
                return true;
            }
            if (!rpgInfo.Preferences.UseBlinkArrow) return false;
            ChatMessage(player, String.Format("Blinked recently! You might get dizzy, give it a rest. Time left to blink again: {0}", RPGHelper.TimeLeft(availableAt, time)));
            if (rpgInfo.Preferences.AutoToggleBlinkArrow)
                rpgInfo.Preferences.UseBlinkArrow = false;
            return false;
        }

        public object GetBuildingBlock(Vector3 position)
        {
            var hits = Physics.OverlapSphere(position, 3f);
            foreach (var hit in hits)
            {
                if (hit.GetComponentInParent<BuildingBlock>() != null)
                {
                    return hit.GetComponentInParent<BuildingBlock>();
                }
            }
            return null;
        }

        //public void OnItemAddedToContainer(ItemContainer itemContainer, Item item)
        //{
        //    var player = itemContainer.playerOwner;
        //    if(player == null) return;
        //    item.info.displayDescription
        //}
    }

    public static class HuntTablesGenerator
    {
        public static Dictionary<int, long> GenerateXPTable(int maxLevel, int baseExp, float levelMultiplier, int levelModule, float moduleReducer)
        {
            var xpTable = new Dictionary<int, long>();
            long previousLevel = baseExp;
            xpTable.Add(0, baseExp);
            for (int i = 0; i < maxLevel; i++)
            {
                if (i%levelModule == 0)
                    levelMultiplier -= moduleReducer;
                long levelRequiredXp = (long)(previousLevel * levelMultiplier);
                xpTable.Add(i+1, levelRequiredXp);
                previousLevel = levelRequiredXp;
            }
            return xpTable;
        }

        public static PluginMessagesConfig GenerateMessageTable()
        {
            var messagesConfig = new PluginMessagesConfig("Hunt", "lightblue");
            messagesConfig.AddMessage(HMK.Help, new List<string>
            {
                "To get an overview about the Hunt RPG, type \"/hunt about\"",
                "To see you available shortcuts commdands, type \"/hunt shortcuts\"",
                "To see you player profile, type \"/hunt profile\"",
                "To see you current xp, type \"/hunt xp\"",
                "To see how to change you profile preferences, type \"/hunt profilepreferences\"",
                "To see you current health, type \"/hunt health\"",
                "To see the skill list type \"/hunt skilllist\"",
                "To see info about a specific skill type \"/hunt skill <skillname>\"",
                "To spend your available stats points, type \"/hunt statset <stats> <points> \". Ex: /hunt statset agi 3",
                "To spend your available skill points, type \"/hunt skillset <skillname> <points> \". Ex: /hunt skillset lumberjack 1",
            });
            messagesConfig.AddMessage(HMK.Shortcuts, new List<string>
            {
                "\"/hunt\" = \"/h\"",
                "\"/hunt profile\" = \"/h p\"",
                "\"/hunt profilepreferences\" = \"/h pp\"",
                "\"/hunt statset\" = \"/h sts\".",
                "You can set multiple stats at a time like this \"/h sts agi 30 str 45\".",
                "\"/hunt skillset\" = \"/h sks\"",
                "You can set multiple skillpoints at a time like this \"/h sks lumberjack 3 miner 2\".",
                "\"/hunt health\" = \"/h h\"",
            });
            messagesConfig.AddMessage(HMK.ProfilePreferences, new List<string>()
            {
                "To see change the % changed need to show the xp message, type \"/hunt xp% <percentnumber>\"",
                "To toggle crafting message type \"/hunt craftmsg\"",
                "To toggle blink arrow skill type \"/hunt ba\"",
                "To toggle blink arrow skill auto toggle type \"/hunt aba\"",
            });         
            messagesConfig.AddMessage(HMK.About, HuntAbout());
            messagesConfig.AddMessage(HMK.DataUpdated, RPGHelper.WrapInColor("Plugin was updated to new version!", OC.Yellow));
            messagesConfig.AddMessage(HMK.DataUpdated, RPGHelper.WrapInColor("Your profile needed to be reset, but your level was saved. You just need to redistribute."));
            messagesConfig.AddMessage(HMK.DataUpdated, RPGHelper.WrapInColor("Furnaces were not saved though, so build new ones for the blacksmith skill to be applied (If you have, or when you get it)!", OC.Red));
            messagesConfig.AddMessage(HMK.InvalidCommand, "You ran the \"{0}\" command incorrectly. Type \"/hunt\" to get help");
            messagesConfig.AddMessage(HMK.SkillInfo, "Type \"/hunt skill <skillname>\" to see the skill info");
            messagesConfig.AddMessage(HMK.NotEnoughtPoints, RPGHelper.WrapInColor("You don't have enought points to set!"));
            messagesConfig.AddMessage(HMK.NotEnoughLevels, RPGHelper.WrapInColor("You dont have the minimum level to learn this skill!"));
            messagesConfig.AddMessage(HMK.NotEnoughStrength, RPGHelper.WrapInColor("You dont have enough strenght to learn this skill!"));
            messagesConfig.AddMessage(HMK.NotEnoughAgility, RPGHelper.WrapInColor("You dont have enough agility to learn this skill!"));
            messagesConfig.AddMessage(HMK.NotEnoughIntelligence, RPGHelper.WrapInColor("You dont have enough intelligence to learn this skill!"));
            messagesConfig.AddMessage(HMK.InvalidSkillName, RPGHelper.WrapInColor("There is no such skill! Type \"/hunt skilllist\" to see the available skills"));
            messagesConfig.AddMessage(HMK.SkillDisabled, RPGHelper.WrapInColor("This skill is blocked in this server."));
            messagesConfig.AddMessage(HMK.ItemNotFound, RPGHelper.WrapInColor("Item {0} not found."));
            messagesConfig.AddMessage(HMK.ResearchBlocked, RPGHelper.WrapInColor("Item {0} research is blocked by in this server."));
            messagesConfig.AddMessage(HMK.SkillNotLearned, RPGHelper.WrapInColor("You havent learned this skill yet."));
            messagesConfig.AddMessage(HMK.AlreadyAtMaxLevel, RPGHelper.WrapInColor("You have mastered this skill already!"));
            return messagesConfig;
        }

        private static List<string> HuntAbout()
        {
            var aboutMessages = new List<string>();
            aboutMessages.Add("=================================================");
            aboutMessages.Add("The Hunt RPG system in development.");
            aboutMessages.Add("It is consisted of levels, stats atributes, skills and later on specializations.");
            aboutMessages.Add("Currently there are 3 attributes, each of then give you and specific enhancement.");
            aboutMessages.Add("Strenght gives you more health, it will not be displayed in the Health Bar, but it is considered for healing and getting hurt.");
            aboutMessages.Add("Agillity gives you dodge change");
            aboutMessages.Add("Intelligence decreases your items crafting time");
            aboutMessages.Add("Right now you can level up by gathering resources.");
            aboutMessages.Add("Each level gives you 1 point in each attribute. And 3 more to distribute.");
            aboutMessages.Add("Each level gives you 1 skill point to distribute");
            aboutMessages.Add("Each skill have its required level, and later on it will require specific stats.");
            aboutMessages.Add("To see the all the available skills and its description type \"/hunt skilllist\"");
            aboutMessages.Add("To learn more about Hunt RPG go to the plugin page at <link>");
            aboutMessages.Add("=================================================");
            return aboutMessages;
        }


        public static Dictionary<string, Skill> GenerateSkillTable()
        {
            var skillTable = new Dictionary<string, Skill>();
            var lumberJack = new Skill(HRK.LumberJack, "This skill allows you to gather wood faster. Each point gives you 10% more wood per hit.", 0, 20);
            var woodAndFleshModifier = new Modifier(HRK.GatherModifier, new List<object>(){10});
            lumberJack.AddModifier(HRK.GatherModifier, woodAndFleshModifier);
            skillTable.Add(HRK.LumberJack, lumberJack);
            var miner = new Skill(HRK.Miner, "This skill allows you to gather stones faster. Each point gives you 5% more stones per hit.", 0, 20);
            miner.AddModifier(HRK.GatherModifier, new Modifier(HRK.GatherModifier, new List<object>(){5}));
            skillTable.Add(HRK.Miner, miner);
            var hunter = new Skill(HRK.Hunter, "This skill allows you to gather resources faster from animals. Each point gives you 10% more resources per hit.", 0, 20);
            hunter.AddModifier(HRK.GatherModifier, woodAndFleshModifier);
            skillTable.Add(HRK.Hunter, hunter);
            var researcher = new Skill(HRK.Researcher, "This skill allows you to research items you have. Each level enables a type of type to be researched and decreases 2 minutes of cooldown. Table: Level 1 - Tools (10 min); Level 2 - Clothes (8 min); Level 3 - Construction and Resources (6 min); Level 4 - Ammunition and Medic (4 min); Level 5 - Weapons (2 min)", 30, 5);
            researcher.SkillpointsPerLevel = 7;
            researcher.Usage = "To research an item type \"/research \"Item Name\" \". In order to research an item, you must have it on your invetory, and have the required skill level for that item tier.";
            researcher.AddRequiredStat("int", (int) Math.Floor(researcher.RequiredLevel*2.5d));
            researcher.AddModifier(HRK.CooldownModifier, new Modifier(HRK.CooldownModifier, new List<object>() {10,2}));
            skillTable.Add(HRK.Researcher, researcher);
            var blacksmith = new Skill(HRK.Blacksmith, "This skill allows your furnaces to melt more resources each time. Each level gives increase the productivity by 1.",30, 5);
            blacksmith.SkillpointsPerLevel = 7;
            blacksmith.AddRequiredStat("str", (int)Math.Floor(blacksmith.RequiredLevel * 2.5d));
            skillTable.Add(HRK.Blacksmith, blacksmith);
            var blinkarrow = new Skill(HRK.BlinkArrow, "This skill allows you to blink to your arrow destination from time to time. Each level deacreases the cooldown in 2 minutes.", 150, 5);
            blinkarrow.Usage = "Just shoot an Arrow at desired blink location. To toogle this skill type \"/h ba\" . To change the auto toggle for this skill type \"/h aba\"";
            blinkarrow.AddModifier(HRK.CooldownModifier, new Modifier(HRK.CooldownModifier, new List<object>() {9, 2}));
            blinkarrow.SkillpointsPerLevel = 10;
            blinkarrow.AddRequiredStat("agi", (int)Math.Floor(blinkarrow.RequiredLevel * 2.5d));
            blinkarrow.Enabled = false;
            skillTable.Add(HRK.BlinkArrow, blinkarrow);
            var tamer = new Skill(HRK.Tamer, "This skill allows you to tame a animal as your pet. Level 1 allows chicken, level 2 allows boar, level 3 allows stag, level 4 allows wolf, level 5 allows bear.", 50, 5);
            tamer.SkillpointsPerLevel = 5;
            tamer.Usage = "Type \"/pet \" to toggle taming. To tame get close to the animal and press your USE button(E). After tamed press USE looking at something, if its terrain he will move, if its a player or other animal it he will attack. If looking at him it will start following you. To set the pet free type \"/pet free\".";
            skillTable.Add(HRK.Tamer, tamer);
            return skillTable;
        }

        public static Dictionary<string, ItemInfo> GenerateItemTable()
        {
            var itemDict = new Dictionary<string, ItemInfo>();
            var itemsDefinition = ItemManager.GetItemDefinitions();
            foreach (var itemDefinition in itemsDefinition)
            {
                var newInfo = new ItemInfo {Shortname = itemDefinition.shortname, CanResearch = true, ItemId = itemDefinition.itemid, ItemCategory = itemDefinition.category.ToString()};
                var blueprint = ItemManager.FindBlueprint(itemDefinition);
                if (blueprint != null)
                    newInfo.BlueprintTime = blueprint.time;
                itemDict.Add(itemDefinition.displayName.translated.ToLower(), newInfo);
            }
            return itemDict;
        }

        public static Dictionary<string, int> GenerateResearchTable()
        {
            var researchTable = new Dictionary<string, int>
            {
                {"Tool", 1},
                {"Attire", 2},
                {"Construction", 3},
                {"Resources", 3},
                {"Medical", 4},
                {"Ammunition", 4},
                {"Weapon", 5}
            };
            return researchTable;
        }

        public static Dictionary<int, string> GenerateTameTable()
        {
            var tameTable = new Dictionary<int, string>
            {
                {1, HPK.CanTameChicken},
                {2, HPK.CanTameBoar},
                {3, HPK.CanTameStag},
                {4, HPK.CanTameWolf},
                {5, HPK.CanTameBear}
            };
            return tameTable;
        }

        public static Dictionary<BuildingGrade.Enum, float> GenerateUpgradeBuildingTable()
        {
            var upgradeBuildingTable = new Dictionary<BuildingGrade.Enum, float>();
            upgradeBuildingTable.Add(BuildingGrade.Enum.Twigs, 1f);
            upgradeBuildingTable.Add(BuildingGrade.Enum.Wood, 1.5f);
            upgradeBuildingTable.Add(BuildingGrade.Enum.Stone, 3f);
            upgradeBuildingTable.Add(BuildingGrade.Enum.Metal, 10f);
            upgradeBuildingTable.Add(BuildingGrade.Enum.TopTier, 3f);
            return upgradeBuildingTable;
        }

        public static Dictionary<string, float> GenerateMaxStatsTable()
        {
            var maxStatsTable = new Dictionary<string, float>();
            maxStatsTable.Add(HRK.StrBlockGain, 0.00095f);
            maxStatsTable.Add(HRK.AgiEvasionGain, 0.000625f);
            maxStatsTable.Add(HRK.IntCraftingReducer, 0.001f);
            return maxStatsTable;
        }
    }


    public class ItemInfo
    {
        public int ItemId { get; set; }
        public string Shortname { get; set; }
        public float BlueprintTime { get; set; }
        public bool CanResearch { get; set; }
        public string ItemCategory { get; set; }
    }

    public class PluginMessagesConfig
    {
        public Dictionary<string, List<string>> Messages { set; get; }
        public string ChatPrefix { set; get; }
        public string ChatPrefixColor { set; get; }

        public PluginMessagesConfig(string chatPrefix, string chatPrefixColor)
        {
            Messages = new Dictionary<string, List<string>>();
            ChatPrefix = chatPrefix;
            ChatPrefixColor = chatPrefixColor;
        }

        public void AddMessage(string key, string message)
        {
            if (Messages.ContainsKey(key))
                Messages[key].Add(message);
            else
                Messages.Add(key, new List<string> {message});
        }

        public void AddMessage(string key, List<string> message)
        {
            if (Messages.ContainsKey(key))
                Messages[key].AddRange(message);
            else
                Messages.Add(key, message);
        }

        public List<string> GetMessage(string key, string[] args = null)
        {
            var strings = new List<string>();
            if (!Messages.ContainsKey(key)) return strings;
            var messageList = Messages[key];
            strings.AddRange(messageList.Select(message => args == null ? message : string.Format(message, args)));
            return strings;
        }
    }

    public class ProfilePreferences
    {
        public ProfilePreferences()
        {
            ShowXPMessagePercent = 0.25f;
            ShowCraftMessage = true;
            UseBlinkArrow = true;
            AutoToggleBlinkArrow = true;
        }

        public float ShowXPMessagePercent { get; set; }
        public bool ShowCraftMessage { get; set; }
        public bool UseBlinkArrow { get; set; }
        public bool AutoToggleBlinkArrow { get; set; }
    }

    public static class RPGHelper
    {
        public static string SteamId(BasePlayer player)
        {
            return player.userID.ToString();
        }

        public static void SkillInfo(StringBuilder sb, Skill skill)
        {
            if (!skill.Enabled) return;
            sb.AppendLine(String.Format("{0} - Required Level: {1}", RPGHelper.WrapInColor(skill.Name, OC.LightBlue), skill.RequiredLevel));
            if (skill.SkillpointsPerLevel > 1)
                sb.AppendLine(String.Format("Each skill level costs {0} skillpoints",
                    skill.SkillpointsPerLevel));

            if (skill.RequiredStats.Count > 0)
            {
                StringBuilder sbs = new StringBuilder();
                foreach (var requiredStat in skill.RequiredStats)
                    sbs.Append(String.Format("{0}: {1} |", requiredStat.Key, requiredStat.Value));
                sb.AppendLine(String.Format("Required stats: {0}", sbs));
            }
            sb.AppendLine(String.Format("{0}", skill.Description));
            if (skill.Usage != null)
                sb.AppendLine(String.Format("{0}{1}",RPGHelper.WrapInColor("Usage: ", OC.Teal) ,skill.Usage));
            sb.AppendLine("-----------------");
        }

        public static string WrapInColor(string msg, string color=OC.Orange)
        {
            return String.Format("<color={1}>{0}</color>", msg, color);
        }

        public static float GetEvasion(RPGInfo rpgInfo, float pointMultiplier)
        {
            return rpgInfo.Agility * pointMultiplier;
        }

        public static float GetBlock(RPGInfo rpgInfo, float pointMultiplier)
        {
            return rpgInfo.Strength * pointMultiplier;
        }

        public static float GetCraftingReducer(RPGInfo rpgInfo, float pointMultiplier)
        {
            return rpgInfo.Intelligence * pointMultiplier;
        }

        public static string TimeLeft(float availableAt, float time)
        {
            var timeLeft = availableAt - time;
            var formatableTime = new DateTime(TimeSpan.FromSeconds(timeLeft).Ticks);
            var formatedTimeLeft = String.Format("{0:mm\\:ss}", formatableTime);
            return formatedTimeLeft;
        }

        public static bool IsSkillReady(Dictionary<string, float> playerCooldowns, ref float availableAt, float time, string skillKey)
        {
            bool isReady;
            if (playerCooldowns.ContainsKey(skillKey))
            {
                availableAt = playerCooldowns[skillKey];
                isReady = time >= availableAt;
            }
            else
            {
                isReady = true;
                playerCooldowns.Add(skillKey, availableAt);
            }
            return isReady;
        }

        public static string OvenId(BaseOven oven)
        {
            var position = oven.transform.position;
            return String.Format("X{0}Y{1}Z{2}", position.x, position.y, position.z);
        }
    }

    public class RPGInfo
    {
        public RPGInfo(string steamName)
        {
            SteamName = steamName;
            Level = 0;
            Skills = new Dictionary<string, int>();
            Preferences = new ProfilePreferences();
        }

        public bool AddExperience(long xp,long requiredXp)
        {
            Experience += xp;
            if (Experience < requiredXp) return false;
            if (Level == HK.MaxLevel) return false;
            Experience = Experience-requiredXp;
            LevelUp();
            return true;
        }

        public void Died()
        {
            var removedXP = (long)(Experience*HK.DeathReducer);
            Experience -= removedXP;
            if (Experience < 0)
                Experience = 0;
        }

        private void LevelUp()
        {
            Level++;
            Agility++;
            Strength++;
            Intelligence++;
            StatsPoints += 3;
            SkillPoints += 1;
        }

        public bool AddAgi(int points)
        {
            int absPoints = Math.Abs(points);
            if (StatsPoints < absPoints) return false;
            StatsPoints -= absPoints;
            Agility += absPoints;
            return true;
        }

        public bool AddStr(int points)
        {
            int absPoints = Math.Abs(points);
            if (StatsPoints < absPoints) return false;
            StatsPoints -= absPoints;
            Strength += absPoints;
            return true;
        }

        public bool AddInt(int points)
        {
            int absPoints = Math.Abs(points);
            if (StatsPoints < absPoints) return false;
            StatsPoints -= absPoints;
            Intelligence += absPoints;
            return true;
        }

        public int AddSkill(Skill skill, int points, out string reason)
        {
            int pointsToAdd = Math.Abs(points);
            var requiredPoints = pointsToAdd * skill.SkillpointsPerLevel;
            if (SkillPoints < requiredPoints)
            {
                reason = HMK.NotEnoughtPoints;
                return 0;
            }
            if (Level < skill.RequiredLevel)
            {
                reason = HMK.NotEnoughLevels;
                return 0;
            }
            foreach (var requiredStat in skill.RequiredStats)
            {
                switch (requiredStat.Key.ToLower())
                {
                    case "str":
                        if (Strength < requiredStat.Value)
                        {
                            reason = HMK.NotEnoughStrength;
                            return 0;
                        }
                    break;
                    case "agi":
                        if (Agility < requiredStat.Value)
                        {
                            reason = HMK.NotEnoughAgility;
                            return 0;
                        }
                    break;
                    case "int":
                        if (Intelligence < requiredStat.Value)
                        {
                            reason = HMK.NotEnoughIntelligence;
                            return 0;
                        }
                        break;
                }
            }
            if (Skills.ContainsKey(skill.Name))
            {
                int existingPoints = Skills[skill.Name];
                if (existingPoints + points > skill.MaxPoints)
                    pointsToAdd = skill.MaxPoints - existingPoints;
                if(pointsToAdd >  0)
                    Skills[skill.Name] += pointsToAdd;
            }
            else
            {
                if (points > skill.MaxPoints)
                    pointsToAdd = skill.MaxPoints;
                Skills.Add(skill.Name, pointsToAdd);
            }
            
            if (pointsToAdd <= 0)
            {
                reason = HMK.AlreadyAtMaxLevel;
                return 0;
            }
            reason = "";
            SkillPoints -= pointsToAdd * skill.SkillpointsPerLevel;
            return pointsToAdd;
        }

        public string SteamName { get; set; }
        public int Level { get; set; }
        public long Experience { get; set; }
        public int Agility { get; set; }
        public int Strength { get; set; }
        public int Intelligence { get; set; }
        public int StatsPoints { get; set; }
        public int SkillPoints { get; set; }
        public Dictionary<string,int> Skills { get; set; }

        public ProfilePreferences Preferences { get; set; }

    }

    public class Skill
    {
        public Skill(string name, string description, int requiredLevel, int maxPoints)
        {
            Name = name;
            Enabled = true;
            Description = description;
            RequiredLevel = requiredLevel;
            MaxPoints = maxPoints;
            RequiredSkills = new Dictionary<string, int>();
            Modifiers = new Dictionary<string, Modifier>();
            RequiredStats = new Dictionary<string, int>();
            SkillpointsPerLevel = 1;
        }

        public void AddRequiredStat(string stat, int points)
        {
            if(!RequiredStats.ContainsKey(stat))
                RequiredStats.Add(stat, points);
        }

        public void AddRequiredSkill(string skillName, int pointsNeeded)
        {
            if (!RequiredSkills.ContainsKey(skillName))
                RequiredSkills.Add(skillName, pointsNeeded);
        }

        public void AddModifier(string modifier, Modifier handler)
        {
            if (!Modifiers.ContainsKey(modifier))
                Modifiers.Add(modifier, handler);
        }

        public string Name { get; set; }
        public bool Enabled { get; set; }
        public string Description { get; set; }
        public string Usage { get; set; }
        public int RequiredLevel { get; set; }
        public int MaxPoints { get; set; }
        public Dictionary<string,int> RequiredSkills { get; set; }
        public Dictionary<string, Modifier> Modifiers { get; set;}
        public Dictionary<string,int> RequiredStats { get; set; }
        public int SkillpointsPerLevel { get; set; }
    }

    public class Modifier
    {
        public Modifier(string identifier, List<object> args)
        {
            Identifier = identifier;
            Args = args;
        }

        public string Identifier { get; set; }

        public List<object> Args { get; set; }
    }
 


    public class SkillMethods
    {
        const string IncorrectNumberOfParameters = "Incorrect number of parameters";

        public static int GatherModifier(int skillpoints, int levelmodule, int itemamount)
        {
            var baseMultiplier = (float)skillpoints / (float) levelmodule;
            baseMultiplier += 1;
            float newAmount = (float) (baseMultiplier * (float) itemamount);
            return (int)Math.Ceiling(newAmount);
        }
        
        public static float CooldownModifier(int skillpoints, int basecooldown, int levelmodule, float currenttime)
        {
            float baseCooldown = basecooldown* 60;
            float timeToReduce = ((skillpoints - 1) * levelmodule) * 60;
            float finalCooldown = baseCooldown - timeToReduce;
            return finalCooldown + currenttime;
        }
    }
}

namespace Hunt.RPG.Keys
{
    static class HK
    {
        public const string ConfigVersion = "VERSION";
        public const string DataVersion = "DATA_VERSION";
        public const string DataFileName = "Hunt_Data";
        public const string Profile = "PROFILE";
        public const string Furnaces = "FURNACES";
        public const string MessagesTable = "MESSAGESTABLE";
        public const string XPTable = "XPTABLE";
        public const string MaxStatsTable = "MAXSTATSTABLE";
        public const string SkillTable = "SKILLTABLE";
        public const string ItemTable = "ITEMTABLE";
        public const string ResearchSkillTable = "RESEARCHSKILLTABLE";
        public const string TameTable = "TAMETABLE";
        public const string UpgradeBuildTable = "UPGRADEBUILDTABLE";
        public const int MaxLevel = 200;
        public const int BaseXP = 383;
        public const float LevelMultiplier = 1.105f;
        public const int LevelModule = 10;
        public const float ModuleReducer = 0.005f;
        public const float DeathReducer = 0.05f;
    }
    static class HMK
    {
        public const string SkillDisabled = "skill_disabled";
        public const string ResearchBlocked = "research_blocked";
        public const string ProfilePreferences = "preferences";
        public const string Help = "help";
        public const string Shortcuts = "hunt_shortcuts";
        public const string DataUpdated = "data_updated";
        public const string AlreadyAtMaxLevel = "already_at_max_level";
        public const string SkillInfo = "skill_info";
        public const string NotEnoughIntelligence = "not_enought_int";
        public const string NotEnoughAgility = "not_enought_agi";
        public const string NotEnoughStrength = "not_enought_str";
        public const string NotEnoughLevels = "not_enought_levels";
        public const string About = "hunt_about";
        public const string SkillNotLearned = "skill_not_learner";
        public const string ItemNotFound = "item_not_found";
        public const string InvalidCommand = "invalid_command";
        public const string NotEnoughtPoints = "not_enought_points";
        public const string InvalidSkillName = "invalid_skill_name";
    }

    public static class HPK
    {
        public const string CanTame = "cannpc";
        public const string CanTameChicken = "canchicken";
        public const string CanTameBoar = "canboar";
        public const string CanTameStag = "canstag";
        public const string CanTameWolf = "canwolf";
        public const string CanTameBear = "canbear";
    }
    static class HRK
    {
        public const string Tamer = "tamer";
        public const string BlinkArrow = "blinkarrow";
        public const string Blacksmith = "blacksmith";
        public const string Researcher = "researcher";
        public const string LumberJack = "lumberjack";
        public const string Miner = "miner";
        public const string Hunter = "hunter";
        public const string GatherModifier = "gather";
        public const string CooldownModifier = "cooldown";
        public const string IntCraftingReducer = "int_crafting_reducer_percent";
        public const string AgiEvasionGain = "agi_evasion_percent_gain";
        public const string StrBlockGain = "str_block_percent_gain";
    }
    public static class OC
    {
        public const string Aqua = "aqua";
        public const string Black = "black";
        public const string Blue = "blue";
        public const string Brown = "brown";
        public const string Cyan = "cyan";
        public const string DarkBlue = "darkblue";
        public const string Magenta = "magenta";
        public const string Green = "green";
        public const string LightBlue = "lightblue";
        public const string Maroon = "maroon";
        public const string Navy = "navy";
        public const string Olive = "olive";
        public const string Orange = "orange";
        public const string Purple = "purple";
        public const string Red = "red";
        public const string Teal = "teal";
        public const string Yellow = "yellow";
    }
}

