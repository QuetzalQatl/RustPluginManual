// Reference: Oxide.Ext.Rust
// Reference: Newtonsoft.Json
// Reference: LFG

/*
 * The MIT License (MIT)
 * 
 * Copyright (c) 2015 Looking For Gamers, Inc. <support@lfgame.rs>
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:

 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

//Microsoft NameSpaces
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// Rust Unity Namespaces
using Rust;
using UnityEngine;

//Oxide NameSpaces
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Logging;
using Oxide.Core.Plugins;

//External NameSpaces
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using LFG;

namespace Oxide.Plugins
{
    [Info("Factions", "Looking For Gamers <support@lfgame.rs>", "0.1.1")]
    public class Factions : RustPlugin
    {
        #region Other Classes
        public class ConfigObj
        {
            public Dictionary<string, List<string>> messages { set; get; }
            public string chatPrefix { set; get; }
            public string chatPrefixColor { set; get; }

            public ConfigObj()
            {
                this.messages = new Dictionary<string, List<string>>();
                this.chatPrefix = "Factions";
                this.chatPrefixColor = "green";
            }

            public void addMessage(string key, List<string> message)
            {
                this.messages.Add(key, message);
            }

            public List<string> getMessage(string key, string[] args)
            {
                List<string> strings = new List<string>();
                List<string> messageList;

                if (this.messages.ContainsKey(key))
                {
                    messageList = (List<string>) this.messages[key];
                    foreach (string message in messageList)
                    {
                        strings.Add(string.Format(message, args));
                    }
                }

                return strings;
            }
        }
        #endregion


        public ConfigObj config;
        private string configPath;
        private bool loaded = false;
        private List<Faction> factions;
        private string factionDatafile = "Factions_Data-factions";

        #region hook methods
        void SetupConfig()
        {
            if (this.loaded)
            {
                return;
            }

            LoadConfig();
            this.configPath = Manager.ConfigPath + string.Format("\\{0}.json", Name);
            this.config = JsonConvert.DeserializeObject<ConfigObj>((string)JsonConvert.SerializeObject(Config["Config"]).ToString());
            
            // This all seems 
            try
            {
                this.factions = Interface.GetMod().DataFileSystem.ReadObject<List<Faction>>(this.factionDatafile);
            }
            catch (Exception e)
            {
                this.factions = new List<Faction>();
                this.SaveData();
            }

            if (this.factions == null)
            {
                this.factions = new List<Faction>();
            }

            this.loaded = true;
        }

        void Loaded()
        {
            this.SetupConfig();
            Print("Factions by Looking For Gamers, has been started");
        }

        void SaveData()
        {
            Interface.GetMod().DataFileSystem.WriteObject(this.factionDatafile, this.factions);
        }

        [HookMethod("LoadDefaultConfig")]
        void CreateDefaultConfig()
        {
            ConfigObj localConfig = new ConfigObj();

            localConfig.addMessage(
                "help", 
                new List<string>()
                { 
                    "To create a faction, type \"/faction create <name>\".",
                    "To request to join a faction, type \"/faction join <name>\".",
                    "To invite, type \"/faction invite <player>\".",
                    "To get faction info, type \"/faction info <name>\".",
                    "You can also use \"/f\"."
                }
            );
            localConfig.addMessage("created", new List<string>() { "You have created the \"{0}\" faction." });
            localConfig.addMessage("notCreated", new List<string>() { "The \"{0}\" faction doesn't exists." });
            localConfig.addMessage("alreadyCreated", new List<string>() { "The \"{0}\" faction already exists." });
            localConfig.addMessage("alreadyInFaction", new List<string>() { "You are already a member of a faction." });
            localConfig.addMessage("invalidCommand", new List<string>() { "You ran the \"{0}\" command incorrectly. Type \"/faction help\" to get help" });
            localConfig.addMessage("invitationSent", new List<string>() { "You have invited \"{0}\" to your faction." });
            localConfig.addMessage(
                "invitationReceived", 
                new List<string>() 
                { 
                    "You have been invited to the \"{0}\" faction.",
                    "To accept, type \"/faction join {0}\".",
                    "To reject, type \"/faction reject {0}\".",
                }
            );
            localConfig.addMessage("invitationRejected", new List<string>() { "You have rejected the invitiation from the \"{0}\" faction." });
            localConfig.addMessage("requestSent", new List<string>() { "You have requested to join the \"{0}\" faction." });
            localConfig.addMessage(
                "requestReceived",
                new List<string>()
                {
                    "\"{0}\" is requesting to join your faction.",
                    "Accept with \"/faction accept {0}\".",
                    "Deny with \"/faction deny {0}\"."
                }
            );
            localConfig.addMessage("notInFaction", new List<string>() { "You are not a member of a faction." });
            localConfig.addMessage("notOnline", new List<string>() { "\"{0}\" is either not online, or not a real user." });
            localConfig.addMessage("notAcceptee", new List<string>() { "\"{0}\" has not requested to join your group." });
            localConfig.addMessage("membershipGranted", new List<string>() { "You have been accepted into the \"{0}\" faction." });
            localConfig.addMessage("membershipDenied", new List<string>() { "You have been denied from the \"{0}\" faction." });
            localConfig.addMessage("playerAccepted", new List<string>() { "\"{0}\" has been added to your faction." });
            localConfig.addMessage("playerDenied", new List<string>() { "\"{0}\" has NOT been added to your faction." });
            localConfig.addMessage("playerKicked", new List<string>() { "You have kicked \"{0}\" from your faction." });
            localConfig.addMessage("playerKickReceived", new List<string>() { "You have been kicked from the \"{0}\" faction." });
            localConfig.addMessage(
                "factionInfo",
                new List<string>()
                {
                    "The \"{0}\" faction:",
                    "Owner: {1}",
                    "Members: {2}"
                }
            );

            this.config = localConfig;
            Config["Config"] = this.config;
            SaveConfig();

            this.SetupConfig();
        }

        void OnPlayerInit(BasePlayer player)
        {
            Faction faction = this.getPlayerOwnedFaction(player);
            if (faction != null)
            {
                foreach (string request in faction.requests)
                {
                    BasePlayer requestee = this.getActivePlayer(request);
                    if (requestee != null)
                    {
                        this.chatMessage(player, config.getMessage("requestReceived", new string[] { requestee.displayName.ToString() }));
                    }
                }
            }


            foreach (Faction f in this.factions)
            {
                if (f.hasInvite(player))
                {
                    this.chatMessage(player, config.getMessage("invitationReceived", new string[] { f.name }));
                }
            }
        }


        [HookMethod("OnEntityAttacked")]
        object OnEntityAttacked(MonoBehaviour entity, HitInfo hitinfo)
        {
            //**
            if (hitinfo == null)
            {
                Print("No hit info at all");
                return null;
            }
            if (hitinfo.Initiator == null)
            {
                Print("Attacker is null");
                return null;
            }

            BasePlayer attacker = hitinfo.Initiator as BasePlayer;
            BasePlayer defender = entity as BasePlayer;

            if (attacker == null || defender == null)
            {
                //Print("Attacker Type: " + hitinfo.Initiator.GetType().ToString());
                //Print("Defender Type: " + entity.GetType().ToString());
                return null;
            }

            Faction attackerFaction = this.getPlayerFaction(attacker);
            Faction defenderFaction = this.getPlayerFaction(defender);
            if (attackerFaction == null)
            {
                Print("Attacker doesn't have a faction");
                return null;
            }
            if (defenderFaction == null)
            {
                Print("Defender doesn't have a faction");
                return null;
            }

            if (attackerFaction.Equals(defenderFaction))
            {
                return false;
            }
            //*/
            return null;
        }
        #endregion

        #region chat commands

        [ChatCommand("f")]
        void cmdChatFactionShortcut(BasePlayer player, string command, string[] args)
        {
            this.cmdChatFaction(player, command, args);
        }

        [ChatCommand("faction")]
        void cmdChatFaction(BasePlayer player, string command, string[] args)
        {
            #region help
            if (args.Length == 0 || (args.Length == 1 && args[0].ToLower() == "help"))
            {
                this.chatMessage(player, config.getMessage("help", new string[] { }));
                return;
            }
            #endregion

            string name;
            Faction faction;
            BasePlayer acceptee;
            BasePlayer invitee;
            Print("Running the " + args[0].ToLower() + " command");
            switch (args[0].ToLower())
            {
                #region default
                default:
                    this.cmdChatFaction(player, command, new string[] { });
                    return;
                #endregion

                 #region create
                case "create":
                    if (args.Length < 2)
                    {
                        this.chatMessage(player, config.getMessage("invalidCommand", new string[] { "create" }));
                        return;
                    }

                    if (this.playerHasFaction(player))
                    {
                        this.chatMessage(player, config.getMessage("alreadyInFaction", new string[] { }));
                        return;
                    }

                    name = string.Join(" ", args).Replace("create ", "");
                    faction = this.getFactionByName(name);
                    if (faction != null)
                    {
                        this.chatMessage(player, config.getMessage("alreadyCreated", new string[] { faction.name }));
                        return;
                    }

                    this.createFaction(name, player);
                    this.chatMessage(player, config.getMessage("created", new string[] { name }));

                    return;
                #endregion

                #region join
                case "join":
                    if (args.Length < 2)
                    {
                        this.chatMessage(player, config.getMessage("invalidCommand", new string[] {"join"}));
                        return;
                    }

                    if (this.playerHasFaction(player))
                    {
                        this.chatMessage(player, config.getMessage("alreadyInFaction", new string[] { }));
                        return;
                    }

                    name = string.Join(" ", args).Replace("join ", "");
                    Print("Does the " + name + " faction exist");
                    faction = this.getFactionByName(name);
                    if (faction == null)
                    {
                        this.chatMessage(player, config.getMessage("notCreated", new string[] { name }));
                        return;
                    }

                    this.sendRequestToFaction(faction, player);

                    return;
                #endregion

                #region invite
                case "invite":
                    if (args.Length < 2)
                    {
                        this.chatMessage(player, config.getMessage("invalidCommand", new string[] { "invite" }));
                        return;
                    }

                    name = string.Join(" ", args).Replace("invite ", "");
                    invitee = this.getPlayerByName(name);
                    if (invitee == null)
                    {
                        this.chatMessage(player, config.getMessage("notOnline", new string[] { name }));
                        return;
                    }

                    faction = this.getPlayerOwnedFaction(player);
                    if (faction == null)
                    {
                        this.chatMessage(player, config.getMessage("notInFaction", new string[] { invitee.displayName }));
                        return;
                    }
                    if (faction.hasInvite(invitee))
                    {
                        return;
                    }

                    Print("Inviting " + invitee.displayName + " to " + faction.name);

                    this.inviteMember(faction, invitee);
                    this.chatMessage(player, config.getMessage("invitationSent", new string[] { invitee.displayName }));
                    this.chatMessage(invitee, config.getMessage("invitationReceived", new string[] { faction.name }));

                    return;
                #endregion

                #region reject
                case "reject":
                    if (args.Length < 2)
                    {
                        this.chatMessage(player, config.getMessage("invalidCommand", new string[] { "reject" }));
                        return;
                    }

                    name = string.Join(" ", args).Replace("reject ", "");
                    faction = this.getFactionByName(name);
                    if (faction == null)
                    {
                        this.chatMessage(player, config.getMessage("notCreated", new string[] { name }));
                        return;
                    }

                    this.rejectInvitation(player, faction);
                    this.chatMessage(player, config.getMessage("invitationRejected", new string[] { faction.name }));

                    return;
                #endregion

                #region accept
                case "accept":
                    if (args.Length < 2)
                    {
                        this.chatMessage(player, config.getMessage("invalidCommand", new string[] { "accept" }));
                        return;
                    }

                    name = string.Join(" ", args).Replace("accept ", "");
                    acceptee = this.getPlayerByName(name);
                    if (acceptee == null)
                    {
                        this.chatMessage(player, config.getMessage("notOnline", new string[] { name }));
                        return;
                    }

                    faction = this.getPlayerOwnedFaction(player);
                    if (!faction.hasRequest(acceptee))
                    {
                        this.chatMessage(player, config.getMessage("notAcceptee", new string[] { acceptee.displayName }));
                        return;
                    }

                    this.promoteToMember(faction, player);
                    this.chatMessage(player, config.getMessage("playerAccepted", new string[] { acceptee.displayName }));

                    return;
                #endregion

                #region deny
                case "deny":
                    if (args.Length < 2)
                    {
                        this.chatMessage(player, config.getMessage("invalidCommand", new string[] { "deny" }));
                        return;
                    }

                    name = string.Join(" ", args).Replace("deny ", "");
                    acceptee = this.getPlayerByName(name);
                    if (acceptee == null)
                    {
                        this.chatMessage(player, config.getMessage("notOnline", new string[] { name }));
                        return;
                    }

                    faction = this.getPlayerOwnedFaction(player);
                    if (!faction.hasRequest(acceptee))
                    {
                        this.chatMessage(player, config.getMessage("notAcceptee", new string[] { acceptee.displayName }));
                        return;
                    }

                    this.rejectFromMember(faction, player);
                    this.chatMessage(player, config.getMessage("playerDenied", new string[] { acceptee.displayName }));

                    return;
                #endregion

                #region kick
                case "kick":
                    if (args.Length < 2)
                    {
                        this.chatMessage(player, config.getMessage("invalidCommand", new string[] { "kick" }));
                        return;
                    }

                    name = string.Join(" ", args).Replace("kick ", "");
                    BasePlayer kickee = this.getPlayerByName(name);
                    if (kickee == null)
                    {
                        this.chatMessage(player, config.getMessage("notOnline", new string[] { name }));
                        return;
                    }

                    faction = this.getPlayerOwnedFaction(player);
                    if (!faction.hasMember(kickee))
                    {
                        this.chatMessage(player, config.getMessage("notAcceptee", new string[] { kickee.displayName }));
                        return;
                    }

                    this.kickMember(faction, player);
                    this.chatMessage(player, config.getMessage("playerKicked", new string[] { kickee.displayName }));
                    this.chatMessage(kickee, config.getMessage("playerKickReceived", new string[] { faction.name }));

                    return;
                #endregion

                #region info
                case "info":
                    if (args.Length < 2)
                    {
                        this.chatMessage(player, config.getMessage("invalidCommand", new string[] { "info" }));
                        return;
                    }
                    
                    name = string.Join(" ", args).Replace("info ", "");
                    faction = this.getFactionByName(name);
                    if (faction == null)
                    {
                        this.chatMessage(player, config.getMessage("notCreated", new string[] { name }));
                        return;
                    }

                    this.chatMessage(
                        player,
                        config.getMessage(
                            "factionInfo",
                            new string[]
                            {
                                faction.name,
                                this.getFactionOwner(faction),
                                faction.members.Count().ToString()
                            }
                        )
                    );

                    return;
                #endregion
            }
        }
        #endregion

        #region console commands
        [ConsoleCommand("factions.reload")]
        void cmdConsoleReload(ConsoleSystem.Arg arg)
        {
            this.loaded = false;
            this.SetupConfig();
            this.Print("Factions Reloaded");
        }

        [ConsoleCommand("factions.version")]
        void cmdConsoleVersion(ConsoleSystem.Arg arg)
        {
            this.Print(Version.ToString());
        }
        #endregion



        #region private helpers
        private void Print(object msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("{0}: {1}", Title, msg);
        }

        private BasePlayer getActivePlayer(string userId)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                string id = player.userID.ToString();
                if (id.Equals(userId))
                {
                    return player;
                }
            }

            return null;
        }

        private void chatMessage(BasePlayer player, List<string> messages)
        {
            foreach (string message in messages)
            {
                player.ChatMessage(string.Format("<color={0}>{1}</color>: " + message, config.chatPrefixColor, config.chatPrefix));
            }
        }

        public Faction getFactionByName(string name)
        {
            foreach (Faction faction in this.factions)
            {
                if (faction.name.ToLower().Equals(name.ToLower()))
                {
                    return faction;
                }
            }

            return null;
        }

        private bool playerHasFaction(BasePlayer player)
        {
            foreach (Faction faction in this.factions)
            {
                if (faction.hasMember(player))
                {
                    return true;
                }
            }

            return false;
        }

        private string getFactionOwner(Faction faction)
        {
            BasePlayer player = this.getActivePlayer(faction.owner);

            return player == null ? "offline" : player.displayName;
        }

        private Faction getPlayerOwnedFaction(BasePlayer player)
        {
            foreach (Faction faction in this.factions)
            {
                if (faction.isOwnedBy(player))
                {
                    return faction;
                }
            }

            return null;
        }

        private Faction getPlayerFaction(BasePlayer player)
        {
            foreach (Faction faction in this.factions)
            {
                if (faction.hasMember(player))
                {
                    return faction;
                }
            }

            return null;
        }

        /**
         * Has to Save Data
         */
        private void createFaction(string name, BasePlayer player)
        {
            Faction faction = new Faction();
            faction.initialize(name, player);
            this.saveFaction(faction);
        }

        /**
         * Has to Save Data
         */
        private void sendRequestToFaction(Faction faction, BasePlayer player)
        {
            if (faction.hasMember(player) || faction.hasRequest(player))
            {
                return;
            }

            if (faction.hasInvite(player))
            {
                faction.addMember(player);
                this.chatMessage(player, config.getMessage("membershipGranted", new string[] { faction.name }));
            }
            else
            {
                faction.addRequest(player);
                this.chatMessage(player, config.getMessage("requestSent", new string[] { faction.name }));
                this.sendFactionMessage(faction, player);
            }

            this.saveFaction(faction);
        }

        private void sendFactionMessage(Faction faction, BasePlayer requestee)
        {
            BasePlayer owner = this.getActivePlayer(faction.owner);
            if (owner != null)
            {
                this.chatMessage(owner, config.getMessage("requestReceived", new string[] { requestee.displayName.ToString() }));
            }
        }

        /**
         * Has to Save Data
         */
        private void inviteMember(Faction faction, BasePlayer player)
        {
            faction.addInvite(player);
            this.saveFaction(faction);
        }

        private void rejectInvitation(BasePlayer player, Faction faction)
        {
            faction.removePlayer(player);
            this.saveFaction(faction);
        }

        /**
         * Has to Save Data
         */
        private void kickMember(Faction faction, BasePlayer player)
        {
            faction.removePlayer(player);
            this.saveFaction(faction);
        }

        /**
         * Has to Save Data
         */
        private void promoteToMember(Faction faction, BasePlayer player)
        {
            faction.removePlayer(player);
            faction.addMember(player);

            this.saveFaction(faction);

            this.chatMessage(player, config.getMessage("membershipGranted", new string[] { faction.name }));
        }

        /**
         * Has to Save Data
         */
        private void rejectFromMember(Faction faction, BasePlayer player)
        {
            faction.removePlayer(player);

            this.saveFaction(faction);

            this.chatMessage(player, config.getMessage("membershipDenied", new string[] { faction.name }));
        }

        private BasePlayer getPlayerByName(string name)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player.displayName.Equals(name))
                {
                    return player;
                }
            }

            return null;
        }

        private void saveFaction(Faction faction)
        {
            this.Print("Saving the " + faction.name + " faction");
            if (!this.factions.Contains(faction))
            {
                this.factions.Add(faction);
            }
            SaveData();
        }
        #endregion
    }
}


