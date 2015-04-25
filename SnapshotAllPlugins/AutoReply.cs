using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using System.Text;


namespace Oxide.Plugins
{
    [Info("AutoReply", "4seti [Lunatiq] for Rust Planet", "1.3.5", ResourceId = 908)]
    public class AutoReply : RustPlugin
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

		#region Default and private params
		private Dictionary<string, Dictionary<string, DateTime>> antiSpam;
		private int replyInterval = 5;		
		private int minPriveledge = 0;
		
		private Dictionary<string, string> messages;		
		private Dictionary<string, string> defMsg = new Dictionary<string, string>()
		{
			{"attribSet", "Attribute {0} set to {1}"},  
			{"newWord", "New word was added to check: <color=#81F23F>{0}</color> for group: <color=#81F23F>{1}</color>"}, 
			{"newGroup", "New word group was added to check: <color=#81F23F>{0}</color> baseword: {1} with reply: <color=#81F23F>{2}</color>"}, 
			{"removedGroup", "No more words in group! Word group \"<color=#81F23F>{0}</color>\" was removed!"}, 			
			{"newChar", "New char replacement was added to check: <color=#F23F3F>{0}</color>-><color=#81F23F>{1}</color>"}, 
			{"charRemoved", "Char replacement was removed from check: <color=#F23F3F>{0}</color>"},	
			{"charNotFound", "Char replacement not found in check: <color=#F23F3F>{0}</color>"},			
			{"baseWordExist", "This baseword or part of it (<color=#F23F3F>{0}</color>) already exists in group <color=#81F23F>{1}</color>"},
			{"newCharExists", "Char already persist in the check: <color=#81F23F>{0}</color>"}, 
			{"replyChanged", "Reply changed for word group: <color=#81F23F>{0}</color>"}, 			
			{"replyAdded", "Reply added for word group: <color=#81F23F>{0}</color> with number: <color=#81F23F>{1}</color>"}, 	
			{"replyRemoved", "Reply removed for word group: <color=#81F23F>{0}</color> with number: <color=#81F23F>{1}</color>"}, 
			{"replyNotFound", "Reply №<color=#81F23F>{0}</color> not found for word group: <color=#81F23F>{1}</color>"}, 			
			{"Error", "Something went wrong"},
			{"matchChanged", "Match for group: <color=#81F23F>{0}</color>, changed to <color=#81F23F>{1}</color>"},
			{"matchNotFound", "Group: <color=#81F23F>{0}</color> not found"},			
			{"newWordExists", "Word already persist in the check: <color=#F23F3F>{0}</color>"},
			{"wordGroupExist", "Word group with that name exist: <color=#F23F3F>{0}</color>"},
			{"wordGroupDontExist", "Word group with that name don't exist: <color=#F23F3F>{0}</color> use <color=#F23F3F>/ar_new</color> first"},			
			{"newAttr", "New attribute added! Name: {0}, Text: {1}"},
			{"attrRemoved", "Attribute removed! Name: {0}"},	
			{"attrEdited", "Attribute edited! Name: {0}, New value: {1}"},				
			{"attrNoFound", "Attribute not found! Name: {0}"},	
			{"attrExist", "Attribute \"{0}\" already exist"},
			{"newGroupError", "Error! Should be: <color=#F23F3F>/ar_new groupname baseword replymsg params(optional)</color>"},
			{"changeReplyError", "Error! Should be: <color=#F23F3F>/ar_reply add/del/set (set or del is by nums (check /ar_list)) groupname replymsg attribs</color>"},
			{"attrAdded", "Attrib: <color=#F23F3F>{0}</color> added for word group <color=#F23F3F>{1}</color>"},
			{"attrDeleted", "Attrib: <color=#F23F3F>{0}</color> deleted for word group <color=#F23F3F>{1}</color>"},
			{"attrWordExist", "Attrib: <color=#F23F3F>{0}</color> exists in word group <color=#F23F3F>{1}</color>"},
			{"attrNotExist", "Attrib: <color=#F23F3F>{0}</color> do not exists in word group <color=#F23F3F>{1}</color>"},			
			{"attrUnknown", "UNKNOWN Attrib: <color=#F23F3F>{0}</color>"},		
			{"attrCritError", "Error! Should be like that: <color=#81F23F>/ar_wa add/del groupname ReplyNum attrib</color>"},
			{"wordAdded", "Word: <color=#F23F3F>{0}</color> added for word group <color=#F23F3F>{1}</color>"},
			{"wordDeleted", "Word: <color=#F23F3F>{0}</color> deleted for word group <color=#F23F3F>{1}</color>"},
			{"wordWordExist", "Word: <color=#F23F3F>{0}</color> exists in word group <color=#F23F3F>{1}</color>"},
			{"wordNotExist", "Word: <color=#F23F3F>{0}</color> do not exists in word group <color=#F23F3F>{1}</color>"},				
			{"wordCritError", "Error! Should be like that: <color=#81F23F>/ar_word add/del groupname word</color>"},
			{"listGroupReply", "Group Name: <color=#81F23F>{0}</color> with next params:"},
			{"listWords", "Words to lookup: <color=#81F23F>{0}</color>"},
			{"listAttribs", "Attributes for reply: <color=#81F23F>{0}</color>"},
			{"usageOfExc", "Usage of \"!\" at word start is forbidden use \"?\" instead"}
		};
		
