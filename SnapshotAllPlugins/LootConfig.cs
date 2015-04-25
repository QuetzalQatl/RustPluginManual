// Reference: Oxide.Ext.Rust
// Reference: Newtonsoft.Json

using System.Collections.Generic;
using System.Linq;

using Rust;

using System;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using UnityEngine;

using JSONObject = JSON.Object;
using JSONArray = JSON.Array;
using JSONValue = JSON.Value;
using JSONValueType = JSON.ValueType;

namespace Oxide.Plugins
{
    [Info("LootConfig", "Nogrod", "1.0.0")]
    class LootConfig : RustPlugin
    {
        private readonly Regex _findLoot = new Regex("loot|crate|supply_drop", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private string _configpath = "";

        void Loaded()
        {
            _configpath = Manager.ConfigPath + string.Format("\\{0}.json", Name);
        }

        new void LoadDefaultConfig()
        {
        }

        private static JSONObject ToJsonObject(object obj)
        {
            return JSONObject.Parse(ToJsonString(obj));
        }

        private static string ToJsonString(object obj)
        {
            return JsonConvert.SerializeObject(obj, new JsonSerializerSettings
            {
                ContractResolver = new DynamicContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = new List<JsonConverter> { new Newtonsoft.Json.Converters.StringEnumConverter() }
            });
        }

        private bool CreateDefaultConfig()
        {
            Config.Clear();
            Config["Version"] = Protocol.network;
            var containers = Resources.FindObjectsOfTypeAll<LootContainer>();
            foreach (var lootContainer in containers)
            {
                if (Config[lootContainer.lootDefinition.name.Substring(10)] != null) continue;
                var obj = ToJsonObject(lootContainer);
                StripLoot(obj.GetObject("lootDefinition"), obj, "lootDefinition");
                Config[lootContainer.lootDefinition.name.Substring(10)] = JsonObjectToObject(obj);
            }
            try
            {
                Config.Save(_configpath);
            }
            catch (Exception e)
            {
                LocalPuts(e.Message);
                return false;
            }
            LocalPuts("Created new config");
            return LoadConfig();
        }

        private bool LoadConfig()
        {
            try
            {
                Config.Load(_configpath);
            }
            catch (FileNotFoundException)
            {
                return CreateDefaultConfig();
            }
            catch (Exception e)
            {
                LocalPuts(e.Message);
                return false;
            }
            return true;
        }

        private void CheckConfig()
        {
            if (Config["Version"] != null && (int)Config["Version"] == Protocol.network) return;
            LocalPuts("Incorrect config version(" + Config["Version"] + ") move to .old");
            if (Config["Version"] != null) Config.Save(string.Format("{0}.old", _configpath));
            CreateDefaultConfig();
        }

        private void UpdateLoot()
        {
            var containers = Resources.FindObjectsOfTypeAll<LootContainer>();
            var done = new HashSet<LootSpawn>();
            // Cleanup old instances
            var loot = Resources.FindObjectsOfTypeAll<LootSpawn>();
            foreach (var lootContainer in containers)
            {
                done.Add(lootContainer.lootDefinition);
            }
            foreach (var lootSpawn in loot)
            {
                if (!done.Contains(lootSpawn))
                {
                    UnityEngine.Object.Destroy(lootSpawn);
                }
            }
            done.Clear();
            // Cleanup done
            foreach (var lootContainer in containers)
            {
                var obj = ObjectToJsonObject(Config[lootContainer.lootDefinition.name.Substring(10)]).Obj;
                lootContainer.maxDefinitionsToSpawn = obj.GetInt("maxDefinitionsToSpawn", 0);
                lootContainer.minSecondsBetweenRefresh = obj.GetFloat("minSecondsBetweenRefresh", 3600);
                lootContainer.maxSecondsBetweenRefresh = obj.GetFloat("maxSecondsBetweenRefresh", 7200);
                lootContainer.destroyOnEmpty = obj.GetBoolean("destroyOnEmpty", true);
                if (!done.Contains(lootContainer.lootDefinition))
                {
                    UpdateLootSpawn(lootContainer.lootDefinition, obj, "lootDefinition");
                    done.Add(lootContainer.lootDefinition);
                }
            }
        }

        private void UpdateLootSpawn(LootSpawn lootSpawn, JSONObject obj, string path)
        {
            var value = obj.GetValue(path);
            if (value != null && value.Type == JSONValueType.Array && value.Array.Length > 0)
            {
                lootSpawn.items = new ItemAmount[0];
                lootSpawn.blueprints = new ItemAmount[0];
                lootSpawn.subSpawn = new LootSpawn.Entry[value.Array.Length];
                for (var i = 0; i < lootSpawn.subSpawn.Length; i++)
                {
                    lootSpawn.subSpawn[i] = new LootSpawn.Entry { category = ScriptableObject.CreateInstance<LootSpawn>(), weight = value.Array[i].Obj.GetInt("weight", 0) };
                    UpdateLootSpawn(lootSpawn.subSpawn[i].category, value.Array[i].Obj, "category");
                }
                return;
            }
            var itemsValue = obj.GetValue("items");
            if (itemsValue != null && itemsValue.Type == JSONValueType.Array && itemsValue.Array.Length > 0)
            {
                var items = itemsValue.Array;
                lootSpawn.items = new ItemAmount[items.Length];
                for (var i = 0; i < items.Length; i++)
                {
                    var def = ItemManager.FindItemDefinition(items[i].Obj.GetString("item", "unnamed"));
                    //TODO null check
                    lootSpawn.items[i] = new ItemAmount(def, items[i].Obj.GetFloat("amount", 0));
                }
            }
            var blueprintsValue = obj.GetValue("blueprints");
            if (blueprintsValue != null && blueprintsValue.Type == JSONValueType.Array && blueprintsValue.Array.Length > 0)
            {
                var blueprints = blueprintsValue.Array;
                lootSpawn.blueprints = new ItemAmount[blueprints.Length];
                for (var i = 0; i < blueprints.Length; i++)
                {
                    var def = ItemManager.FindItemDefinition(blueprints[i].Obj.GetString("item", "unnamed"));
                    //TODO null check
                    lootSpawn.blueprints[i] = new ItemAmount(def, blueprints[i].Obj.GetFloat("amount", 0));
                }
            }
        }

        void OnServerInitialized()
        {
            if (!LoadConfig())
                return;
            var prefabs = GameManifest.Get().pooledStrings.ToList().ConvertAll(p => p.str).Where(p => _findLoot.IsMatch("loot|crate|supply_drop")).ToArray();
            foreach (var source in prefabs)
            {
                GameManager.server.FindPrefab(source);
            }
            CheckConfig();
            UpdateLoot();
        }

        private void StripLoot(JSONObject obj, JSONObject parent = null, string path = null)
        {
            var value = obj.GetValue("subSpawn");
            if (value != null && value.Type == JSONValueType.Array && value.Array.Length > 0)
            {
                if (parent != null)
                {
                    parent[path] = obj.GetArray("subSpawn");
                }
                obj.Remove("items");
                obj.Remove("blueprints");
                StripSubCategoryLoot(obj.GetArray("subSpawn"));
                return;
            }
            obj.Remove("subSpawn");
            var items = obj.GetValue("items");
            if (items != null && items.Type == JSONValueType.Array)
            {
                foreach (var item in items.Array)
                {
                    StripEntry(item.Obj);
                }
            }
            var bps = obj.GetValue("blueprints");
            if (bps != null && bps.Type == JSONValueType.Array)
            {
                foreach (var bp in bps.Array)
                {
                    StripEntry(bp.Obj);
                }
            }
            if (parent != null && path != null && path.Equals("category"))
            {
                parent["items"] = items;
                parent["blueprints"] = bps;
                parent.Remove("category");
            }
        }

        private static void StripEntry(JSONObject obj)
        {
            obj["item"] = obj.GetObject("itemDef").GetString("shortname", "unnamed");
            obj.Remove("itemDef");
            obj.Remove("itemid");
        }

        private void StripSubCategoryLoot(JSONArray arr)
        {
            //float sum = arr.Sum(x => x.Obj.GetFloat("weight", 0)), curSum = sum;
            foreach (var entry in arr)
            {
                StripLoot(entry.Obj.GetObject("category"), entry.Obj, "category");
                //curSum -= entry.Obj.GetFloat("weight", 0);
                //entry.Obj["percent"] = Math.Round((sum - curSum)/(sum/100f), 2);
            }
        }

        private JSONValue ObjectToJsonObject(object obj)
        {
            if (obj == null)
            {
                return new JSONValue(JSONValueType.Null);
            }
            if (obj is string)
            {
                return new JSONValue((string)obj);
            }
            if (obj is double)
            {
                return new JSONValue((double)obj);
            }
            if (obj is int)
            {
                return new JSONValue((int)obj);
            }
            if (obj is bool)
            {
                return new JSONValue((bool)obj);
            }
            var dict = obj as Dictionary<string, object>;
            if (dict != null)
            {
                var newObj = new JSONObject();
                foreach (var prop in dict)
                {
                    newObj.Add(prop.Key, ObjectToJsonObject(prop.Value));
                }
                return newObj;
            }
            var list = obj as List<object>;
            if (list != null)
            {
                var arr = new JSONArray();
                foreach (var o in list)
                {
                    arr.Add(ObjectToJsonObject(o));
                }
                return arr;
            }
            LocalPuts("Unknown: " + obj.GetType().FullName + " Value: " + obj);
            return new JSONValue(JSONValueType.Null);
        }

        private object JsonObjectToObject(JSONValue obj)
        {
            switch (obj.Type)
            {
                case JSONValueType.String:
                    return obj.Str;
                case JSONValueType.Number:
                    return obj.Number;
                case JSONValueType.Boolean:
                    return obj.Boolean;
                case JSONValueType.Null:
                    return null;
                case JSONValueType.Array:
                    return obj.Array.Select(v => JsonObjectToObject(v.Obj)).ToList();
                case JSONValueType.Object:
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in obj.Obj)
                    {
                        dict[prop.Key] = JsonObjectToObject(prop.Value);
                    }
                    return dict;
                default:
                    LocalPuts("Missing type: " + obj.Type);
                    break;
            }
            return null;
        }

        private void LocalPuts(string msg)
        {
            Puts("{0}: {1}", Title, msg);
        }

        [ConsoleCommand("loot.reload")]
        void cmdConsoleReload(ConsoleSystem.Arg arg)
        {
            if (!LoadConfig())
                return;
            CheckConfig();
            UpdateLoot();
            LocalPuts("Loot config reloaded.");
        }

        class DynamicContractResolver : DefaultContractResolver
        {
            private static bool IsAllowed(JsonProperty property)
            {
                return property.PropertyType.IsPrimitive||
                        property.PropertyType == typeof(ItemAmount[]) ||
                        property.PropertyType == typeof(ItemDefinition) ||
                        property.PropertyType == typeof(String) ||
                        property.PropertyType == typeof(LootSpawn) ||
                        property.PropertyType == typeof(LootSpawn.Entry[]);
            }

            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                var properties = base.CreateProperties(type, memberSerialization);
                return properties.Where(p => (p.DeclaringType == type || p.DeclaringType == typeof(LootContainer)) && IsAllowed(p)).ToList();
            }
        }
    }
}
