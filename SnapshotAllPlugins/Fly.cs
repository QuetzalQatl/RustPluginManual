// Reference: Oxide.Ext.Rust

using System;
using System.Reflection;
using UnityEngine;
 
namespace Oxide.Plugins
{

    [Info("Fly", "Bombardir", "0.5.0", ResourceId = 822)]
	class Fly : RustPlugin
	{
        private static FieldInfo serverinput;
        private static byte authLevel = 2;

		class FlyMode : MonoBehaviour 
		{
			private float speed;
            private Vector3 direction;
            private InputState input;
			private BasePlayer player;
		
			private void CheckParent()
			{
				BaseEntity parentEntity = player.GetParentEntity();
				if (parentEntity != null)
				{
					parentEntity.RemoveChild(player);
					Vector3 CurrPos = parentEntity.transform.position;
					player.parentEntity.Set(null);
					player.parentBone = 0U;
					transform.position = CurrPos;
					player.UpdateNetworkGroup();
					player.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
				}
			}

            private void Move(Vector3 newpos)
            {
                transform.position = newpos;
                player.ClientRPC(null, player, "ForcePositionTo", new object[] { newpos });
            }
		
			void Awake () 
			{
                speed = 10;
                player = GetComponent<BasePlayer>();
				input = serverinput.GetValue(player) as InputState;
				enabled = false;
			}

            void FixedUpdate()
            {
                if (input.IsDown(BUTTON.CHAT))
                    enabled = false;
                else
                {
                    if (!player.IsSpectating())
                    {
                        player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, true);
                        TransformEx.SetLayerRecursive(gameObject, "Invisible");
                        CancelInvoke("MetabolismUpdate");
                        CancelInvoke("InventoryUpdate");
                    }

                    direction = Vector3.zero;
                    if (input.IsDown(BUTTON.FORWARD))
                        direction.z++;
                    if (input.IsDown(BUTTON.RIGHT))
                        direction.x++;
                    if (input.IsDown(BUTTON.LEFT))
                        direction.x--;
                    if (input.IsDown(BUTTON.BACKWARD))
                        direction.z--;

                    if (input.IsDown(BUTTON.FIRE_PRIMARY))
                        if (input.IsDown(BUTTON.PREVIOUS))
                            speed++;
                        else if (input.IsDown(BUTTON.NEXT))
                            speed--;

                    if (direction != Vector3.zero)
                    {
                        CheckParent();
                        Move(transform.position + Quaternion.Euler(input.current.aimAngles) * direction * Time.deltaTime * speed);
                    }
                }
            }

            void OnDisable()
            {
				CheckParent();
                RaycastHit hit;
                if (Physics.Raycast(new Ray(transform.position, Vector3.down), out hit, 25000) || Physics.Raycast(new Ray(transform.position, Vector3.up), out hit, 25000))
                    Move(hit.point);
                player.metabolism.Reset();
                InvokeRepeating("InventoryUpdate", 1f, 0.1f * UnityEngine.Random.Range(0.99f, 1.01f));
                player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, false);
                TransformEx.SetLayerRecursive(gameObject, "Player (Server)");
                player.ChatMessage("Fly deactivated!");
            }
		}
		
        void OnPlayerDisconnected(BasePlayer player)
        {
            FlyMode fly = player.GetComponent<FlyMode>();
            if (fly)
                fly.enabled = false;
        }

		void LoadDefaultConfig()
		{
			Config["AuthLevel"] = 2;
			SaveConfig();
		}
    
        void Init()
        {
            serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Instance | BindingFlags.NonPublic));
			if (Config["AuthLevel"] != null)
				authLevel = Convert.ToByte(Config["AuthLevel"]);
        }
		
		void Unload()
		{	
			var objects = GameObject.FindObjectsOfType(typeof(FlyMode));
			if (objects != null)
				foreach (var gameObj in objects)
					GameObject.Destroy(gameObj);
		} 

        [ChatCommand("fly")]
        void FlyCMD(BasePlayer player, string command, string[] args)
        {
            if (player.net.connection.authLevel >= authLevel)
			{
				FlyMode fly = player.GetComponent<FlyMode>();
				if (!fly)
					fly = player.gameObject.AddComponent<FlyMode>();
				fly.enabled = true;
				SendReply(player, "Fly activated!");
			}
			else
				SendReply(player, "No Permission!");
        }
	}
}