		private Dictionary<string, Dictionary<int, string>> replies;
		private Dictionary<string, Dictionary<int, string>> defReply = new Dictionary<string, Dictionary<int, string>>()
		{
			{"wipe", new Dictionary<int, string>()
				{
					{0, "Last wipe: <color=#81F23F>{0}</color>, Next wipe: <color=#F23F3F>{1}</color>"}
				}
			}
		};
		
		private Dictionary<string, bool> fullMatch;
		private Dictionary<string, bool> defMatch = new Dictionary<string, bool>()
		{
			{"wipe", false}			
		};
		
		private Dictionary<string, List<string>> wordList;
		private Dictionary<string, List<string>> defWords = new Dictionary<string, List<string>>()
		{
			{"wipe", new List<string> {"wipe", "baйп"}}		
		};
		
		private Dictionary<string, string> attributes;
		private Dictionary<string, string> defAttr = new Dictionary<string, string>()
		{
			{"time", "by_plugin"},
			{"player", "by_plugin"},
			{"online", "by_plugin"},
			{"sleepers", "by_plugin"},
			{"lastwipe", "???"},
			{"nextwipe", "???"}
		};
		
		private Dictionary<string, Dictionary<int, List<string>>> attrForWord;
		private Dictionary<string, Dictionary<int, List<string>>> defAtrForWord = new Dictionary<string, Dictionary<int, List<string>>>()
		{
			{"wipe", new Dictionary<int, List<string>>()
				{
					{0, new List<string> {"lastwipe", "nextwipe"}}
				}		
			}
		};
		
		private Dictionary<char, char> replaceChars;
		private Dictionary<char, char> defChar = new Dictionary<char, char>()
		{
			{'с', 'c'},
			{'а', 'a'},
			{'о', 'o'},    
			{'е', 'e'}, 
			{'р', 'p'}, 					
			{'в', 'b'}			
		};		
		#endregion
		#region Default inits
        void Loaded()
        {
            Log("Loaded");
        }
	
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
    
		#endregion
		
