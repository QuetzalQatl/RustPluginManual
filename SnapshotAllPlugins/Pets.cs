// Reference: RustBuild
// Reference: Newtonsoft.Json

using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Pets", "Bombardir", "0.3.2", ResourceId = 851)]
    class Pets : RustPlugin
	{
        private static FieldInfo serverinput;
        private static MethodInfo SetDeltaTimeMethod;
        private static Pets PluginInstance;
        private static BUTTON MainButton;
        private static BUTTON SecondButton;
        private static Dictionary<string, PetInfo> SaveNpcList;
        public enum Act { Move, Attack, Eat, Follow, Sleep, None }

        #region NPC Controller Class

        public class NpcControl : MonoBehaviour 
		{
            private static float ButtonReload = 0.3f;
            private static float DrawReload = 0.05f;
            internal static float LootDistance = 1f;
            internal static float ReloadControl = 60f;
            internal static float MaxControlDistance = 10f;

            internal bool DrawEnabled;
            private InputState input;
            private float NextTimeToPress;
            private float NextTimeToControl;
            private float NextTimeToDraw;

            public NpcAI npc;
            public BasePlayer owner;
		
			void Awake() 
			{
                owner = GetComponent<BasePlayer>();
                input = serverinput.GetValue(owner) as InputState;
				enabled = false;
                NextTimeToPress = 0f;
                NextTimeToControl = 0f;
                NextTimeToDraw = 0f;
                DrawEnabled = GlobalDraw;
			}

            void OnAttacked(HitInfo info)
            {
                if (npc && info.Initiator && npc.action != Act.Attack)
                    npc.Attack(info.Initiator.GetComponent<BaseCombatEntity>());
            }

            void FixedUpdate()
            {
                float time = Time.realtimeSinceStartup;
                if (input.WasJustPressed(MainButton) && NextTimeToPress < time)
                {
                    NextTimeToPress = time + ButtonReload;
                    UpdateAction();
                }
                if (DrawEnabled && npc != null && npc.action < Act.Follow && NextTimeToDraw < time)
                {
                    NextTimeToDraw = time + DrawReload;
                    UpdateDraw();
                }
			}

            void UpdateDraw()
            {
                Vector3 drawpos = npc.action == Act.Move ? npc.targetpoint : npc.targetentity?.transform.position ?? Vector3.zero;
                if (drawpos != Vector3.zero)
                    owner.SendConsoleCommand("ddraw.arrow", new object[] { DrawReload + 0.02f, npc.action == Act.Move ? Color.cyan : npc.action == Act.Attack ? Color.red : Color.yellow, drawpos + new Vector3(0, 5f, 0), drawpos, 1.5f });
            }

            void UpdateAction()
            {
                if (npc != null && input.IsDown(SecondButton))
                    if (npc.action == Act.Follow)
                    {
                        owner.ChatMessage(UnFollowMsg);
                        npc.action = Act.None;
                    }
                    else
                    {
                        owner.ChatMessage(FollowMsg);
                        npc.Attack(owner.GetComponent<BaseCombatEntity>(), Act.Follow);
                    }
                else
                {
                    RaycastHit hit;
                    if (Physics.SphereCast(owner.eyes.position, 0.5f, Quaternion.Euler(input.current.aimAngles) * Vector3.forward, out hit) && hit.transform != transform)
                    {
                        if (npc == null)
                        {
                            BaseNPC hited = hit.transform.GetComponent<BaseNPC>();
                            if (hited != null)
                            {
                                NpcAI OwnedNpc = hited.GetComponent<NpcAI>();
                                if (OwnedNpc != null && OwnedNpc.owner != this)
                                    owner.ChatMessage(NoOwn);
                                else if (NextTimeToControl < Time.realtimeSinceStartup)
                                {
                                    if (!UsePermission || PluginInstance.HasPermission(owner, "can" + hited.modelPrefab.Remove(0, 12).Replace("_skin", "")))
                                    {
                                        if (hit.distance < MaxControlDistance)
                                        {
                                            NextTimeToControl = Time.realtimeSinceStartup + ReloadControl;
                                            owner.ChatMessage(NewPetMsg);
                                            npc = hited.gameObject.AddComponent<NpcAI>();
                                            npc.owner = this;
                                        }
                                        else
                                            owner.ChatMessage(CloserMsg);
                                    }
                                    else
                                        owner.ChatMessage(NoPermPetMsg);
                                }
                                else
                                    owner.ChatMessage(ReloadMsg);
                            }
                        }
                        else
                        {
                            BaseCombatEntity targetentity = hit.transform.GetComponent<BaseCombatEntity>();
                            if (targetentity == null)
                            {
                                npc.targetpoint = hit.point;
                                npc.action = Act.Move;
                            }
                            else
                            {
                                if (targetentity == (BaseCombatEntity)npc.Base)
                                {
                                    if (hit.distance <= LootDistance)
                                    {
                                        owner.inventory.loot.StartLootingEntity((BaseEntity)npc.Base, true);
                                        owner.inventory.loot.AddContainer(npc.inventory);
                                        owner.inventory.loot.SendImmediate();
                                        owner.ClientRPC(owner.net.connection, owner, "RPC_OpenLootPanel", "smallwoodbox");
                                    }
                                }
                                else if (targetentity is BaseCorpse)
                                {
                                    owner.ChatMessage(EatMsg);
                                    npc.Attack(targetentity, Act.Eat);
                                }
                                else
                                {
                                    owner.ChatMessage(AttackMsg);
                                    npc.Attack(targetentity);
                                }
                            }
                        }
                    }
                }
            }
		}

        #endregion
        #region NPC AI Class

        public class NpcAI : MonoBehaviour
        {
            internal static float IgnoreTargetDistance = 70f;
            internal static float HealthModificator = 1.5f;
            internal static float AttackModificator = 2f;
            internal static float SpeedModificator = 1f;

            private static float PointMoveDistance = 1f;
            private static float TargetMoveDistance = 3f;

            private float lastTick;
            private float hungerLose;
            private float thristyLose;
            private float sleepLose;
            private double attackrange;

            internal Act action;
            internal Vector3 targetpoint;
            internal BaseCombatEntity targetentity;

            public NpcControl owner;
            public ItemContainer inventory;
            public BaseNPC Base;
            public NPCAI RustAI;
            public NPCMetabolism RustMetabolism;

            private void Move(Vector3 point)
            {
                Base.state = BaseNPC.State.Normal;
                RustAI.sense.Think();
                Base.steering.Move(Vector3Ex.XZ3D(point - transform.position).normalized, point, NPCSpeed.Gallop);
            }

            internal void OnAttacked(HitInfo info)
            {
                if (info.Initiator && info.Initiator != owner.owner && action != Act.Attack)
                    Attack(info.Initiator.GetComponent<BaseCombatEntity>());
            }

            internal void Attack(BaseCombatEntity ent, Act active = Act.Attack)
            {
                targetentity = ent;
                action = active;
                attackrange = Math.Pow(Vector3Ex.Max(BoundsExtension.XZ3D(Base._collider.bounds).extents) + Base.attack.range + Vector3Ex.Max(BoundsExtension.XZ3D(ent._collider.bounds).extents), 2);
            }

            void Awake()
            {
               RustAI = GetComponent<NPCAI>();
               RustAI.ServerDestroy();
               RustMetabolism = GetComponent<NPCMetabolism>();
               Base = GetComponent<BaseNPC>();
               lastTick = Time.time;
               targetpoint = Vector3.zero;
               action = Act.None;
               hungerLose = RustMetabolism.calories.max*2 / 12000;
               thristyLose = RustMetabolism.hydration.max*3 / 12000;
               sleepLose = RustMetabolism.sleep.max / 12000;
               inventory = new ItemContainer();
               inventory.ServerInitialize((Item)null, 6);
               Base.InitializeHealth(Base.health * HealthModificator, Base.MaxHealth() * HealthModificator);
               Base.locomotion.gallopSpeed *= SpeedModificator;
               Base.locomotion.trotSpeed *= SpeedModificator;
               Base.locomotion.acceleration *= SpeedModificator;
            }

            void FixedUpdate()
            {
                SetDeltaTimeMethod.Invoke( RustAI, new object[] { Time.time - lastTick });
                if ((double)RustAI.deltaTime >= (double)server.NPCTickDelta())
                {
                    lastTick = Time.time;
                    if (!Base.IsStunned())
                    {
                        Base.Tick();

                        if (action != Act.Sleep)
                        {
                            RustMetabolism.sleep.MoveTowards(0.0f, RustAI.deltaTime * sleepLose);
                            RustMetabolism.hydration.MoveTowards(0.0f, RustAI.deltaTime * thristyLose);
                            RustMetabolism.calories.MoveTowards(0.0f, RustAI.deltaTime * hungerLose);
                        }

                        if (action != Act.None)
                            if (action == Act.Move)
                                if (Vector3.Distance(transform.position, targetpoint) < PointMoveDistance)
                                    action = Act.None;
                                else
                                    Move(targetpoint);
                            else if (action == Act.Sleep)
                            {
                                Base.state = BaseNPC.State.Sleeping;
                                Base.sleep.Recover(2f);
                                RustMetabolism.stamina.Run(4f);
                                Base.StartCooldown(2f, true);
                            }
                            else if (targetentity == null)
                            {
                                action = Act.None;
                                Base.state = BaseNPC.State.Normal;
                            }
                            else
                            {
                                float distance = Vector3.Distance(transform.position, targetentity.transform.position);
                                if (distance < IgnoreTargetDistance)
                                {
                                    if (action != Act.Follow && distance <= attackrange)
                                    {
                                        Vector3 normalized = Vector3Ex.XZ3D(targetentity.transform.position - transform.position).normalized;
                                        if (action == Act.Eat)
                                        {
                                            if (Base.diet.Eat(targetentity))
                                            {
                                                Base.Heal(Base.MaxHealth() * 0.01f);
                                                RustMetabolism.calories.Add(RustMetabolism.calories.max * 0.03f);
                                                RustMetabolism.hydration.Add(RustMetabolism.hydration.max * 0.03f);
                                            }
                                        }
                                        else if (Base.attack.Hit(targetentity, (targetentity is BaseNPC ? 1f : 2f) * AttackModificator, false))
                                            transform.rotation = Quaternion.LookRotation(normalized);
                                        Base.steering.Face(normalized);
                                    }
                                    else if (action != Act.Follow || distance > TargetMoveDistance && distance > attackrange)
                                        Move(targetentity.transform.position);
                                }
                                else
                                    action = Act.None;
                            }
                    }
                }
            }

            void OnDestroy ()
            {
                Base.InitializeHealth(Base.health / HealthModificator, Base.MaxHealth() / HealthModificator);
                Base.locomotion.gallopSpeed /= SpeedModificator;
                Base.locomotion.trotSpeed /= SpeedModificator;
                Base.locomotion.acceleration /= SpeedModificator;
                DropUtil.DropItems(inventory, transform.position);
                SaveNpcList.Remove(owner.owner.userID.ToString());
                RustAI.ServerInit();
            }
        }
        #endregion
        #region PetInfo Object to Save
        public class PetInfo
        {
            public uint prefabID;
            public uint parentNPC;
            public float x, y, z;
            public byte[] inventory;
            internal bool NeedToSpawn;

            public PetInfo() 
            {
                NeedToSpawn = true;
            }

            public PetInfo(NpcAI pet)
            {
                x = pet.transform.position.x;
                y = pet.transform.position.y;
                z = pet.transform.position.z;
                prefabID = pet.Base.prefabID;
                parentNPC = pet.Base.net.ID;
                inventory = pet.inventory.Save().ToProtoBytes();
                NeedToSpawn = false;
            }
        }
        #endregion

        #region Config & Initialisation

        private static bool UsePermission = true;
        private static bool GlobalDraw = true;
        private static string CfgButton = "USE";
        private static string CfgSecButton = "RELOAD";
        private static string ReloadMsg = "You can not tame so often! Wait!";
        private static string NewPetMsg = "Now you have a new pet!";
        private static string CloserMsg = "You need to get closer!";
        private static string NoPermPetMsg = "You don't have permission to take this NPC!";
        private static string FollowMsg = "Follow command!";
        private static string UnFollowMsg = "UnFollow command!";
        private static string SleepMsg = "Sleep command!";
        private static string AttackMsg = "Attack!";
        private static string NoPermMsg = "No Permission!";
        private static string ActivatedMsg = "NPC Mode activated!";
        private static string DeactivatedMsg = "NPC Mode deactivated!";
        private static string NotNpc = "You don't have a pet!";
        private static string NpcFree = "Now your per is free!";
        private static string NoOwn = "This npc is already tamed by other player!";
        private static string EatMsg = "Time to eat!";
        private static string DrawEn = "Draw enabled!";
        private static string DrawDis = "Draw disabled!";
        private static string DrawSysDis = "Draw system was disabled by administrator!";
        private static string InfoMsg = "<color=red>Health: {health}%</color>, <color=orange>Hunger: {hunger}%</color>, <color=cyan>Thirst: {thirst}%</color>, <color=teal>Sleepiness: {sleep}%</color>, <color=lightblue>Stamina: {stamina}%</color>";

        void LoadDefaultConfig() { }

        void Init()
        {
            serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Instance | BindingFlags.NonPublic));
            SetDeltaTimeMethod = typeof(NPCAI).GetProperty("deltaTime", (BindingFlags.Public | BindingFlags.Instance)).GetSetMethod(true);
            PluginInstance = this;

            CheckCfg<bool>("Use permissions", ref UsePermission);
            CheckCfg<bool>("Enable draw system", ref GlobalDraw);
            CheckCfg<string>("Main button to controll pet", ref CfgButton);
            CheckCfg<string>("Second button to use follow|unfollow", ref CfgSecButton);
            CheckCfg<float>("Reload time to take new npc", ref NpcControl.ReloadControl);
            CheckCfg<float>("Max distance to take npc", ref NpcControl.MaxControlDistance);
            CheckCfg<float>("Distance to loot npc", ref NpcControl.LootDistance);
            CheckCfg<float>("Distance when target will be ignored by NPC", ref NpcAI.IgnoreTargetDistance);
            CheckCfg<float>("Pet's Health Modificator", ref NpcAI.HealthModificator);
            CheckCfg<float>("Pet's Attack Modificator", ref NpcAI.AttackModificator);
            CheckCfg<float>("Pet's Speed Modificator", ref NpcAI.SpeedModificator);
            CheckCfg<string>("New pet msg", ref NewPetMsg);
            CheckCfg<string>("Closer msg", ref CloserMsg);
            CheckCfg<string>("No take perm msg", ref NoPermPetMsg);
            CheckCfg<string>("Follow msg", ref FollowMsg);
            CheckCfg<string>("UnFollow msg", ref UnFollowMsg);
            CheckCfg<string>("Sleep msg", ref SleepMsg);
            CheckCfg<string>("Attack msg", ref AttackMsg);
            CheckCfg<string>("No command perm msg", ref NoPermMsg);
            CheckCfg<string>("Activated msg", ref ActivatedMsg);
            CheckCfg<string>("Deactivated msg", ref DeactivatedMsg);
            CheckCfg<string>("Reload msg", ref ReloadMsg);
            CheckCfg<string>("No pet msg", ref NotNpc);
            CheckCfg<string>("Free pet msg", ref NpcFree);
            CheckCfg<string>("Already tamed msg", ref NoOwn);
            CheckCfg<string>("Eat msg", ref EatMsg);
            CheckCfg<string>("Draw enabled msg", ref DrawEn);
            CheckCfg<string>("Draw disabled msg", ref DrawDis);
            CheckCfg<string>("Draw system disabled msg", ref DrawSysDis);
            CheckCfg<string>("Info msg", ref InfoMsg);
            SaveConfig();

            MainButton = ConvertStringToButton(CfgButton);
            SecondButton = ConvertStringToButton(CfgSecButton);

            if (UsePermission)
            {
                permission.RegisterPermission("cannpc", this);
                permission.RegisterPermission("canstag", this);
                permission.RegisterPermission("canbear", this);
                permission.RegisterPermission("canwolf", this);
                permission.RegisterPermission("canchicken", this);
                permission.RegisterPermission("canboar", this);
            }

            try { SaveNpcList = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<string, PetInfo>>("Pets"); } catch { }
            if (SaveNpcList == null) SaveNpcList = new Dictionary<string, PetInfo>();
        }

        #endregion

        #region Unload Hook (destroy all plugin's objects)

        void Unload()
		{
            DestroyAll<NpcControl>();
            DestroyAll<NpcAI>();
		}

        #endregion

        #region Hook OnAttacked for NpcAI

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity is BaseNPC)
            {
                NpcAI ai = entity.GetComponent<NpcAI>();
                if (ai != null)
                    ai.OnAttacked(hitInfo);
            }
        }

        #endregion

        #region Hook OnPlayerInit (load player's pet)

        void OnPlayerInit(BasePlayer player)
        {
            PetInfo info;
            if (SaveNpcList.TryGetValue(player.userID.ToString(), out info) && info.NeedToSpawn)
            {
                Puts("Loading pet...");
                BaseEntity pet = GameManager.server.CreateEntity(StringPool.Get(info.prefabID), new Vector3(info.x, info.y, info.z));
                if (pet != null)
                {
                    NpcControl comp = player.gameObject.AddComponent<NpcControl>();
                    pet.Spawn(true);
                    comp.npc = pet.gameObject.AddComponent<NpcAI>();
                    comp.npc.owner = comp;
                    comp.npc.inventory.Load(ProtoBuf.ItemContainer.Deserialize(info.inventory));
                    info.NeedToSpawn = false;
                }
            }
        }

        #endregion

        #region Hook OnServerInitialized (kill all pets, then spawn when owner will connect)

        void OnServerInitialized()
        {
            if (Time.realtimeSinceStartup < 100)
                foreach(KeyValuePair<string, PetInfo> entry in SaveNpcList)
                {
                    BaseNetworkable parent = BaseNetworkable.serverEntities.Find(entry.Value.parentNPC);
                    if (parent != null)
                        parent.KillMessage();
                }
        }

        #endregion

        #region Hook OnServerSave (save all pets)

        void OnServerSave()
        {
            UnityEngine.Object[] objects = GameObject.FindObjectsOfType(typeof(NpcAI));
            if (objects != null)
            {
                Puts("Saving pets...");
                foreach (UnityEngine.Object gameObj in objects)
                {
                    NpcAI pet = gameObj as NpcAI;
                    SaveNpcList[pet.owner.owner.userID.ToString()] = new PetInfo(pet);
                }
                Interface.GetMod().DataFileSystem.WriteObject("Pets", SaveNpcList);
            }
        }

        #endregion

        #region PET Command (activate/deactivate npc mode)

        [ChatCommand("pet")]
        void pet(BasePlayer player, string command, string[] args)
        {
            if (!UsePermission || HasPermission(player, "cannpc"))
			{
                NpcControl comp = player.GetComponent<NpcControl>() ?? player.gameObject.AddComponent<NpcControl>();
                if (args.Length > 0)
                {
                    switch (args[0])
                    {
                        case "free":
                            if (comp.npc)
                            {
                                GameObject.Destroy(comp.npc);
                                SendReply(player, NpcFree);
                            }
                            else
                                SendReply(player, NotNpc);
                            break;
                        case "draw":
                            if (GlobalDraw)
                                if (comp.DrawEnabled)
                                {
                                    comp.DrawEnabled = false;
                                    SendReply(player, DrawDis);
                                }
                                else
                                {
                                    comp.DrawEnabled = true;
                                    SendReply(player, DrawEn);
                                }
                            else
                                SendReply(player, DrawSysDis);
                            break;
                        case "sleep":
                            if (comp.npc)
                            {
                                SendReply(player, SleepMsg);
                                comp.npc.action = Act.Sleep;
                            }
                            else
                                SendReply(player, NotNpc);
                            break;
                        case "info":
                            if (comp.npc)
                            {
                                NPCMetabolism meta = comp.npc.RustMetabolism;
                                SendReply(player, InfoMsg
                                    .Replace("{health}", Math.Round(comp.npc.Base.health*  100/comp.npc.Base.MaxHealth()).ToString())
                                    .Replace("{hunger}", Math.Round(meta.hydration.value * 100 / meta.hydration.max).ToString())
                                    .Replace("{thirst}", Math.Round(meta.calories.value * 100 / meta.calories.max).ToString())
                                    .Replace("{sleep}", Math.Round(meta.sleep.value * 100 / meta.sleep.max).ToString())
                                    .Replace("{stamina}", Math.Round(meta.stamina.value * 100 / meta.stamina.max).ToString()));
                            }
                            else
                                SendReply(player, NotNpc);
                            break;
                    }
                }
                else
                {
                    if (comp.enabled)
                    {
                        comp.enabled = false;
                        SendReply(player, DeactivatedMsg);
                    }
                    else
                    {
                        comp.enabled = true;
                        SendReply(player, ActivatedMsg);
                    }
                }
			}
			else
                SendReply(player, NoPermMsg);
        }

        #endregion

        #region Some other plugin methods

        private bool HasPermission(BasePlayer player, string perm)
        {
            return permission.UserHasPermission(player.userID.ToString(), perm);
        }

        private static void DestroyAll<T>()
        {
            UnityEngine.Object[] objects = GameObject.FindObjectsOfType(typeof(T));
            if (objects != null)
                foreach (UnityEngine.Object gameObj in objects)
                    GameObject.Destroy(gameObj);
        }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] == null)
                Config[Key] = var;
            else
                try { var = (T) Convert.ChangeType(Config[Key], typeof(T)); }
                catch { Config[Key] = var; }
        }

        private static BUTTON ConvertStringToButton(string button)
        {
            switch (button)
            {
                case "FORWARD": return BUTTON.FORWARD;
                case "BACKWARD": return BUTTON.BACKWARD;
                case "LEFT": return BUTTON.LEFT;
                case "RIGHT": return BUTTON.RIGHT;
                case "JUMP": return BUTTON.JUMP;
                case "DUCK": return BUTTON.DUCK;
                case "SPRINT": return BUTTON.SPRINT;
                case "INVENTORY": return BUTTON.INVENTORY;
                case "FIRE_PRIMARY": return BUTTON.FIRE_PRIMARY;
                case "FIRE_SECONDARY": return BUTTON.FIRE_SECONDARY;
                case "CHAT": return BUTTON.CHAT;
                case "RELOAD": return BUTTON.RELOAD;
                case "PREVIOUS": return BUTTON.PREVIOUS;
                case "SLOT1": return BUTTON.SLOT1;
                case "SLOT2": return BUTTON.SLOT2;
                case "SLOT3": return BUTTON.SLOT3;
                case "SLOT4": return BUTTON.SLOT4;
                case "SLOT5": return BUTTON.SLOT5;
                case "SLOT6": return BUTTON.SLOT6;
                case "SLOT7": return BUTTON.SLOT7;
                case "SLOT8": return BUTTON.SLOT8;
                case "LOOK_ALT": return BUTTON.LOOK_ALT;
                default: return BUTTON.USE;
            }
        }

        #endregion
    }
}


/* Change Log
 * 0.1.0 (26/03/15)
    - Small optimization and code clean up
    - Configurable Button
    - Inventories!

 * 0.1.1 (05/04/15)
    - Fixed all errors (Null, save)

 * 0.2.0 (05/04/15)
    - Pets save!

 * 0.3.0 (10/04/15)
    - Low size save file.
    - Metabolism 20% faster.
    - Now u can modify standart MAXHP\SPEED\ATTACK
    - Changed Follow\Unfollow cmd use
    - AI slightly improved

 * 0.3.1 (10/04/15)
    - Changed OnAttacked hookname

 * 0.3.2 (11/04/15)
    - Config fix
*/