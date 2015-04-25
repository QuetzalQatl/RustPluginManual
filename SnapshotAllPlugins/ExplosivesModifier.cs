using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{

    [Info("Explosives Modifier", "Mughisi", "1.1.1")]
    class ExplosivesModifier : RustPlugin
    {

        #region Configuration Data

        bool configChanged;

        // Explosive damage values.
        string defaultChatPrefix = "Bomb Squad";
        string defaultChatPrefixColor = "#008000ff";

        string chatPrefix;
        string chatPrefixColor;

        // Timed Explosive settings
        float defaultTimedExplosiveDamageModifier = 100;
        float defaultTimedExplosiveRadiusModifier = 100;

        float timedExplosiveDamageModifier;
        float timedExplosiveRadiusModifier;

        // F1 Grenade settings
        float defaultF1GrenadeDamageModifier = 100;
        float defaultF1GrenadeRadiusModifier = 100;
        bool defaultStickyGrenades = false;

        float f1GrenadeDamageModifier;
        float f1GrenadeRadiusModifier;
        bool stickyGrenades;

        // Messages
        string defaultHelpTextPlayerTimedExplosives = "Timed Explosives deal {0}% of their normal damage and their radius is set to {1}%.";
        string defaultHelpTextPlayerF1Grenades = "F1 Grenades deal {0}% of their normal damage and their radius is set to {1}%.\r\nSticky grenades are {2}.";
        string defaultHelpTextAdmin = "Modify explosive and grenade damage by using one of the following commands: \r\n" +
                                      " /explosivedamage <type:timed|grenade> <value:percentage> \r\n" +
                                      "  examples: '/explosivedamage timed 50' - For 50% of normal explosives damage \r\n" +
                                      "            '/explosivedamage grenade 200' - For 200% of normal grenade damage \r\n" +
                                      "\r\n" +
                                      "Modify explosive and grenade damage radius by using one of the following commands: \r\n" +
                                      " /explosiveradius <type:timed|grenade> <value:percentage> \r\n" +
                                      "  examples: '/explosiveradius timed 200' - For 200% of normal explosive explosion radius \r\n" +
                                      "            '/explosivedamage grenade 50' - For 50% of normal grenade explosion radius" +
                                      "\r\n" +
                                      "Toggle sticky grenades by using the following command: \r\n" +
                                      " /stickygrenades";

        string defaultTimedExplosivesDamageModified = "Timed Explosives {1} changed to {0}% of the normal {1}.";
        string defaultF1GrenadesDamageModified = "F1 Grenades {1} changed to {0}% of the normal {1}.";
        string defaultF1GrenadesSticky = "You have {0} sticky grenades.";
        string defaultNotAllowed = "You are not allowed to use this command.";
        string defaultInvalidArgument = "Invalid arguments supplied!\r\nUse '/{0}  <type:timed|grenade> <value:percentage> ' where value is the % of the original value.";

        string helpTextPlayerTimedExplosives;
        string helpTextPlayerF1Grenades;
        string helpTextAdmin;
        string timedExplosivesDamageModified;
        string f1GrenadesDamageModified;
        string f1GrenadesSticky;
        string modified;
        string notAllowed;
        string invalidArgument;

        #endregion

        protected override void LoadDefaultConfig()
        {
            Log("Creating a new config file");
            Config.Clear();
            LoadVariables();
        }

        void Loaded()
        {
            LoadVariables();

            // Save config changes when required
            if (configChanged)
            {
                Log("Config file was updated.");
                SaveConfig();
            }

        }

        void LoadVariables()
        {
            // Settings
            chatPrefix = Convert.ToString(GetConfigValue("Settings", "ChatPrefix", defaultChatPrefix));
            chatPrefixColor = Convert.ToString(GetConfigValue("Settings", "ChatPrefixColor", defaultChatPrefixColor));

            // Timed Explosive Settings
            timedExplosiveDamageModifier = float.Parse(Convert.ToString(GetConfigValue("TimedExplosives", "DamageModifier", defaultTimedExplosiveDamageModifier)), System.Globalization.CultureInfo.InvariantCulture);
            timedExplosiveRadiusModifier = float.Parse(Convert.ToString(GetConfigValue("TimedExplosives", "RadiusModifier", defaultTimedExplosiveRadiusModifier)), System.Globalization.CultureInfo.InvariantCulture);

            // F1 Grenades Settings
            f1GrenadeDamageModifier = float.Parse(Convert.ToString(GetConfigValue("F1Grenades", "DamageModifier", defaultF1GrenadeDamageModifier)), System.Globalization.CultureInfo.InvariantCulture);
            f1GrenadeRadiusModifier = float.Parse(Convert.ToString(GetConfigValue("F1Grenades", "RadiusModifier", defaultF1GrenadeRadiusModifier)), System.Globalization.CultureInfo.InvariantCulture);
            stickyGrenades = bool.Parse(Convert.ToString(GetConfigValue("F1Grenades", "StickyGrenades", defaultStickyGrenades)));

            // Messages
            helpTextPlayerTimedExplosives = Convert.ToString(GetConfigValue("Messages", "HelpTextPlayerTimedExplosives", defaultHelpTextPlayerTimedExplosives));
            helpTextPlayerF1Grenades = Convert.ToString(GetConfigValue("Messages", "HelpTextPlayerF1Grenades", defaultHelpTextPlayerF1Grenades));
            helpTextAdmin = Convert.ToString(GetConfigValue("Messages", "HelpTextAdmin", defaultHelpTextAdmin));
            timedExplosivesDamageModified = Convert.ToString(GetConfigValue("Messages", "TimedExplosivesModified", defaultTimedExplosivesDamageModified));
            f1GrenadesDamageModified = Convert.ToString(GetConfigValue("Messages", "GrenadesModified", defaultF1GrenadesDamageModified));
            f1GrenadesSticky = Convert.ToString(GetConfigValue("Messages", "StickyGrenades", defaultF1GrenadesSticky));
            notAllowed = Convert.ToString(GetConfigValue("Messages", "NotAllowed", defaultNotAllowed));
            invalidArgument = Convert.ToString(GetConfigValue("Messages", "InvalidArgument", defaultInvalidArgument));
        }

        [ChatCommand("explosivedamage")]
        void ExplosiveDamage(BasePlayer player, string command, string[] args)
        {
            ChangeExplosive(player, command, args, "DamageModifier");
        }

        [ChatCommand("explosiveradius")]
        void ExplosiveRadius(BasePlayer player, string command, string[] args)
        {
            ChangeExplosive(player, command, args, "RadiusModifier");
        }

        [ChatCommand("stickygrenades")]
        void StickyGrenades(BasePlayer player, string command, string[] args)
        {
            if (!IsAllowed(player)) return;

            stickyGrenades = !stickyGrenades;
            SetConfigValue("F1Grenades", "StickyGrenades", stickyGrenades);
            SendChatMessage(player, f1GrenadesSticky, (stickyGrenades ? "enabled" : "disabled"));
        }

        void ChangeExplosive(BasePlayer player, string command, string[] args, string type)
        {
            if (!IsAllowed(player)) return;

            if (args.Length != 2)
            {
                SendChatMessage(player, invalidArgument, command);
                return;
            }

            float newModifier = 0f;
            if ((args[0] != "timed" || args[0] != "grenade") && !float.TryParse(args[1], out newModifier))
            {
                SendChatMessage(player, invalidArgument, command);
                return;
            }

            string configCategory = "";
            if (args[0] == "timed")
            {
                configCategory = "TimedExplosives";
                if (type == "DamageModifier")
                    timedExplosiveDamageModifier = newModifier;
                if (type == "RadiusModifier")
                    timedExplosiveRadiusModifier = newModifier;
                SendChatMessage(player, timedExplosivesDamageModified, newModifier, type.Replace("Modifier", "").ToLower());
            }
            if (args[0] == "grenade")
            {
                configCategory = "F1Grenades";
                if (type == "DamageModifier")
                    f1GrenadeDamageModifier = newModifier;
                if (type == "RadiusModifier")
                    f1GrenadeRadiusModifier = newModifier;
                SendChatMessage(player, f1GrenadesDamageModified, newModifier, type.Replace("Modifier", "").ToLower());
            }

            SetConfigValue(configCategory, type, newModifier);
            return;
        }

        #region Hooks

        void OnEntitySpawned(BaseEntity entity)
        {
            var explosive = entity as TimedExplosive;
            if (explosive)
            {
                if (entity.name == "timed.explosive.deployed(Clone)")
                {
                    if (timedExplosiveDamageModifier != 100)
                    {
                        foreach (global::Rust.DamageTypeEntry entry in explosive.damageTypes)
                            entry.amount *= timedExplosiveDamageModifier / 100;
                    }

                    if (timedExplosiveRadiusModifier != 100)
                        explosive.explosionRadius *= timedExplosiveRadiusModifier / 100;
                }

                if (entity.name == "grenade.f1.deployed(Clone)")
                {
                    foreach (global::Rust.DamageTypeEntry entry in explosive.damageTypes)
                        entry.amount *= f1GrenadeDamageModifier / 100;

                    if (f1GrenadeRadiusModifier != 100)
                        explosive.explosionRadius *= f1GrenadeRadiusModifier / 100;

                    explosive.canStick = stickyGrenades;
                }
            }
        }

        void SendHelpText(BasePlayer player)
        {
            SendChatMessage(player, helpTextPlayerTimedExplosives, timedExplosiveDamageModifier, timedExplosiveRadiusModifier);
            SendChatMessage(player, helpTextPlayerF1Grenades, f1GrenadeDamageModifier, f1GrenadeRadiusModifier, (stickyGrenades ? "enabled" : "disabled"));

            if (player.net.connection.authLevel == 2)
                SendChatMessage(player, helpTextAdmin);
        }

        #endregion

        #region Helper Methods

        void Log(string message) => Puts("{0} : {1}", Title, message);

        void SendChatMessage(BasePlayer player, string message, params object[] arguments) =>
            PrintToChat(player, $"<color={chatPrefixColor}>{chatPrefix}</color>: {message}", arguments);

        bool IsAllowed(BasePlayer player)
        {
            if (player.net.connection.authLevel == 2) return true;
            SendChatMessage(player, notAllowed);
            return false;
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