		[HookMethod("OnServerInitialized")]
        void OnServerInitialized()
        {
            try
            {
                LoadConfig();
                var version = GetConfig<Dictionary<string, object>>("version", null);
                VersionNumber verNum = new VersionNumber(Convert.ToUInt16(version["Major"]), Convert.ToUInt16(version["Minor"]), Convert.ToUInt16(version["Patch"]));
				
				//Get message dictionary for plugin commands (for Admins) from config
				messages = new Dictionary<string, string>();
                var cfgMessages = GetConfig<Dictionary<string, object>>("messages", null);
                if (cfgMessages != null)
                    foreach (var pair in cfgMessages)
                        messages[pair.Key] = Convert.ToString(pair.Value);
				
							
				//Get replies list from config
				replies = new Dictionary<string, Dictionary<int, string>>();
				var cfgReply = GetConfig<Dictionary<string, object>>("replies", null);
				if (cfgReply != null)
					foreach (var pair in cfgReply)
					{
						//silly workaround, ToList and IEnum tries of usage failed :P
						var d = new Dictionary<int, string>();						
						foreach (var v in pair.Value as Dictionary<string, object>)
						{
							d[Convert.ToInt32(v.Key)] = (string)v.Value;
						}
						replies[pair.Key] = d;
					}    
				//Get char replace list from config
				replaceChars = new Dictionary<char, char>();
				var cfgChar = GetConfig<Dictionary<string, object>>("replaceChars", null);
                if (cfgChar != null)
                    foreach (var pair in cfgChar)
                        replaceChars[Convert.ToChar(pair.Key)] = Convert.ToChar(pair.Value);
				
				//Get char replace list from config
				fullMatch = new Dictionary<string, bool>();
				var cfgMatch = GetConfig<Dictionary<string, object>>("fullMatch", null);
                if (cfgMatch != null)
                    foreach (var pair in cfgMatch)
                        fullMatch[pair.Key] = Convert.ToBoolean(pair.Value);
						
				//Get wordlist from config
				wordList = new Dictionary<string, List<string>>();
				antiSpam = new Dictionary<string, Dictionary<string, DateTime>>();
				var cfgWords = GetConfig<Dictionary<string, object>>("wordList", null);
				if (cfgWords != null)
                    foreach (var pair in cfgWords)
					{
						//silly workaround, ToList and IEnum tries of usage failed :P
						var c = new List<string>();
						foreach (var v in pair.Value as List<object>)
                            c.Add((string)v);
						wordList[pair.Key] = c;
						antiSpam[pair.Key] = new Dictionary<string, DateTime>();
					}
				
				//Get attribs for words
				attrForWord = new Dictionary<string, Dictionary<int, List<string>>>();
				var cfgAFW = GetConfig<Dictionary<string, object>>("attrForWord", null);
				if (cfgAFW != null)
                    foreach (var pair in cfgAFW)
					{
						//silly workaround, ToList and IEnum tries of usage failed :P						
						var d = new Dictionary<int, List<string>>();
						foreach (var v in pair.Value as Dictionary<string, object>)
						{
							var c = new List<string>();
							foreach(var k in v.Value as List<object>)
								c.Add((string)k);
							d[Convert.ToInt32(v.Key)] = c;
						}
						attrForWord[pair.Key] = d;
					}   
				//Get attributes list from config
				attributes = new Dictionary<string, string>();
				var cfgAttr = GetConfig<Dictionary<string, object>>("attributes", null);
				if (cfgAttr != null)
                    foreach (var pair in cfgAttr)
                        attributes[pair.Key] = Convert.ToString(pair.Value);
						
				#region version checker
				if (verNum < Version)
                {
                    //placeholder for future version updates
					foreach (var pair in defMsg)
                        if (!messages.ContainsKey(pair.Key))
                            messages[pair.Key] = pair.Value;
							
					foreach (var pair in defAttr)
                        if (!attributes.ContainsKey(pair.Key))
                            attributes[pair.Key] = pair.Value;
					Config["attributes"] = attributes;
                    Config["messages"] = messages;
					Config["version"] = Version;
                    SaveConfig();
                    Warn("Config version updated to: " + Version.ToString() + " please check it");
                }		
				#endregion				
				
				replyInterval = GetConfig<int>("replyInterval", 30);	
				minPriveledge = GetConfig<int>("minPriveledge", 0);	
            }
            catch (Exception ex)
            {
                Error("OnServerInitialized failed: " + ex.Message);
            }
            
        }
		
		private void LoadVariables()
        {
            Config["messages"] = defMsg;
			Config["replies"] = defReply;
			Config["replaceChars"] = defChar;
			Config["wordList"] = defWords;
			Config["attributes"] = defAttr;
			Config["attrForWord"] = defAtrForWord;
			Config["fullMatch"] = defMatch;
			Config["replyInterval"] = 30;
			Config["minPriveledge"] = 0;
            Config["version"] = Version;
        }
        [HookMethod("OnRunCommand")]
        private object OnRunCommand(ConsoleSystem.Arg arg)
        {
			BasePlayer player = null;
			string msg = "";
			try
			{
				if (arg == null) return null;
				if (arg.connection.player == null) return null;
				if (arg.cmd.namefull.ToString() != "chat.say") return null;
				
				if (arg.connection.player is BasePlayer)
				{
					player = arg.connection.player as BasePlayer;
					if (player.net.connection.authLevel > minPriveledge) return null;
				}
				else return null;
				
				msg = arg.GetString(0, "text").ToLower();		
				
				if (msg == null) return null;
				else if (msg == "") return null;
				else if (msg.Substring(0, 1).Equals("/") || msg.Substring(0, 1).Equals("!")) return null;
				
				if (player == null) return null;
			}
			catch
			{
				return null;
			}