namespace LFG
{
    public class Faction
    {
        public string name { set; get; }
        public string owner { set; get; }
        public List<string> members { set; get; }
        public List<string> requests { set; get; }
        public List<string> invites { set; get; }
        public Faction()
        {
            this.members = new List<string>();
            this.requests = new List<string>();
            this.invites = new List<string>();
        }

        public void initialize(string name, BasePlayer owner)
        {
            this.name = name;
            this.owner = owner.userID.ToString();
            this.addMember(owner);
        }

        public bool isOwnedBy(BasePlayer player)
        {
            string id = player.userID.ToString();
            return id.Equals(this.owner);
        }

        public bool hasMember(BasePlayer player)
        {
            return this.members.Contains(player.userID.ToString());
        }

        public bool hasRequest(BasePlayer player)
        {
            return this.requests.Contains(player.userID.ToString());
        }

        public bool hasInvite(BasePlayer player)
        {
            return this.invites.Contains(player.userID.ToString());
        }

        public void addMember(BasePlayer player)
        {
            string id = player.userID.ToString();
            // Don't do anything if the user is already a member. This should happen here.
            if (this.members.Contains(id))
            {
                return;
            }

            // Remove user requests and invites
            if (this.requests.Contains(id))
            {
                this.requests.Remove(id);
            }
            if (this.invites.Contains(id))
            {
                this.invites.Remove(id);
            }

            this.members.Add(id);
        }

        public void addInvite(BasePlayer player)
        {
            string id = player.userID.ToString();
            //If the user is already a member, or is already invited, don't do anything
            if (this.members.Contains(id) || this.invites.Contains(id))
            {
                return;
            }

            // If the user has already requested, just make them a member
            if (this.requests.Contains(id))
            {
                this.addMember(player);
                return;
            }

            this.invites.Add(id);
        }

        public void addRequest(BasePlayer player)
        {
            string id = player.userID.ToString();
            //If the user is already a member, or has already requested, don't do anything
            if (this.members.Contains(id) || this.requests.Contains(id))
            {
                return;
            }

            // If the user has already been invited, just make them a member
            if (this.invites.Contains(id))
            {
                this.addMember(player);
                return;
            }

            this.requests.Add(id);
        }

        public void removePlayer(BasePlayer player)
        {
            string id = player.userID.ToString();
            if (this.members.Contains(id))
            {
                this.members.Remove(id);
            }
            if (this.requests.Contains(id))
            {
                this.requests.Remove(id);
            }
            if (this.invites.Contains(id))
            {
                this.invites.Remove(id);
            }
        }
    }
}