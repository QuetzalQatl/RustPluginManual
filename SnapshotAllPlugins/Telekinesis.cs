// Reference: Oxide.Ext.Rust

using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;  
using Rust;

namespace Oxide.Plugins
{

    [Info("Telekinesis", "Bombardir", 0.4)]
	class Telekinesis : RustPlugin
	{
        private static FieldInfo serverinput;
        private static byte authLevel = 2;
		private static List<MonoBehaviour> GodList = new List<MonoBehaviour>();
		
        class TelekinesisComponent : MonoBehaviour 
		{
            public float MaxDistance = 450;
            public float MinDistance = 5;
            public float speed = 3;
			public float RotateSpeed = 2;
            private BaseNetworkable target;
			private BasePlayer TargetPlayer;
            private float distance;
			private float TimeSincePressed;
            private bool IsRotate = false;
            private InputState input;
		
			void Awake() 
			{
				input = serverinput.GetValue( GetComponent<BasePlayer>() ) as InputState;
				TimeSincePressed = Time.realtimeSinceStartup;
				enabled = false;
			}

            void Update()
            {
                if (input.WasJustPressed(BUTTON.USE))
                {
                    if (Time.realtimeSinceStartup - TimeSincePressed > 0.1)
                    {
                        TimeSincePressed = Time.realtimeSinceStartup;
                        if (target != null)
                        {
                            if (TargetPlayer)
                                GodList.Remove(TargetPlayer as MonoBehaviour);
                            target = null; 
                            TargetPlayer = null;
                        }
                        else
                        {
                            RaycastHit hit;
                            Vector3 raypos = transform.position;
                            raypos.y++;
                            if (Physics.Raycast(new Ray(raypos, Quaternion.Euler(input.current.aimAngles) * Vector3.forward), out hit, MaxDistance) && hit.transform != transform)
                            {
                                target = hit.transform.GetComponentInParent<BaseNetworkable>();
                                if (target)
                                {
                                    distance = Vector3.Distance(transform.position, target.transform.position);
                                    TargetPlayer = target.GetComponent<BasePlayer>();
                                    if (TargetPlayer)
                                        GodList.Add(TargetPlayer as MonoBehaviour);
                                }
                            }
                        }
                    }
                }
                else if (input.WasJustPressed(BUTTON.RELOAD))
                    if (Time.realtimeSinceStartup - TimeSincePressed > 0.1)
                    {
                        TimeSincePressed = Time.realtimeSinceStartup;
                        IsRotate = !IsRotate;
                    }
                    
                if (target)   
                {
                    if (input.IsDown(BUTTON.PREVIOUS))
                    {
                        distance++;
                        if (distance > MaxDistance)
                            distance = MaxDistance;
                    } 
                    else if (input.IsDown(BUTTON.NEXT))
                    { 
                        distance--;
                        if (distance < MinDistance)
                            distance = MinDistance; 
                    }
 
                    target.transform.position = Vector3.Lerp(target.transform.position, transform.position +  Quaternion.Euler(input.current.aimAngles) * Vector3.forward * distance, Time.deltaTime * speed);
					if (IsRotate)
						target.transform.rotation = Quaternion.Euler(input.current.aimAngles*RotateSpeed);
                    if (TargetPlayer != null && !TargetPlayer.IsSleeping())
                        TargetPlayer.ClientRPC(null, TargetPlayer, "ForcePositionTo", new object[] { target.transform.position });
                    else
                        target.SendNetworkUpdate(BasePlayer.NetworkQueue.UpdateDistance);
                }
            }
		}
		
		
		object OnEntityAttacked(MonoBehaviour entity, HitInfo hitinfo)
		{
			if (entity != null && hitinfo != null)
				if (GodList.Contains(entity))
					{
						for (int index = 0; index < 17; ++index)
							hitinfo.damageTypes.Set( (DamageType) index, 0.0f );
						entity.GetComponent<BasePlayer>().metabolism.bleeding.value = 0.0f;
						hitinfo.HitMaterial = 0U;
						return true;
					}
			return null; 
		} 
    
		void LoadDefaultConfig()
		{
			Config["AuthLevel"] = 2;
			SaveConfig();
		}  
	
        void Init()
        {
            serverinput = typeof(BasePlayer).GetField("serverInput", (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
			if (Config["AuthLevel"] != null)
				authLevel = Convert.ToByte(Config["AuthLevel"]);
        }
		
		void Unload()
		{	
			var objects = GameObject.FindObjectsOfType(typeof(TelekinesisComponent));
			if (objects != null)
				foreach (var gameObj in objects)
					GameObject.Destroy(gameObj);
		}   
 
        [ChatCommand("tls")]
        void Fly(BasePlayer player, string command, string[] args)
        {
            if (player.net.connection.authLevel >= authLevel)
			{
                TelekinesisComponent telekinesis = player.GetComponent<TelekinesisComponent>();
                if (!telekinesis)
                    telekinesis = player.gameObject.AddComponent<TelekinesisComponent>();
					
				if (args.Length > 1)
					switch (args[0])
					{
						case "speed":
							telekinesis.speed = Convert.ToSingle(args[1]);
							SendReply(player, "Now, the speed = "+args[1]);
							break;
						case "max":
							telekinesis.MaxDistance = Convert.ToSingle(args[1]);
							SendReply(player, "Now, the max distance = "+args[1]);
							break;
						case "min":
							telekinesis.MinDistance = Convert.ToSingle(args[1]);
							SendReply(player, "Now, the min distance = "+args[1]);
							break;
						case "rotate":
							telekinesis.RotateSpeed = Convert.ToSingle(args[1]);
							SendReply(player, "Now, the rotate speed = "+args[1]);
							break;
						default:
							SendReply(player, "Variables: speed, max, min, rotate");
							break;
					}
				else
					if (telekinesis.enabled)
					{
						telekinesis.enabled = false;
						SendReply(player, "Telekinesis deactivated!");	
					}
					else
					{
						telekinesis.enabled = true;
						SendReply(player, "Telekinesis activated!");
					}
			}
			else
				SendReply(player, "No Permission!");
        }
	}
}