			//Fixing alphabets abuse			
			foreach(var pair in replaceChars)
			{
				msg = msg.Replace(pair.Key, pair.Value);				
			}				
			bool found = false;
			string foundGroup = "";
			foreach(var pair in wordList)
			{	
				foreach(var item in pair.Value)
				{
					if(!fullMatch[pair.Key])
					{
						if (msg.Contains(item))
						{
							found = true;
							foundGroup = pair.Key;
							break;
						}
					}
					else
					{
						if (msg == item)
						{
							found = true;
							foundGroup = pair.Key;
							break;
						}					
					}
				}
			}
			
			if (found)
			{
				string userID = player.userID.ToString();	
				if (antiSpam[foundGroup].ContainsKey(userID))
				{							
					if ((DateTime.Now - antiSpam[foundGroup][userID]).Seconds > replyInterval)
					{						
						
						replyToPlayer(player, foundGroup);
						antiSpam[foundGroup][userID] = DateTime.Now;
					}
				}
				else
				{		
					antiSpam[foundGroup].Add(userID, DateTime.Now);
					replyToPlayer(player, foundGroup);
				}
				return false;
			}			
			return null;
		}
		
		private void replyToPlayer(BasePlayer player, string group)
		{		
			foreach(var v in replies[group])
			{
				player.ChatMessage(replyBuilder(v.Value, attrForWord[group][v.Key], player.displayName));
			}								
		}
		
		private string replyBuilder(string text, List<string> attr, string playerName)
		{
			for(int i = 0; i < attr.Count; i++)
			{	
				string attrText;
				//meh, don't blame me for that :'(
				//Sky.Cycle.DateTime:ToString("HH:mm:ss")
				if (attributes[attr[i]] == "by_plugin")
				{
					switch(attr[i])
					{
						case "time":
							attrText = DateTime.Now.ToString("HH:mm:ss");
							break;
						case "player":
							attrText = playerName;
							break;
						case "online":
							attrText = BasePlayer.activePlayerList.Count.ToString();
							break;
						case "sleepers":
							attrText = BasePlayer.sleepingPlayerList.Count.ToString();
							break;
						default:
							attrText = "";
							break;
					}			
				}
				else
					attrText = attributes[attr[i]];
				text = text.Replace("{"+i+"}", attrText);
			}	
			return text;			
		}
		
		[ChatCommand("ar")]
		void cmdAr(BasePlayer player, string cmd, string[] args)
		{
			if (player.net.connection.authLevel == 0) return;			
			if (args.Length > 2)
			{
				if (args[0] == "c") //adding/deleting new char to replace
				{
					try
					{
						if(args[1] == "add") 
						{
							if (!replaceChars.ContainsKey(Convert.ToChar(args[2])))
							{
								replaceChars.Add(Convert.ToChar(args[2].ToLower()), Convert.ToChar(args[3].ToLower()));
								Config["replaceChars"] = replaceChars;
								player.ChatMessage(string.Format(messages["newChar"], args[2], args[3]));	
							}	
							else	
							{
								player.ChatMessage(string.Format(messages["newCharExists"], args[2]));	
							}
						}	
						else if(args[1] == "del")
						{	
							if (replaceChars.ContainsKey(Convert.ToChar(args[2])))
							{
								replaceChars.Remove(Convert.ToChar(args[2].ToLower()));								
								Config["replaceChars"] = replaceChars;		
								player.ChatMessage(string.Format(messages["charRemoved"], args[2]));
							}
							else
							{
								player.ChatMessage(string.Format(messages["charNotFound"], args[2]));
							}							
						}
					}		
					catch		
					{
						player.ChatMessage(messages["Error"]);	
					}					
				}
				else if(args[0] == "a") //adding/deleting new attribute for word list
				{
					try
					{
						if(args[1] == "add") 
						{
							if (!attributes.ContainsKey(args[2]))
							{
								attributes.Add(args[2].ToLower(), args[3]);
								Config["attributes"] = attributes;
								player.ChatMessage(string.Format(messages["newAttr"], args[2], args[3]));	
							}	
							else	
							{
								player.ChatMessage(string.Format(messages["attrExist"], args[2]));	
							}	
						}
						else if(args[1] == "del") 
						{
							if (attributes.ContainsKey(args[2]))
							{
								if (attributes[args[2]] == "by_plugin")
								{
									player.ChatMessage(string.Format(messages["Error"]));
									return;
								}
								removeAttr(args[2].ToLower());
								attributes.Remove(args[2].ToLower());
								Config["attributes"] = attributes;
								player.ChatMessage(string.Format(messages["attrRemoved"], args[2]));	
							}	
							else	
							{
								player.ChatMessage(string.Format(messages["attrNotFound"], args[2]));	
							}	
						}
						else if(args[1] == "set") 
						{
							if (attributes.ContainsKey(args[2]))
							{
								if (attributes[args[2]] == "by_plugin")
								{
									player.ChatMessage(string.Format(messages["Error"]));
									return;
								}
								attributes[args[2].ToLower()] = args[3];
								Config["attributes"] = attributes;
								player.ChatMessage(string.Format(messages["attrEdited"], args[2], args[3]));	
							}	
							else	
							{
								player.ChatMessage(string.Format(messages["attrNotFound"], args[2]));	
							}	
						}
					}		
					catch		
					{
						player.ChatMessage(messages["Error"]);	
					}				
				}
				SaveConfig();
			}
		}
		
