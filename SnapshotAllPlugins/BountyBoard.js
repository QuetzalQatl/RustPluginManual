var BountyBoard = {
	Title: "Bounty Board",
	Author: "Killparadise",
	Version: V(1, 0, 1),
	HasConfig: true,
	Init: function() {
		this.getData();
		global = importNamespace("");
	},

	OnServerInitialized: function() {
		this.msgs = this.Config.Messages;
		this.prefix = this.Config.Prefix;
		command.AddChatCommand("bty", this.Plugin, "cmdBounty");
	},

	LoadDefaultConfig: function() {
		this.Config.authLevel = 2;
		this.Config.Settings = {
			"autoBounties": true,
			"maxBounty": 100000,
			"targetModifier": 2,
			"staffCollect": false,
			"useEcon": false
		};

		this.Config.Prefix = "BountyBoard";

		this.Config.Messages = {
			"curBounty": "The Current Bounty on your head is: ",
			"invSyn": "Syntax Invalid, Please try again. {cmd}",
			"noBty": "Target has no Bounty!",
			"setTar": "Target set. Happy Hunting.",
			"setTrgWarn": " Made you his target! Watch out!",
			"curTar": "Your current target is: ",
			"offline": "That Player is currently offline.",
			"btyClaim": "<color=lime>plyrName</color> has taken the bounty of <color=green>btyAmt</color> from <color=red>deadPlyr</color>!",
			"staff": "Sorry, Staff cannot collect Bounties from slain players.",
			"btyPlaced": "Someone placed a <color=green>{bty}</color> bounty on you!",
			"notEnough": "Not Enough <color=red>{RssName}</color>",
			"overMax": "You cannot exceed the max bounty of <color=red>{maxBty}</color>",
			"notFound": "Item Not Found.",
			"currBty": "The Current Bounty on your head is: ",
			"resetData": "BountyBoard Data Reset",
			"btySet": "<color=green>{bty}</color> bounty has been set!",
			"negBty": "You cannot set a negative bounty!"
		};

		this.Config.Help = [

			"/bty - Check the current bounty on your head",
	    "/bty add playername amt itemname - Add a bounty onto a targeted player.",
	    "/bty board - shows the Bounty Board of everyone who has a bounty."
		];
		this.Config.AdminHelp = [

			"/bty reset - resets all of the bounty board data"
		];
	},

	OnPlayerInit: function(player) {
		this.checkPlayerData(player);
	},

	//----------------------------------------
	//          Finding Player Info
	//----------------------------------------
	findPlayerByName: function(player, args) {
		try {
			var global = importNamespace("");
			var found = [],
				matches = [];
			var playerName = args[1].toLowerCase();
			var itPlayerList = global.BasePlayer.activePlayerList.GetEnumerator();
			while (itPlayerList.MoveNext()) {

				var displayName = itPlayerList.Current.displayName.toLowerCase();

				if (displayName.search(playerName) > -1) {
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
				rust.SendChatMessage(player, this.prefix, this.msgs.NoPlyrs, "0");
				return false;
			}
		} catch (e) {
			print(e.message.toString());
		}
	},

	findPlayerByID: function(playerid) {
		var global = importNamespace("");
		var targetPlayer = global.BasePlayer.Find(playerid);
		if (targetPlayer) {
			return targetPlayer;
		} else {
			return false;
		}
	},

	//----------------------------------------
	//          Data Handling
	//----------------------------------------
	getData: function() {
		BountyData = data.GetData('Bounty');
		BountyData = BountyData || {};
		BountyData.PlayerData = BountyData.PlayerData || {};
		BountyData.Board = BountyData.Board || {};
	},

	saveData: function() {
		data.SaveData('Bounty');
	},

	checkPlayerData: function(player) {
		var steamID = rust.UserIDFromPlayer(player);
		var authLvl = player.net.connection.authLevel;
		BountyData.PlayerData[steamID] = BountyData.PlayerData[steamID] || {};
		BountyData.PlayerData[steamID].Target = BountyData.PlayerData[steamID].Target || "";
		BountyData.PlayerData[steamID].Bounty = BountyData.PlayerData[steamID].Bounty || [];
		BountyData.PlayerData[steamID].BountyType = BountyData.PlayerData[steamID].BountyType || [];
		BountyData.PlayerData[steamID].isStaff = BountyData.PlayerData[steamID].isStaff || (authLvl > 0) || false;
		this.saveData();
	},

	//----------------------------------------
	//          Command Handling
	//----------------------------------------
	cmdBounty: function(player, cmd, args) {
		try {
			var steamID = rust.UserIDFromPlayer(player);
			var authLvl = player.net.connection.authLevel;
			switch (args[0]) {
				case "add":
					this.addBounty(player, cmd, args);
					break;
				case "target":
					this.setTarget(player, cmd, args);
					break;
				case "board":
					this.checkBoard(player, cmd, args);
					break;
				case "help":
					this.BtyHelp(player);
					break;
				case "reset":
					if (authLvl >= this.Config.authLevel) {
						this.resetData(player, cmd, args);
					} else if (authLvl < this.Config.authLevel) {
						rust.SendChatMessage(player, this.prefix, this.msgs.noPerms, "0");
						return false;
					} else {
						rust.SendChatMessage(player, this.prefix, this.msgs.invSyn.replace("{cmd}", "/bty arg"), "0");
						return false;
					}
				default:
					if (BountyData.PlayerData[steamID] === undefined) {
						print("Player Data not Found for " + steamID + " Attempting to build");
						this.checkPlayerData(player);
					} else {
						if (BountyData.PlayerData[steamID].Bounty.length > 0) {
							rust.SendChatMessage(player, this.prefix, this.msgs.currBty + " " + "<color=green>" + BountyData.PlayerData[steamID].Bounty + "</color>", "0");
						} else {
							rust.SendChatMessage(player, this.prefix, this.msgs.currBty + " " + "<color=green>" + "0" + "</color>", "0");
						}
					}
					break;
			}
		} catch (e) {
			print(e.message.toString())
		}
	},

	resetData: function(player, cmd, args) {
		try {
			delete BountyData.PlayerData;
			delete BountyData.Board;
			this.saveData();
			this.getData();
			rust.SendChatMessage(player, this.prefix, this.msgs.resetData, "0");
		} catch (e) {
			print(e.message.toString())
		}
	},

	setTarget: function(player, cmd, args) {
		if (args.length === 2) {
			var pName = this.findPlayerByName(player, args);
			var steamID = rust.UserIDFromPlayer(player);
		} else if (args.length === 1) {
			rust.SendChatMessage(player, this.prefix, this.msgs.curTar, "0");
		} else {
			rust.SendChatMessage(player, this.prefix, this.msgs.invSyn.replace("{cmd}", "/bty target playername"), "0");
		}

		if (pName[0].displayName !== player.displayName && BountyData.PlayerData[pName[1]].Bounty !== "" && pName[0].IsConnected()) {
			BountyData.PlayerData[steamID].Target = pName[0].displayName;
			rust.SendChatMessage(player, this.prefix, this.msgs.setTar, "0");
			rust.SendChatMessage(pName[0], this.prefix, player.displayName + this.msgs.setTrgWarn, "0");
		} else if (!pName[0].IsConnected()) {
			rust.SendChatMessage(player, this.prefix, this.msgs.offline, "0");
		} else {
			rust.SendChatMessage(player, this.prefix, this.msgs.noBty, "0");
		}
	},

	addBounty: function(player, cmd, args) {
		try {
			var steamID = rust.UserIDFromPlayer(player);
			var authLvl = player.net.connection.authLevel;
			var main = player.inventory.containerMain;
			var mainList = main.itemList.GetEnumerator();
			var argObj = {
				"plyrName": args[1],
				"amt": Number(args[2]),
				"itemName": args[3]
			};

			var targetPlayer = this.findPlayerByName(player, args);
			if (!BountyData.PlayerData[targetPlayer[1]]) this.checkPlayerData(targetPlayer[0]);
			while (mainList.MoveNext()) {
				var name = mainList.Current.info.shortname,
					amount = mainList.Current.amount,
					condition = mainList.Current.condition;
				if (name === argObj.itemName && argObj.amt <= amount && argObj.amt <= this.Config.Settings.maxBounty && argObj.amt > 0) {
					break;
				}
			}

			if (argObj.amt > amount) {
				rust.SendChatMessage(player, this.prefix, this.msgs.notEnough.replace("{RssName}", argObj.itemName), "0");
				return false;
			} else if (argObj.amt <= 0) {
				rust.SendChatMessage(player, this.prefix, this.msgs.negBty, "0");
			} else if (argObj.amt > this.Config.Settings.maxBounty) {
				rust.SendChatMessage(player, this.prefix, this.msgs.overMax.replace("{maxBty}", this.Config.Settings.maxBounty), "0");
				return false;
			} else if (name !== argObj.itemName) {
				rust.SendChatMessage(player, this.prefix, this.msgs.notFound, "0");
				return false;
			}

			var definition = global.ItemManager.FindItemDefinition(name);
			main.Take(null, Number(definition.itemid), argObj.amt);
			if (this.checkForDupes(targetPlayer[1], argObj.itemName, argObj.amt)) {} else {
				BountyData.PlayerData[targetPlayer[1]].Bounty.push(argObj.amt + " " + argObj.itemName);
				BountyData.PlayerData[targetPlayer[1]].BountyType.push(argObj.itemName);
			}
			rust.SendChatMessage(targetPlayer[0], this.prefix, this.msgs.btyPlaced.replace("{bty}", argObj.amt + " " + argObj.itemName), "0");
			rust.SendChatMessage(player, this.prefix, this.msgs.btySet.replace("{bty}", argObj.amt + " " + argObj.itemName), "0");
			if (argObj.amt > amount) {
				rust.SendChatMessage(player, this.prefix, this.msgs.notEnough.replace("{RssName}", argObj.itemName), "0");
				return false;
			}
			this.saveData();
			this.updateBoard(targetPlayer[1], false, argObj.amt, argObj.itemName);
		} catch (e) {
			print(e.message.toString())
		}
	},

	//----------------------------------------
	//          Board Handling
	//----------------------------------------
	updateBoard: function(targetID, claimed, amount, itemName) {
		//TODO: update the bounty board with new bounties and claimed bounties.
		var getPlayer = this.findPlayerByID(targetID);

		if (claimed && itemName === null && amount === 0) {
			delete BountyData.Board[targetID];
			BountyData.PlayerData[targetID].Bounty = [];
			BountyData.PlayerData[targetID].BountyType = [];
			return this.saveData();
		}

		if (BountyData.Board[targetID] === undefined) {
			BountyData.Board[targetID] = {};
			BountyData.Board[targetID].Name = getPlayer.displayName;
			BountyData.Board[targetID].Amount = [amount + " " + itemName];
			BountyData.Board[targetID].ItemType = [itemName];
		} else if (claimed === false && BountyData.Board[targetID] !== undefined) {
			BountyData.Board[targetID].Amount = BountyData.PlayerData[targetID].Bounty;
			BountyData.Board[targetID].ItemType = BountyData.PlayerData[targetID].BountyType;
		}
		this.saveData();
	},

	checkForDupes: function(targetID, itemName, amt) {
		try {
			var boardData = BountyData.Board[targetID];
			var playerData = BountyData.PlayerData[targetID];
			var i = 0;
			if (boardData === undefined) {
				return false;
			}
			for (i; i < boardData.Amount.length; i++) {
				var itemTypeName = boardData.Amount[i].split(" ").pop();
				if (itemName === itemTypeName) {
					var storedAmt = boardData.Amount[i].split(" ").shift();
					var newAmt = Number(storedAmt) + Number(amt);
					boardData.Amount[i] = newAmt + " " + itemName;
					boardData.ItemType[i] = itemName;
					playerData.Bounty[i] = newAmt + " " + itemName;
					playerData.BountyType[i] = itemName;
					return true;
				}
			}
			return false;
		} catch (e) {
			print(e.message.toString());
		}
	},

	checkBoard: function(player, cmd, args) {
		rust.SendChatMessage(player, "", "<color=orange>------Bounty Board------</color>", "0");
		for (var key in BountyData.Board) {
			rust.SendChatMessage(player, "", "<color=red>" + BountyData.Board[key].Name + ": " + BountyData.Board[key].Amount + "</color>", "0");
		}
		rust.SendChatMessage(player, "", "<color=orange>------Happy Hunting------</color>", "0");
	},

	claimBounty: function(victimID, attackerID) {
		var amount = BountyData.Board[victimID].Amount,
			item = BountyData.Board[victimID].ItemType,
			claimed = false;
		var getPlayer = this.findPlayerByID(attackerID);
		var i = 0;
		for (i; i < amount.length; i++) {
			this.giveItem(getPlayer, item[i], amount[i].split(" ").shift());
		}
		claimed = true;
		this.updateBoard(victimID, claimed, 0, null);
	},

	giveItem: function(player, itemName, amount) {
		try {
			itemName = itemName.toLowerCase();
			var definition = global.ItemManager.FindItemDefinition(itemName);
			if (definition == null) return print("Unable to Find an Item for Bounty.");
			player.inventory.GiveItem(global.ItemManager.CreateByItemID(Number(definition.itemid), Number(amount), false), player.inventory.containerMain);
		} catch (e) {
			print(e.message.toString());
		}
	},

	OnEntityDeath: function(entity, hitinfo) {
		try {
			var victim = entity;
			var attacker = hitinfo.Initiator;

			if (victim.ToPlayer() && attacker.ToPlayer() && victim.displayName !== attacker.displayName) {
				var victimID = rust.UserIDFromPlayer(victim),
					attackerID = rust.UserIDFromPlayer(attacker);
				if (!BountyData.PlayerData[victimID] && !victim.IsConnected()) {
					return false;
				} else if (!BountyData.PlayerData[victimID] && victim.IsConnected()) {
					print("Data File not found for " + victim.displayName + ", attempting build now...");
					this.checkPlayerData(victim);
				} else if (!BountyData.PlayerData[attackerID]) {
					print("Data File not found for " + attacker.displayName + ", attempting build now...");
					this.checkPlayerData(attacker);
				}

				if (BountyData.PlayerData[attackerID].isStaff && !this.Config.Settings.staffCollect) {
					rust.SendChatMessage(attacker, this.prefix, this.msgs.staff, "0");
					return false;
				}
				if (BountyData.PlayerData[victimID].Bounty.length > 0 && victim.displayName !== attacker.displayName) {
					var rpObj = {
						plyrName: attacker.displayName,
						btyAmt: BountyData.PlayerData[victimID].Bounty,
						deadPlyr: victim.displayName
					}
					rust.BroadcastChat(this.prefix, this.msgs.btyClaim.replace(/plyrName|btyAmt|deadPlyr/g, function(matched) {
						return rpObj[matched]
					}), "0");
					this.claimBounty(victimID, attackerID);

				} else if (victim.ToPlayer() && victim.displayName === attacker.displayName) {
					return false;
				}
			}
		} catch (e) {
			print(e.message.toString())
		}
	},

	BtyHelp: function(player) {
		rust.SendChatMessage(player, null, "--------------BountyBoard Commands------------", "0");
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
	}
}
