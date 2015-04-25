// Reference: Oxide.Ext.Rust
// Reference: Newtonsoft.Json

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
using Oxide.Core.Logging;
using Oxide.Core.Plugins;

//External NameSpaces
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Oxide.Plugins
{
    [Info("Better Start", "Looking For Gamers <support@lfgame.rs>", "1.1.3", ResourceId = 811)]
    public class BetterStart : RustPlugin
    {
        #region Other Classes
        public class configObj
        {
            public int startingHealth { set; get; }
            public int startingCalories { set; get; }
            public int startingHydration { set; get; }
            public List<itemsContainer> startingItems { set; get; }
            public List<string> startingBlueprints { set; get; }
            public configObj()
            {
                this.startingItems = new List<itemsContainer>();
                this.startingBlueprints = new List<string>();
            }
        }

        public class itemsContainer
        {
            public string name { set; get; }
            public List<itemMeta> items { set; get; }
            public itemsContainer()
            {
                this.items = new List<itemMeta>();
            }
        }

        public class itemMeta
        {
            public string name { set; get; }
            public int amount { set; get; }
            public itemMeta()
            {
                this.amount = 1;
            }
        }
        #endregion

        public configObj config;
        private string configPath;
        private bool loaded = false;

        #region hook methods
        void SetupConfig()
        {
            if (this.loaded)
            {
                return;
            }

            LoadConfig();
            this.configPath = Manager.ConfigPath + string.Format("\\{0}.json", Name);
            this.config = JsonConvert.DeserializeObject<configObj>((string)JsonConvert.SerializeObject(Config["Config"]).ToString());
            this.loaded = true;
        }

        void Loaded()
        {
            //Puts("\n\n---------------------------------------------------------------------------------------------------------------------\n\n");
            this.Print("Better Start by Looking For Gamers, has been started");
            this.SetupConfig();
        }

        [HookMethod("OnServerInitialized")]
        void OnServerInitialized()
        {
            this.SetupConfig();
        }

        [HookMethod("LoadDefaultConfig")]
        void CreateDefaultConfig()
        {
            configObj localConfig = new configObj();

            localConfig.startingHealth = 100;
            localConfig.startingCalories = 400;
            localConfig.startingHydration = 1000;

            localConfig.startingItems = JsonConvert.DeserializeObject<List<itemsContainer>>(
                "[{\"name\": \"wear\", \"items\": [{\"name\": \"burlap_trousers\"}, {\"name\": \"burlap_shoes\"}, {\"name\": \"burlap_shirt\"}, {\"name\": \"burlap_gloves\"}, {\"name\": \"coffeecan_helmet\"}]}" +
                ",{\"name\": \"belt\", \"items\": [{\"name\": \"stonehatchet\"}, {\"name\": \"torch\"}]}" +
                ",{\"name\": \"main\", \"items\": [{\"name\": \"bandage\",\"amount\": 3}]}]"
            );
            localConfig.startingBlueprints = JsonConvert.DeserializeObject<List<string>>(
                "['lantern', 'pickaxe', 'stonehatchet', 'arrow_wooden', 'spear_wooden', 'bow_hunting', 'lantern']"
            );

            this.config = localConfig;
            Config["Config"] = this.config;
            Config.Save(this.configPath);
            this.SetupConfig();
        }

        [HookMethod("OnPlayerSpawn")]
        void OnPlayerSpawn(BasePlayer player, Network.Connection connection)
        {
            player.health = config.startingHealth;
            player.metabolism.calories.value = config.startingCalories;
            player.metabolism.hydration.value = config.startingHydration;

            this.givePlayerStartingItems(player);
            this.teachPlayerBlueprints(player);
        }
        #endregion

        #region private helpers
        private void teachPlayerBlueprints(BasePlayer player)
        {
            PlayerBlueprints blueprints = player.blueprints;
            foreach (string name in config.startingBlueprints)
            {
                Item item = ItemManager.CreateByName(name, 1);
                item.isBlueprint = true;
                blueprints.Learn(item.info);
            }
        }

        private void givePlayerStartingItems(BasePlayer player)
        {
            player.inventory.Strip();
            foreach (itemsContainer element in config.startingItems)
            {
                foreach (itemMeta item in element.items)
                {
                    this.giveItem(player, element.name, item);
                }
            }
        }

        private void giveItem(BasePlayer player, string inventoryName, itemMeta meta)
        {
            var manager = SingletonComponent<ItemManager>.Instance;
            PlayerInventory inventory = player.inventory;
            ItemContainer container;
            if (inventoryName == "wear")
            {
                container = inventory.containerWear;
            }
            else if (inventoryName == "belt")
            {
                container = inventory.containerBelt;
            }
            else if (inventoryName == "main")
            {
                container = inventory.containerMain;
            }
            else
            {
                throw new Exception(inventoryName);
            }

            bool blueprint = false;
            if (meta.name.IndexOf("_bp") != -1)
            {
                blueprint = true;
                meta.name = meta.name.Substring(0, meta.name.Length - 3);
            }

            Item item = ItemManager.CreateByName(meta.name, (int)meta.amount);
            item.isBlueprint = blueprint;
            inventory.GiveItem(item, container);
        }

        private void Print(object msg)
        {
            Puts("{0}: {1}", Title, (string)msg);
        }
        #endregion

        #region console commands
        [ConsoleCommand("betterstart.reload")]
        void cmdConsoleReload(ConsoleSystem.Arg arg)
        {
            this.SetupConfig();
            this.Print("Better Start Config reloaded.");
        }

        [ConsoleCommand("betterstart.version")]
        void cmdConsoleVersion(ConsoleSystem.Arg arg)
        {
            this.Print(Version.ToString());
        }
        #endregion
    }


}