		private bool checkWord(BasePlayer player, string baseWord)
		{
			bool found = true;
			string foundGroup = "";
			foreach(var pair in wordList)
			{	
				foreach(var item in pair.Value)
				{					
					if(!fullMatch[pair.Key])
					{
						if (baseWord.Contains(item))
						{
							found = false;
							foundGroup = pair.Key;					
							break;
						}
					}
					else
					{
						if (baseWord == item)
						{
							found = false;
							foundGroup = pair.Key;			
							break;
						}					
					}					
				}
			}
			if (!found)
			{
				player.ChatMessage(string.Format(messages["baseWordExist"], baseWord, foundGroup));				
			}
			return found;
		}
		
		//Adding new word group
		[ChatCommand("ar_new")]
		void cmdArNew(BasePlayer player, string cmd, string[] args)
		{
			//replies, wordList, attrForWord should be innitially filled
			if (player.net.connection.authLevel == 0) return;			
			if (args.Length > 2)
			{
				if (!replies.ContainsKey(args[0]))
				{
					string groupName = args[0].ToLower();
					string baseWord = args[1].ToLower();
					if (baseWord.Substring(0, 1).Equals("!"))
					{
						player.ChatMessage(messages["usageOfExc"]);	
						return;
					}
					string reply = args[2];					
					
					if (!checkWord(player, baseWord))
						return;
					replies[groupName] = new Dictionary<int, string>();
					replies[groupName][0] = reply;
					wordList[groupName] = new List<string>(){baseWord};
					attrForWord[groupName] = new Dictionary<int, List<string>>();
					attrForWord[groupName][0] = new List<string>();		
					fullMatch[groupName] = false;					
					if (args.Length > 3){						
						for (int i = 3; i < args.Length; i++)
						{
							if (!attributes.ContainsKey(args[i]))
							{
								player.ChatMessage(string.Format(messages["attrUnknown"], args[i]));
								return;
							}
							attrForWord[groupName][0].Add(args[i]);
						}
					}
					player.ChatMessage(string.Format(messages["newGroup"], groupName, baseWord, reply));
					Config["replies"] = replies;
					Config["wordList"] = wordList;
					Config["attrForWord"] = attrForWord;
					Config["fullMatch"] = fullMatch;
					antiSpam[groupName] = new Dictionary<string, DateTime>();
					SaveConfig();					
				}
				else
				{
					player.ChatMessage(string.Format(messages["wordGroupExist"], args[0]));
					return;
				}
			
			}
			else
				player.ChatMessage(messages["newGroupError"]);	
		}
		
