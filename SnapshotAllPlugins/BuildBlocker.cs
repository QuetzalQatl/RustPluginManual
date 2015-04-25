using UnityEngine;
using System;

namespace Oxide.Plugins
{
    [Info("BuildBlocker", "Bombardir", "1.2.5" )]
    class BuildBlocker : RustPlugin
    {
        #region Config

        private static bool OnRock = false;
        private static bool InRock = true;
        private static bool InCave = false;
        private static bool InWarehouse = true;
        private static bool InMetalBuilding = true;
        private static bool InHangar = true;
        private static bool InTank = true;
        private static bool InBase = true;
        private static bool UnTerrain = true;
        private static bool UnBridge = false;
        private static bool UnRadio = false;
        private static bool BlockHorizontalSigns = true;
        private static bool BlockStructuresHeight = false;
        private static bool BlockDeployablesHeight = false;
        private static int MaxHeight = 100;
        private static bool BlockStructuresWater = false;
        private static bool BlockDeployablesWater = false;
        private static int MaxWater = -2;
        private static int AuthLVL = 2;
        private static string Msg = "Hey! You can't build here!";
        private static string MsgHeight = "You can't build here! (Height limit 100m)";
        private static string MsgSign = "You can't build horizontal sign on other sign!";
        private static string MsgWater = "You can't build here! (Water limit -2m)";

        void LoadDefaultConfig() { }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
				Config[Key] = var;
        }

        void Init() 
        {
            CheckCfg<bool>("Block On Rock", ref OnRock);
            CheckCfg<bool>("Block In Rock", ref InRock);
            CheckCfg<bool>("Block In Rock Cave", ref InCave);
            CheckCfg<bool>("Block In Base", ref InBase);
            CheckCfg<bool>("Block In Warehouse", ref InWarehouse);
            CheckCfg<bool>("Block In Metal Building", ref InMetalBuilding);
            CheckCfg<bool>("Block In Hangar", ref InHangar);
            CheckCfg<bool>("Block Under Terrain", ref UnTerrain);
            CheckCfg<bool>("Block Under|On Metal Sphere", ref InTank);
            CheckCfg<bool>("Block Under|On Bridge", ref UnBridge);
            CheckCfg<bool>("Block Under|On Radar", ref UnRadio);
            CheckCfg<bool>("Block Horizontal Signs on other Signs", ref BlockHorizontalSigns);
            CheckCfg<int>("Max Height Limit", ref MaxHeight);
            CheckCfg<bool>("Block Structures above the max height", ref BlockStructuresHeight);
            CheckCfg<bool>("Block Deployables above the max height", ref BlockDeployablesHeight);
            CheckCfg<int>("Max Under Water Height Limit", ref MaxWater);
            CheckCfg<bool>("Block Structures under water", ref BlockStructuresWater);
            CheckCfg<bool>("Block Deployables under water", ref BlockDeployablesWater);
            CheckCfg<string>("Block Water Message", ref MsgWater);
            CheckCfg<string>("Block Height Message", ref MsgHeight);
            CheckCfg<string>("Block Sign Message", ref MsgSign);
            CheckCfg<string>("Block Message", ref Msg); 
            CheckCfg<int>("Ignore Auth Lvl", ref AuthLVL);
            SaveConfig(); 
        }  
        #endregion 
         
        private void CheckBlock(BaseNetworkable StartBlock, BasePlayer sender, bool CheckHeight, bool CheckWater)
        {
            if (StartBlock && sender.net.connection.authLevel < AuthLVL && !StartBlock.isDestroyed)
            {
                Vector3 Pos = StartBlock.transform.position;
                if (StartBlock.name == "foundation.steps(Clone)")
                    Pos.y += 1.3f;

                if (CheckHeight || CheckWater)
                {
                   float height = TerrainMeta.HeightMap.GetHeight(Pos);
                    if (CheckHeight && Pos.y - height > MaxHeight)
                    {
                        SendReply(sender, MsgHeight);
                        StartBlock.Kill(BaseNetworkable.DestroyMode.Gib);
                        return;
                    }
                    else if (CheckWater && height < 0 && height < MaxWater && Pos.y < 2.8 )
                    {
                        SendReply(sender, MsgWater);
                        StartBlock.Kill(BaseNetworkable.DestroyMode.Gib);
                        return;
                    }
                }

                if (BlockHorizontalSigns && StartBlock.GetComponent<Signage>())
                {
                    Vector3 euler = StartBlock.transform.rotation.eulerAngles;
                    if (euler.z == 0)
                    {
                        SendReply(sender, MsgSign);
                        StartBlock.Kill(BaseNetworkable.DestroyMode.Gib);
                        return;
                    }
                }

                Pos.y = Pos.y + 100;
                RaycastHit[] hits = Physics.RaycastAll(Pos, Vector3.down, 102.8f);
                Pos.y = Pos.y - 100;
                for (int i = 0; i < hits.Length; i++)
                {
                    RaycastHit hit = hits[i];
                    if (hit.collider)
                    {
                        string ColName = hit.collider.name;
                        if (UnTerrain && ColName == "Terrain" && hit.point.y > Pos.y || 
                            InBase && ColName.StartsWith("base", StringComparison.CurrentCultureIgnoreCase) || 
                            InMetalBuilding && ColName == "Metal_building_COL" || 
                            UnBridge && ColName == "Bridge_top" || 
                            UnRadio && ColName.StartsWith("dish") || 
                            InWarehouse && ColName.StartsWith("Warehouse") || 
                            InHangar && ColName.StartsWith("Hangar") ||
                            InTank && ColName == "howie_spheretank_blockin" ||
                            ColName == "COL" && (hit.point.y < Pos.y ? OnRock : hit.collider.bounds.Contains(Pos) ? InRock : InCave))
                        {
                            SendReply(sender, Msg);
                            StartBlock.Kill(BaseNetworkable.DestroyMode.Gib);
                            break;
                        }
                        if (ColName == "Terrain")
                            break;
                    }
                }
            }
        }

        void OnEntityBuilt(Planner plan, GameObject obj)
        {
            CheckBlock(obj.GetComponent<BaseNetworkable>(), plan.ownerPlayer, BlockStructuresHeight, BlockStructuresWater);
        }

        void OnItemDeployed(Deployer deployer, BaseEntity deployedentity)
        {
            if (!(deployedentity is BaseLock))
                CheckBlock((BaseNetworkable) deployedentity, deployer.ownerPlayer, BlockDeployablesHeight, BlockDeployablesWater);
        }
    }
}