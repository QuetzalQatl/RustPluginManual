// Reference: Oxide.Ext.Rust
// Reference: Newtonsoft.Json

/*
 * The MIT License (MIT)
 * Copyright (c) 2015 feramor@computer.org
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:

 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

//Microsoft NameSpaces
using System;
using System.Collections.Generic;

//Oxide NameSpaces
using Oxide.Core;
using Oxide.Core.Logging;
using Oxide.Core.Plugins;

//External NameSpaces
using Newtonsoft.Json;
using Oxide.Core.Libraries;

namespace Oxide.Plugins
{
    [Info("Polling Plugin", "Feramor", "1.0.11", ResourceId = 793)]
    public class Polling : RustPlugin
    {
        #region Other Classes
        public class PollingAnswerDataObject
        {
            public string AnswerText { set; get; }
            public int VoteCount { set; get; }
        }
        public class PollingMainDataObject
        {
            public int ID { set; get; }
            public string Type { set; get; }
            public int Timer { set; get; }
            public string Question { set; get; }
            public string Target { set; get; }
            public List<PollingAnswerDataObject> Answers { set; get; }
            public int isActive { set; get; }
            public List<string> UserList { set; get; }
        }
        public class myConfigObj
        {
            public bool Time_Enabled { set; get; }
            public int Time_Auth { set; get; }
            public bool Kick_Enabled { set; get; }
            public int Kick_Auth { set; get; }
            public bool Ban_Enabled { set; get; }
            public int Ban_Auth { set; get; }
            public bool Airdrop_Enabled { set; get; }
            public int Airdrop_Auth { set; get; }
            public bool Custom_Enabled { set; get; }
            public int Custom_Auth { set; get; }
            public myConfigObj()
            {
            }
        }
        #endregion

        List<PollingMainDataObject> History;
        PollingMainDataObject Current;
        myConfigObj myConfig;
        private static Logger logger = Interface.GetMod().RootLogger;
        public Core.Configuration.DynamicConfigFile mydata;
        Dictionary<string, object> Language = new Dictionary<string, object>();
        Dictionary<string, object> NewConfig = new Dictionary<string, object>();
        Oxide.Core.Libraries.Timer.TimerInstance CurrentPollTimer = null;
        [ChatCommand("poll")]
        private void cmdChatPoll(BasePlayer Player, string Command, string[] Args)
        {
            if (Args.Length > 0)
            {
                if (isCommand(Args[0]))
                {
                    switch (Args[0].ToUpper(new System.Globalization.CultureInfo("en-US")))
                    {
                        #region ? Command
                        case "?":
                            if (Current == null)
                                myPrintToChat(Player, Language["NoPoll"].ToString());
                            else if (Current.UserList.Contains(Player.userID.ToString()) == true)
                                myPrintToChat(Player, Language["AlreadyVoted"].ToString());
                            else
                            {
                                myPrintToChat(Player, Language["CurrentPoll"].ToString(), Current.Question);
                                int i = 1;
                                foreach (PollingAnswerDataObject CurrentAns in Current.Answers)
                                {
                                    myPrintToChat(Player, "{0}){1}", i++.ToString(), CurrentAns.AnswerText.ToString());
                                }
                                myPrintToChat(Player, Language["HowToVote"].ToString(), "1", Current.Answers.Count.ToString());
                            }
                            break;
                        #endregion
                        #region Clear Command
                        case "CLEAR":
                            if (Player.net.connection.authLevel != 2)
                            {
                                myPrintToChat(Player, Language["Permission"].ToString());
                            }
                            else
                            {
                                if (Current != null)
                                {
                                    myPrintToChat(Player, Language["AlreadyPoll"].ToString());
                                }
                                else
                                {
                                    History = new List<PollingMainDataObject>();
                                    SaveHistory(History);
                                    History = LoadHistory();
                                    Current = GetCurrentPoll(History);
                                }
                            }
                            break;
                        #endregion Clear Command
                        #region Help Command
                        case "HELP":
                            if (Player.net.connection.authLevel != 2)
                            {
                                myPrintToChat(Player, Language["Help0"].ToString());
                                myPrintToChat(Player, Language["Help1"].ToString());
                                myPrintToChat(Player, Language["Help2"].ToString());
                                myPrintToChat(Player, Language["Help3"].ToString());
                            }
                            else
                            {
                                myPrintToChat(Player, Language["Help0"].ToString());
                                myPrintToChat(Player, Language["Help1"].ToString());
                                myPrintToChat(Player, Language["Help2"].ToString());
                                myPrintToChat(Player, Language["Help3"].ToString());
                                myPrintToChat(Player, Language["Help4"].ToString());
                                myPrintToChat(Player, Language["Help5"].ToString());
                                myPrintToChat(Player, Language["Help6"].ToString());
                                myPrintToChat(Player, Language["Help7"].ToString());
                                myPrintToChat(Player, Language["Help8"].ToString());
                                myPrintToChat(Player, Language["Help9"].ToString());
                            }
                            break;
                        #endregion
                        #region Time Command
                        case "TIME":
                            if (myConfig.Time_Enabled == false)
                            {
                                myPrintToChat(Player, Language["DisabledPoll"].ToString());
                            }
                            else if (Player.net.connection.authLevel >= myConfig.Time_Auth)
                            {
                                if (Args.Length == 3)
                                {
                                    if (IsNumeric(Args[1]))
                                    {
                                        int Timer = Convert.ToInt32(Args[1]);
                                        string DayTime = Args[2].ToUpper(new System.Globalization.CultureInfo("en-US"));

                                        if (DayTime.Equals("DAY") || DayTime.Equals("NIGHT"))
                                        {
                                            if (Current == null)
                                            {
                                                Current = new PollingMainDataObject();
                                                Current.Answers = new List<PollingAnswerDataObject>();
                                                Current.ID = History.Count + 1;
                                                Current.Timer = Convert.ToInt32((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds) + Timer;
                                                Current.Question = string.Format(Language["QestionTime"].ToString(), DayTime);
                                                Current.Target = DayTime;
                                                Current.Type = Args[0].ToUpper(new System.Globalization.CultureInfo("en-US"));
                                                Current.UserList = new List<string>();
                                                PollingAnswerDataObject newAnswer = new PollingAnswerDataObject();
                                                newAnswer.AnswerText = Language["Yes"].ToString();
                                                newAnswer.VoteCount = 0;
                                                Current.Answers.Add(newAnswer);
                                                newAnswer = new PollingAnswerDataObject();
                                                newAnswer.AnswerText = Language["No"].ToString();
                                                newAnswer.VoteCount = 0;
                                                Current.Answers.Add(newAnswer);
                                                //Current.Answers.Sort(delegate(PollingAnswerDataObject p1, PollingAnswerDataObject p2) { return p1.AnswerText.CompareTo(p2.AnswerText); });
                                                Current.isActive = 1;
                                                History.Add(Current);
                                                SaveHistory(History);
                                                foreach (var ActiveUser in BasePlayer.activePlayerList)
                                                {
                                                    myPrintToChat(ActiveUser, Language["PollStarted"].ToString(), Current.Question);
                                                    int i = 1;
                                                    foreach (PollingAnswerDataObject CurrentAns in Current.Answers)
                                                    {
                                                        myPrintToChat(ActiveUser, "{0}){1}", i++.ToString(), CurrentAns.AnswerText.ToString());
                                                    }
                                                    myPrintToChat(ActiveUser, Language["HowToVote"].ToString(), "1", Current.Answers.Count.ToString());
                                                }
                                            }
                                            else
                                            {
                                                myPrintToChat(Player, Language["AlreadyPoll"].ToString());
                                            }
                                        }
                                        else
                                        {
                                            myPrintToChat(Player, Language["WrongPoll"].ToString());
                                        }
                                    }
                                    else
                                    {
                                        myPrintToChat(Player, Language["WrongPoll"].ToString());
                                    }
                                }
                                else
                                {
                                    myPrintToChat(Player, Language["WrongPoll"].ToString());
                                }
                            }
                            else
                            {
                                myPrintToChat(Player, Language["Permission"].ToString());
                            }
                            break;
                        #endregion
                        #region Kick Command
                        case "KICK":
                            if (myConfig.Kick_Enabled == false)
                            {
                                myPrintToChat(Player, Language["DisabledPoll"].ToString());
                            }
                            else if (Player.net.connection.authLevel >= myConfig.Kick_Auth)
                            {
                                if (Args.Length == 3)
                                {
                                    if (IsNumeric(Args[1]))
                                    {
                                        int Timer = Convert.ToInt32(Args[1]);
                                        BasePlayer SelectedUser = null;

                                        foreach (var ActiveUser in BasePlayer.activePlayerList)
                                        {
                                            if (ActiveUser.displayName == Args[2] || ActiveUser.userID.ToString() == Args[2])
                                                SelectedUser = ActiveUser;
                                        }

                                        if (SelectedUser != null)
                                        {
                                            if (Current == null)
                                            {
                                                Current = new PollingMainDataObject();
                                                Current.Answers = new List<PollingAnswerDataObject>();
                                                Current.ID = History.Count + 1;
                                                Current.Timer = Convert.ToInt32((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds) + Timer;
                                                Current.Question = string.Format(Language["QuestionKick"].ToString(), SelectedUser.displayName.ToString());
                                                Current.Target = SelectedUser.userID.ToString();
                                                Current.Type = Args[0].ToUpper(new System.Globalization.CultureInfo("en-US"));
                                                Current.UserList = new List<string>();
                                                PollingAnswerDataObject newAnswer = new PollingAnswerDataObject();
                                                newAnswer.AnswerText = Language["Yes"].ToString();
                                                newAnswer.VoteCount = 0;
                                                Current.Answers.Add(newAnswer);
                                                newAnswer = new PollingAnswerDataObject();
                                                newAnswer.AnswerText = Language["No"].ToString();
                                                newAnswer.VoteCount = 0;
                                                Current.Answers.Add(newAnswer);
                                                //Current.Answers.Sort(delegate(PollingAnswerDataObject p1, PollingAnswerDataObject p2) { return p1.AnswerText.CompareTo(p2.AnswerText); });
                                                Current.isActive = 1;
                                                History.Add(Current);
                                                SaveHistory(History);
                                                foreach (var ActiveUser in BasePlayer.activePlayerList)
                                                {
                                                    myPrintToChat(ActiveUser, Language["PollStarted"].ToString(), Current.Question);
                                                    int i = 1;
                                                    foreach (PollingAnswerDataObject CurrentAns in Current.Answers)
                                                    {
                                                        myPrintToChat(ActiveUser, "{0}){1}", i++.ToString(), CurrentAns.AnswerText.ToString());
                                                    }
                                                    myPrintToChat(ActiveUser, Language["HowToVote"].ToString(), "1", Current.Answers.Count.ToString());
                                                }
                                            }
                                            else
                                            {
                                                myPrintToChat(Player, Language["AlreadyPoll"].ToString());
                                            }
                                        }
                                        else
                                        {
                                            myPrintToChat(Player, Language["NoUser"].ToString(), Args[2]);
                                        }
                                    }
                                    else
                                    {
                                        myPrintToChat(Player, Language["WrongPoll"].ToString());
                                    }
                                }
                                else
                                {
                                    myPrintToChat(Player, Language["WrongPoll"].ToString());
                                }
                            }
                            else
                            {
                                myPrintToChat(Player, Language["Permission"].ToString());
                            }
                            break;
                        #endregion
                        #region Ban Command
                        case "BAN":
                            if (myConfig.Ban_Enabled == false)
                            {
                                myPrintToChat(Player, Language["DisabledPoll"].ToString());
                            }
                            else if (Player.net.connection.authLevel >= myConfig.Ban_Auth)
                            {
                                if (Args.Length == 3)
                                {
                                    if (IsNumeric(Args[1]))
                                    {
                                        int Timer = Convert.ToInt32(Args[1]);
                                        BasePlayer SelectedUser = null;

                                        foreach (var ActiveUser in BasePlayer.activePlayerList)
                                        {
                                            if (ActiveUser.displayName == Args[2] || ActiveUser.userID.ToString() == Args[2])
                                                SelectedUser = ActiveUser;
                                        }

                                        if (SelectedUser != null)
                                        {
                                            if (Current == null)
                                            {
                                                Current = new PollingMainDataObject();
                                                Current.Answers = new List<PollingAnswerDataObject>();
                                                Current.ID = History.Count + 1;
                                                Current.Timer = Convert.ToInt32((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds) + Timer;
                                                Current.Question = string.Format("Do you want to ban {0} ?", SelectedUser.displayName.ToString());
                                                Current.Target = SelectedUser.userID.ToString();
                                                Current.Type = Args[0].ToUpper(new System.Globalization.CultureInfo("en-US"));
                                                Current.UserList = new List<string>();
                                                PollingAnswerDataObject newAnswer = new PollingAnswerDataObject();
                                                newAnswer.AnswerText = Language["Yes"].ToString();
                                                newAnswer.VoteCount = 0;
                                                Current.Answers.Add(newAnswer);
                                                newAnswer = new PollingAnswerDataObject();
                                                newAnswer.AnswerText = Language["No"].ToString();
                                                newAnswer.VoteCount = 0;
                                                Current.Answers.Add(newAnswer);
                                                //Current.Answers.Sort(delegate(PollingAnswerDataObject p1, PollingAnswerDataObject p2) { return p1.AnswerText.CompareTo(p2.AnswerText); });
                                                Current.isActive = 1;
                                                History.Add(Current);
                                                SaveHistory(History);
                                                foreach (var ActiveUser in BasePlayer.activePlayerList)
                                                {
                                                    myPrintToChat(ActiveUser, Language["PollStarted"].ToString(), Current.Question);
                                                    int i = 1;
                                                    foreach (PollingAnswerDataObject CurrentAns in Current.Answers)
                                                    {
                                                        myPrintToChat(ActiveUser, "{0}){1}", i++.ToString(), CurrentAns.AnswerText.ToString());
                                                    }
                                                    myPrintToChat(ActiveUser, Language["HowToVote"].ToString(), "1", Current.Answers.Count.ToString());
                                                }
                                            }
                                            else
                                            {
                                                myPrintToChat(Player, Language["AlreadyPoll"].ToString());
                                            }
                                        }
                                        else
                                        {
                                            myPrintToChat(Player, Language["NoUser"].ToString(), Args[2]);
                                        }
                                    }
                                    else
                                    {
                                        myPrintToChat(Player, Language["WrongPoll"].ToString());
                                    }
                                }
                                else
                                {
                                    myPrintToChat(Player, Language["WrongPoll"].ToString());
                                }
                            }
                            else
                            {
                                myPrintToChat(Player, Language["Permission"].ToString());
                            }
                            break;
                        #endregion
                        #region AIRDROP Command
                        case "AIRDROP":
                            if (myConfig.Airdrop_Enabled == false)
                            {
                                myPrintToChat(Player, Language["DisabledPoll"].ToString());
                            }
                            else if (Player.net.connection.authLevel >= myConfig.Airdrop_Auth)
                            {
                                if (Args.Length == 2)
                                {
                                    if (IsNumeric(Args[1]))
                                    {
                                        int Timer = Convert.ToInt32(Args[1]);
                                        if (Current == null)
                                        {
                                            Current = new PollingMainDataObject();
                                            Current.Answers = new List<PollingAnswerDataObject>();
                                            Current.ID = History.Count + 1;
                                            Current.Timer = Convert.ToInt32((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds) + Timer;
                                            Current.Question = Language["QuestionAirdrop"].ToString();
                                            Current.Target = "-";
                                            Current.Type = Args[0].ToUpper(new System.Globalization.CultureInfo("en-US"));
                                            Current.UserList = new List<string>();
                                            PollingAnswerDataObject newAnswer = new PollingAnswerDataObject();
                                            newAnswer.AnswerText = Language["Yes"].ToString();
                                            newAnswer.VoteCount = 0;
                                            Current.Answers.Add(newAnswer);
                                            newAnswer = new PollingAnswerDataObject();
                                            newAnswer.AnswerText = Language["No"].ToString();
                                            newAnswer.VoteCount = 0;
                                            Current.Answers.Add(newAnswer);
                                            //Current.Answers.Sort(delegate(PollingAnswerDataObject p1, PollingAnswerDataObject p2) { return p1.AnswerText.CompareTo(p2.AnswerText); });
                                            Current.isActive = 1;
                                            History.Add(Current);
                                            SaveHistory(History);
                                            foreach (var ActiveUser in BasePlayer.activePlayerList)
                                            {
                                                myPrintToChat(ActiveUser, Language["PollStarted"].ToString(), Current.Question);
                                                int i = 1;
                                                foreach (PollingAnswerDataObject CurrentAns in Current.Answers)
                                                {
                                                    myPrintToChat(ActiveUser, "{0}){1}", i++.ToString(), CurrentAns.AnswerText.ToString());
                                                }
                                                myPrintToChat(ActiveUser, Language["HowToVote"].ToString(), "1", Current.Answers.Count.ToString());
                                            }
                                        }
                                        else
                                        {
                                            myPrintToChat(Player, Language["AlreadyPoll"].ToString());
                                        }
                                    }
                                    else
                                    {
                                        myPrintToChat(Player, Language["WrongPoll"].ToString());
                                    }
                                }
                                else
                                {
                                    myPrintToChat(Player, Language["WrongPoll"].ToString());
                                }
                            }
                            else
                            {
                                myPrintToChat(Player, Language["Permission"].ToString());
                            }
                            break;
                        #endregion
                        #region CUSTOM Command
                        case "CUSTOM":
                            if (myConfig.Custom_Enabled == false)
                            {
                                myPrintToChat(Player, Language["DisabledPoll"].ToString());
                            }
                            else if (Player.net.connection.authLevel >= myConfig.Custom_Auth)
                            {
                                if (Args.Length >= 5)
                                {
                                    if (IsNumeric(Args[1]))
                                    {
                                        int Timer = Convert.ToInt32(Args[1]);
                                        if (Current == null)
                                        {
                                            Current = new PollingMainDataObject();
                                            Current.Answers = new List<PollingAnswerDataObject>();
                                            Current.ID = History.Count + 1;
                                            Current.Timer = Convert.ToInt32((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds) + Timer;
                                            Current.Question = Args[2];
                                            Current.Target = "-";
                                            Current.Type = Args[0].ToUpper(new System.Globalization.CultureInfo("en-US"));
                                            Current.UserList = new List<string>();

                                            for (int i = 4; i <= Args.Length; i++)
                                            {
                                                PollingAnswerDataObject newAnswer = new PollingAnswerDataObject();
                                                newAnswer.AnswerText = Args[i - 1];
                                                newAnswer.VoteCount = 0;
                                                Current.Answers.Add(newAnswer);
                                            }

                                            //Current.Answers.Sort(delegate(PollingAnswerDataObject p1, PollingAnswerDataObject p2) { return p1.AnswerText.CompareTo(p2.AnswerText); });
                                            Current.isActive = 1;
                                            foreach (var ActiveUser in BasePlayer.activePlayerList)
                                            {
                                                myPrintToChat(ActiveUser, Language["PollStarted"].ToString(), Current.Question);
                                                int i = 1;
                                                foreach (PollingAnswerDataObject CurrentAns in Current.Answers)
                                                {
                                                    myPrintToChat(ActiveUser, "{0}){1}", i++.ToString(), CurrentAns.AnswerText.ToString());
                                                }
                                                myPrintToChat(ActiveUser, Language["HowToVote"].ToString(), "1", Current.Answers.Count.ToString());
                                            }
                                        }
                                        else
                                        {
                                            myPrintToChat(Player, Language["AlreadyPoll"].ToString());
                                        }
                                    }
                                    else
                                    {
                                        myPrintToChat(Player, Language["WrongPoll"].ToString());
                                    }
                                }
                                else
                                {
                                    myPrintToChat(Player, Language["WrongPoll"].ToString());
                                }
                            }
                            else
                            {
                                myPrintToChat(Player, Language["Permission"].ToString());
                            }
                            break;
                        #endregion
                        #region HISTORY Command
                        case "HISTORY":
                            if (Args.Length > 1)
                            {
                                if (IsNumeric(Args[1]))
                                {
                                    foreach (PollingMainDataObject Selected in History)
                                    {
                                        if (Selected.ID.ToString() == Args[1])
                                        {
                                            myPrintToChat(Player, Language["VoteResults"].ToString(), Selected.Question);
                                            foreach (PollingAnswerDataObject CurrentAnswer in Selected.Answers)
                                            {
                                                double percent = 0.0;
                                                if (Selected.UserList.Count > 0)
                                                {
                                                    percent = (CurrentAnswer.VoteCount * 100.0) / Selected.UserList.Count;
                                                }
                                                myPrintToChat(Player, "{0}  :  {1}({2}%)", CurrentAnswer.AnswerText, CurrentAnswer.VoteCount, percent.ToString("00.00"));
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                List<PollingMainDataObject> rHistory = new List<PollingMainDataObject>(History);
                                rHistory.Reverse();
                                myPrintToChat(Player, "Poll History : Last 5 Poll (Total Poll {0})", rHistory.Count);

                                int i = 0;
                                foreach (PollingMainDataObject Selected in rHistory)
                                {
                                    if (i < 5)
                                    {
                                        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                                        epoch = epoch.AddSeconds(rHistory[i].Timer);
                                        myPrintToChat(Player, string.Format("{0}){1} - {2}", rHistory[i].ID.ToString(), rHistory[i].Question.ToString(), epoch.ToLocalTime().ToString("d/M/yyyy HH:mm:ss")));
                                        i++;
                                    }
                                }
                                rHistory.Clear();
                                GC.Collect();
                            }
                            break;
                        #endregion
                        default:
                            myPrintToChat(Player, Language["WrongPoll"].ToString());
                            break;
                    }
                }
                else
                {
                    #region User Polling
                    if (IsNumeric(Args[0]))
                    {
                        if (Current == null)
                            myPrintToChat(Player, Language["NoPoll"].ToString());
                        else
                        {
                            if (Current.UserList.Contains(Player.userID.ToString()))
                            {
                                myPrintToChat(Player, Language["AlreadyVoted"].ToString());
                            }
                            else
                            {
                                int choice = Convert.ToInt32(Args[0]);
                                if (Current.Answers.Count >= choice && choice > 0)
                                {
                                    Current.Answers[choice - 1].VoteCount += 1;
                                    Current.UserList.Add(Player.userID.ToString());
                                    myPrintToChat(Player, Language["Voted"].ToString(), Current.Answers[choice - 1].AnswerText);
                                    SaveHistory(History);
                                }
                                else
                                {
                                    myPrintToChat(Player, "Current Poll:  {0}", Current.Question);
                                    int i = 1;
                                    foreach (PollingAnswerDataObject CurrentAns in Current.Answers)
                                    {
                                        myPrintToChat(Player, "{0}){1}", i++.ToString(), CurrentAns.AnswerText.ToString());
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        myPrintToChat(Player, Language["WrongPoll"].ToString());
                    }
                    #endregion
                }
            }
            else
            {
                myPrintToChat(Player, Language["WrongPoll"].ToString());
            }
        }
        void Init()
        {
            LoadConfig();
            History = LoadHistory();
            Current = GetCurrentPoll(History);
            myConfig = JsonConvert.DeserializeObject<myConfigObj>((string)JsonConvert.SerializeObject(Config["Config"]).ToString());
            logger.Write(Oxide.Core.Logging.LogType.Info, "Polling : Loaded history file.Currently there are {0} polls in the history.", History.Count.ToString());
            if (Config["Language"] != null)
            {
                Language = (Dictionary<string, object>)Config["Language"];

            }
            if (Config["NewConfig"] != null)
            {
                NewConfig = (Dictionary<string, object>)Config["NewConfig"];

            }
            LoadDefaultLang();
        }
        void LoadDefaultLang()
        {
            //V 1.0.5
            AddLanguage("VoteFailed", "Vote Failed :  {0}");
            AddLanguage("VotePassed", "Vote Passed :  {0}");
            AddLanguage("VoteResults", "Vote Results :  {0}");
            AddLanguage("HaventVoted", "You haven't voted for : {0}");
            AddLanguage("HowTo", "You can use /poll ? to see current poll.");
            AddLanguage("NoPoll", "There is no active poll right now!!!");
            AddLanguage("AlreadyVoted", "You have already voted for this poll!!!");
            AddLanguage("CurrentPoll", "Current Poll :  {0}");
            AddLanguage("DisabledPoll", "This type of poll disabled by Server Owner.");
            AddLanguage("PollStarted", "Poll Started :  {0}");
            AddLanguage("AlreadyPoll", "There is already a poll running.");
            AddLanguage("WrongPoll", "Use /Poll Help to learn how to use Polling System.");
            AddLanguage("Permission", "You don't have permission to use this command!.");
            AddLanguage("NoUser", "There is no user found with name {0}");
            AddLanguage("Voted", "You voted {0} for this poll.");
            AddLanguage("Yes", "Yes");
            AddLanguage("No", "No");
            //V 1.0.6
            AddLanguage("QestionTime", "Do you want to change time to {0} ?");
            AddLanguage("QuestionKick", "Do you want to kick {0} ?");
            AddLanguage("QuestionBan", "Do you want to ban {0} ?");
            AddLanguage("QuestionAirdrop", "Do you want an Airdrop?");
            AddLanguage("HowToVote", "You can use /Poll {0}-{1} to vote");

            AddLanguage("KickChat", "{0} has been kicked by poll results.");
            AddLanguage("KickConsole", "You have been kicked by poll results.");
            AddLanguage("BanChat", "{0} has been banned by poll results.");
            AddLanguage("BanConsole", "You have been banned by poll results.");

            //Help Texts
            AddLanguage("Help0", "*** Avaible user commands ***");
            AddLanguage("Help1", "/Poll Help - For this menu");
            AddLanguage("Help2", "/Poll History [ID] - to see previous polls");
            AddLanguage("Help3", "/Poll [Choice] - to vote for active poll");
            AddLanguage("Help4", "*** Avaible admin commands ***");
            AddLanguage("Help5", "/Poll \"Kick\" \"TIMER\" \"Username\" - For vote to kick ppl");
            AddLanguage("Help6", "/Poll \"Ban\" \"TIMER\" \"Username\" - For vote to ban ppl");
            AddLanguage("Help7", "/Poll \"Airdrop\" \"TIMER\"- For vote to start airdrop");
            AddLanguage("Help8", "/Poll \"Time\" \"TIMER\" \"Day/Night\" - For vote to change server time");
            AddLanguage("Help9", "/Poll \"Custom\" \"TIMER\" \"QUESTION\" \"ANSWER1\" \"ANSWER2\" \"ANSWER3\" ..... - For vote to custom question");

            //Tag
            AddLanguage("ChatTag", "Polling");

            //New Config
            AddConfig("EventCmd", "event.run");
            AddConfig("SkipNight", false);
            AddConfig("SkipTime", "20:00");

            Config["Language"] = Language;
            Config["NewConfig"] = NewConfig;
            SaveConfig();
        }
        void AddLanguage(string Key, string Value)
        {
            try
            {
                Language.Add(Key, Value);
            }
            catch { }
        }
        void AddConfig(string Key, object Value)
        {
            try
            {
                NewConfig.Add(Key, Value);
            }
            catch { }
        }
        public void EndVote()
        {
            switch (Current.Type)
            {
                #region TIME Command
                case "TIME":
                    Current.isActive = 0;
                    if (Current.Answers[0].VoteCount <= Current.Answers[1].VoteCount)
                    {
                        foreach (var ActiveUser in BasePlayer.activePlayerList)
                        {
                            myPrintToChat(ActiveUser, Language["VoteFailed"].ToString(), Current.Question);
                            foreach (PollingAnswerDataObject CurrentAnswer in Current.Answers)
                            {
                                double percent = 0.0;
                                if (Current.UserList.Count > 0)
                                {
                                    percent = (CurrentAnswer.VoteCount * 100.0) / Current.UserList.Count;
                                }
                                myPrintToChat(ActiveUser, "{0}  :  {1}({2}%)", CurrentAnswer.AnswerText, CurrentAnswer.VoteCount, percent.ToString("00.00"));
                            }
                        }
                    }
                    else
                    {
                        foreach (var ActiveUser in BasePlayer.activePlayerList)
                        {
                            myPrintToChat(ActiveUser, Language["VotePassed"].ToString(), Current.Question);
                            foreach (PollingAnswerDataObject CurrentAnswer in Current.Answers)
                            {
                                double percent = 0.0;
                                if (Current.UserList.Count > 0)
                                {
                                    percent = (CurrentAnswer.VoteCount * 100.0) / Current.UserList.Count;
                                }
                                myPrintToChat(ActiveUser, "{0}  :  {1}({2}%)", CurrentAnswer.AnswerText, CurrentAnswer.VoteCount, percent.ToString("00.00"));
                            }
                        }
                        List<TOD_Sky> Skies = TOD_Sky.Instances;
                        foreach (TOD_Sky Sky in Skies)
                        {
                        if (Current.Target == "DAY")
                            Sky.Cycle.Hour = 10;
                        else
                            Sky.Cycle.Hour = 22;
                        }
                    }
                    break;
                #endregion
                #region KICK Command
                case "KICK":
                    Current.isActive = 0;
                    if (Current.Answers[0].VoteCount <= Current.Answers[1].VoteCount)
                    {
                        foreach (var ActiveUser in BasePlayer.activePlayerList)
                        {
                            myPrintToChat(ActiveUser, Language["VoteFailed"].ToString(), Current.Question);
                            foreach (PollingAnswerDataObject CurrentAnswer in Current.Answers)
                            {
                                double percent = 0.0;
                                if (Current.UserList.Count > 0)
                                {
                                    percent = (CurrentAnswer.VoteCount * 100.0) / Current.UserList.Count;
                                }
                                myPrintToChat(ActiveUser, "{0}  :  {1}({2}%)", CurrentAnswer.AnswerText, CurrentAnswer.VoteCount, percent.ToString("00.00"));
                            }
                        }
                    }
                    else
                    {
                        foreach (var ActiveUser in BasePlayer.activePlayerList)
                        {
                            myPrintToChat(ActiveUser, Language["VotePassed"].ToString(), Current.Question);
                            foreach (PollingAnswerDataObject CurrentAnswer in Current.Answers)
                            {
                                double percent = 0.0;
                                if (Current.UserList.Count > 0)
                                {
                                    percent = (CurrentAnswer.VoteCount * 100.0) / Current.UserList.Count;
                                }
                                myPrintToChat(ActiveUser, "{0}  :  {1}({2}%)", CurrentAnswer.AnswerText, CurrentAnswer.VoteCount, percent.ToString("00.00"));
                            }
                            myPrintToChat(ActiveUser, Language["KickChat"].ToString(), BasePlayer.FindByID(Convert.ToUInt64(Current.Target)).displayName.ToString());
                        }
                        foreach (var ActiveUser in BasePlayer.activePlayerList)
                        {
                            if (ActiveUser.userID.ToString() == Current.Target)
                                Network.Net.sv.Kick(ActiveUser.net.connection, Language["KickConsole"].ToString());
                        }
                    }
                    break;
                #endregion
                #region BAN Command
                case "BAN":
                    Current.isActive = 0;
                    if (Current.Answers[0].VoteCount <= Current.Answers[1].VoteCount)
                    {
                        foreach (var ActiveUser in BasePlayer.activePlayerList)
                        {
                            myPrintToChat(ActiveUser, Language["VoteFailed"].ToString(), Current.Question);
                            foreach (PollingAnswerDataObject CurrentAnswer in Current.Answers)
                            {
                                double percent = 0.0;
                                if (Current.UserList.Count > 0)
                                {
                                    percent = (CurrentAnswer.VoteCount * 100.0) / Current.UserList.Count;
                                }
                                myPrintToChat(ActiveUser, "{0}  :  {1}({2}%)", CurrentAnswer.AnswerText, CurrentAnswer.VoteCount, percent.ToString("00.00"));
                            }
                        }
                    }
                    else
                    {
                        foreach (var ActiveUser in BasePlayer.activePlayerList)
                        {
                            myPrintToChat(ActiveUser, Language["VotePassed"].ToString(), Current.Question);
                            foreach (PollingAnswerDataObject CurrentAnswer in Current.Answers)
                            {
                                double percent = 0.0;
                                if (Current.UserList.Count > 0)
                                {
                                    percent = (CurrentAnswer.VoteCount * 100.0) / Current.UserList.Count;
                                }
                                myPrintToChat(ActiveUser, "{0}  :  {1}({2}%)", CurrentAnswer.AnswerText, CurrentAnswer.VoteCount, percent.ToString("00.00"));
                            }
                            myPrintToChat(ActiveUser, Language["BanChat"].ToString(), BasePlayer.FindByID(Convert.ToUInt64(Current.Target)).displayName.ToString());
                        }
                        foreach (var ActiveUser in BasePlayer.activePlayerList)
                        {
                            if (ActiveUser.userID.ToString() == Current.Target)
                            {
                                ConsoleSystem.Run.Server.Quiet(string.Format("banid {0} \"{1}\" \"{2}\"", ActiveUser.userID.ToString(), ActiveUser.displayName, "User have been banned by poll results.").ToString(), null);
                                ConsoleSystem.Run.Server.Quiet("server.writecfg", null);
                                Network.Net.sv.Kick(ActiveUser.net.connection, Language["BanConsole"].ToString());

                            }
                        }
                    }
                    break;
                #endregion
                #region AIRDROP Command
                case "AIRDROP":
                    Current.isActive = 0;
                    if (Current.Answers[0].VoteCount <= Current.Answers[1].VoteCount)
                    {
                        foreach (var ActiveUser in BasePlayer.activePlayerList)
                        {
                            myPrintToChat(ActiveUser, Language["VoteFailed"].ToString(), Current.Question);
                            foreach (PollingAnswerDataObject CurrentAnswer in Current.Answers)
                            {
                                double percent = 0.0;
                                if (Current.UserList.Count > 0)
                                {
                                    percent = (CurrentAnswer.VoteCount * 100.0) / Current.UserList.Count;
                                }
                                myPrintToChat(ActiveUser, "{0}  :  {1}({2}%)", CurrentAnswer.AnswerText, CurrentAnswer.VoteCount, percent.ToString("00.00"));
                            }
                        }
                    }
                    else
                    {
                        foreach (var ActiveUser in BasePlayer.activePlayerList)
                        {
                            myPrintToChat(ActiveUser, Language["VotePassed"].ToString(), Current.Question);
                            foreach (PollingAnswerDataObject CurrentAnswer in Current.Answers)
                            {
                                double percent = 0.0;
                                if (Current.UserList.Count > 0)
                                {
                                    percent = (CurrentAnswer.VoteCount * 100.0) / Current.UserList.Count;
                                }
                                myPrintToChat(ActiveUser, "{0}  :  {1}({2}%)", CurrentAnswer.AnswerText, CurrentAnswer.VoteCount, percent.ToString("00.00"));
                            }
                        }
                        ConsoleSystem.Run.Server.Quiet(NewConfig["EventCmd"].ToString(), null);
                    }
                    break;
                #endregion
                #region CUSTOM Command
                case "CUSTOM":
                    Current.isActive = 0;
                    foreach (var ActiveUser in BasePlayer.activePlayerList)
                    {
                        myPrintToChat(ActiveUser, Language["VoteResults"].ToString(), Current.Question);
                        foreach (PollingAnswerDataObject CurrentAnswer in Current.Answers)
                        {
                            double percent = 0.0;
                            if (Current.UserList.Count > 0)
                            {
                                percent = (CurrentAnswer.VoteCount * 100.0) / Current.UserList.Count;
                            }
                            myPrintToChat(ActiveUser, "{0}  :  {1}({2}%)", CurrentAnswer.AnswerText, CurrentAnswer.VoteCount, percent.ToString("00.00"));
                        }

                    }
                    break;
                #endregion
            }
            SaveHistory(History);
            Current = null;
            CurrentPollTimer = null;
        }
        [HookMethod("OnTick")]
        void OnTick()
        {
            if (Current != null)
            {
                if (CurrentPollTimer == null)
                {
                    if (Current.isActive == 1)
                    {
                        if (Current.Timer != -1)
                        {
                            long CurrentTimer = Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
                            float Diff = Convert.ToSingle(Current.Timer - CurrentTimer);
                            if (Diff <= 0)
                                Diff = 1;
                            CurrentPollTimer = Interface.GetMod().GetLibrary<Oxide.Core.Libraries.Timer>("Timer").Once(Diff, () => EndVote(), this);

                            logger.Write(LogType.Info, "Polling : Poll started {0} for {1}", Current.Question,Diff);
                        }
                    }
                }
            }
            else
            {
                if ((bool)NewConfig["SkipNight"] == true)
                {
                    TOD_Sky CurrentTime = TOD_Sky.Instance;
                    if (CurrentTime.Cycle.DateTime.ToString("HH:mm") == ((string)NewConfig["SkipTime"].ToString()))
                    {
                        if (Current == null)
                        {
                            logger.Write(LogType.Warning, "Auto skip poll running");
                            Current = new PollingMainDataObject();
                            Current.Answers = new List<PollingAnswerDataObject>();
                            Current.ID = History.Count + 1;
                            Current.Timer = Convert.ToInt32((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds) + 60;
                            Current.Question = string.Format(Language["QestionTime"].ToString(), "DAY");
                            Current.Target = "DAY";
                            Current.Type = "TIME";
                            Current.UserList = new List<string>();
                            PollingAnswerDataObject newAnswer = new PollingAnswerDataObject();
                            newAnswer.AnswerText = Language["Yes"].ToString();
                            newAnswer.VoteCount = 0;
                            Current.Answers.Add(newAnswer);
                            newAnswer = new PollingAnswerDataObject();
                            newAnswer.AnswerText = Language["No"].ToString();
                            newAnswer.VoteCount = 0;
                            Current.Answers.Add(newAnswer);
                            //Current.Answers.Sort(delegate(PollingAnswerDataObject p1, PollingAnswerDataObject p2) { return p1.AnswerText.CompareTo(p2.AnswerText); });
                            Current.isActive = 1;
                            History.Add(Current);
                            SaveHistory(History);
                            foreach (var ActiveUser in BasePlayer.activePlayerList)
                            {
                                myPrintToChat(ActiveUser, Language["PollStarted"].ToString(), Current.Question);
                                int i = 1;
                                foreach (PollingAnswerDataObject CurrentAns in Current.Answers)
                                {
                                    myPrintToChat(ActiveUser, "{0}){1}", i++.ToString(), CurrentAns.AnswerText.ToString());
                                }
                                myPrintToChat(ActiveUser, Language["HowToVote"].ToString(), "1", Current.Answers.Count.ToString());
                            }
                        }
                    }
                }

            }
        }
        [HookMethod("LoadDefaultConfig")]
        void myLoadDefaultConfig()
        {
            myConfigObj myNewConfig = new myConfigObj();
            myNewConfig.Time_Enabled = true;
            myNewConfig.Time_Auth = 2;

            myNewConfig.Kick_Enabled = true;
            myNewConfig.Kick_Auth = 2;

            myNewConfig.Ban_Enabled = true;
            myNewConfig.Ban_Auth = 2;

            myNewConfig.Airdrop_Enabled = true;
            myNewConfig.Airdrop_Auth = 2;

            myNewConfig.Custom_Enabled = true;
            myNewConfig.Custom_Auth = 2;

            Config["Config"] = myNewConfig;
            myConfig = myNewConfig;

            logger.Write(Oxide.Core.Logging.LogType.Info, "Polling : Default Config loaded.");
        }
        void OnPlayerInit(BasePlayer new_player)
        {
            if (Current != null)
            {
                if (Current.UserList.Contains(new_player.userID.ToString()) == false)
                {
                    myPrintToChat(new_player, Language["HaventVoted"].ToString(), Current.Question);
                    myPrintToChat(new_player, Language["HowTo"].ToString());
                }
            }
        }
        public void SaveHistory(List<PollingMainDataObject> Context)
        {
            Interface.GetMod().DataFileSystem.WriteObject<List<PollingMainDataObject>>("Polling", Context);
        }
        public List<PollingMainDataObject> LoadHistory()
        {
            List<PollingMainDataObject> outObject = null;
            try
            {
                outObject = Interface.GetMod().DataFileSystem.ReadObject<List<PollingMainDataObject>>("Polling");
            }
            catch
            {
                outObject = new List<PollingMainDataObject>();
            }
            return outObject;
        }
        public PollingMainDataObject GetCurrentPoll(List<PollingMainDataObject> History)
        {
            PollingMainDataObject ReturnValue = null;
            foreach (PollingMainDataObject Current in History)
            {
                if (Current.isActive == 1)
                {
                    ReturnValue = Current;
                }
            }
            return ReturnValue;
        }
        public bool isCommand(string arg)
        {
            List<string> CommandList = new List<string>();
            CommandList.Add("HELP");
            CommandList.Add("TIME");
            CommandList.Add("KICK");
            CommandList.Add("BAN");
            CommandList.Add("AIRDROP");
            CommandList.Add("CUSTOM");
            CommandList.Add("HISTORY");
            CommandList.Add("?");
            CommandList.Add("CLEAR");
            if (CommandList.IndexOf(arg.ToUpper(new System.Globalization.CultureInfo("en-US"))) != -1)
                return true;
            else
                return false;
        }
        public static System.Boolean IsNumeric(System.Object Expression)
        {
            if (Expression == null || Expression is DateTime)
                return false;

            if (Expression is Int16 || Expression is Int32 || Expression is Int64 || Expression is Decimal || Expression is Single || Expression is Double || Expression is Boolean)
                return true;

            try
            {
                if (Expression is string)
                    Double.Parse(Expression as string);
                else
                    Double.Parse(Expression.ToString());
                return true;
            }
            catch { }
            return false;
        }
        protected void myPrintToChat(BasePlayer Player, string format, params object[] Args)
        {
            Player.SendConsoleCommand("chat.add", 0, string.Format("<color=orange>{0}</color>  {1}", Language["ChatTag"].ToString(), string.Format(format, Args)), 1.0);
        }
    }
}