		//Change reply
		[ChatCommand("ar_reply")]
		void cmdArReply(BasePlayer player, string cmd, string[] args)
		{
			if (player.net.connection.authLevel == 0) return;			
			if (args.Length > 2)
			{
				string groupName = args[1];
				if (replies.ContainsKey(groupName))
				{
					string mode = args[0];					
					if (mode == "add")
					{
						string reply = args[2];
						int newReply = replies[groupName].Count;
						replies[groupName][newReply] = reply;
						player.ChatMessage(string.Format(messages["replyAdded"], groupName, newReply));
						Config["replies"] = replies;						
						attrForWord[groupName][newReply] = new List<string>();
						if (args.Length > 3){						
						for (int i = 3; i < args.Length; i++)
						{
							if (!attributes.ContainsKey(args[i]))
							{
								player.ChatMessage(string.Format(messages["attrUnknown"], args[i]));
								return;
							}
							attrForWord[groupName][newReply].Add(args[i]);
						}
						}
						Config["attrForWord"] = attrForWord;
						SaveConfig();
						
					}	
					else if(mode == "del")
					{
						int removeKey = Convert.ToInt32(args[2]);
						if(replies[groupName].ContainsKey(removeKey))
						{
							replies[groupName].Remove(removeKey);
							player.ChatMessage(string.Format(messages["replyRemoved"], groupName, removeKey));					
							Config["replies"] = replies;
							attrForWord[groupName].Remove(removeKey);
							Config["attrForWord"] = attrForWord;
							SaveConfig();
						}
						else
							player.ChatMessage(string.Format(messages["replyNotFound"], removeKey, groupName));				
					}
					else if(mode == "set")
					{
						int setKey = Convert.ToInt32(args[2]);
						string reply = args[3];
						if(replies[groupName].ContainsKey(setKey))
						{
							replies[groupName][setKey] = reply;
							player.ChatMessage(string.Format(messages["replyChanged"], groupName, setKey));						
							Config["replies"] = replies;
							SaveConfig();
						}
						else
							player.ChatMessage(string.Format(messages["replyNotFound"], setKey, groupName));		
					}
					else
						player.ChatMessage(messages["Error"]);
				}
				else
				{
					player.ChatMessage(string.Format(messages["wordGroupDontExist"], args[0]));
					return;
				}
			
			}
			else
				player.ChatMessage(messages["changeReplyError"]);	
		}
		
		//Add attrib for word group
		[ChatCommand("ar_wa")]
		void cmdArWA(BasePlayer player, string cmd, string[] args)
		{
			if (player.net.connection.authLevel == 0) return;			
			if (args.Length > 2)
			{
				if (attrForWord.ContainsKey(args[1]))
				{
					string groupName = args[1].ToLower();
					string mode = args[0].ToLower();
					try
					{
						int key = Convert.ToInt32(args[2]);
						string attr = args[3].ToLower();
						if (!attributes.ContainsKey(attr))
						{
							player.ChatMessage(string.Format(messages["attrUnknown"], attr));
							return;
						}
						if (mode == "add")
						{
							if (!attrForWord[groupName][key].Contains(attr))
							{
								attrForWord[groupName][key].Add(attr);
								player.ChatMessage(string.Format(messages["attrAdded"], attr, groupName));
								Config["attrForWord"] = attrForWord;
								SaveConfig();
								return;							
							}
							else
								player.ChatMessage(string.Format(messages["attrWordExist"], attr, groupName));	
							return;
						}
						else if (mode == "del")
						{
							if (attrForWord[groupName][key].Contains(attr))
							{
								attrForWord[groupName][key].Remove(attr);
								player.ChatMessage(string.Format(messages["attrDeleted"], attr, groupName));
								Config["attrForWord"] = attrForWord;
								SaveConfig();
								return;							
							}
							else
								player.ChatMessage(string.Format(messages["attrNotExist"], attr, groupName));	
							return;			
						}
						else
						{
							player.ChatMessage(messages["attrCritError"]);	
						}
					}
					catch
					{
						player.ChatMessage(messages["attrCritError"]);
					}
				}
				else
				{
					player.ChatMessage(string.Format(messages["wordGroupDontExist"], args[0]));
					return;
				}
			
			}
			else
				player.ChatMessage(messages["attrCritError"]);	
		}
		
