// Reference: Oxide.Ext.Rust
// Reference: Newtonsoft.Json

using System.Collections.Generic;
using System.Linq;

using Rust;

using System;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using JSONObject = JSON.Object;
using JSONArray = JSON.Array;
using JSONValue = JSON.Value;
using JSONValueType = JSON.ValueType;

namespace Oxide.Plugins
{
    [Info("ConstructionConfig", "Nogrod", "1.0.1", ResourceId = 859)]
    class ConstructionConfig : RustPlugin
    {
        private string _configpath = "";

        void Loaded()
        {
            _configpath = Manager.ConfigPath + string.Format("\\{0}.json", Name);
        }

        void LoadDefaultConfig()
        {

        }

        private static JSONObject ToJsonObject(object obj)
        {
            return JSONObject.Parse(ToJsonString(obj));
        }

        private static JSONArray ToJsonArray(object obj)
        {
            return JSONArray.Parse(ToJsonString(obj));
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

        private static void StripObject(JSONObject obj)
        {
            if (obj == null) return;
            var keys = obj.Select(entry => entry.Key).ToList();
            foreach (var key in keys)
            {
                if (!key.Equals("shortname") && !key.Equals("itemid"))
                    obj.Remove(key);
            }
        }

        private static void StripArray(JSONArray arr, string key)
        {
            if (arr == null) return;
            foreach (var obj in arr)
            {
                StripObject(obj.Obj[key].Obj);
            }
        }

        private bool CreateDefaultConfig()
        {
            Config.Clear();
            Config["Version"] = Protocol.network;
            var constructions = new Dictionary<string, object>();
            Config["Constructions"] = constructions;
            var protectionProperties = new HashSet<ProtectionProperties>();
            var constructionPrefabs = PrefabAttribute.server.GetAll<Construction>();
            foreach (var construct in constructionPrefabs)
            {
                var construction = new Dictionary<string, object>();
                var grades = new Dictionary<string, object>();
                construction["costMultiplier"] = construct.costMultiplier;
                construction["healthMultiplier"] = construct.healthMultiplier;
                for (var g = 0; g < construct.grades.Length; g++)
                {
                    var grade = construct.grades[g];
                    if (grade == null) continue;
                    var dict = new Dictionary<string, object>();
                    dict["baseHealth"] = grade.gradeBase.baseHealth;
                    var costToBuild = ToJsonArray(grade.gradeBase.baseCost);
                    foreach (var cost in costToBuild)
                    {
                        cost.Obj["itemDef"] = cost.Obj.GetObject("itemDef").GetString("shortname", "unnamed");
                    }
                    dict["baseCost"] = JsonObjectToObject(costToBuild);
                    if (grade.gradeBase.damageProtecton != null)
                    {
                        protectionProperties.Add(grade.gradeBase.damageProtecton);
                    }
                    grades[((BuildingGrade.Enum)g).ToString()] = dict;
                }
                construction["grades"] = grades;
                constructions[construct.hierachyName] = construction;
            }
            var protections = new Dictionary<string, object>();
            Config["DamageProtections"] = protections;
            foreach (var protectionProperty in protectionProperties)
            {
                var damageProtection = new Dictionary<string, object>();
                for (var i = 0; i < protectionProperty.amounts.Length; i++)
                {
                    damageProtection[Enum.GetName(typeof(DamageType), i)] = protectionProperty.amounts[i];
                }
                protections[protectionProperty.name] = damageProtection;
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
            Config.Save(string.Format("{0}.old", _configpath));
            CreateDefaultConfig();
        }

        void OnServerInitialized()
        {
            if (!LoadConfig())
                return;
            CheckConfig();
            UpdateConstructions();
        }

        private void UpdateConstructions()
        {
            var constructions = Config["Constructions"] as Dictionary<string, object>;
            if (constructions == null)
            {
                LocalPuts("No constructions in config");
                return;
            }
            var oldGrades = new HashSet<BuildingGrade>();
            var protectionProperties = new HashSet<ProtectionProperties>();
            var constructionPrefabs = PrefabAttribute.server.GetAll<Construction>();
            var manager = SingletonComponent<ItemManager>.Instance;
            foreach (var common in constructionPrefabs)
            {
                if (constructions[common.hierachyName] == null)
                {
                    LocalPuts(common.hierachyName + " doesn't exist in config");
                    continue;
                }
                var construction = ObjectToJsonObject(constructions[common.hierachyName]);
                common.costMultiplier = construction.Obj.GetFloat("costMultiplier", 0);
                var healthChanged = common.healthMultiplier != construction.Obj.GetFloat("healthMultiplier", 0);
                common.healthMultiplier = construction.Obj.GetFloat("healthMultiplier", 0);
                var grades = construction.Obj.GetObject("grades");
                for (var g = 0; g < common.grades.Length; g++)
                {
                    var gradeType = (BuildingGrade.Enum) g;
                    if (!grades.ContainsKey(gradeType.ToString()))
                    {
                        common.grades[g] = null;
                        continue;
                    }
                    if (common.grades[g] == null)
                    {
                        LocalPuts("Can't create grade: " + gradeType + " for: " + common.hierachyName);
                        continue;
                    }
                    var grade = UnityEngine.Object.Instantiate(common.grades[g].gradeBase);
                    grade.name = grade.name.Replace("(Clone)", "");
                    oldGrades.Add(common.grades[g].gradeBase);
                    common.grades[g].gradeBase = grade;
                    var newGrade = grades.GetObject(gradeType.ToString());
                    UpdateConstructionHealth(grade, newGrade.GetFloat("baseHealth", 0), healthChanged);
                    grade.baseCost.Clear();
                    var costToBuild = newGrade.GetArray("baseCost");
                    foreach (var cost in costToBuild)
                    {
                        var itemid = cost.Obj.GetInt("itemid", 0);
                        var definition = manager.itemList.Find(x => x.itemid == itemid);
                        grade.baseCost.Add(new ItemAmount(definition, cost.Obj.GetFloat("amount", 0)));
                    }
                    if (grade.damageProtecton != null)
                    {
                        protectionProperties.Add(grade.damageProtecton);
                    }
                }
            }
            foreach (var oldGrade in oldGrades)
            {
                UnityEngine.Object.Destroy(oldGrade);
            }
            var protections = Config["DamageProtections"] as Dictionary<string, object>;
            if (protections == null)
                return;
            foreach (var protectionProperty in protectionProperties)
            {
                protectionProperty.Clear();
                var damageProtection = protections[protectionProperty.name] as Dictionary<string, object>;
                if (damageProtection == null) continue;
                foreach (var o in damageProtection)
                {
                    protectionProperty.Add((DamageType) Enum.Parse(typeof (DamageType), o.Key), (float)Convert.ToDouble(o.Value));
                }
            }
        }

        private void UpdateConstructionHealth(BuildingGrade grade, float newHealth, bool healthChanged)
        {
            if (!healthChanged && grade.baseHealth == newHealth) return;
            grade.baseHealth = newHealth;
            var bb = UnityEngine.Object.FindObjectsOfType<BuildingBlock>().Where(b => b.currentGrade.gradeBase == grade);
            foreach (var buildingBlock in bb)
            {
                buildingBlock.SetHealthToMax();
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

        [ConsoleCommand("construction.reload")]
        void cmdConsoleReload(ConsoleSystem.Arg arg)
        {
            if (!LoadConfig())
                return;
            CheckConfig();
            UpdateConstructions();
            LocalPuts("Config reloaded.");
        }

        [ConsoleCommand("construction.reset")]
        void cmdConsoleReset(ConsoleSystem.Arg arg)
        {
            if (!CreateDefaultConfig())
                return;
            UpdateConstructions();
        }

        class DynamicContractResolver : DefaultContractResolver
        {
            private static bool IsAllowed(JsonProperty property)
            {
                return property.PropertyType.IsPrimitive ||
                        property.PropertyType == typeof(List<ItemAmount>) ||
                        property.PropertyType == typeof(ItemDefinition) ||
                        property.PropertyType == typeof(BuildingGrade) ||
                        property.PropertyType == typeof(ConstructionGrade) ||
                        property.PropertyType == typeof(String);
            }

            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                var properties = base.CreateProperties(type, memberSerialization);
                return properties.Where(p => p.DeclaringType == type && IsAllowed(p)).ToList();
            }
        }
    }
}
