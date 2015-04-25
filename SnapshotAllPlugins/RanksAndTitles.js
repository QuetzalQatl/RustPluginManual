var RanksAndTitles = {
	Title: "Ranks And Titles",
	Author: "Killparadise",
	Version: V(1, 4, 8),
	ResourceId: 830,
	Url: "http://oxidemod.org/resources/ranks-and-titles.830/",
	HasConfig: true,
	Init: function() {
		global = importNamespace("");
		UnityEngine = importNamespace("UnityEngine");
		this.loadTitleData();
		command.AddChatCommand("rt", this.Plugin, "switchCmd");
		command.AddChatCommand("rtdebug", this.Plugin, "debug");
	},

	OnServerInitialized: function() {
		msgs = this.Config.Messages;
		prefix = this.Config.Prefix;
		GroupsAPI = plugins.Find('RotAG-Groups');
		chatHandler = plugins.Find('chathandler');
		clansOn = plugins.Find('RustIOClans');
		if (chatHandler) {
			chatHandler = true;
		} else {
			chatHandler = false;
		}
		if (GroupsAPI) {
			GroupsAPI = true;
		} else {
			GroupsAPI = false;
		}
		this.updateConfig();
	},

	updateConfig: function() {
		if (this.Config.Version !== "1.4") {
			print("[RanksAndTitles] Updating Config, to latest version.")
			this.LoadDefaultConfig();
		} else {
			return false;
		}
	},

	LoadDefaultConfig: function() {
		this.Config.authLevel = 2;
		this.Config.Version = "1.4";
		this.Config.Settings = {
			"Settings": true,
			"deBugOff": true,
			"karma": true,
			"colorSupport": true,
			"useTitles": false,
			"noAdmin": false,
			"usePunishSystem": true,
			"chatNameColor": "#1bd228",
			"staffchatNameColor": "#1bd228"
		};
		this.Config.Punishment = [{
			"rank": 0,
			"multiplier": 2
		}, {
			"rank": 0.5,
			"multiplier": 2.5
		}, {
			"rank": 1,
			"multiplier": 3
		}, {
			"rank": 2,
			"multiplier": 4
		}, {
			"rank": 3,
			"multiplier": 5
		}, {
			"rank": 4,
			"multiplier": 6
		}, {
			"rank": 5,
			"multiplier": 7
		}, {
			"rank": 6,
			"multiplier": 8
		}];
		this.Config.Titles = [{
			"authLvl": 0,
			"title": "Player",
			"Exclude": false,
			"Color": "#FFFFFF"
		}, {
			"authLvl": 0,
			"title": "Donor",
			"Exclude": true,
			"Color": "#ffa500ff"
		}, {
			"authLvl": 1,
			"title": "Mod",
			"Exclude": true,
			"Color": "#add8e6ff"
		}, {
			"authLvl": 2,
			"title": "Admin",
			"Exclude": true,
			"Color": "#800000ff"
		}, {
			"authLvl": 2,
			"title": "Owner",
			"Exclude": true,
			"Color": "#505886"
		}];
		this.Config.Ranks = [{
			"rank": 0,
			"title": "Civilian",
			"karma": 0,
			"killsNeeded": 0,
			"Color": "#FFFFFF",
			"karmaModifier": 1
		}, {
			"rank": 0.5,
			"title": "Small Timer",
			"karma": -1.0,
			"killsNeeded": 1.0,
			"Color": "#ff0000ff",
			"karmaModifier": 1.0
		}, {
			"rank": 0.5,
			"title": "Wannabe",
			"karma": 1.0,
			"killsNeeded": 1.0,
			"Color": "#0000a0ff",
			"karmaModifier": 1.0
		}, {
			"rank": 1.0,
			"title": "Murderer",
			"karma": -10.0,
			"killsNeeded": 10.0,
			"Color": "#ff0000ff",
			"karmaModifier": 5.0
		}, {
			"rank": 1.0,
			"title": "Recruit",
			"karma": 10.0,
			"killsNeeded": "disabled",
			"Color": "#0000a0ff",
			"karmaModifier": 5.0
		}, {
			"rank": 2.0,
			"title": "Serial Killer",
			"karma": -20.0,
			"killsNeeded": 20.0,
			"Color": "#ff0000ff",
			"karmaModifier": 15.0
		}, {
			"rank": 2.0,
			"title": "Soldier",
			"karma": 20.0,
			"killsNeeded": "disabled",
			"Color": "#0000a0ff",
			"karmaModifier": 15.0
		}, {
			"rank": 3.0,
			"title": "Bandit",
			"karma": -25.0,
			"killsNeeded": 25.0,
			"Color": "#ff0000ff",
			"karmaModifier": 30.0
		}, {
			"rank": 3.0,
			"title": "General",
			"karma": 25.0,
			"killsNeeded": "disabled",
			"Color": "#0000a0ff",
			"karmaModifier": 30.0
		}, {
			"rank": 4.0,
			"title": "Captain",
			"karma": -35.0,
			"killsNeeded": 35.0,
			"Color": "#ff0000ff",
			"karmaModifier": 40.0
		}, {
			"rank": 4.0,
			"title": "Icon",
			"karma": 35.0,
			"killsNeeded": "disabled",
			"Color": "#0000a0ff",
			"karmaModifier": 40.0
		}, {
			"rank": 5.0,
			"title": "Bandit Lord",
			"karma": -50.0,
			"killsNeeded": 50.0,
			"Color": "#ff0000ff",
			"karmaModifier": 50.0
		}, {
			"rank": 5.0,
			"title": "Hero",
			"karma": 50.0,
			"killsNeeded": "disabled",
			"Color": "#0000a0ff",
			"karmaModifier": 50.0
		}, {
			"rank": 6.0,
			"title": "Badass",
			"karma": -100.0,
			"killsNeeded": 100.0,
			"Color": "#ff0000ff",
			"karmaModifier": 100.0
		}, {
			"rank": 6.0,
			"title": "Legend",
			"karma": 100.0,
			"killsNeeded": "disabled",
			"Color": "#0000a0ff",
			"karmaModifier": 100.0
		}];
		this.Config.Prefix = {
			"Prefixes": true,
			"ranks": "Ranks",
			"titles": "Titles",
			"ranksandtitles": "RanksAndTitles"
		};
		this.Config.Messages = {
			"Messages": true,
			"Promoted": "You've been Promoted to: ",
			"NoPlyrs": "No Players Found...",
			"plyrWiped": "Player Wiped!",
			"dataRfrsh": "Data Refreshed!",
			"noPerms": "You do not have permission to use this command.",
			"setSuccs": "Player Title Set Successfully!",
			"needTitle": "You need to enter a title for the player!",
			"kills": "Your Kill count is: ",
			"deaths": "Your Death count is: ",
			"kdr": "Your KDR is currently: ",
			"karma": "Your current Karma is: ",
			"rank": "Your current Rank is: ",
			"title": "Your current Title is: ",
			"badSyntaxRt": "The command syntax was incorrect, please use /rt set playername title",
			"infoRanks": "Ranks for players this is an automated system based on kills & karma; this also supports a Bandit Vs Hero Karma System.",
			"infoTitles": "Titles is a system for a community or calm server, allowing owners to set and create custom user titles.",
			"convert": "The Server Admin has switched to Ranks, please use /rtrefresh to reload your player ranks and titles data.",
			"finished": "Great! The plugin will now build the correct data and configurations.",
			"errors": "Incorrect command structure, please try again.",
			"customFnd": "Using Custom Title... Skipping Change...",
			"loseKarma": "You've lost Karma!",
			"gainKarma": "You've gained Karma!",
			"reset": "Player Reset back to Ranks Tree!",
			"cleardata": "Data has been fully cleared!",
			"titlesSet": "The Server is currently using Titles only. No stats avaliable!",
			"switchRanks": "Successfully turned titles only Off!",
			"switchTitles": "Successfully turned titles only On!",
			"broadcast": "The Admin has switched the Titles system, please use /rt refresh to reload player data!",
			"adminsOn": "Admins ranks turned on.",
			"adminsOff": "Admins rankings turned off.",
			"badSyntaxRemove": "Incorrect Syntax please use /rt remove playername",
			"help": "/rt help - Get RanksAndTitles Command Help",
			"badSyntaxKarma": "Invalid syntax please use /rt karma",
			"clearData": "Server Data Wiped...",
			"noData": "No Player Data Found... Attempting to Build.",
			"debugDis": "Debug is currently disabled.",
			"debugRan": "Ran Debug! Thanks!",
			"Demoted": "You've been demoted!",
			"setKarma": "Karma successfully set!",
			"setKarma0": "You can only use numbers to set a players karma.",
			"plyrKarma": " Karma level is: ",
			"checkFailed": "Check failed..",
			"addKarma": "Karma added to player successfully",
			"removeKarma": "Karma removed from player successfully",
			"punishMsg": "You've killed a rankName you've lost an extra karmaAmt Karma!"
		};
		this.Config.Help = [

			"/rt - display your rank or title",
			"/rt stats - get your current stats if in ranks mode",
			"/rt refresh - refreshes your data file, recommended only used after system switch"
		];
		this.Config.AdminHelp = [

			"/rt wipe playername - Wipes the sleceted players Kills, Deaths, KDR, and Karma",
			"/rt set playername title - Sets a custom title to the selected player, this must be a title in config (NOT RANK)",
			"/rt remove playername - removes a given players custom title, and sets them back into the ransk tree",
			"/rt switch - switch titles only mode on and off, this will use config title automatically without Ranks system",
			"/rt noadmin - Removes admins (auth 2 or higher) from ranks system no kills, or ranks will be given.",
			"/rt kset playername karma - set a selected players karma level",
			"/rt kcheck playername - check the selected players karma",
			"/rt kadd playername karma - adds the entered amount of karma to the selected player",
			"/rt krem playername karma - removes the entered amount of karma from the selected player"
		];
	},

	/*-----------------------------------------------------------------
	            When the Player finishes loading in
	------------------------------------------------------------------*/
	OnPlayerInit: function(player) {
		var steamID = rust.UserIDFromPlayer(player);
		this.checkPlayerData(player, steamID);
	},

	/*-----------------------------------------------------------------
	            All of our data handling
	------------------------------------------------------------------*/
	loadTitleData: function() {
		//Lets get our own data and then check to see if theres a groups data file
		TitlesData = data.GetData('RanksandTitles');
		TitlesData = TitlesData || {};
		TitlesData.PlayerData = TitlesData.PlayerData || {};
		GroupData = data.GetData("Groups");
		GroupData = GroupData || {};
	},

	checkPlayerData: function(player, steamID) {
		//Okay lets check our data file for player data
		try {
			var authLvl = player.net.connection.authLevel;
			TitlesData.PlayerData[steamID] = TitlesData.PlayerData[steamID] || {};
			TitlesData.PlayerData[steamID].PlayerID = TitlesData.PlayerData[steamID].PlayerID || steamID;
			TitlesData.PlayerData[steamID].RealName = TitlesData.PlayerData[steamID].RealName || this.getName(player);
			TitlesData.PlayerData[steamID].Title = TitlesData.PlayerData[steamID].Title || "";
			TitlesData.PlayerData[steamID].Rank = TitlesData.PlayerData[steamID].Rank || 0;
			TitlesData.PlayerData[steamID].Kills = TitlesData.PlayerData[steamID].Kills || 0;
			TitlesData.PlayerData[steamID].KDR = TitlesData.PlayerData[steamID].KDR || 0;
			TitlesData.PlayerData[steamID].Deaths = TitlesData.PlayerData[steamID].Deaths || 0;
			TitlesData.PlayerData[steamID].Karma = TitlesData.PlayerData[steamID].Karma || 0;
			TitlesData.PlayerData[steamID].isAdmin = TitlesData.PlayerData[steamID].isAdmin || (authLvl >= 2) || false;
			TitlesData.PlayerData[steamID].hidden = TitlesData.PlayerData[steamID].hidden || false;
			this.saveData();
			this.setRankTitle(steamID, player);
		} catch (e) {
			print(e.message.toString());
		}
	},
	//This function is here so that if a player has an exisiting Group Tag, we don't grab that tag.
	//same with the color tag, but that wont be an issue soon.
	getName: function(player) {
		if (GroupsAPI || clansOn) {
			var realName = player.displayName.split("] ").pop();
		} else {
			var realName = player.displayName;
		}
		return realName;
	},

	saveData: function() {
		//Save our data to our titles data file
		data.SaveData('RanksandTitles');
	},

	//Player data refresh, plugs into the /rt refresh command, this runs through the players data and re assignns everything.
	//Checks if data is present, if not it will attempt to build the players data spot
	refreshData: function(player, cmd, args) {
		var steamID = rust.UserIDFromPlayer(player);
		if (TitlesData.PlayerData[steamID] === undefined) {
			print("No Data found, Attempting to build Data");
			rust.SendChatMessage(player, prefix.ranksandtitles, msgs.noData, "0");
		} else {
			rust.SendChatMessage(player, prefix.ranksandtitles, msgs.dataRfrsh, "0");
		}
		TitlesData.PlayerData[steamID].Title = "";
		this.checkPlayerData(player, steamID);
	},

	/*-----------------------------------------------------------------
	            Our functions to find players
	------------------------------------------------------------------*/

	//Find player by name this supports partial names, full names, and steamIDs its also case-insensitive
	findPlayerByName: function(player, args) {
		try {
			var global = importNamespace("");
			var found = [],
				foundID;
			var playerName = args[1].toLowerCase();
			var itPlayerList = global.BasePlayer.activePlayerList.GetEnumerator();
			while (itPlayerList.MoveNext()) {

				var displayName = itPlayerList.Current.displayName.toLowerCase();

				if (displayName.search(playerName) > -1) {
					print("found match " + displayName);
					found.push(itPlayerList.Current);
				}

				if (playerName.length === 17) {
					if (rust.UserIDFromPlayer(displayName).search(playerName)) {
						found.push(itPlayerList.Current);
					}
				}
			}

			if (found.length) {
				foundID = rust.UserIDFromPlayer(found[0]);
				found.push(foundID);
				return found;
			} else {
				rust.SendChatMessage(player, prefix.titles, msgs.NoPlyrs, "0");
				return false;
			}
		} catch (e) {
			print(e.message.toString());
		}
	},

	//This function is rarely used and currently is no longer needed if I remember correct.
	//Best to ignore it for now.
	findPlayer: function(playerid) {
		var global = importNamespace("");
		var targetPlayer = global.BasePlayer.Find(playerid);
		if (targetPlayer) {
			return targetPlayer;
		} else {
			return false;
		}
	},

	//This is a dangerous function as it wipes the entire player data section of our plugins data.
	clearData: function(player, cmd, args) {
		try {
			delete TitlesData.PlayerData;
			rust.SendChatMessage(player, prefix.ranksandtitles, msgs.clearData, "0");
			TitlesData.PlayerData = TitlesData.PlayerData || {};
			this.saveData();
			this.loadTitleData();
		} catch (e) {
			print(e.message.toString());
		}
	},

	/*--------------------Switch Command Handling----------------------------*/

	//This is our switch case statement this is called by /rt it then grabs the second word in the text and compares it to one below
	//if a match is found, it will launch that function accordingly.
	switchCmd: function(player, cmd, args) {
		try {
			var steamID = rust.UserIDFromPlayer(player),
				authLvl = player.net.connection.authLevel,
				useTitles = this.Config.Settings.useTitles;
			switch (args[0]) {
				case "stats":
					if (!useTitles) {
						this.checkStats(player, cmd, args);
					} else {
						rust.SendChatMessage(player, prefix.ranksandtitles, msgs.titlesSet, "0");
						return false;
					}
					break;
				case "hide":
					if (authLvl >= this.Config.authLevel) {
						this.hideCmd(player, cmd, args);
					} else {
						rust.SendChatMessage(player, prefix.ranksandtitles, msgs.noPerms, "0");
						return false;
					}
					break;
				case "wipe":
					if (authLvl >= this.Config.authLevel) {
						this.wipePlayer(player, cmd, args);
					} else {
						rust.SendChatMessage(player, prefix.ranksandtitles, msgs.noPerms, "0");
						return false;
					}
					break;
				case "set":
					if (authLvl >= this.Config.authLevel && args.length >= 2) {
						this.giveTitle(player, cmd, args);
					} else if (authLvl < this.Config.authLevel) {
						rust.SendChatMessage(player, prefix.ranksandtitles, msgs.noPerms, "0");
						return false;
					} else {
						rust.SendChatMessage(player, prefix.ranksandtitles, msgs.badSyntaxRt, "0");
						return false;
					}
					break;
				case "kset":
					if (args.length >= 1 && authLvl >= this.Config.authLevel) {
						this.setKarma(player, cmd, args);
					} else {
						rust.SendChatMessage(player, prefix.ranksandtitles, msgs.noPerms, "0");
					}
					break;
				case "kcheck":
					if (args.length >= 1 && authLvl >= this.Config.authLevel) {
						this.checkKarma(player, cmd, args);
					} else {
						rust.SendChatMessage(player, prefix.ranksandtitles, msgs.noPerms, "0");
					}
					break;
				case "kadd":
					if (args.length >= 1 && authLvl >= this.Config.authLevel) {
						this.addKarma(player, cmd, args);
					} else {
						rust.SendChatMessage(player, prefix.ranksandtitles, msgs.noPerms, "0");
					}
					break;
				case "krem":
					if (args.length >= 1 && authLvl >= this.Config.authLevel) {
						this.removeKarma(player, cmd, args);
					} else {
						rust.SendChatMessage(player, prefix.ranksandtitles, msgs.noPerms, "0");
					}
					break;
				case "remove":
					if (authLvl >= this.Config.authLevel && args.length >= 1) {
						this.removeTitle(player, cmd, args);
					} else if (authLvl < this.Config.authLevel) {
						rust.SendChatMessage(player, prefix.ranksandtitles, msgs.noPerms, "0");
						return false;
					} else {
						rust.SendChatMessage(player, prefix.ranksandtitles, msgs.badSyntaxRemove, "0");
						return false;
					}
					break;
				case "clear":
					if (authLvl >= this.Config.authLevel) {
						this.clearData(player, cmd, args);
					} else {
						rust.SendChatMessage(player, prefix.ranksandtitles, msgs.noPerms, "0");
						return false;
					}
					break;
				case "refresh":
					this.refreshData(player, cmd, args);
					break;
				case "switch":
					if (useTitles && (authLvl >= this.Config.authLevel)) {
						this.Config.Settings.useTitles = false;
						rust.SendChatMessage(player, prefix.ranksandtitles, msgs.switchRanks, "0");
						rust.BroadcastChat(prefix.ranksandtitles, "<color=red>" + msgs.broadcast + "</color>", "0");
					} else if ((authLvl >= this.Config.authLevel) && !useTitles) {
						this.Config.Settings.useTitles = true;
						rust.SendChatMessage(player, prefix.ranksandtitles, msgs.switchTitles, "0");
						rust.BroadcastChat(prefix.ranksandtitles, "<color=red>" + msgs.broadcast + "</color>", "0");
					} else {
						rust.SendChatMessage(player, prefix.ranksandtitles, msgs.noPerms, "0");
						break;
					}
					this.SaveConfig();
					break;
				case "noadmin":
					if (authLvl >= this.Config.authLevel) {
						this.noAdmin(player, cmd, args);
					} else {
						rust.SendChatMessage(player, prefix.ranksandtitles, msgs.noPerms, "0");
					}
					break;
				case "help":
					this.rtHelp(player, cmd, args);
					break;
				default:
					if (!useTitles && TitlesData.PlayerData[steamID] !== undefined) {
						rust.SendChatMessage(player, prefix.ranks, msgs.rank + TitlesData.PlayerData[steamID].Rank + " (" + TitlesData.PlayerData[steamID].Title + ")", "0");
					} else if (useTitles && TitlesData.PlayerData[steamID] !== undefined) {
						rust.SendChatMessage(player, prefix.titles, msgs.title + " " + TitlesData.PlayerData[steamID].Title, "0");
					} else {
						rust.SendChatMessage(player, prefix.ranksandtitles, msgs.noData, "0");
						this.checkPlayerData(player, steamID);
					}
					break;
			}
		} catch (e) {
			print(e.message.toString());
		}
	},

	/*-----------------------------------------------------------------
	                 Get our Counts and set Ranks
	 ------------------------------------------------------------------*/

	//This is the arithmatic function to grab the closes karma number from our ranks
	getClosest: function(arr, closestTo) {
		try {
			arr = this.getRanksArray();
			if (arr.length > 0) {

				for (var i = 0; i < arr.length; i++) {
					if (closestTo >= 0) {
						if (arr[i] <= closestTo && arr[i] >= 0) closest = arr[i];
					} else if (closestTo <= 0) {
						if (arr[i] >= closestTo && arr[i] <= 0) closest = arr[i];
					}
				}
			}
			return closest;
		} catch (e) {
			print(e.message.toString())
		}
	},

	getRanksArray: function() {
		try {
			var temp = [];

			for (var i = 0; i < this.Config.Ranks.length; i++) {
				if (this.Config.Settings.karma) {
					temp.push(this.Config.Ranks[i].karma);
				} else {
					if (this.Config.Ranks[i].killsNeeded !== "disabled") {
						temp.push(this.Config.Ranks[i].killsNeeded);
					}
				}
			}
			return temp;
		} catch (e) {
			print(e.message.toString());
		}
	},

	checkPunish: function(killerID, victimID) {
		try {
			if (this.Config.Settings.usePunishSystem && this.Config.Settings.karma) {
				var punish = this.Config.Punishment;
				var player = this.findPlayer(killerID),
					karma;
				for (var i = 0; i < punish.length; i++) {
					if (TitlesData.PlayerData[killerID].Rank === punish[i].rank && TitlesData.PlayerData[victimID].Karma < 0 && TitlesData.PlayerData[killerID].Karma < 0) {
						karma = TitlesData.PlayerData[victimID].Karma * punish[i].multiplier;
					} else if (TitlesData.PlayerData[killerID].Rank === punish[i].rank && TitlesData.PlayerData[victimID].Karma > 0 && TitlesData.PlayerData[killerID].Karma > 0) {
						karma = TitlesData.PlayerData[victimID].Karma * punish[i].multiplier;
					} else {
						return 0;
					}
				}
				var rplObj = {
					rankName: "<color=lime>" + TitlesData.PlayerData[victimID].Title + "</color>",
					karmaAmt: "<color=red>" + karma + "</color>"
				};
				rust.SendChatMessage(player, prefix.ranksandtitles, msgs.punishMsg.replace(/rankName|karmaAmt/gi, function(matched) {
					return rplObj[matched];
				}), "0");
				return karma;
			} else {
				return 0;
			}
		} catch (e) {
			print(e.message.toString());
		}
	},
	//this is our main hub all player data hits this function and is then sent else where if need be
	//or it will continue through the process. This is the default ranks function
	//it checks certain features and if a special case is not found, it will run its code.
	setRankTitle: function(playerID, player) {
		if (playerID === "Test") return true;

		for (var ii = 0; ii < this.Config.Titles.length; ii++) {
			if (TitlesData.PlayerData[playerID].Title === this.Config.Titles[ii].title) {
				return false;
			}
		}

		if (TitlesData.PlayerData[playerID].isAdmin && noAdmin) {
			print("Admins turned off for rankings. Skipping Admin.");
			return false;
		} else if (useTitles) {
			return this.setTitle(playerID, player);
		} else {
			var i = 0,
				j = this.Config.Ranks.length,
				useTitles = this.Config.Settings.useTitles,
				kills = TitlesData.PlayerData[playerID].Kills,
				karma = TitlesData.PlayerData[playerID].Karma,
				karmaOn = this.Config.Settings.karma,
				noAdmin = this.Config.Settings.noAdmin,
				oldRank = TitlesData.PlayerData[playerID].Rank;

			for (i; i < j; i++) {
				if (karmaOn && this.getClosest([], karma) === this.Config.Ranks[i].karma) {
					TitlesData.PlayerData[playerID].Title = this.Config.Ranks[i].title;
					TitlesData.PlayerData[playerID].Rank = this.Config.Ranks[i].rank;
				} else if (!karmaOn && this.getClosest([], kills) === this.Config.Ranks[i].killsNeeded) {
					TitlesData.PlayerData[playerID].Title = this.Config.Ranks[i].title;
					TitlesData.PlayerData[playerID].Rank = this.Config.Ranks[i].rank;
				}
			}
			this.checkPromo(oldRank, TitlesData.PlayerData[playerID].Rank, player);
		}
		this.saveData();
	},

	//This is our function if Titles Only mode is set to true, this function is called by our main hub and then sets titles instead
	//of ranks to a players name. it then sends this data back to our data file to be saved
	setTitle: function(playerID, player) {
		if (playerID === "Test") return true;
		var i = 0,
			j = this.Config.Titles.length,
			useTitles = this.Config.Settings.useTitles;

		for (i; i < j; i++) {
			if (useTitles) {
				TitlesData.PlayerData[playerID].Title = this.Config.Titles[i].title;
			}
		}
		this.saveData();
	},

	/*-----------------------------------------------------------------
	            Check for promotions
	------------------------------------------------------------------*/

	//This is called by our main hub if a players rank increases it will display the message "you've been promoted!"
	//this may not exist for long and may be merged into the main hub function soon.
	checkPromo: function(oldRank, currRank, player) {
		var steamID = rust.UserIDFromPlayer(player);
		if (currRank > oldRank) {
			rust.SendChatMessage(player, prefix.ranks, "<color=green>" + msgs.Promoted + "</color>" + " " + TitlesData.PlayerData[steamID].Title, "0");
		} else if (currRank < oldRank) {
			rust.SendChatMessage(player, prefix.ranks, "<color=red>" + msgs.Demoted + "</color>", "0");
		}

	},

	/*-----------------------------------------------------------------
	                    Grab Karma and Karma Commands
	------------------------------------------------------------------*/

	//A simple function to allow our users to set custom karma modifiers for each rank this searches our config file
	//for karmaModifier to the matching title of the killed players ID it then send the found number back to the death function
	//to add or subtract the appropriate karma.
	getKarma: function(victimID) {
		var i = 0,
			j = this.Config.Ranks.length;
		for (i; i < j; i++) {
			if (this.Config.Ranks[i].title === TitlesData.PlayerData[victimID].Title) {
				return this.Config.Ranks[i].karmaModifier;
			} else {
				return 1;
			}
		}
	},

	/*-----------------------------------------------------------------
	                    Check for Deaths
	------------------------------------------------------------------*/

	//This is our death function this is the primary function, if this breaks. Everything breaks
	//it has several fail safe checks to make sure data is present, and make sure it isn't corrupt
	//if so it stops the check and tells the admin about the problem needing to be addressed
	//Hopefully soon this function will maintain itself 100% automatically it's due for a good re write to optimize its functionality
	//It then assigns ids, makes sure everyone involved was players, makes sure which system to use
	//and then sends appropriate data where it needs to go, it then runs the players through the
	//KDR and ranks functions (remember our hub?) this process may also change during the re write.
	OnEntityDeath: function(entity, hitinfo) {
		try {
			var victim = entity,
				attacker = hitinfo.Initiator,
				victimID,
				killerID;
			var useTitles = this.Config.Settings.useTitles;

			if (victim.ToPlayer() && attacker.ToPlayer() && !useTitles && victim.displayName !== attacker.displayName) {
				var killer = attacker.ToPlayer();
				killerID = rust.UserIDFromPlayer(killer);
				victimID = rust.UserIDFromPlayer(victim);

				if (!TitlesData.PlayerData[killerID]) {
					print("Killer does not have registered Data in Data File.");
					print("Attempting to create killer Data file...");
					this.checkPlayerData(killer, killerID);
				} else if (!TitlesData.PlayerData[victimID] && victim.IsConnected()) {
					print("Victim does not have registered Data in Data File");
					print("Attempting to create Victim Data File...");
					this.checkPlayerData(victim, victimID);
				}

				var karmaOn = this.Config.Settings.karma;
				if (karmaOn && TitlesData.PlayerData[victimID].Karma >= 0) {
					TitlesData.PlayerData[killerID].Kills += 1;
					TitlesData.PlayerData[victimID].Deaths += 1;
					TitlesData.PlayerData[killerID].Karma -= (this.getKarma(victimID) - this.checkPunish(killerID, victimID));
					rust.SendChatMessage(killer, prefix.ranks, msgs.loseKarma + " (" + this.getKarma(victimID) + ")", "0");
				} else if (karmaOn && TitlesData.PlayerData[victimID].Karma < 0) {
					TitlesData.PlayerData[killerID].Kills += 1;
					TitlesData.PlayerData[victimID].Deaths += 1;
					TitlesData.PlayerData[killerID].Karma += (this.getKarma(victimID) + this.checkPunish(killerID, victimID));
					rust.SendChatMessage(killer, prefix.ranks, msgs.gainKarma + " (" + this.getKarma(victimID) + ")", "0");
				} else {
					TitlesData.PlayerData[killerID].Kills += 1;
					TitlesData.PlayerData[victimID].Deaths += 1;
				}
				this.setRankTitle(killerID, killer);
				this.updateKDR(TitlesData.PlayerData[victimID].Kills, TitlesData.PlayerData[victimID].Deaths, victim.ToPlayer());
				this.updateKDR(TitlesData.PlayerData[killerID].Kills, TitlesData.PlayerData[killerID].Deaths, killer);
			} else if (victim.ToPlayer() && victim.displayName === attacker.displayName) {
				var victimID = rust.UserIDFromPlayer(victim);
				TitlesData.PlayerData[victimID].Deaths += 1;
				this.updateKDR(TitlesData.PlayerData[victimID].Kills, TitlesData.PlayerData[victimID].Deaths, victim.ToPlayer());
			} else {
				return false;
			}
		} catch (e) {
			print(e.message.toString());
		}
	},

	/*-----------------------------------------------------------------
	                     Command Handling
	------------------------------------------------------------------*/

	setKarma: function(player, cmd, args) {
		var getPlayer = this.findPlayerByName(player, args);
		var karmaAmt = Number(args[2]);
		if (getPlayer && typeof(karmaAmt) === "number") {
			TitlesData.PlayerData[getPlayer[1]].Karma = karmaAmt;
			this.saveData();
			rust.SendChatMessage(player, prefix.ranks, msgs.setKarma, "0");
		} else {
			rust.SendChatMessage(player, prefix.ranks, msgs.setKarma0, "0");
		}
	},

	checkKarma: function(player, cmd, args) {
		var getPlayer = this.findPlayerByName(player, args);
		if (getPlayer) {
			rust.SendChatMessage(player, prefix.ranks, getPlayer[0].displayName + msgs.plyrKarma + TitlesData.PlayerData[getPlayer[1]].Karma, "0");
		} else {
			rust.SendChatMessage(player, prefix.ranks, msgs.checkFailed, "0");
		}
	},

	addKarma: function(player, cmd, args) {
		var getPlayer = this.findPlayerByName(player, args);
		var karmaAmt = Number(args[2]);
		if (typeof(karmaAmt) === "number") {
			TitlesData.PlayerData[getPlayer[1]].Karma += karmaAmt;
			this.saveData();
			rust.SendChatMessage(player, prefix.ranks, msgs.addKarma, "0");
		}
	},

	removeKarma: function(player, cmd, args) {
		var getPlayer = this.findPlayerByName(player, args);
		var karmaAmt = Number(args[2]);
		if (typeof(karmaAmt) === "number") {
			TitlesData.PlayerData[getPlayer[1]].Karma -= karmaAmt;
			this.saveData();
			rust.SendChatMessage(player, prefix.ranks, msgs.removeKarma, "0");
		}
	},

	//this function is caused by our death checker, this sends data to our data file to keep track of a KDR for the
	//player normally it is called twice each kill (called at the same time) luckily it processes and handles the
	//Ids efficiently so it knows where to send what.
	updateKDR: function(kills, deaths, player) {
		var steamID = rust.UserIDFromPlayer(player);
		var killsToDeaths = kills / deaths;
		killsToDeaths = Math.ceil(killsToDeaths * 100) / 100;
		TitlesData.PlayerData[steamID].KDR = killsToDeaths;
		this.saveData();
	},

	//this is our function that is called by /rt stats it is setup so it will display the currently asking players
	//stats from the data file. It searches the playerdata via steamID.
	checkStats: function(player, cmd, args) {
		var steamID = rust.UserIDFromPlayer(player);
		rust.SendChatMessage(player, prefix.ranks, msgs.kills + TitlesData.PlayerData[steamID].Kills, "0");
		rust.SendChatMessage(player, prefix.ranks, msgs.deaths + TitlesData.PlayerData[steamID].Deaths, "0");
		rust.SendChatMessage(player, prefix.ranks, msgs.kdr + TitlesData.PlayerData[steamID].KDR, "0");
		if (this.Config.Settings.karma) rust.SendChatMessage(player, prefix.ranks, msgs.karma + TitlesData.PlayerData[steamID].Karma, "0");
	},

	//Wipe player resets the chosen players kills, deaths, kdr, and their karma (if its turned on) it does a quick search
	//for the player using our find function and then sends the data to the target player to replace the current set data
	//it then saves and finishes up.
	wipePlayer: function(player, cmd, args) {
		var target = this.findPlayerByName(player, args) || "";
		if (target[1].length) {
			TitlesData.PlayerData[target[1]].Kills = 0;
			TitlesData.PlayerData[target[1]].Deaths = 0;
			TitlesData.PlayerData[target[1]].KDR = 0;
			if (this.Config.Settings.karma) TitlesData.PlayerData[target[1]].Karma = 0;
			rust.SendChatMessage(player, prefix.ranks, msgs.plyrWiped, "0");
			this.saveData();
			this.setRankTitle(target[1], target[0]);
		} else if (!target[1].length) {
			rust.SendChatMessage(player, prefix.ranks, msgs.NoPlyrs, "0");
			return false;
		} else {
			rust.SendChatMessage(player, prefix.ranks, msgs.NoPlyrs, "0");
			return false;
		}
	},

	//This function is used by the remove command, when called it will find the target player
	//grab his file, and then set his title to nothing. It will then run him through the hub function
	//to set his new ranks title instead. This is so if a player with a custom title wishes to
	//go back to the ranks system, he can do so upon asking an admin.
	removeTitle: function(player, cmd, args) {
		try {
			var getPlayer = this.findPlayerByName(player, args);
			TitlesData.PlayerData[getPlayer[1]].Title = "";
			this.saveData();
			this.setRankTitle(getPlayer[1], getPlayer[0]);
			rust.SendChatMessage(player, prefix.ranks, msgs.reset + " " + getPlayer[0].displayName, "0");
		} catch (e) {
			print(e.message.toString());
		}
	},

	//These are our series of commands that are useable and called on by our Switch above. Each one
	//speaks for itself and should be easy to tell what it does.

	hideCmd: function(player, cmd, args) {
		var steamID = rust.UserIDFromPlayer(player);
		if (!TitlesData.PlayerData[steamID].hidden) {
			TitlesData.PlayerData[steamID].hidden = true;
			rust.SendChatMessage(player, prefix.titles, "Your tag is now hidden!", "0");
		} else {
			TitlesData.PlayerData[steamID].hidden = false;
			rust.SendChatMessage(player, prefix.titles, "Your tag is no longer hidden!", "0");
		}
		this.saveData();
	},

	giveTitle: function(player, cmd, args) {

		var getPlayer = this.findPlayerByName(player, args);
		var getPlayerData = TitlesData.PlayerData,
			j = this.Config.Titles.length,
			i = 0;
		if (args[2].length) {
			for (i; i < j; i++) {
				if (args[2].toLowerCase() === this.Config.Titles[i].title.toLowerCase()) {
					getPlayer[0].displayName = getPlayerData[getPlayer[1]].RealName + "[" + this.Config.Titles[i].title + "]";
					TitlesData.PlayerData[getPlayer[1]].Title = this.Config.Titles[i].title;
				}
			}
			rust.SendChatMessage(player, prefix.ranksandtitles, msgs.setSuccs, "0");
		} else {
			rust.SendChatMessage(player, prefix.ranksandtitles, msgs.needTitle, "0");
			return false;
		}
		this.saveData();
	},

	noAdmin: function(player, cmd, args) {
		var noAdmin = this.Config.Settings.noAdmin;
		if (noAdmin) {
			this.Config.Settings.noAdmin = false;
			rust.SendChatMessage(player, prefix.ranksandtitles, msgs.adminsOn, "0");
		} else {
			this.Config.Settings.noAdmin = true;
			rust.SendChatMessage(player, prefix.ranksandtitles, msgs.adminsOff, "0");
		}
		this.SaveConfig();
	},

	rtHelp: function(player, cmd, args) {
		rust.SendChatMessage(player, null, "--------------RanksAndTitles Commands------------", "0");
		var authLvl = player.net.connection.authLevel;
		for (var i = 0; i < this.Config.Help.length; i++) {
			rust.SendChatMessage(player, null, this.Config.Help[i], "0");
		}
		if (authLvl >= 2) {
			rust.SendChatMessage(player, null, "<color=orange>--------------Admin Commands------------</color>", "0");
			for (var j = 0; j < this.Config.AdminHelp.length; j++) {
				rust.SendChatMessage(player, null, this.Config.AdminHelp[j], "0");
			}
		}
	},

	SendHelpText: function(player) {
		rust.SendChatMessage(player, prefix.ranksandtitles, msgs.help, "0");
	},

	/*-----------------------------------------------------------------
	                        Chat Handling(New)
	------------------------------------------------------------------*/

	//This function is used by playerchat to grab the correct colors for the rank, or title used by the player
	//it will then send back the found color for the chat function to use.
	getColor: function(steamID) {
		//This is for custom titles being set with ranks may be removed with recent fixes
		for (var i = 0; i < this.Config.Titles.length; i++) {
			if (TitlesData.PlayerData[steamID].Title === this.Config.Titles[i].title) {
				color = this.Config.Titles[i].Color;
				return color;
			}
		}


		if (!this.Config.Settings.useTitles) {
			for (var i = 0; i < this.Config.Ranks.length; i++) {
				if (TitlesData.PlayerData[steamID].Title === this.Config.Ranks[i].title) {
					color = this.Config.Ranks[i].Color;
					return color;
				}
			}
		} else {
			for (var i = 0; i < this.Config.Titles.length; i++) {
				if (TitlesData.PlayerData[steamID].Title === this.Config.Titles[i].title) {
					color = this.Config.Titles[i].Color;
					return color;
				}
			}
		}
	},

	grabPlayerData : function(steamID, key) {
        return TitlesData.PlayerData[steamID][key]
    },

	//Our char function is called whenever a chat message is sent, it grabs a slew of information including, player files, the message
	//the player title and realname, and the steamId, using all of this it checks to make sure the chat wasn't empty or a command
	//then if checks if color support is activated if so it will sent the chat with the colored title
	//if its not enabled then it will send a default message with the players assigned title without the color.
	//We have to make sure we return false afterwards or else we will get double chat messages with every chat sent.
	OnPlayerChat: function(arg) {
		try {
			if (!chatHandler) {
				var global = importNamespace("");
				var player = arg.connection.player,
					authLevel = player.net.connection.authLevel,
					msg = arg.GetString(0, "text");
				var steamID = rust.UserIDFromPlayer(player);
				var title = TitlesData.PlayerData[steamID].Title,
					hidden = TitlesData.PlayerData[steamID].hidden,
					colorOn = this.Config.Settings.colorSupport,
					color = this.getColor(steamID);
				var displayName = player.displayName;
				if (msg.substring(1, 1) === "/" || msg === "") return false;
				if (hidden || title === "") {
					global.ConsoleSystem.Broadcast("chat.add", steamID, "<color=" + this.Config.Settings.chatNameColor + ">" + displayName + "</color> " + msg);
					print(player.displayName + ": " + msg);
					return false;
				} else if (colorOn && authLevel === 0) {
					global.ConsoleSystem.Broadcast("chat.add", steamID, "<color=" + this.Config.Settings.chatNameColor + ">" + displayName + "</color>" + " <color=" + color + ">[" + title + "]</color> " + msg);
					print(player.displayName + ": " + msg);
					return false;
				} else if (!colorOn && authLevel === 0) {
					global.ConsoleSystem.Broadcast("chat.add", steamID, "<color=" + this.Config.Settings.chatNameColor + ">" + displayName + " [" + title + "] " + "</color>" + msg);
					print(player.displayName + ": " + msg);
					return false;
				} else if (authLevel >= 1 && colorOn) {
					global.ConsoleSystem.Broadcast("chat.add", steamID, "<color=" + this.Config.Settings.staffchatNameColor + ">" + displayName + "</color>" + " <color=" + color + ">[" + title + "]</color> " + msg);
					print(player.displayName + ": " + msg);
					return false;
				} else if (authLevel >= 1 && !colorOn) {
					global.ConsoleSystem.Broadcast("chat.add", steamID, "<color=" + this.Config.Settings.staffchatNameColor + ">" + displayName + " [" + title + "] " + "</color> " + msg);
					print(player.displayName + ": " + msg);
					return false;
				}
			} else {
				return null;
			}
		} catch (e) {
			print(e.message.toString());
		}
	},

	/*-----------------------------------------------------------------
	                        Debugger
	------------------------------------------------------------------*/
	//Simple debugger that will place lots of information into the console for issue disputes
	debug: function(player, cmd, args) {
		try {
			var authLvl = player.net.connection.authLevel;
			if (!this.Config.Settings.deBugOff || authLvl >= 2) {
				var steamID = rust.UserIDFromPlayer(player);
				GroupData = data.GetData("Groups");
				print("----Starting Debug-----");
				print("--------Current Player Debug-----------");
				print("Player ID: " + TitlesData.PlayerData[steamID].PlayerID);
				print("Titles Real Name: " + TitlesData.PlayerData[steamID].RealName);
				print("Real name after adjustment: " + TitlesData.PlayerData[steamID].RealName.split("] ").pop());
				print("Groups Name: " + GroupData.PlayerData[steamID].RealName);
				print("Group: " + GroupData.PlayerData[steamID].Group);
				print("Player Title: " + TitlesData.PlayerData[steamID].Title);
				print("Player Rank: " + TitlesData.PlayerData[steamID].Rank);
				print("Player Kills: " + TitlesData.PlayerData[steamID].Kills);
				print("Player Deaths: " + TitlesData.PlayerData[steamID].Deaths);
				print("-------End Player Debug-------");
				print("--------Server Info-------");
				print("Config Version: " + this.Config.version);
				print("Groups Returns Data: " + GroupsAPI);
				print("Colors On: " + this.Config.Settings.colorSupport);
				print("Karma On: " + this.Config.Settings.karma);
				print("Use Titles Only Enabled: " + this.Config.Settings.useTitles);
				print("Admins turned off: " + this.Config.Settings.onAdmin);
				print("Number of Ranks: " + this.Config.Ranks.length);
				print("Number of Titles: " + this.Config.Titles.length);
				print("------End Server Info------");
				print("-----Start Function Debug-------");
				print("setRankTitle Responding: " + this.setRankTitle("Test"));
				print("setTitle Responding: " + this.setTitle("Test"));
				print("setCustomTitle Responding: " + this.setCustomTitle("Test"));
				print("-------End Function Debug--------");
				print("----End Debug----");
				rust.SendChatMessage(player, prefix.ranksandtitles, msgs.debugRan, "0");
			} else {
				rust.SendChatMessage(player, prefix.ranksandtitles, msgs.debugDis, "0");
			}
		} catch (e) {
			print(e.message.toString());
		}
	}
}
