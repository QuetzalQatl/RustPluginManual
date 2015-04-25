// Reference: Oxide.Ext.Rust

using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("RemoverTool", "Reneb & Mughisi", "2.2.12")]
    class RemoverTool : RustPlugin
    {
    	private static DateTime epoch;
        private bool Changed;

        private int removeAuth;
        private int removeAdmin;
        private int removeAll;
        private int removeTarget;
        private double deactivateTimer;
        private double deactivateMaxTimer;
        private bool refundAllowed;
        private bool refundAllGrades;
        private decimal refundRate;
        private bool useToolCupboard;
        private string noAccess;
        private string cantRemove;
        private string tooFar;
        private string noBuildingfound;
        private string noToolCupboard;
        private string noToolCupboardAccess;
        private string noTargetFound;
        private string helpBasic;
        private string helpAdmin;
        private string helpAll;
        private string helpRay;

        private Dictionary<BasePlayer, double> deactivationTimer;
        private Dictionary<BasePlayer, string> removing;
        private double nextCheck;
        private List<BasePlayer> todelete;
        private FieldInfo buildingPrivlidges;
        private FieldInfo serverinput;

        void Loaded() 
        {
        	epoch = new System.DateTime(1970, 1, 1);
            Changed = false;
            deactivationTimer = new Dictionary<BasePlayer, double>();
            removing = new Dictionary<BasePlayer, string>();
            todelete = new List<BasePlayer>();
            nextCheck = CurrentTime() + 1.0;
            buildingPrivlidges = typeof(BasePlayer).GetField("buildingPrivlidges", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            LoadVariables();
        }
        void OnServerInitialized()
        {
            
        }
        double CurrentTime()
        {
            return System.DateTime.UtcNow.Subtract(epoch).TotalSeconds;
        }
        object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                Changed = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                Changed = true;
            }
            return value;
        }
        void LoadVariables()
        {
            removeTarget = Convert.ToInt32(GetConfig("Remove", "target", 1));
            removeAuth = Convert.ToInt32(GetConfig("Remove", "basic", 0));
            removeAdmin = Convert.ToInt32(GetConfig("Remove", "admin", 2));
            removeAll = Convert.ToInt32(GetConfig("Remove", "all", 2));
            deactivateTimer = Convert.ToDouble(GetConfig("RemoveTimer", "default", 30));
            deactivateMaxTimer = Convert.ToDouble(GetConfig("RemoveTimer", "max", 120));
            refundAllowed = Convert.ToBoolean(GetConfig("Refund", "activated", true));
            refundAllGrades = Convert.ToBoolean (GetConfig ("Refund", "all-grades", false));
            refundRate = Convert.ToDecimal(GetConfig("Refund", "rate", 0.5));
            useToolCupboard = Convert.ToBoolean(GetConfig("ToolCupboard", "activated", true));

            noAccess = Convert.ToString(GetConfig("Messages", "NoAccess", "You don't have the permissions to use this command"));
            cantRemove = Convert.ToString(GetConfig("Messages", "cantRemove", "You are not allowed to remove this"));
            tooFar = Convert.ToString(GetConfig("Messages", "tooFar", "You must get closer to remove this"));
            noBuildingfound = Convert.ToString(GetConfig("Messages", "noBuildingFound", "Couldn't find any structure to remove"));
            noToolCupboard = Convert.ToString(GetConfig("Messages", "noToolCupboard", "You need a Tool Cupboard to remove this"));
            noToolCupboardAccess = Convert.ToString(GetConfig("Messages", "noToolCupboardAccess", "You need access to all Tool Cupboards around you to do this"));
            noTargetFound = Convert.ToString(GetConfig("Messages", "noTargetFound", "Target player not found"));

            helpBasic = Convert.ToString(GetConfig("Messages", "helpBasic", "/remove Optional:Time - To remove start removing"));
            helpAdmin = Convert.ToString(GetConfig("Messages", "helpAdmin", "/remove admin Optional:Time - To remove start removing anything"));
            helpAll = Convert.ToString(GetConfig("Messages", "helpAll", "/remove all Optional:Time - To remove start removing an entire building"));
            helpRay = Convert.ToString(GetConfig("Messages", "helpRay", "/rayremove - To remove the entire building that you are looking at"));
            

            if (Changed)
            {
                SaveConfig();
                Changed = false;
            }
        }
        protected override void LoadDefaultConfig()
        {
            Puts("RemoverTool: Creating a new config file");
            Config.Clear();
            LoadVariables();
        }
        void RemoveAllFrom(Vector3 sourcepos)
        {
            List<Vector3> checkFrom = new List<Vector3>();
            List<UnityEngine.Collider> wasKilled = new List<UnityEngine.Collider>();
            checkFrom.Add(sourcepos);
            var current = 0;
            while (true)
            {
                current++;
                if (current > checkFrom.Count)
                    break;
                var hits = UnityEngine.Physics.OverlapSphere(checkFrom[current - 1], 3f);
                foreach (var hit in hits)
                {
                    if (!(wasKilled.Contains(hit)))
                    {
                        wasKilled.Add(hit);
                        if (hit.GetComponentInParent<BuildingBlock>() != null)
                        {
                            BuildingBlock fbuildingblock = hit.GetComponentInParent<BuildingBlock>();
                            checkFrom.Add(fbuildingblock.transform.position);
                            if (!fbuildingblock.isDestroyed)
                                fbuildingblock.KillMessage();
                        } 
                        else if (hit.GetComponentInParent<BasePlayer>() == null && hit.GetComponentInParent<BaseEntity>() != null)
                        {
                            if(!(hit.GetComponentInParent<BaseEntity>().isDestroyed))
                                hit.GetComponentInParent<BaseEntity>().KillMessage();
                        }
                    }
                }
            }
        }
        void Refund(BasePlayer player, BaseEntity entity)
        {
            if (entity as WorldItem)
            {
                WorldItem worlditem = entity as WorldItem;
                if (worlditem.item != null && worlditem.item.info != null)
                    player.inventory.GiveItem(worlditem.item.info.itemid, 1, true);
            }
            else if( entity as BuildingBlock )
                RefundBuildingBlock(player, entity as BuildingBlock);
        }
        void RefundBuildingBlock(BasePlayer player, BuildingBlock buildingblock)
        {
            if (buildingblock.blockDefinition != null) {
                int buildingblockGrade = (int) buildingblock.grade;

                if (refundAllGrades) {
                    for (int i = buildingblockGrade; i >= 0; i--) {
                        if (buildingblock.blockDefinition.grades [i] != null) {
                            List<ItemAmount> currentCost = buildingblock.blockDefinition.grades [i].costToBuild as List<ItemAmount>;
                            foreach (ItemAmount ia in currentCost) {
                                player.inventory.GiveItem (ia.itemid, Convert.ToInt32 ((decimal) ia.amount * refundRate), true);
                            }
                        }
                    }
                } else {
                    if (buildingblock.blockDefinition.grades [buildingblockGrade] != null) {
                        List<ItemAmount> currentCost = buildingblock.blockDefinition.grades [buildingblockGrade].costToBuild as List<ItemAmount>;
                        foreach (ItemAmount ia in currentCost) {
                            player.inventory.GiveItem (ia.itemid, Convert.ToInt32 ((decimal) ia.amount * refundRate), true);
                        }
                    }

                    if (buildingblock.blockDefinition.grades [0] != null) {
                        List<ItemAmount> currentCost = buildingblock.blockDefinition.grades [0].costToBuild as List<ItemAmount>;
                        foreach (ItemAmount ia in currentCost) {
                            player.inventory.GiveItem (ia.itemid, Convert.ToInt32 ((decimal) ia.amount * refundRate), true);
                        }
                    }
                }
            }
        }
        void OnPlayerAttack(BasePlayer player, HitInfo hitinfo)
        {
            if (hitinfo.HitEntity != null)
            {
                DoHit(player,hitinfo);
            }
        }
        void DoHit(BasePlayer player, HitInfo hitinfo)
        {
            if (removing.Count == 0) return;
            if (!(removing.ContainsKey(player))) return;
            BaseEntity target = hitinfo.HitEntity as BaseEntity;
            if (target.isDestroyed) return;
            string ttype = removing[player];
            if (ttype == "all")
            {
                RemoveAllFrom(target.transform.position);
                return;
            }
            if (!(CanRemoveTarget(player, target, ttype)))
            {
                SendReply(player, cantRemove);
                return;
            }

            if (hitinfo.HitEntity as DroppedItem && ttype != "admin")
                return;

            if (refundAllowed)
                Refund(player,target);

            target.KillMessage();
        }
        bool hasTotalAccess(BasePlayer player)
        {
            List<BuildingPrivlidge> playerpriv = buildingPrivlidges.GetValue(player) as List<BuildingPrivlidge>;
            if (playerpriv.Count == 0)
            {
                SendReply(player, noToolCupboard);
                return false;
            }
            foreach (BuildingPrivlidge priv in playerpriv.ToArray())
            {
                List<ProtoBuf.PlayerNameID> authorized = priv.authorizedPlayers;
                bool foundplayer = false;
                foreach (ProtoBuf.PlayerNameID pni in authorized.ToArray())
                {
                    if (pni.userid == player.userID)
                        foundplayer = true;
                }
                if (!foundplayer)
                {
                    SendReply(player, noToolCupboardAccess);
                    return false;
                }
            }
            return true;
        }
        bool CanRemoveTarget(BasePlayer player, BaseEntity entity, string ttype)
        {
            if (entity is BasePlayer) return false;
            if (ttype == "admin") return true;
            if (entity as BuildingBlock)
            {
                object returnhook = Interface.GetMod().CallHook("FindBlockData", new object[] { entity as BuildingBlock });
                if (returnhook != null)
                {
                    if (!(returnhook is bool))
                    {
                        string ownerid = returnhook.ToString();
                        if (ownerid == player.userID.ToString())
                            return true;
                    }
                }
            }
            if (useToolCupboard)
            {
                if (hasTotalAccess(player))
                {
                    if (Vector3.Distance(player.transform.position, entity.transform.position) < 3f)
                        return true;
                    else
                        SendReply(player, tooFar);
                }
            }
            return false;
        }
        void OnTick()
        {
            if (CurrentTime() >= nextCheck)
            {
                var currenttime = CurrentTime();                
                if (removing.Count > 0)
                {
                    foreach (KeyValuePair<BasePlayer, string> pair in removing)
                    {
                        BasePlayer player = pair.Key;
                        if (deactivationTimer.ContainsKey(player) && (player.net != null))
                        {
                            double timetodel = deactivationTimer[player];
                            if (currenttime > timetodel)
                                todelete.Add(player);
                        }
                        else
                            todelete.Add(player);
                    }
                    foreach (BasePlayer player in todelete)
                        DeactivateRemove(player);
                    todelete.Clear();
                }
                nextCheck = currenttime + 1;
            }
        }
        void ActivateRemove(BasePlayer player, string ttype, double removetime)
        {
            if (removing.ContainsKey(player))
                removing[player] = ttype;
            else
                removing.Add(player, ttype);

            if (deactivationTimer.ContainsKey(player))
                deactivationTimer[player] = (removetime + CurrentTime());
            else
                deactivationTimer.Add(player, (removetime + CurrentTime()));

            SendReply(player, string.Format("Remover Tool ({0}): Activated for {1} seconds", ttype, removetime.ToString()));
        }
        void DeactivateRemove(BasePlayer player)
        {
            if (removing.ContainsKey(player))
                removing.Remove(player);
            if (deactivationTimer.ContainsKey(player))
                deactivationTimer.Remove(player);
            if(player.net != null)
                SendReply(player, "Remover Tool: Deactivated");
        }
        void TriggerRemove(BasePlayer player, string[] args, string ttype)
        {
            if (!(hasAccess(player, ttype)))
            {
                SendReply(player, noAccess);
                return;
            }
            var activating = true;
            if (removing.ContainsKey(player))
                activating = false;
            double removetime;
            if (args != null && args.Length >= 1)
            {
                if (double.TryParse(args[args.Length-1].ToString(), out removetime))
                {
                    activating = true;
                    if (removetime > deactivateMaxTimer)
                        removetime = deactivateMaxTimer;
                }
                else
                    removetime = deactivateTimer;
            }
            else
                removetime = deactivateTimer;
            if (activating)
                ActivateRemove(player, ttype, removetime);
            else
                DeactivateRemove(player);
        }
        bool hasAccess(BasePlayer player, string ttype)
        {
            if (ttype == "normal" && player.net.connection.authLevel >= removeAuth)
                return true;
            if (ttype == "admin" && player.net.connection.authLevel >= removeAdmin)
                return true;
            if (ttype == "all" && player.net.connection.authLevel >= removeAll)
                return true;
            if (ttype == "target" && player.net.connection.authLevel >= removeTarget)
                return true;
            return false;
        }
        void TargetRemove(BasePlayer player, string[] args)
        {
            if (!(hasAccess(player, "target")))
            {
                SendReply(player, noAccess);
                return;
            }
            var target = BasePlayer.Find(args[1].ToString());
            if (target == null || target.net == null || target.net.connection == null)
            {
                SendReply(player, noTargetFound);
                return;
            }
            var activating = true;
            if (removing.ContainsKey(target))
                activating = false;
            double removetime;
            if (args != null && args.Length >= 2)
            {
                if (double.TryParse(args[1].ToString(), out removetime))
                {
                    activating = true;
                    if (removetime > deactivateMaxTimer)
                        removetime = deactivateMaxTimer;
                }
                else
                    removetime = deactivateTimer;
            }
            else
                removetime = deactivateTimer;
            if (activating)
                ActivateRemove(target, "normal", removetime);
            else
                DeactivateRemove(target);
        }
        [ChatCommand("remove")]
        void cmdChatRemove(BasePlayer player, string command, string[] args)
        {
            if (args == null || args.Length == 0)
            {
                TriggerRemove(player, args, "normal");
                return;
            }
            if (args[0].ToString() == "admin")
            {
                TriggerRemove(player, args, "admin");
            }
            else if (args[0].ToString() == "all")
            {
                TriggerRemove(player, args, "all");
            }
            else
            {
                try {
                    int n;
                    if (int.TryParse(args[0], out n))
                        TriggerRemove(player, args, "normal");
                    else
                        TargetRemove(player, args);
                } catch
                {
                    TriggerRemove(player, args, "normal");
                }
            }

        }
        object FindBlockFromRay(Vector3 Pos, Vector3 Aim)
        {
            var hits = UnityEngine.Physics.RaycastAll(Pos, Aim);
            float distance = 100000f;
            object target = false;
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<BuildingBlock>() != null)
                {
                    if (hit.distance < distance)
                    {
                        distance = hit.distance;
                        target = hit.collider.GetComponentInParent<BuildingBlock>();
                    }
                }
            }
            return target;
        }
        [ChatCommand("rayremove")]
        void cmdChatRayRemove(BasePlayer player, string command, string[] args)
        {
            if (player.net.connection.authLevel < removeAll)
            {
                SendReply(player, "You are not allowed to use this command");
                return;
            }
            var input = serverinput.GetValue(player) as InputState;
            var currentRot = Quaternion.Euler(input.current.aimAngles);
            var target = FindBlockFromRay(player.transform.position, currentRot * Vector3.forward);
            if (target is bool)
            {
                SendReply(player, noBuildingfound);
                return;
            }
            BuildingBlock blocksource = target as BuildingBlock;
            RemoveAllFrom(blocksource.transform.position);
        }

        void SendHelpText(BasePlayer player)
        {
            if(hasAccess(player,"normal"))
                SendReply(player,helpBasic);
            if(hasAccess(player,"admin"))
                SendReply(player,helpAdmin);
            if (hasAccess(player, "all"))
            {
                SendReply(player, helpAll);
                SendReply(player, helpRay);
            }
        }
    }
}