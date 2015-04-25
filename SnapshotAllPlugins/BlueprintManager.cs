// Reference: Oxide.Ext.Rust

using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("BlueprintManager", "Nogrod", "1.0.4", ResourceId = 833)]
    class BlueprintManager : RustPlugin
    {
        private readonly Dictionary<string, string> _itemShortname = new Dictionary<string, string>();
        private int _authLevel = 2;
        private int _authLevelOther = 2;
        private bool _giveAllOnConnect;
        private bool _configChanged;

        void OnServerInitialized()
        {
            _authLevel = Convert.ToInt32(GetConfig("authLevel", 2));
            _authLevelOther = Convert.ToInt32(GetConfig("authLevelOther", 2));
            _giveAllOnConnect = Convert.ToBoolean(GetConfig("giveAllOnConnect", false));
            if (_configChanged) {
                SaveConfig();
                _configChanged = false;
            }
            InitializeShortname();
        }

        void LoadDefaultConfig()
        {
            GetConfig("authLevel", 2);
            GetConfig("authLevelOther", 2);
            GetConfig("giveAllOnConnect", false);
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (_giveAllOnConnect)
            {
                var definitions = ItemManager.GetItemDefinitions();
                foreach (var definition in definitions)
                {
                    player.blueprints.Learn(definition);
                }
                SendReply(player, "You learned all blueprints");
            }
        }

        private void InitializeShortname()
        {
            _itemShortname.Clear();
            var definitions = ItemManager.GetItemDefinitions();
            foreach (var definition in definitions)
            {
                _itemShortname.Add(definition.displayName.english.ToLower(), definition.shortname);
            }
        }

        bool CheckAccess(BasePlayer player, int authLevel)
        {
            if (player.net.connection.authLevel >= authLevel)
            {
                return true;
            }
            SendReply(player, "You are not allowed to use this command");
            return false;
        }

        private object GetConfig(string key, object defaultValue)
        {
            if (Config[key] != null) return Config[key];
            Config[key] = defaultValue;
            _configChanged = true;
            return Config[key];
        }

        [ChatCommand("bpadd")]
        void cmdChatBpAdd(BasePlayer player, string command, string[] args)
        {
            if (!CheckAccess(player, _authLevel)) return;
            if ((args == null) || (args.Length == 0))
            {
                SendReply(player, "/bpadd \"Item\" [\"PlayerName\"]");
                return;
            }
            LocalPuts(string.Format("{0} used /bpadd {1}", player.displayName, string.Join(" ", args)));
            var name = args[0];
            name = name.ToLower();
            if (_itemShortname.ContainsKey(name))
                name = _itemShortname[name];
            var definition = ItemManager.FindItemDefinition(name);
            if (definition == null)
            {
                SendReply(player, string.Format("Item does not exist: {0}", name));
                return;
            }
            var targetPlayer = player;
            if (args.Length > 1)
            {
                if (!CheckAccess(player, _authLevelOther)) return;
                targetPlayer = BasePlayer.Find(args[1]);
                if (targetPlayer == null)
                {
                    SendReply(player, string.Format("Player not found: {0}", args[1]));
                    return;
                }
            }
            targetPlayer.blueprints.Learn(definition);
            SendReply(targetPlayer, string.Format("You learned {0}", definition.displayName.translated));
            if (targetPlayer != player)
            {
                SendReply(player, string.Format("{0} learned {1}", targetPlayer.displayName, definition.displayName.translated));
            }
        }

        [ChatCommand("bpall")]
        void cmdChatBpAll(BasePlayer player, string command, string[] args)
        {
            if (!CheckAccess(player, _authLevel)) return;
            LocalPuts(string.Format("{0} used /bpall {1}", player.displayName, string.Join(" ", args)));
            var targetPlayer = player;
            if (args != null && args.Length > 0)
            {
                if (!CheckAccess(player, _authLevelOther)) return;
                targetPlayer = BasePlayer.Find(args[0]);
                if (targetPlayer == null)
                {
                    SendReply(player, string.Format("Player not found: {0}", args[0]));
                    return;
                }
            }
            var definitions = ItemManager.GetItemDefinitions();
            foreach (var definition in definitions)
            {
                targetPlayer.blueprints.Learn(definition);
            }
            SendReply(targetPlayer, "You learned all blueprints");
            if (targetPlayer != player)
            {
                SendReply(player, string.Format("{0} learned all blueprints", targetPlayer.displayName));
            }
        }

        [ChatCommand("bpreset")]
        void cmdChatBpReset(BasePlayer player, string command, string[] args)
        {
            if (!CheckAccess(player, _authLevel)) return;
            LocalPuts(string.Format("{0} used /bpreset {1}", player.displayName, string.Join(" ", args)));
            var targetPlayer = player;
            if (args != null && args.Length > 0)
            {
                if (!CheckAccess(player, _authLevelOther)) return;
                targetPlayer = BasePlayer.Find(args[0]);
                if (targetPlayer == null)
                {
                    SendReply(player, string.Format("Player not found: {0}", args[0]));
                    return;
                }
            }
            var data = Persistence.GetPlayerInfo(targetPlayer.userID);
            data.blueprints = null;
            PlayerBlueprints.InitializePersistance(data);
            targetPlayer.SendFullSnapshot();
            SendReply(targetPlayer, "You forgot all blueprints");
            if (targetPlayer != player)
            {
                SendReply(player, string.Format("{0} forgot all blueprints", targetPlayer.displayName));
            }
        }

        [ChatCommand("bpaddall")]
        void cmdChatBpAddAll(BasePlayer player, string command, string[] args)
        {
            if (!CheckAccess(player, _authLevelOther)) return;
            LocalPuts(string.Format("{0} used /bpaddall {1}", player.displayName, string.Join(" ", args)));
            List<ItemDefinition> definitions;
            if (args != null && args.Length > 0)
            {
                definitions = new List<ItemDefinition>();
                foreach (var arg in args)
                {
                    foreach (var def in arg.Split(','))
                    {
                        var name = def.ToLower();
                        if (_itemShortname.ContainsKey(name))
                            name = _itemShortname[name];
                        var itemDef = ItemManager.FindItemDefinition(name);
                        if (itemDef == null)
                        {
                            SendReply(player, string.Format("Item not found: {0}", def));
                            return;
                        }
                        definitions.Add(itemDef);
                    }
                }
            }
            else
            {
                definitions = ItemManager.GetItemDefinitions();
            }
            var players = BasePlayer.activePlayerList;
            foreach (var basePlayer in players)
            {
                foreach (var definition in definitions)
                {
                    basePlayer.blueprints.Learn(definition);
                }
                SendReply(basePlayer, "You learned all blueprints");
                if (basePlayer != player)
                {
                    SendReply(player, string.Format("{0} learned all blueprints", basePlayer.displayName));
                }
            }
        }

        [ChatCommand("bpremoveall")]
        void cmdChatBpRemoveAll(BasePlayer player, string command, string[] args)
        {
            if (!CheckAccess(player, _authLevelOther)) return;
            LocalPuts(string.Format("{0} used /bpremoveall {1}", player.displayName, string.Join(" ", args)));
            List<int> definitions;
            if (args != null && args.Length > 0)
            {
                definitions = new List<int>();
                foreach (var arg in args)
                {
                    foreach (var def in arg.Split(','))
                    {
                        var name = def.ToLower();
                        if (_itemShortname.ContainsKey(name))
                            name = _itemShortname[name];
                        var itemDef = ItemManager.FindItemDefinition(name);
                        if (itemDef == null)
                        {
                            SendReply(player, string.Format("Item not found: {0}", def));
                            return;
                        }
                        definitions.Add(itemDef.itemid);
                    }
                }
            }
            else
            {
                definitions = ItemManager.GetItemDefinitions().ConvertAll(i => i.itemid);
            }
            var defaultBlueprints = new List<int>(SingletonComponent<ItemManager>.Instance.defaultBlueprints);
            definitions.RemoveAll(d => defaultBlueprints.Contains(d));
            var players = BasePlayer.activePlayerList;
            foreach (var basePlayer in players)
            {
                var data = Persistence.GetPlayerInfo(basePlayer.userID);
                if (data.blueprints.complete.RemoveAll(a => definitions.Contains(a)) > 0)
                    basePlayer.SendFullSnapshot();
            }
            SendReply(player, "Removed learned blueprints");
        }

        private void LocalPuts(string msg)
        {
            Puts("{0}: {1}", Title, msg);
        }
    }
}
