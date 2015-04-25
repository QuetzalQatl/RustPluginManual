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
    [Info("Stack Sizes", "Looking For Gamers <support@lfgame.rs>", "1.1.3", ResourceId = 812)]
    public class StackSizes : RustPlugin
    {
        #region Other Classes
        public class configObj
        {
            public List<itemMeta> items { set; get; }
            public configObj() { items = new List<itemMeta>(); }
        }

        public class itemMeta
        {
            public string name { set; get; }
            public int stackSize { set; get; }
            public itemMeta() { stackSize = 1; }
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

            try
            {
                this.SetStackSizes();
                this.loaded = true;
            }
            catch (NullReferenceException e)
            {
                this.loaded = false;
            }
        }

        void Loaded()
        {
            //Puts("\n\n---------------------------------------------------------------------------------------------------------------------\n\n");
            this.SetupConfig();
            this.Print("StackSizes by Looking For Gamers, has been started");
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

            localConfig.items = JsonConvert.DeserializeObject<List<itemMeta>>(
                "[" +
                "{'name': 'bone_fragments', 'stackSize': 10000}," +
                "{'name': 'charcoal', 'stackSize': 10000}," +
                "{'name': 'cloth', 'stackSize': 10000}," +
                "{'name': 'fat_animal', 'stackSize': 10000}," +
                "{'name': 'gunpowder', 'stackSize': 10000}," +
                "{'name': 'lowgradefuel', 'stackSize': 10000}," +
                "{'name': 'metal_fragments', 'stackSize': 10000}," +
                "{'name': 'metal_ore', 'stackSize': 10000}," +
                "{'name': 'metal_refined', 'stackSize': 10000}," +
                "{'name': 'paper', 'stackSize': 10000}," +
                "{'name': 'stones', 'stackSize': 100000}," +
                "{'name': 'sulfur', 'stackSize': 10000}," +
                "{'name': 'sulfur_ore', 'stackSize': 100000}," +
                "{'name': 'wood', 'stackSize': 100000}" +
                "]"
            );

            this.config = localConfig;
            Config["Config"] = this.config;

            Config.Save(this.configPath);
            LoadConfig();
        }

        private void SetStackSizes()
        {
            foreach (itemMeta meta in config.items)
            {
                Item item = ItemManager.CreateByName(meta.name, 1);
                item.info.stackable = meta.stackSize;
            }
        }

        private void Print(object msg)
        {
            Puts("{0}: {1}", Title, (string) msg);
        }
        #endregion

        #region console commands
        [ConsoleCommand("stacksizes.reload")]
        void cmdConsoleReload(ConsoleSystem.Arg arg)
        {
            this.SetupConfig();
            this.Print("StackSizes Config reloaded.");
        }

        [ConsoleCommand("stacksizes.version")]
        void cmdConsoleVersion(ConsoleSystem.Arg arg)
        {
            this.Print(Version.ToString());
        }
        #endregion
    }
     
    
}