		//Change matching
		[ChatCommand("ar_match")]
		void cmdArMatch(BasePlayer player, string cmd, string[] args)
		{
			if (player.net.connection.authLevel == 0) return;		
			if (args.Length > 1)
			{
				string group = args[0];
				bool match = Convert.ToBoolean(args[1]);
				if (fullMatch.ContainsKey(group))
				{
					fullMatch[group] = match;
					Config["fullMatch"] = fullMatch;
					player.ChatMessage(string.Format(messages["matchChanged"], group, match.ToString()));							
					SaveConfig();
				}
				else
					player.ChatMessage(string.Format(messages["matchNotFound"], group));							
			}
			else
				player.ChatMessage(messages["Error"]);	
		}
		//Add attrib for word group
		[ChatCommand("ar_word")]
		void cmdArWord(BasePlayer player, string cmd, string[] args)
		{
			if (player.net.connection.authLevel == 0) return;			
			if (args.Length > 2)
			{
				if (attrForWord.ContainsKey(args[1]))
				{
					string groupName = args[1].ToLower();
					string mode = args[0].ToLower();
					string word = args[2].ToLower();

					if (mode == "add")
					{
						if (!checkWord(player, word))
							return;
						if (!wordList[groupName].Contains(word))
						{
							if (word.Substring(0, 1).Equals("!"))
							{
								player.ChatMessage(messages["usageOfExc"]);	
								return;
							}
							wordList[groupName].Add(word);
							player.ChatMessage(string.Format(messages["wordAdded"], word, groupName));
							Config["wordList"] = wordList;
							SaveConfig();
							return;							
						}
						else
							player.ChatMessage(string.Format(messages["wordWordExist"], word, groupName));	
						return;
					}
					else if (mode == "del")
					{
						if (wordList[groupName].Contains(word))
						{
							wordList[groupName].Remove(word);
							player.ChatMessage(string.Format(messages["wordDeleted"], word, groupName));
							if (wordList[groupName].Count > 0)
							{
								Config["wordList"] = wordList;
								SaveConfig();
							}
							else
								removeGroup(player, groupName);
							return;							
						}
						else
							player.ChatMessage(string.Format(messages["wordNotExist"], word, groupName));	
						return;			
					}
					else
					{
						player.ChatMessage(messages["wordCritError"]);	
					}
				}
				else
				{
					player.ChatMessage(string.Format(messages["wordGroupDontExist"], args[0]));
					return;
				}
			
			}
			else
				player.ChatMessage(messages["wordCritError"]);	
		}	
		
		[ChatCommand("ar_list")]
		void cmdArList(BasePlayer player, string cmd, string[] args)
		{
			if (player.net.connection.authLevel == 0) return;
			
			if (args.Length == 0)				
				foreach(var pair in replies)
				{					
					string groupName = pair.Key;					
					player.ChatMessage(string.Format(messages["listGroupReply"], pair.Key));
					player.ChatMessage(string.Format(messages["listWords"], string.Join(", ", wordList[groupName].ToArray())));						
					foreach(var v in pair.Value)
					{
						player.ChatMessage(string.Format("[{0}] - {1}", v.Key, v.Value.QuoteSafe()));
						player.ChatMessage(string.Format("A:{0}", string.Join(", ", attrForWord[groupName][v.Key].ToArray())));
					}						
					player.ChatMessage("--------------------------------------------------");	
				}	
			else
				if (args[0] == "attr")				
					foreach(var pair in attributes)
					{
						player.ChatMessage(string.Format("{0} -> {1}", pair.Key, pair.Value.QuoteSafe()));					
					}
				else if (args[0] == "match")				
					foreach(var pair in fullMatch)
					{
						player.ChatMessage(string.Format("{0} -> {1}", pair.Key, pair.Value.ToString()));					
					}
		}
		
		private void removeGroup(BasePlayer player, string groupname)
		{
			if (replies.ContainsKey(groupname))
			{
				replies.Remove(groupname);
				wordList.Remove(groupname);
				attrForWord.Remove(groupname);
				fullMatch.Remove(groupname);
				antiSpam.Remove(groupname);
				
				Config["replies"] = replies;
				Config["wordList"] = wordList;
				Config["attrForWord"] = attrForWord;
				Config["fullMatch"] = fullMatch;
				SaveConfig();
				
				player.ChatMessage(string.Format(messages["removedGroup"], groupname));
					
			}		
		}
		
		private void removeAttr(string attr)
		{
			foreach(var pair in attrForWord)
			{	
				foreach(var v in pair.Value)
					attrForWord[pair.Key][v.Key].Remove(attr);		
			}
			Config["attrForWord"] = attrForWord;
			SaveConfig();
		}
    }
}