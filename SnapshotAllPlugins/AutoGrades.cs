/*
	Created By AlexALX (c) 2015
*/
using System;
using System.Collections.Generic;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Automatic Build Grades", "AlexALX", "0.0.1")]
    public class AutoGrades : RustPlugin
    {
	
		private Dictionary<string,int> playerGrades;
	
		public AutoGrades() {
			//HasConfig = true;
		}
		
        void Loaded() {
            playerGrades = new Dictionary<string, int>();
        }
		/* Not sure if this needed, maybe someone reconnect or so...
        [HookMethod("OnPlayerDisconnected")]
        void OnPlayerDisconnected(BasePlayer player)
        {
			var steamId = player.userID.ToString();
            if (playerGrades.ContainsKey(steamId)) {
				playerGrades.Remove(steamId);
			}
        }*/
	
		private int PlayerGrade(string steamId, bool cache = true) {
            if (playerGrades.ContainsKey(steamId)) return playerGrades[steamId];
			if (!cache) return 0;
            playerGrades[steamId] = 0;
            return playerGrades[steamId];
		}
		
		[HookMethod("OnEntityBuilt")]
        void OnEntityBuilt(Planner planner, UnityEngine.GameObject gameObject)
        {	
			var player = planner.ownerPlayer;
            if (!player.IsAdmin()) return;
			BuildingBlock buildingBlock = gameObject.GetComponent<BuildingBlock>();
			if (buildingBlock==null) return;
			var steamId = player.userID.ToString();
			var pgrade = PlayerGrade(steamId,false);
			if (pgrade>0) {
				var grd = (BuildingGrade.Enum) pgrade;
				buildingBlock.SetGrade(grd);
				buildingBlock.SetHealthToMax();
			}
		}
		
        [ChatCommand("bgrade")]
        void ChatBuildGrade(BasePlayer player, string command, string[] args) {
            if (!player.IsAdmin()) { 
				SendReply(player, "<color='#DD0000'>You have no access to this command.</color>"); return; 
			}
			var chatmsg = new List<string>();
			var steamId = player.userID.ToString();
            if (args.Length>0) {
				switch (args[0])
				{
					case "1":
					case "2":
					case "3":
					case "4":
						var pgrade = PlayerGrade(steamId);
						playerGrades[steamId] = Convert.ToInt32(args[0]);
						chatmsg.Add("<color='#00DD00'>You successfully set auto update to <color='#DD0000'>" + ((BuildingGrade.Enum) playerGrades[steamId]).ToString() + "</color>.</color>");
					break;
					case "0":
						playerGrades.Remove(steamId);
						chatmsg.Add("<color='#00DD00'>You successfully <color='#DD0000'>disabled</color> auto update.</color>");
					break;
					default:
						chatmsg.Add("<color='#DD0000'>Invalid building grade.</color>");
					break;
				}
			} else {
				var pgrade = PlayerGrade(steamId,false);
				chatmsg.Add("Automatic Build Grade command usage:\n");
				chatmsg.Add("<color='#00DD00'>/bgrade 1</color> - auto update to wood");
				chatmsg.Add("<color='#00DD00'>/bgrade 2</color> - auto update to stone");
				chatmsg.Add("<color='#00DD00'>/bgrade 3</color> - auto update to metal");
				chatmsg.Add("<color='#00DD00'>/bgrade 4</color> - auto update to armored");
				chatmsg.Add("<color='#00DD00'>/bgrade 0</color> - disable auto update");
				var curtxt = ((BuildingGrade.Enum) pgrade).ToString();
				if (pgrade==0) curtxt = "Disabled";
				chatmsg.Add("\nCurrent mode: <color='#DD0000'>" + curtxt + "</color>");
			}
			player.ChatMessage(string.Join("\n", chatmsg.ToArray()));
        }
	
	}
	
}