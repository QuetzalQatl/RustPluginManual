using Oxide.Core;
//using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using System.Text;


namespace Oxide.Plugins
{
    [Info("MazeGen", "4seti [Lunatiq] for Rust Planet", "0.1.2", ResourceId = 947)]
    public class MazeGen : RustPlugin
    {

        #region Utility Methods

        private void Log(string message)
        {
            Puts("{0}: {1}", Title, message);
        }

        private void Warn(string message)
        {
            PrintWarning("{0}: {1}", Title, message);
        }

        private void Error(string message)
        {
            PrintError("{0}: {1}", Title, message);
        }

        #endregion

        #region VARS
        [PluginReference] Plugin ZoneManager;
        [PluginReference] Plugin RemoverTool;
        private Quaternion currentRot;
        static FieldInfo supports = typeof(BuildingBlock).GetField("supports", (BindingFlags.Instance | BindingFlags.NonPublic));
        private FieldInfo serverinput = typeof(BasePlayer).GetField("serverInput", BindingFlags.NonPublic | BindingFlags.Instance);
        private FieldInfo keycode = typeof(KeyLock).GetField("keyCode", BindingFlags.NonPublic | BindingFlags.Instance);
        private FieldInfo codelock = typeof(CodeLock).GetField("code", BindingFlags.NonPublic | BindingFlags.Instance);
        private MethodInfo inventoryClear = typeof(ItemContainer).GetMethod("Clear", BindingFlags.NonPublic | BindingFlags.Instance);        
        private FieldInfo firstKeyCreated = typeof(KeyLock).GetField("firstKeyCreated", BindingFlags.NonPublic | BindingFlags.Instance);
        private object closestEnt;
        private Vector3 closestHitpoint;
        private Dictionary<string, Vector3> mazes = new Dictionary<string, Vector3>();
        private int mazeTPAcess = 2;
        private int tpUsageDelay = 60;

        private Dictionary<string, string> defMsg = new Dictionary<string, string>()
                {
                    {"Usage", "<color=#FFB300>Usage: /maze NAME xSize ySize zSize GenType(0-4)</color>"},
                    {"TooLarge", "<color=#FFB300>For now maximum size of Lab is 10.000 cells, in case of lags</color>"},
                    {"RemoveReq", "<color=#FFB300>RemoverTool required to do so!</color>"},
                    {"MazeRm", "<color=#FFB300>Usage: /maze_rm name, type /maze_list for all mazes avail</color>"},
                    {"MazeErrorNotFound", "<color=#FFB300>Maze with name \"{0}\" not found!</color>"},               
                    {"MazeNewError", "<color=#FFB300>Maze with name \"{0}\" already exists!</color>"},                    
                    {"MazeList", "<color=#81F23F>Next mazes are avaliable:</color>"},
                    {"MazeListEmpty", "<color=#FFB300>No Mazes avaliable!</color>"},
                    {"MazeNew", "<color=#81F23F>New maze created! SizeX: {0} - SizeY: {1} - SizeZ: {2}, Mode: {3}</color>"},
                    {"Enter_message", "<color=#81F23F>You are entering Labyrinth, good luck, stranger!</color>"},
                    {"TooSoonTP", "<color=#FFB300>You are trying to teleport too soon from last one!</color>"},
                    {"TPUsage", "<color=#FFB300>Usage: /maze_tp NAME, names are listed in /maze_list!</color>"}
                };
        private bool topFloor = false;
        private bool mazeAutoZone = true; 
        private bool EntranceExit = true;
        private bool L_Ladders = true;
        #endregion

        void Loaded()
        {
            Log("Loaded");
        }
		
		private Dictionary<string, string> messages = new Dictionary<string,string>();
        private Dictionary<string, DateTime> mazeTPUsage = new Dictionary<string, DateTime>();
		
		protected override void LoadDefaultConfig()
        {
            Warn("Creating a new config file");
            Config.Clear();
            LoadVariables();
        }
		
		// Gets a config value of a specific type
        private T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null)
                return defaultValue;
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }
		
		[HookMethod("OnServerInitialized")]
        void OnServerInitialized()
        {
            try
            {
                LoadConfig();               
                var version = GetConfig<Dictionary<string, object>>("version", null);
                VersionNumber verNum = new VersionNumber(Convert.ToUInt16(version["Major"]), Convert.ToUInt16(version["Minor"]), Convert.ToUInt16(version["Patch"]));
                var cfgMessages = GetConfig<Dictionary<string, object>>("messages", null);               
                if (cfgMessages != null)
                    foreach (var pair in cfgMessages)
                        messages[pair.Key] = Convert.ToString(pair.Value);

                //Booleans
                mazeTPAcess = GetConfig<int>("mazeTPAcess", 2);
                mazeAutoZone = GetConfig<bool>("mazeAutoZone", true);
                EntranceExit = GetConfig<bool>("EntranceExit", true);
                tpUsageDelay = GetConfig<int>("tpUsageDelay", 30);
                L_Ladders = GetConfig<bool>("L_Ladders", true);
                topFloor = GetConfig<bool>("topFloor", false);

                if (verNum < Version || messages.Count < defMsg.Count)
                {
                    foreach (var pair in defMsg)
                        if (!messages.ContainsKey(pair.Key))
                            messages[pair.Key] = pair.Value;
                    Config["version"] = Version;
                    Config["messages"] = messages;
                    Config["mazeTPAcess"] = mazeTPAcess;
                    Config["EntranceExit"] = EntranceExit;
                    Config["mazeAutoZone"] = mazeAutoZone;
                    Config["tpUsageDelay"] = tpUsageDelay;
                    Config["L_Ladders"] = L_Ladders;
                    SaveConfig();
                    Warn("Config version updated to: " + Version.ToString() + " please check it");
                }
                LoadMazesData();
            }
            catch (Exception ex)
            {
                Error("OnServerInitialized failed: " + ex.Message);
            }
            
        }

		void LoadVariables()
        {
            Config["messages"] = defMsg;
            Config["topFloor"] = topFloor;
            Config["mazeTPAcess"] = mazeTPAcess;
            Config["mazeAutoZone"] = mazeAutoZone;
            Config["EntranceExit"] = EntranceExit;
            Config["L_Ladders"] = L_Ladders;
            Config["version"] = Version;
        }

        [ChatCommand("maze_tp")]
        void cmdMazeTP(BasePlayer player, string cmd, string[] args)
        {
            if (player.net.connection.authLevel < mazeTPAcess) return;
            if (args.Length == 0)
            {
                player.ChatMessage(messages["TPUsage"]);
                return;
            }
            string userID = player.userID.ToString();
            if (mazeTPUsage.ContainsKey(userID))
            {
                if ((DateTime.Now - mazeTPUsage[userID]).Minutes >= tpUsageDelay || hasAccess(player)) // TP For admins or for normal users with delay
                {
                    DoMazeTP(player, args[0], userID);
                }
                else
                {
                    player.ChatMessage(messages["TooSoonTP"]);
                }

            }
            else
            {
                DoMazeTP(player, args[0], userID);
            }
        }

        private void DoMazeTP(BasePlayer player, string name, string userID)
        {
            if (mazes.ContainsKey(name))
            {
                ForcePlayerPosition(player, mazes[name]);
                mazeTPUsage[userID] = DateTime.Now;
            }
            else
                player.ChatMessage(string.Format(messages["MazeErrorNotFound"], name));
        }



        [ChatCommand("maze_rm")]
        void cmdMazeRm(BasePlayer player, string cmd, string[] args)       
        {
            if (!hasAccess(player)) return;
            if (RemoverTool == null)
            {
                player.ChatMessage(messages["RemoveReq"]);
                return;
            }
            if (args.Length > 0)
            {
                string maze = args[0];
                if (mazes.ContainsKey(maze))
                {
                    var vector = mazes[maze];
                    RemoverTool.Call("RemoveAllFrom", vector);
                    mazes.Remove(maze);
                    if (ZoneManager != null) ZoneManager.Call("EraseZone", maze);
                    SaveMazesData();
                    player.ChatMessage("Maze removed!");
                }
                else
                {
                    player.ChatMessage(string.Format(messages["MazeErrorNotFound"], maze));
                }
            }
            else
            {
                player.ChatMessage(messages["MazeRm"]);
            }
            
        }
        [ChatCommand("maze_list")]
        void cmdMazeList(BasePlayer player, string cmd, string[] args)
        {
            if (player.net.connection.authLevel < mazeTPAcess) return;
            if (mazes.Count > 0)
            {
                player.ChatMessage(messages["MazeList"]);
                foreach (var maze in mazes)
                {
                    player.ChatMessage(maze.Key);
                }
            }
            else
                player.ChatMessage(messages["MazeListEmpty"]);
        }
        [ChatCommand("maze")]
        void cmdMaze(BasePlayer player, string cmd, string[] args)
        {
            // Check if the player is an admin.
            if (!hasAccess(player)) return;

            if (args == null || args.Length == 0)
            {
                player.ChatMessage(messages["Usage"]);
                return;
            }

            if (args.Length > 4)
            {
                string name = args[0];
                if (mazes.ContainsKey(name))
                {
                    player.ChatMessage(string.Format(messages["MazeNewError"], name));
                    return;
                }

                int sx = Convert.ToInt32(args[1]);
                int sy = Convert.ToInt32(args[2]);
                int sz = Convert.ToInt32(args[3]);
                if (sx < 2 || sy < 2 || sz < 1)
                {
                    player.ChatMessage(messages["Usage"]);
                    return;
                }
                int genType = Convert.ToInt32(args[4]);
                if (sx * sy * sz > 10000)
                {
                    player.ChatMessage(messages["TooLarge"]);
                    return;
                }               

                // Adjust height so you don't automatically paste in the ground
                float heightAdjustment = 0.5f;

                // Get player camera view directly from the player
                if (!TryGetPlayerView(player, out currentRot))
                {
                    SendReply(player, "Couldn't find your eyes");
                    return;
                }

                // Get what the player is looking at
                if (!TryGetClosestRayPoint(player.transform.position, currentRot, out closestEnt, out closestHitpoint))
                {
                    SendReply(player, "Couldn't find any Entity");
                    return;
                }

                // Check if what the player is looking at is a collider
                var baseentity = closestEnt as Collider;
                if (baseentity == null)
                {
                    SendReply(player, "You are not looking at a Structure, or something is blocking the view.");
                    return;
                }

                closestHitpoint.y = closestHitpoint.y + heightAdjustment;
                
                List<object> structureData = new List<object>();
                List<object> deployData = new List<object>();
                Maze maze = new Maze(sx, sy, sz, genType);                               

                player.ChatMessage(string.Format(messages["MazeNew"], sx, sy, sz, genType));

                for (int x = 0; x < sx; x++)
                {
                    for (int y = 0; y < sy; y++)
                    {
                        for (int z = 0; z < sz; z++)
                        {
                            //Building Objects
                            structureData.AddRange(maze.CompleteMaze[x, y, z].GetCellObjects(sz, topFloor, EntranceExit, L_Ladders));
                            //Deployments - wall.low when L_Ladders = true, else - box + cupboards
                            deployData.AddRange(maze.CompleteMaze[x, y, z].GetCellDeploys(EntranceExit, L_Ladders));
                        }
                    }
                }
                if (EntranceExit)
                {
                    Vector3 OriginRotation = new Vector3(0f, currentRot.eulerAngles.y, 0f);
                    Quaternion OriginRot = Quaternion.Euler(OriginRotation); ;
                    Vector3 TempPos = OriginRot * (new Vector3(0f, 0f, 6f));
                    Vector3 NewPos = TempPos + closestHitpoint;
                    mazes.Add(name, NewPos);
                    structureData.AddRange(maze.GetEntranceExit());
                }
                else
                    mazes.Add(name, closestHitpoint);
                SaveMazesData();

                PasteBuilding(structureData, closestHitpoint, currentRot.eulerAngles.y, heightAdjustment);
                PasteDeployables(deployData, closestHitpoint, currentRot.eulerAngles.y, heightAdjustment, player);
                if (ZoneManager != null && mazeAutoZone)
                {
                    string zone_radius = ((int)Math.Pow((sx * 1.5f) * (sx * 1.5f) + (sy * 1.5f) * (sy * 1.5f), 0.5) + 15).ToString();
                    
                    string[] zoneargs = new string[] { "name", name, "eject", "false", "radius", zone_radius, "pvpgod", "true", 
                        "pvegod", "true", "sleepgod", "true", "enter_message", messages["Enter_message"], "undestr", "true", "nobuild", "true", "notp", "true", "nokits", 
                        "true", "nodeploy", "true", "nosuicide", "true" };
                    Vector3 zone_place = new Vector3(closestHitpoint.x + (sx / 2) * 3, closestHitpoint.y + (sy / 2) * 3, closestHitpoint.z);
                    ZoneManager.Call("CreateOrUpdateZone", name, zoneargs, zone_place);
                }
            }
            else
                player.ChatMessage(messages["Usage"]);
        }

        void SaveMazesData()
        {
            Dictionary<string, object> mazes_norm = new Dictionary<string, object>();
            Dictionary<string, float> posNormalized;
            foreach (var maze in mazes)
            {
                posNormalized = new Dictionary<string, float>();
                posNormalized.Add("x", maze.Value.x);
                posNormalized.Add("y", maze.Value.y);
                posNormalized.Add("z", maze.Value.z);
                mazes_norm.Add(maze.Key, posNormalized);
            }
            Interface.GetMod().DataFileSystem.WriteObject<Dictionary<string, object>>("MazeGen-data", mazes_norm);
            Log("Data Saved");
        }
        void LoadMazesData()
        {
            try
            {
                Dictionary<string, object> mazes_norm = new Dictionary<string, object>();
                Dictionary<string, object> posNormalized;
                mazes_norm = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<string, object>>("MazeGen-data");
                mazes = new Dictionary<string, Vector3>();
                foreach (var maze in mazes_norm)
                {
                    posNormalized = maze.Value as Dictionary<string, object>;
                    mazes.Add(maze.Key, new Vector3(Convert.ToSingle(posNormalized["x"]), Convert.ToSingle(posNormalized["y"]), 
                        Convert.ToSingle(posNormalized["z"])));
                }
            }
            catch
            {
                mazes = new Dictionary<string, Vector3>();
                Warn("Mazes data corrupted! New data created");
                SaveMazesData();
            }
        }

        #region copy-paste stolen methods :P thx to Reneb
        bool TryGetPlayerView(BasePlayer player, out Quaternion viewAngle)
        {
            viewAngle = new Quaternion(0f, 0f, 0f, 0f);
            var input = serverinput.GetValue(player) as InputState;
            if (input == null || input.current == null || input.current.aimAngles == Vector3.zero)
                return false;

            viewAngle = Quaternion.Euler(input.current.aimAngles);
            return true;
        }

        bool TryGetClosestRayPoint(Vector3 sourcePos, Quaternion sourceDir, out object closestEnt, out Vector3 closestHitpoint)
        {
            Vector3 sourceEye = sourcePos + new Vector3(0f, 1.5f, 0f);
            Ray ray = new Ray(sourceEye, sourceDir * Vector3.forward);

            var hits = Physics.RaycastAll(ray);
            float closestdist = 999999f;
            closestHitpoint = sourcePos;
            closestEnt = false;
            foreach (var hit in hits)
            {
                if (hit.distance < closestdist)
                {
                    closestdist = hit.distance;
                    closestEnt = hit.collider;
                    closestHitpoint = hit.point;
                }
            }
            if (closestEnt is bool)
                return false;
            return true;
        }

        void PasteBuilding(List<object> structureData, Vector3 targetPoint, float targetRot, float heightAdjustment)
        {
            Vector3 OriginRotation = new Vector3(0f, targetRot, 0f);
            //Quaternion OriginRot = Quaternion.EulerRotation(OriginRotation);
            Quaternion OriginRot = Quaternion.Euler(OriginRotation); ; 
            foreach (Dictionary<string, object> structure in structureData)
            {
                Dictionary<string, object> structPos = structure["pos"] as Dictionary<string, object>;
                Dictionary<string, object> structRot = structure["rot"] as Dictionary<string, object>;
                string prefabname = (string)structure["prefabname"];
                BuildingGrade.Enum grade = (BuildingGrade.Enum)structure["grade"];
                Quaternion newAngles = Quaternion.Euler((new Vector3(Convert.ToSingle(structRot["x"]), Convert.ToSingle(structRot["y"]), Convert.ToSingle(structRot["z"]))) + OriginRotation);
                Vector3 TempPos = OriginRot * (new Vector3(Convert.ToSingle(structPos["x"]), Convert.ToSingle(structPos["y"]), Convert.ToSingle(structPos["z"])));
                Vector3 NewPos = TempPos + targetPoint;
                GameObject newPrefab = GameManager.server.FindPrefab(prefabname);
                if (newPrefab != null)
                {
                    var block = SpawnStructure(newPrefab, NewPos, newAngles, grade);
                    block.enableStability = false;
                    //block.isClient = true;
                    //supports.SetValue(block, null);
                    //var supp = supports.GetValue(block) as BuildingBlock.Support;                
                    if (block && block.HasSlot(BaseEntity.Slot.Lock))
                    {
                        TryPasteLock(block, structure);
                    }
                }
            }
        }
        void PasteDeployables(List<object> deployablesData, Vector3 targetPoint, float targetRot, float heightAdjustment, BasePlayer player)
        {
            Vector3 OriginRotation = new Vector3(0f, targetRot, 0f);
            Quaternion OriginRot = Quaternion.Euler(OriginRotation);
            foreach (Dictionary<string, object> deployable in deployablesData)
            {

                Dictionary<string, object> structPos = deployable["pos"] as Dictionary<string, object>;
                Dictionary<string, object> structRot = deployable["rot"] as Dictionary<string, object>;
                string prefabname = (string)deployable["prefabname"];

                Quaternion newAngles = Quaternion.Euler((new Vector3(Convert.ToSingle(structRot["x"]), Convert.ToSingle(structRot["y"]), Convert.ToSingle(structRot["z"]))) + OriginRotation);
                Vector3 TempPos = OriginRot * (new Vector3(Convert.ToSingle(structPos["x"]), Convert.ToSingle(structPos["y"]), Convert.ToSingle(structPos["z"])));
                Vector3 NewPos = TempPos + targetPoint;

                GameObject newPrefab = GameManager.server.FindPrefab(prefabname);
                if (newPrefab != null)
                {
                    BaseEntity entity = GameManager.server.CreateEntity(newPrefab, NewPos, newAngles);
                    if (entity == null) return;
                    entity.SendMessage("SetDeployedBy", player, SendMessageOptions.DontRequireReceiver);
                    entity.Spawn(true);
                    if (entity.GetComponent<StorageContainer>())
                    {
                        var box = entity.GetComponent<StorageContainer>();
                        inventoryClear.Invoke(box.inventory, null);
                        var items = deployable["items"] as List<object>;
                        foreach (var itemDef in items)
                        {
                            var item = itemDef as Dictionary<string, object>;
                            var i = ItemManager.CreateByItemID(Convert.ToInt32(item["id"]), Convert.ToInt32(item["amount"]), Convert.ToBoolean(item["blueprint"]));
                            i.MoveToContainer(box.inventory);
                        }

                        if (box.HasSlot(BaseEntity.Slot.Lock))
                            TryPasteLock(box, deployable);
                    }
                    else if (entity.GetComponent<Signage>())
                    {
                        var sign = entity.GetComponent<Signage>();
                        var signData = deployable["sign"] as Dictionary<string, object>;
                        sign.text = (string)signData["text"];
                        if (Convert.ToBoolean(signData["locked"]))
                            sign.SetFlag(BaseEntity.Flags.Locked, true);
                        sign.SendNetworkUpdate();
                    }
                }
                else
                {
                    SendReply(player, prefabname);
                }

            }
        }

        bool hasAccess(BasePlayer player)
        {
            if (player.net.connection.authLevel < 1)
            {
                SendReply(player, "You are not allowed to use this command");
                return false;
            }
            return true;
        }

        void TryPasteLock(BaseCombatEntity lockableEntity, IDictionary<string, object> structure)
        {
            BaseEntity lockentity = null;
            if (structure.ContainsKey("codelock"))
            {
                lockentity = GameManager.server.CreateEntity("build/locks/lock.code", Vector3.zero, new Quaternion());
                lockentity.OnDeployed(lockableEntity);
                var code = (string)structure["codelock"];
                if (!string.IsNullOrEmpty(code))
                {
                    var @lock = lockentity.GetComponent<CodeLock>();
                    codelock.SetValue(@lock, (string)structure["codelock"]);
                    @lock.SetFlag(BaseEntity.Flags.Locked, true);
                }
            }
            else if (structure.ContainsKey("keycode"))
            {
                lockentity = GameManager.server.CreateEntity("build/locks/lock.key", Vector3.zero, new Quaternion());
                lockentity.OnDeployed(lockableEntity);
                var code = Convert.ToInt32(structure["keycode"]);
                var @lock = lockentity.GetComponent<KeyLock>();
                if ((code & 0x80) != 0)
                {
                    // Set the keycode only if that lock had keys before. Otherwise let it be random.
                    keycode.SetValue(@lock, (code & 0x7F));
                    firstKeyCreated.SetValue(@lock, true);
                    @lock.SetFlag(BaseEntity.Flags.Locked, true);
                }

            }

            if (lockentity)
            {
                lockentity.gameObject.Identity();
                lockentity.SetParent(lockableEntity, "lock");
                lockentity.Spawn(true);
                lockableEntity.SetSlot(BaseEntity.Slot.Lock, lockentity);
            }
        }

        BuildingBlock SpawnStructure(GameObject prefab, Vector3 pos, Quaternion angles, BuildingGrade.Enum grade)
        {
            GameObject build = UnityEngine.Object.Instantiate(prefab);
            if (build == null) return null;
            BuildingBlock block = build.GetComponent<BuildingBlock>();
            if (block == null) return null;
            block.transform.position = pos;
            block.transform.rotation = angles;
            block.gameObject.SetActive(true);
            block.blockDefinition = PrefabAttribute.server.Find<Construction>(block.prefabID);
            block.Spawn(true);
            block.SetGrade(grade);
            block.health = block.MaxHealth();
            return block;

        }
        #endregion
        public class Maze
        {           
            public MazeCell[,,] CompleteMaze;

            public Maze(int x_size, int y_size, int z_size, int genType = 0)
            {
                CompleteMaze = new MazeCell[x_size, y_size, z_size];
                for (int x = 0; x < x_size; x++)
                {
                    for (int y = 0; y < y_size; y++)
                    {
                        for (int z = 0; z < z_size; z++)
                        {
                            CompleteMaze[x, y, z] = new MazeCell(x, y, z);
                        }
                    }
                }
                GenerateMaze(x_size, y_size, z_size, genType);

            }
            private void GenerateMaze(int maxX, int maxY, int maxZ, int genType = 0)
            {
                System.Random rnd = new System.Random();
                int sX = rnd.Next(maxX), sY = rnd.Next(maxY), sZ = rnd.Next(maxZ); //Random Generation Start Point attempt
                //int sX = 0, sY = 0, sZ = 0;
                CompleteMaze[0, 0, 0].WBot = false;
               
                List<MazeCell> mList = new List<MazeCell>();                
                
                mList.Add(CompleteMaze[rnd.Next(maxX), rnd.Next(maxY), rnd.Next(maxZ)]);
                CompleteMaze[sX, sY, sZ].visited = true;

                if (maxZ > 1)
                    CompleteMaze[maxX - 1, maxY - 1, rnd.Next(1, maxZ)].WTop = false;
                else
                    CompleteMaze[maxX - 1, maxY - 1, 0].WTop = false;
                int index = 0;
                int dir = 0;
                int nX = 0, nY = 0, nZ = 0;
                int tX, tY, tZ;
                bool found = false;

                //TEST - making UP/Down ways much less to apear (attempt atleast) :P
                int floor_chance_init = (int)((maxX * maxY * maxZ)/50) > 0 ? (int)((maxX * maxY * maxZ)/50) : 1;
                int floor_chance = floor_chance_init;
                int floor_switch = 1;
                int norm_item = 0;
                do
                {
                    switch (genType)
                    {
                        case 0:
                            //Latest
                            index = mList.Count - 1;
                            break;
                        case 1:
                            //Random!
                            index = rnd.Next(mList.Count);
                            break;
                        case 2:
                            if (rnd.Next(2) == 1) //50/50 Newest/Random
                                index = rnd.Next(mList.Count);
                            else
                                index = rnd.Next(mList.Count - 1);
                            break;
                        case 3:
                            if (rnd.Next(4) == 1)//75/25 Newest/Random
                                index = rnd.Next(mList.Count);
                            else
                                index = rnd.Next(mList.Count - 1);
                            break;
                        case 4:
                            if (rnd.Next(4) != 1)//25/75 Newest/Random
                                index = rnd.Next(mList.Count);
                            else
                                index = rnd.Next(mList.Count - 1);
                            break;
                        default:
                            index = mList.Count - 1;
                            break;
                    }

                    nX = mList[index].x;
                    nY = mList[index].y;
                    nZ = mList[index].z;
                    //int[] randomNumbers;
                    var randomNumbers = Enumerable.Range(0, 5).OrderBy(x => rnd.Next()).Take(5).ToList();
                    //var randomNumbers = Enumerable.Range(0, 6).OrderBy(x => rnd.Next()).Take(6).ToList();
                    found = false;
                    tX = nX; tY = nY; tZ = nZ;

                    foreach (int item in randomNumbers)
                    {
                        norm_item = item;
                        if (item == 4)
                        {                            
                            int up_down = rnd.Next(floor_chance + 1);
                            if (up_down == floor_chance)
                            {                                
                                norm_item = 4;
                            }
                            else if (up_down == floor_chance - 1)
                            {
                                norm_item = 5;
                            }
                            else
                                continue;
                        }
                        switch (norm_item)
                        {
                            case 0:
                                tY = nY + 1; tX = nX; tZ = nZ; found = true;
                                break;
                            case 1:
                                tY = nY; tX = nX + 1; tZ = nZ; found = true;
                                break;
                            case 2:
                                tY = nY - 1; tX = nX; tZ = nZ; found = true;
                                break;
                            case 3:
                                tY = nY; tX = nX - 1; tZ = nZ; found = true;
                                break;
                            case 4:
                                tY = nY; tX = nX; tZ = nZ + 1; found = true;
                                break;
                            case 5:
                                tY = nY; tX = nX; tZ = nZ - 1; found = true;
                                break;
                        }

                        if (tY >= 0 && tX >= 0 && tZ >= 0 && tX < maxX && tY < maxY && tZ < maxZ && found)
                        {
                            if (!CompleteMaze[tX, tY, tZ].visited)//Double Floor check
                                if ((tZ - nZ > 0 && CompleteMaze[tX, tY, tZ].Floor) || (tZ - nZ < 0 && CompleteMaze[nX, nY, nZ].Floor) || tZ - nZ == 0)
                                {
                                    if (tZ - nZ > 0 && tZ > 1)
                                    {
                                        if (CompleteMaze[tX, tY, tZ - 2].Floor)
                                            dir = norm_item;
                                            break;
                                    }
                                    else
                                    {
                                        dir = norm_item;
                                        break;
                                    }
                                }
                        }
                        found = false;
                    }
                    if (!found)
                    {
                        if (mList.Count == 1)//Temp check for tricky params when maze generation being corrupted
                            if (CheckAroundAvail(mList[0]))
                            {
                                if (mList[0].z < CompleteMaze.GetLength(2) - 1)
                                {
                                    if (!CompleteMaze[mList[0].x, mList[0].y, mList[0].z + 1].visited)
                                    {
                                        CompleteMaze[nX, nY, nZ].Floor = false;
                                        mList.Add(CompleteMaze[nX, nY, nZ + 1]);
                                        CompleteMaze[nX, nY, nZ + 1].visited = true;
                                    }
                                }
                                else if (mList[0].z > 0)
                                {
                                    if (!CompleteMaze[mList[0].x, mList[0].y, mList[0].z - 1].visited)
                                    {
                                        CompleteMaze[nX, nY, nZ - 1].Floor = false;
                                        mList.Add(CompleteMaze[nX, nY, nZ - 1]);
                                        CompleteMaze[nX, nY, nZ - 1].visited = true;
                                    }
                                }
                            }
                            else
                                mList.RemoveAt(index);
                        else
                            mList.RemoveAt(index);
                    }
                    else
                    {
                        for (int m = mList.Count - 1; m > 0; m--) //Test Algorythm, should increase passage length for 1-4 creation variants
                        {
                            if (!CheckAroundAvail(mList[m]))
                                mList.RemoveAt(m);
                        }
                        if (dir == 4 || dir == 5)
                        {
                            floor_switch++;
                            floor_chance *= floor_switch;
                        }
                        else
                        {
                            if (floor_chance > 100) floor_chance = (int)Math.Sqrt(floor_chance);
                            else if (floor_chance > floor_chance_init * 2)
                                floor_chance = floor_chance - 1 > 0 ? floor_chance - 1 : 3;
                        }
                        switch (dir)
                        {
                            case 0:
                                CompleteMaze[nX, nY, nZ].WTop = false;
                                CompleteMaze[tX, tY, tZ].WBot = false;
                                break;
                            case 1:
                                CompleteMaze[nX, nY, nZ].WRight = false;
                                CompleteMaze[tX, tY, tZ].WLeft = false;
                                break;
                            case 2:
                                CompleteMaze[nX, nY, nZ].WBot = false;
                                CompleteMaze[tX, tY, tZ].WTop = false;
                                break;
                            case 3:
                                CompleteMaze[nX, nY, nZ].WLeft = false;
                                CompleteMaze[tX, tY, tZ].WRight = false;
                                break;
                            case 4:
                                CompleteMaze[nX, nY, nZ].Floor = false;
                                break;
                            case 5:
                                CompleteMaze[tX, tY, tZ].Floor = false;
                                break;
                        }
                        CompleteMaze[tX, tY, tZ].visited = true;
                        mList.Add(CompleteMaze[tX, tY, tZ]);
                    }
                } while (mList.Count > 0);
            }
            private bool CheckAroundAvail(MazeCell mc)
            {
                if (mc.x > 0)                
                    if (!CompleteMaze[mc.x - 1, mc.y, mc.z].visited)
                        return true;
                if (mc.y > 0)
                    if (!CompleteMaze[mc.x, mc.y - 1, mc.z].visited)
                        return true;
                if (mc.z > 0)
                    if (!CompleteMaze[mc.x, mc.y, mc.z - 1].visited)
                        return true;
                if (mc.x < CompleteMaze.GetLength(0) - 1)
                    if (!CompleteMaze[mc.x + 1, mc.y, mc.z].visited)
                        return true;
                if (mc.y < CompleteMaze.GetLength(1) - 1)
                    if (!CompleteMaze[mc.x, mc.y + 1, mc.z].visited)
                        return true;
                if (mc.z < CompleteMaze.GetLength(2) - 1)
                    if (!CompleteMaze[mc.x, mc.y, mc.z + 1].visited)
                        return true;

                return false;
            }

            private static object GetDeploy(string dName, Vector3 playerRot, Vector3 pos)
            {
                Dictionary<string, object> data = new Dictionary<string, object>();
                Dictionary<string, object> posCleanData = new Dictionary<string, object>();
                Dictionary<string, object> rotCleanData = new Dictionary<string, object>();

                if (dName == "items/large_woodbox_deployed")
                    data.Add("items", new List<object>());

                data.Add("prefabname", dName);

                posCleanData.Add("x", pos.x);
                posCleanData.Add("y", pos.y);
                posCleanData.Add("z", pos.z);
                data.Add("pos", posCleanData);

                rotCleanData.Add("x", playerRot.x);
                rotCleanData.Add("y", playerRot.y);
                rotCleanData.Add("z", playerRot.z);
                data.Add("rot", rotCleanData);
                return data;
            }

            public List<object> GetEntranceExit()
            {
                List<object> entranceExit = new List<object>();
                Vector3 playerRot = new Vector3(0, 0, 0);
                Vector3 blockV3 = new Vector3(0, 0, 6f);
                entranceExit.Add(GetBlock("build/foundation", 4, playerRot, blockV3));
                blockV3 = new Vector3(0, 0, 3f);
                entranceExit.Add(GetBlock("build/foundation", 4, playerRot, blockV3));
                blockV3 = new Vector3(0, 0, 0);
                entranceExit.Add(GetBlock("build/foundation", 4, playerRot, blockV3));
                blockV3 = new Vector3(0, 0, 4.5f);
                entranceExit.Add(GetBlock("build/wall", 4, new Vector3(0, -90f, 0), blockV3));
                blockV3 = new Vector3(-1.5f, 0, 6f);
                entranceExit.Add(GetBlock("build/wall", 4, playerRot, blockV3));
                blockV3 = new Vector3(1.5f, 0, 6f);
                entranceExit.Add(GetBlock("build/wall", 4, playerRot, blockV3)); 
                blockV3 = new Vector3(0, 0, 3f);
                entranceExit.Add(GetBlock("build/block.halfheight", 4, playerRot, blockV3));
                blockV3 = new Vector3(0, 1.5f, 3f);
                entranceExit.Add(GetBlock("build/block.halfheight.slanted", 4, new Vector3(0f, 180f, 0f), blockV3));
                blockV3 = new Vector3(0, 0, 0f);
                entranceExit.Add(GetBlock("build/block.halfheight.slanted", 4, new Vector3(0f, 180f, 0f), blockV3)); 

                return entranceExit;
            }

            private static object GetBlock(string bName, int grade, Vector3 playerRot, Vector3 pos)
            {
                Dictionary<string, object> data = new Dictionary<string, object>();
                Dictionary<string, object> posCleanData = new Dictionary<string, object>();
                Dictionary<string, object> rotCleanData = new Dictionary<string, object>();


                data.Add("prefabname", bName);
                data.Add("grade", grade);

                posCleanData.Add("x", pos.x);
                posCleanData.Add("y", pos.y);
                posCleanData.Add("z", pos.z);
                data.Add("pos", posCleanData);

                rotCleanData.Add("x", playerRot.x);
                rotCleanData.Add("y", playerRot.y);
                rotCleanData.Add("z", playerRot.z);
                data.Add("rot", rotCleanData);
                return data;
            }

            public class MazeCell
            {
                public bool WTop = true;
                public bool WLeft = true;
                public bool WRight = true;
                public bool WBot = true;
                public bool visited = false;
                public bool Floor = true;
                public int x, y, z;
                public MazeCell(int _x, int _y, int _z)
                {
                    x = _x;
                    y = _y;
                    z = _z;
                }
                public List<object> GetCellDeploys(bool EntranceExit = true, bool L_Ladders = true)
                {
                    List<object> cellDeploys = new List<object>();
                    Vector3 playerRot = new Vector3(0, 0, 0);
                    Vector3 blockV3;
                    float zShift = 3f;
                    float xShift = 3f;
                    float yShift = 3f;
                    float entranceShift = EntranceExit ? 9f : 0f;
                    if (!this.Floor && !L_Ladders)
                    {
                        //Crazy way to create random rotated ladder
                        blockV3 = new Vector3(x * xShift + xShift / 4, z * zShift, y * yShift + yShift / 4 + entranceShift);
                        cellDeploys.Add(GetDeploy("items/cupboard.tool.deployed", playerRot, blockV3));
                        blockV3 = new Vector3(x * xShift - xShift / 5, z * zShift, y * yShift + yShift / 4 + entranceShift);
                        cellDeploys.Add(GetDeploy("items/large_woodbox_deployed", playerRot, blockV3));

                    }
                    return cellDeploys;
                }

                public List<object> GetCellObjects(int zMax = 1, bool topFloor = false, bool EntranceExit = true, bool L_Ladders = true)
                {
                    List<object> cellObjects = new List<object>();
                    float zShift = 3f;
                    float xShift = 3f;
                    float yShift = 3f;
                    float entranceShift = EntranceExit ? 9f : 0f;
                    Vector3 wallRot = new Vector3(0, -90f, 0);                    
                    Vector3 playerRot = new Vector3(0, 0, 0);
                    Vector3 blockV3;
                    //Foundation
                    if (z == 0)
                    {
                        blockV3 = new Vector3(x * xShift, 0, y * yShift + entranceShift);
                        cellObjects.Add(GetBlock("build/foundation", 4, playerRot, blockV3));
                    }
                    if (x == 0 && this.WLeft)
                    {
                        blockV3 = new Vector3(-xShift / 2, z * zShift, y * yShift + entranceShift);
                        cellObjects.Add(GetBlock("build/wall", 4, playerRot, blockV3));                        
                    }

                    if (y == 0 && this.WBot)
                    {
                        blockV3 = new Vector3(x * xShift, z * zShift, -yShift / 2 + entranceShift);
                        cellObjects.Add(GetBlock("build/wall", 4, wallRot, blockV3));                        
                    }

                    //normal walls
                    //TOP
                    if (this.WTop)
                    {
                        blockV3 = new Vector3((x * xShift), z * zShift, y * yShift + yShift / 2 + entranceShift);
                        cellObjects.Add(GetBlock("build/wall", 4, wallRot, blockV3));
                    }
                    //Right
                    if (this.WRight)
                    {
                        blockV3 = new Vector3((x * xShift) + xShift / 2, z * zShift, y * yShift + entranceShift);
                        cellObjects.Add(GetBlock("build/wall", 4, playerRot, blockV3));
                    }

                    if (this.Floor && (z < zMax - 1 || topFloor))
                    {
                        blockV3 = new Vector3(x * xShift, z * zShift + zShift, y * yShift + entranceShift);
                        cellObjects.Add(GetBlock("build/floor", 4, playerRot, blockV3));                        
                    }
                    else if (z < zMax - 1 && L_Ladders)
                    {
                        if (!this.WBot)
                        {
                            //Low Wall ladder - Rotation |""
                            blockV3 = new Vector3(x * xShift - 0.4f, z * zShift + 0.9f, y * yShift - 0.7f + entranceShift);
                            cellObjects.Add(GetBlock("build/wall.low", 4, new Vector3(-35f, 0, 90f), blockV3)); //|
                            blockV3 = new Vector3(x * xShift + 0.3f, z * zShift + 2.3f, y * yShift + 0.3f + entranceShift);
                            cellObjects.Add(GetBlock("build/wall.low", 4, new Vector3(-30f, 90f, 90f), blockV3)); //""
                        }
                        else if (!this.WTop) //Rotation |_ 
                        {
                            //Low Wall
                            blockV3 = new Vector3(x * xShift - 0.3f, z * zShift + 0.9f, y * yShift + 0.7f + entranceShift);
                            cellObjects.Add(GetBlock("build/wall.low", 4, new Vector3(40f, 0, 90f), blockV3));//|
                            blockV3 = new Vector3(x * xShift + 0.3f, z * zShift + 2.3f, y * yShift - 1.2f + entranceShift);
                            cellObjects.Add(GetBlock("build/wall.low", 4, new Vector3(-30f, 90f, 90f), blockV3));//_
                        }
                        else if (!this.WLeft) //Rotation ""| 
                        {
                            blockV3 = new Vector3(x * xShift + 1.5f, z * zShift + 2.3f, y * yShift - 0.7f + entranceShift);
                            cellObjects.Add(GetBlock("build/wall.low", 4, new Vector3(30f, 0, 90f), blockV3)); //|
                            blockV3 = new Vector3(x * xShift - 0.7f, z * zShift + 0.9f, y * yShift + 0.3f + entranceShift);
                            cellObjects.Add(GetBlock("build/wall.low", 4, new Vector3(-35f, 90f, 90f), blockV3)); //""
                        }
                        else //Rotation |_
                        {
                            blockV3 = new Vector3(x * xShift - 0.4f, z * zShift + 2.3f, y * yShift + 0.6f + entranceShift);
                            cellObjects.Add(GetBlock("build/wall.low", 4, new Vector3(-35f, 0, 90f), blockV3));//|
                            blockV3 = new Vector3(x * xShift + 0.5f, z * zShift + 0.9f, y * yShift - 1.2f + entranceShift);
                            cellObjects.Add(GetBlock("build/wall.low", 4, new Vector3(40f, 90f, 90f), blockV3)); //""
                        }
                        ////Low Wall
                        //blockV3 = new Vector3(x * xShift-0.4f, z * zShift + zShift, y * yShift);
                        //cellObjects.Add(GetBlock("build/wall.low", 4, new Vector3(0f, 0, 90f), blockV3));                        

                        //blockV3 = new Vector3(x * xShift, z * zShift, y * yShift);
                        //cellObjects.Add(GetBlock("build/block.halfheight.slanted", 4, new Vector3(0, 0, 0), blockV3));                        
                    }

                    return cellObjects;

                }               
            }
        }

    }
}