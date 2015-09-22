using GWvW_Overlay.DataModel;
using GWvW_Overlay.Keyboard;
using GWvW_Overlay.Resources.Lang;
using Logitech_LCD;
using Logitech_LCD.Applets;
using Newtonsoft.Json;
/*
 * Objective names https://gist.github.com/codemasher/bac2b4f87e7af128087e (smiley.1438)
 */
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using GWvW_Overlay.Properties;
using GWvW_Overlay.Service;
using Logitech_LED;
using MumbleLink_CSharp_GW2;

namespace GWvW_Overlay
{
    public partial class MainWindow
    {
        delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        static readonly WinEventDelegate ProcDelegate = WinEventProc;
        public static readonly GW2Link DataLink = new GW2Link();
        readonly Keyboard.KeyboardListener _kListener = new Keyboard.KeyboardListener();
        readonly IntPtr _hhook;

        static MainWindow _handleThis;

        bool _resetMatch;
        static bool _inGame;
        private bool? _adjustingHeight;

        readonly Timer _t1 = new Timer();
        readonly Timer _mapDetectTimer = new Timer(100);
        private int _currentMapId = 0;
        readonly Timer _t3 = new Timer();



        //JSON Data
        Match_Details_ _matchDetails = new Match_Details_();
        private readonly WvwMatch_ _wvwMatch = new WvwMatch_();

        public WvwMatch_ WvwMatch { get { return _wvwMatch; } }

        Matches_ _jsonMatches = new Matches_();

        public Guild GuildData = new Guild();

        readonly CampLogger LogWindow = new CampLogger();

        public BaseApplet applet;

        //About & Settings Windows
        private SetOptions optionWindow;
        private About aboutWindow;
        public void ClickTroughActivate()
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            Natives.SetWindowLong(handle, Natives.GWL_ExStyle, Natives.WS_EX_Transparent);

            LogWindow.ClickTroughActivate();
        }

        public void ClickTroughVoid()
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            Natives.SetWindowLong(handle, Natives.GWL_ExStyle, Natives.WS_EX_Layered);

            LogWindow.ClickTroughVoid();
        }

        static void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            var wTitle = new StringBuilder(13);
            Natives.GetWindowText(hwnd, wTitle, 13);

            if (wTitle.ToString() == "Guild Wars 2" && _inGame != true)
            {
                _inGame = true;
                _handleThis.ClickTroughActivate();
            }
            else if (wTitle.ToString() != "Guild Wars 2" && _inGame)
            {
                _inGame = false;
                _handleThis.ClickTroughVoid();
            }
        }
        public MainWindow()
        {
            RtvMatches();

            var initTask = new Task(() =>
            {
                LocationService.Match = WvwMatch;
                LocationService.Connection.StartSending();
            });

            InitializeComponent();
            MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight - 50.0;
            SourceInitialized += Window_SourceInitialized;

            _hhook = SetWinEventHook(Natives.EVENT_SYSTEM_FOREGROUND, Natives.EVENT_SYSTEM_FOREGROUND, IntPtr.Zero,
                ProcDelegate, 0, 0, Natives.WINEVENT_SKIPOWNPROCESS); // | Natives.WINEVENT_SKIPOWNPROCESS

            MainWindow1.WindowStartupLocation = WindowStartupLocation.CenterScreen;


            _kListener.KeyDown += KListener_KeyDown;
            _kListener.KeyUp += KListener_KeyUp;

            _t1.Interval = 4000;
            _t1.Elapsed += RtvMatchDetails;
            _t1.Start();


            _mapDetectTimer.Elapsed += (sender, args) =>
            {
                if (WvwMatch.Details == null) return;
                var map = DataLink.GetCoordinates().MapId;
                if (map != _currentMapId && Map.KnownMap(map))
                {

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        BorderlandSelected(Map.ColorId[map], EventArgs.Empty);
                    }));

                }
                _currentMapId = map;
            };

            _mapDetectTimer.Enabled = Settings.Default.auto_switch_map;

            _t3.Interval = 1000;
            _t3.Elapsed += UpdateTimers;

            _handleThis = this;

            Console.WriteLine(CmbbxHomeServerSelection.Items.Count);
            foreach (World_Names_ item in CmbbxHomeServerSelection.Items)
            {
                if (item.id == Settings.Default.home_server)
                    CmbbxHomeServerSelection.SelectedItem = item;
            }

            BuildMenu();

            if (LogitechLcd.Instance.IsConnected(LcdType.Color))
            {
                applet = new ColorDisplayApplet(this, WvwMatch);
            }
            else if (LogitechLcd.Instance.IsConnected(LcdType.Mono))
            {
                //applet = new MonoDisplayApplet();
            }


        }

        public void UpdatePosition(Object source, EventArgs e)
        {
            if (WvwMatch.Details == null)
                return;

            foreach (Map map in WvwMatch.Details.Maps)
            {
                foreach (Objective obj in map.Objectives)
                {
                    DateTime cur = DateTime.Now;

                    if (obj.ObjData.top != 0.0)
                    {
                        Objective obj1 = obj;
                        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        {
                            obj1.ObjData.left = Width * (obj1.ObjData.left_base / obj1.ObjData.res_width);
                            obj1.ObjData.top = Height * (obj1.ObjData.top_base / obj1.ObjData.res_height);
                            WvwMatch.PlayerPositions.CanvasHeight = Height;
                            WvwMatch.PlayerPositions.CanvasWidth = Width;
                        }));
                    }
                }
            }

        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {

            LogWindow.Show();
            if (!(bool)Properties.Settings.Default["show_tracker"])
                LogWindow.Hide();


            //Test
            /*
            Dictionary<string, string> dict1 = new Dictionary<string, string>() { { "time", DateTime.Now.ToString("t") }, { "objective", "Objective Name" }, { "owner_color", "green" }, { "owner", "released" } };
            Dictionary<string, string> dict2 = new Dictionary<string, string>() { { "time", DateTime.Now.ToString("t") }, { "objective", "Objective Name" }, { "owner_color", "red" }, { "owner", "[Tag] Name" } };

            Dictionary<string, string> dict3 = new Dictionary<string, string>() 
                                    { 
                                        {"time", DateTime.Now.ToString("t")},
                                        {"objective",  "Objective Name"},
                                        {"from", "Server Name 1"},
                                        {"from_color", "blue"},
                                        {"to", "Server Name 2"},
                                        {"to_color", "green"},
                                    };

            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                LogWindow.AddEventLog(dict1, true);
                LogWindow.AddEventLog(dict2, true);
                LogWindow.AddEventLog(dict3, false);
            }));*/

            if (!(bool)Properties.Settings.Default["auto_matchup"])
            {
                cnvsMatchSelection.Visibility = Visibility.Visible;

            }
            else
            {
                cnvsMatchSelection.Visibility = Visibility.Hidden;
                AutoMatchSetActiveMatch();
                GetBorderlandSelection();
            }

            //rtvMatchDetails(null, null);
        }


        public static void SetKeyboardColor(WvwMatch_ WvwMatch)
        {
            if (Settings.Default.server_color_lightning && LogitechLed.Instance.IsInit)
            {
                switch (WvwMatch.HomeServerColor)
                {
                    case "red":
                        LogitechLed.Instance.SetLighting(100, 0, 0);
                        break;
                    case "green":
                        LogitechLed.Instance.SetLighting(0, 100, 0);
                        break;
                    case "blue":
                        LogitechLed.Instance.SetLighting(0, 0, 100);
                        break;
                    default:
                        LogitechLed.Instance.SetLighting(100, 100, 100);
                        break;

                }
            }
        }

        public void GetBorderlandSelection()
        {
            SetKeyboardColor(WvwMatch);
            CnvsBlSelection.Visibility = Visibility.Visible;
            LblBlueBl.Content = WvwMatch.GetServerName("blue");
            LblGreenBl.Content = WvwMatch.GetServerName("green");
            LblRedBl.Content = WvwMatch.GetServerName("red");
        }

        public void AutoMatchSetActiveMatch()
        {
            foreach (var match in WvwMatch.Match)
            {
                if (match.blue_world_id == (int)Settings.Default["home_server"]
                    || match.green_world_id == (int)Settings.Default["home_server"]
                    || match.red_world_id == (int)Settings.Default["home_server"])
                {
                    WvwMatch.Options.active_match = match.wvw_match_id;
                    RtvMatchDetails(null, null);
                    BuildMenu();
                    break;
                }
            }
        }

        public void BuildMenu()
        {
            var mainMenu = new ContextMenu();

            var matches = new MenuItem { Header = "Matches" };
            var y = WvwMatch.GetMatchesList();
            foreach (var x in y)
            {
                var i = new MenuItem { Header = x.Value, Tag = x.Key };
                i.Click += MatchSelected;
                matches.Items.Add(i);
            }
            mainMenu.Items.Add(matches);

            var menuOptions = new MenuItem { Header = "Options" };
            menuOptions.Click += ShowOptionsWindow;
            mainMenu.Items.Add(menuOptions);

            if (WvwMatch.Options.active_match != null)
            {
                var blBlue = new MenuItem { Header = string.Format(Strings.blueBorderland + " ({0})", WvwMatch.GetServerName("blue")), Tag = "BlueHome" };
                blBlue.Click += BorderlandSelected;

                var blRed = new MenuItem { Header = string.Format(Strings.redBorderland + " ({0})", WvwMatch.GetServerName("red")), Tag = "RedHome" };
                blRed.Click += BorderlandSelected;

                var blGreen = new MenuItem { Header = string.Format(Strings.greenBorderland + " ({0})", WvwMatch.GetServerName("green")), Tag = "GreenHome" };
                blGreen.Click += BorderlandSelected;

                var blEb = new MenuItem { Header = Strings.eternalBattlegrounds, Tag = "Center" };
                blEb.Click += BorderlandSelected;

                mainMenu.Items.Add(new Separator());

                mainMenu.Items.Add(blGreen);
                mainMenu.Items.Add(blBlue);
                mainMenu.Items.Add(blRed);
                mainMenu.Items.Add(blEb);

            }
            var aboutWin = new MenuItem { Header = Strings.about };
            aboutWin.Click += ShowAboutWin;
            mainMenu.Items.Add(aboutWin);

            var exitApp = new MenuItem { Header = Strings.exit };
            exitApp.Click += ExitApp;

            mainMenu.Items.Add(exitApp);
            ContextMenu = mainMenu;
        }

        public void UpdateTimers(Object source, ElapsedEventArgs e)
        {
            SetKeyboardColor(WvwMatch);
            if (WvwMatch.Details == null)
                return;

            AdjustScore();

            var cur = DateTime.Now;
            string eventCamps = "";

            for (int i = 0; i < WvwMatch.Details.Maps.Count; i++)
            {
                int map = i;

                for (int m = 0; m < WvwMatch.Details.Maps[map].Objectives.Count; m++)
                {
                    int obj = m;
                    WvwMatch.Details.Maps[map].Objectives[obj].ownedTime = ""; //just here to fire the OnPropertyChanged Event.
                    if (WvwMatch.Details.Maps[map].Objectives[obj].id >= 62) // Skip Ruins of Power. No the best way to go about it...
                        continue;

                    TimeSpan diff = cur.Subtract(WvwMatch.Details.Maps[map].Objectives[obj].last_change);
                    TimeSpan left = TimeSpan.FromMinutes(5) - diff;
                    if (diff < TimeSpan.FromMinutes(5))
                    {
                        if (WvwMatch.Details.Maps[map].Objectives[obj].ObjData.type == "camp" && WvwMatch.Options.active_bl == WvwMatch.Details.Maps[map].Type)
                        {
                            /* Dictionary<string, string> dict = new Dictionary<string, string>()
                             {
                                 {"objective", WvwMatch.Details.maps[map].objectives[obj].ObjData.name},
                                 {"time_left", left.ToString(@"mm\:ss")}
                             };

                             Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                             {
                                 LogWindow.AddCampLog(dict);
                             }));*/
                            eventCamps += string.Format("{0}\t{1}\n", left.ToString(@"mm\:ss"), WvwMatch.Details.Maps[map].Objectives[obj].ObjData.name);
                        }

                        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        {
                            WvwMatch.Details.Maps[map].Objectives[obj].time_left = left.ToString(@"mm\:ss");
                        }));
                    }
                    else
                    {

                        if (WvwMatch.Details.Maps[map].Objectives[obj].ObjData.type == "camp" && WvwMatch.Options.active_bl == WvwMatch.Details.Maps[map].Type)
                        {
                            /*Dictionary<string, string> dict = new Dictionary<string, string>()
                            {
                                {"objective", WvwMatch.Details.maps[map].objectives[obj].ObjData.name},
                                {"time_left", "N/A"}
                            };

                            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                            {
                                LogWindow.AddCampLog(dict);
                            }));*/
                            eventCamps += string.Format("{0}\t{1}\n", "N/A", WvwMatch.Details.Maps[map].Objectives[obj].ObjData.name);
                        }
                        Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        {
                            WvwMatch.Details.Maps[map].Objectives[obj].time_left = " ";
                        }));
                    }
                }
            }
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                LogWindow.txtblk_eventCamp.Text = eventCamps;
            }));
        }

        public void AdjustScore()
        {
            //Tracker

            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    if (LogWindow != null)
                    {
                        LogWindow.RedScoreLocal.Width = 120 * (WvwMatch.Details.Maps[WvwMatch.Options.active_blid].Scores[0] / WvwMatch.Details.Maps[WvwMatch.Options.active_blid].ScoresSum);
                        LogWindow.BlueScoreLocal.Width = 120 * (WvwMatch.Details.Maps[WvwMatch.Options.active_blid].Scores[1] / WvwMatch.Details.Maps[WvwMatch.Options.active_blid].ScoresSum);
                        LogWindow.GreenScoreLocal.Width = 120 * (WvwMatch.Details.Maps[WvwMatch.Options.active_blid].Scores[2] / WvwMatch.Details.Maps[WvwMatch.Options.active_blid].ScoresSum);

                        LogWindow.LblCampCount.Content = WvwMatch.Details.Maps[WvwMatch.Options.active_blid].CountObjType("camp", WvwMatch.HomeServerColor);
                        LogWindow.LblTowerCount.Content = WvwMatch.Details.Maps[WvwMatch.Options.active_blid].CountObjType("tower", WvwMatch.HomeServerColor);
                        LogWindow.LblCastleCount.Content = WvwMatch.Details.Maps[WvwMatch.Options.active_blid].CountObjType("castle", WvwMatch.HomeServerColor);
                        LogWindow.LblKeepCount.Content = WvwMatch.Details.Maps[WvwMatch.Options.active_blid].CountObjType("keep", WvwMatch.HomeServerColor);
                    }
                }));


            //Main map
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                RedBarGlobal.Width = ScoreBoard.Width * (WvwMatch.Details.Scores[0] / WvwMatch.Details.ScoresSum);
                BlueBarGlobal.Width = (ScoreBoard.Width * (WvwMatch.Details.Scores[1] / WvwMatch.Details.ScoresSum)) + 1;

                //not impl
                RedBarBL.Width = 40;
                BlueBarBL.Width = 10;
            }));
        }

        public void RtvMatches()
        {
            _jsonMatches = JsonConvert.DeserializeObject<Matches_>(Utils.GetJson("https://api.guildwars2.com/v1/wvw/matches.json"));
            _jsonMatches.wvw_matches.Sort((x, y) => y.wvw_match_id != null ? (x.wvw_match_id != null ? String.Compare(x.wvw_match_id, y.wvw_match_id, StringComparison.Ordinal) : 0) : 0);
            WvwMatch.Match = _jsonMatches.wvw_matches;
            _jsonMatches = null;
        }

        public void RtvMatchDetails(Object source, System.Timers.ElapsedEventArgs e)
        {
            if (WvwMatch.Options.active_match == null)
                return;

            _matchDetails = JsonConvert.DeserializeObject<Match_Details_>(Utils.GetJson("https://api.guildwars2.com/v1/wvw/match_details.json?match_id=" + WvwMatch.Options.active_match));
            /*This is for debug only*/
            //_matchDetails = JsonConvert.DeserializeObject<Match_Details_>("{\"match_id\":\"2-1\",\"scores\":[93697,108880,92013],\"maps\":[{\"type\":\"RedHome\",\"scores\":[36353,14334,11250],\"objectives\":[{\"id\":50,\"owner\":\"Green\"},{\"id\":32,\"owner\":\"Red\"},{\"id\":33,\"owner\":\"Red\"},{\"id\":35,\"owner\":\"Red\"},{\"id\":37,\"owner\":\"Red\",\"owner_guild\":\"4FF2B6BA-65D7-445D-BECF-BCDE54597764\"},{\"id\":38,\"owner\":\"Red\"},{\"id\":39,\"owner\":\"Red\"},{\"id\":40,\"owner\":\"Red\"},{\"id\":51,\"owner\":\"Red\"},{\"id\":52,\"owner\":\"Red\"},{\"id\":53,\"owner\":\"Red\"},{\"id\":36,\"owner\":\"Blue\"},{\"id\":34,\"owner\":\"Green\"},{\"id\":62,\"owner\":\"Neutral\"},{\"id\":63,\"owner\":\"Neutral\"},{\"id\":64,\"owner\":\"Neutral\"},{\"id\":65,\"owner\":\"Neutral\"},{\"id\":66,\"owner\":\"Neutral\"}],\"bonuses\":[{\"type\":\"bloodlust\",\"owner\":\"Red\"}]},{\"type\":\"GreenHome\",\"scores\":[14955,15939,31918],\"objectives\":[{\"id\":41,\"owner\":\"Red\"},{\"id\":42,\"owner\":\"Red\",\"owner_guild\":\"07EF98A9-1645-4155-8B25-464FF06E66D8\"},{\"id\":43,\"owner\":\"Red\"},{\"id\":44,\"owner\":\"Red\",\"owner_guild\":\"CE2AE45E-7747-46F1-8CB8-E9A68A08B2BC\"},{\"id\":46,\"owner\":\"Red\"},{\"id\":55,\"owner\":\"Red\"},{\"id\":45,\"owner\":\"Blue\"},{\"id\":48,\"owner\":\"Blue\"},{\"id\":49,\"owner\":\"Blue\",\"owner_guild\":\"02BC88DA-335C-41F2-92A1-C65B91538F2E\"},{\"id\":47,\"owner\":\"Green\"},{\"id\":54,\"owner\":\"Green\"},{\"id\":56,\"owner\":\"Green\"},{\"id\":57,\"owner\":\"Green\"},{\"id\":72,\"owner\":\"Neutral\"},{\"id\":73,\"owner\":\"Neutral\"},{\"id\":74,\"owner\":\"Neutral\"},{\"id\":75,\"owner\":\"Neutral\"},{\"id\":76,\"owner\":\"Neutral\"}],\"bonuses\":[{\"type\":\"bloodlust\",\"owner\":\"Green\"}]},{\"type\":\"BlueHome\",\"scores\":[8800,42879,12136],\"objectives\":[{\"id\":59,\"owner\":\"Red\",\"owner_guild\":\"3DC7ABF2-E87B-4E41-9D34-45C6836E4CE2\"},{\"id\":23,\"owner\":\"Blue\"},{\"id\":25,\"owner\":\"Blue\"},{\"id\":27,\"owner\":\"Blue\"},{\"id\":28,\"owner\":\"Blue\"},{\"id\":30,\"owner\":\"Blue\"},{\"id\":24,\"owner\":\"Green\"},{\"id\":26,\"owner\":\"Green\"},{\"id\":29,\"owner\":\"Green\",\"owner_guild\":\"9B665122-BA17-480A-9ECE-308992D1BE20\"},{\"id\":31,\"owner\":\"Green\"},{\"id\":58,\"owner\":\"Green\"},{\"id\":60,\"owner\":\"Green\"},{\"id\":61,\"owner\":\"Green\"},{\"id\":67,\"owner\":\"Neutral\"},{\"id\":68,\"owner\":\"Neutral\"},{\"id\":69,\"owner\":\"Neutral\"},{\"id\":70,\"owner\":\"Neutral\"},{\"id\":71,\"owner\":\"Neutral\"}],\"bonuses\":[{\"type\":\"bloodlust\",\"owner\":\"Blue\"}]},{\"type\":\"Center\",\"scores\":[33589,35728,36709],\"objectives\":[{\"id\":1,\"owner\":\"Red\",\"owner_guild\":\"2D83EB3F-C5B0-4575-A0E8-D79B14979CA8\"},{\"id\":5,\"owner\":\"Red\"},{\"id\":8,\"owner\":\"Red\"},{\"id\":17,\"owner\":\"Red\"},{\"id\":18,\"owner\":\"Red\"},{\"id\":19,\"owner\":\"Red\"},{\"id\":20,\"owner\":\"Red\"},{\"id\":2,\"owner\":\"Blue\",\"owner_guild\":\"C88F28BE-C4D1-E411-925A-AC162DAE5AD5\"},{\"id\":7,\"owner\":\"Blue\"},{\"id\":15,\"owner\":\"Blue\"},{\"id\":16,\"owner\":\"Blue\"},{\"id\":21,\"owner\":\"Blue\"},{\"id\":22,\"owner\":\"Blue\"},{\"id\":3,\"owner\":\"Green\",\"owner_guild\":\"D039163B-ED2F-47C9-914C-EA59C39A5533\"},{\"id\":4,\"owner\":\"Green\"},{\"id\":6,\"owner\":\"Green\"},{\"id\":9,\"owner\":\"Green\",\"owner_guild\":\"6F057328-3843-E411-AA11-AC162DAAE275\"},{\"id\":10,\"owner\":\"Green\"},{\"id\":11,\"owner\":\"Green\"},{\"id\":12,\"owner\":\"Green\"},{\"id\":13,\"owner\":\"Green\",\"owner_guild\":\"4136858D-3C44-E511-A3E6-AC162DC0E835\"},{\"id\":14,\"owner\":\"Green\",\"owner_guild\":\"FB2B5C28-27E5-4986-B60F-0E017C02809D\"}],\"bonuses\":[]}]}");
            if (WvwMatch.Details == null || _resetMatch)
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    LogWindow.ResetText();
                }));
                WvwMatch.Details = _matchDetails;
                _resetMatch = false;
                WvwMatch.GetBLID();

            }
            else
            {
                WvwMatch.Details.match_id = _matchDetails.match_id;
                WvwMatch.Details.Scores = _matchDetails.Scores;


                WvwMatch.Details.Maps.ForEach(map =>
                {
                    var remoteMap = _matchDetails.Maps.Find(m => m.Type == map.Type);

                    map.Scores = remoteMap.Scores;


                    remoteMap.Objectives.ForEach(obj =>
                    {
                        var localObj = map.Objectives.Find(o => o.id == obj.id);

                        if (obj.owner_guild != null)
                        {
                            GuildData.GetGuildById(obj.owner_guild);
                        }
                        if (localObj.id != obj.id) Console.WriteLine("Comparing {0} and {1}", localObj.id, obj.id);
                        if (localObj.owner != obj.owner)
                        {
                            if (WvwMatch.Options.active_bl == map.Type)
                            {
                                var dict = new Dictionary<string, string>
                                    { 
                                        {"time", DateTime.Now.ToString("t")},
                                        {"objective", localObj.ObjData.name},
                                        {"from", WvwMatch.GetServerName(localObj.owner)},
                                        {"from_color", localObj.owner},
                                        {"to", WvwMatch.GetServerName(obj.owner)},
                                        {"to_color", obj.owner},
                                    };
                                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => LogWindow.AddEventLog(dict, false)));
                            }

                            localObj.owner = obj.owner;
                            localObj.owner_guild = obj.owner_guild;
                            localObj.last_change = DateTime.Now;
                        }
                        if (localObj.owner_guild != obj.owner_guild &&
                            localObj.owner == obj.owner)
                        {
                            localObj.owner_guild = obj.owner_guild;

                            if (WvwMatch.Options.active_bl == map.Type)
                            {
                                var guildInfo = GuildData.GetGuildById(localObj.owner_guild);
                                var dict = new Dictionary<string, string>
                                    {
                                        {"time", DateTime.Now.ToString("t")},
                                        {"objective", localObj.ObjData.name},
                                        {"owner_color", localObj.owner}, {"owner", guildInfo == null ? "released" : string.Format("[{1}] {0}", guildInfo[0], guildInfo[1])}
                                    };

                                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => LogWindow.AddEventLog(dict, true)));
                            }

                        }
                    });



                });

            }

            // Fill objective names and icons positions
            if (WvwMatch.Details.Maps[3].Objectives[0].ObjData.name == null)
            {
                for (int i = 0; i < WvwMatch.Details.Maps.Count; i++)
                {
                    int map = i;
                    var objData = JsonConvert.DeserializeObject<List<WvwObjective>>(Utils.GetJson(string.Format("Resources/obj_{0}.json", WvwMatch.Details.Maps[map].Type)));
                    foreach (var obj in objData)
                    {
                        for (int y = 0; y < WvwMatch.Details.Maps[map].Objectives.Count; y++)
                        {
                            var objct = y;
                            if (obj.id == WvwMatch.Details.Maps[map].Objectives[objct].id)
                                WvwMatch.Details.Maps[map].Objectives[objct].ObjData = obj;
                        }
                    }
                }
            }

            _t3.Start();
        }

        public ImageSource GetPng(string type, string color)
        {
            var y = color == "none" ? string.Format("Resources/{0}.png", type) : string.Format("Resources/{0}_{1}.png", type, color.ToLower());
            ImageSource x = new BitmapImage(new Uri(y, UriKind.Relative));
            return x;
        }

        void KListener_KeyDown(object sender, Keyboard.RawKeyEventArgs args)
        {
            if (args.Key.ToString() == Properties.Settings.Default["hotkey"].ToString() && !(bool)Properties.Settings.Default["alwaysTop"])
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    IntPtr handle = new WindowInteropHelper(this).Handle;
                    Natives.ShowWindow(handle, 4);
                }));
            }
        }

        void KListener_KeyUp(object sender, Keyboard.RawKeyEventArgs args)
        {
            if (args.Key.ToString() == Properties.Settings.Default["hotkey"].ToString() && !(bool)Properties.Settings.Default["alwaysTop"])
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    IntPtr handle = new WindowInteropHelper(this).Handle;
                    Natives.ShowWindow(handle, 6);
                }));
            }
        }

        public void MatchSelected(object sender, EventArgs e)
        {
            if (WvwMatch.Options.active_match != (string)((MenuItem)sender).Tag)
            {
                _resetMatch = true;
                Icons.ItemsSource = null;
            }

            WvwMatch.Options.active_match = (string)((MenuItem)sender).Tag;
            RtvMatchDetails(null, null);
            BuildMenu();
            //ContextMenu.IsOpen = true;

            GetBorderlandSelection();
        }

        public void BorderlandSelected(object sender, EventArgs e)
        {
            string selectedBl = null;

            var label = sender as Label;
            if (label != null)
                selectedBl = (string)label.Tag;

            var item = sender as MenuItem;
            if (item != null)
                selectedBl = (string)item.Tag;

            var str = sender as String;
            if (str != null)
                selectedBl = str;

            if (selectedBl == null)
                selectedBl = "Center";

            WvwMatch.Options.active_bl = selectedBl;
            Icons.ItemsSource = WvwMatch.Details.Maps[WvwMatch.Options.blid[selectedBl]].Objectives;
            WvwMatch.MarkersVisibility = Visibility.Visible;

            if (LogWindow != null)
                LogWindow.lblBLTitle.Content = WvwMatch.Options.active_bl_title;

            MainWindow1.InvalidateVisual();

            CnvsBlSelection.Visibility = Visibility.Hidden;
        }

        public void ExitApp(object sender, EventArgs e)
        {
            _t1.Stop();
            _t3.Stop();
            _mapDetectTimer.Stop();
            UnhookWinEvent(_hhook);
            LogWindow.Close();
            Application.Current.Shutdown();
        }

        private void Drag(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                try
                {
                    DragMove();
                }
                catch
                {


                }

        }

        private void ShowOptionsWindow(object sender, EventArgs e)
        {
            if ((optionWindow == null) || (!optionWindow.IsVisible))
            {
                optionWindow = new SetOptions(LogWindow, WvwMatch, MainWindow1);
            }
            optionWindow.Show();
            optionWindow.Focus();
        }

        private void ShowAboutWin(object sender, EventArgs e)
        {
            if ((aboutWindow == null) || (!aboutWindow.IsVisible))
            {
                aboutWindow = new About();
            }
            aboutWindow.Show();
            aboutWindow.Focus();
        }

        public static Point GetMousePosition() // mouse position relative to screen
        {
            var w32Mouse = new Natives.Win32Point();
            Natives.GetCursorPos(ref w32Mouse);
            return new Point(w32Mouse.X, w32Mouse.Y);
        }


        private void Window_SourceInitialized(object sender, EventArgs ea)
        {
            var hwndSource = (HwndSource)HwndSource.FromVisual((Window)sender);
            if (hwndSource != null) hwndSource.AddHook(DragHook);

            Console.WriteLine(CmbbxHomeServerSelection.Items.Count);
            foreach (World_Names_ item in CmbbxHomeServerSelection.Items)
            {
                if (item.id == (int)Properties.Settings.Default["home_server"])
                    CmbbxHomeServerSelection.SelectedItem = item;
            }
        }

        private IntPtr DragHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            double aspectRatio = WvwMatch.Options.width / WvwMatch.Options.height;
            switch ((Natives.WM)msg)
            {
                case Natives.WM.WINDOWPOSCHANGING:
                    {
                        var pos = (Natives.WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(Natives.WINDOWPOS));

                        if ((pos.flags & (int)Natives.SWP.NOMOVE) != 0)
                            return IntPtr.Zero;

                        var hwndSource = HwndSource.FromHwnd(hwnd);
                        if (hwndSource != null)
                        {
                            var wnd = (Window)hwndSource.RootVisual;
                            if (wnd == null)
                                return IntPtr.Zero;
                        }

                        // determine what dimension is changed by detecting the mouse position relative to the 
                        // window bounds. if gripped in the corner, either will work.
                        if (!_adjustingHeight.HasValue)
                        {
                            Point p = GetMousePosition();

                            double diffWidth = Math.Min(Math.Abs(p.X - pos.x), Math.Abs(p.X - pos.x - pos.cx));
                            double diffHeight = Math.Min(Math.Abs(p.Y - pos.y), Math.Abs(p.Y - pos.y - pos.cy));
                            _adjustingHeight = diffHeight > diffWidth;
                        }

                        if (_adjustingHeight.Value)
                            pos.cy = (int)(pos.cx / aspectRatio); // adjusting height to width change
                        else
                            pos.cx = (int)(pos.cy * aspectRatio); // adjusting width to heigth change

                        Marshal.StructureToPtr(pos, lParam, true);
                        handled = true;
                    }
                    break;
                case Natives.WM.EXITSIZEMOVE:
                    _adjustingHeight = null; // reset adjustment dimension and detect again next time window is resized
                    break;
            }

            return IntPtr.Zero;
        }

        private void MainClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            UnhookWinEvent(_hhook);
            LogWindow.Close();
            Properties.Settings.Default.Save();
        }

        private void lblSelectionOK_Click(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            if (CmbbxHomeServerSelection.SelectedValue == null && LstbxMatchSelection.SelectedValue == null)
            {
                MessageBox.Show("Please select the Home Server or one of the avaliable matches.");
                return;
            }
            //If auto-match-up is TRUE and home-server is NOT selected
            if (ChkbxAutoMatchSelect.IsChecked != null && (bool)ChkbxAutoMatchSelect.IsChecked &&
                CmbbxHomeServerSelection.SelectedValue == null)
            {
                MessageBox.Show("Automatic Match Selection requires \"Home Server\" to be set.");
                return;
            }

            //Check if auto-match-up is TRUE and home-server is selected
            if (ChkbxAutoMatchSelect.IsChecked != null && (bool)ChkbxAutoMatchSelect.IsChecked &&
                CmbbxHomeServerSelection.SelectedValue != null)
            {
                Properties.Settings.Default["auto_matchup"] = (bool)ChkbxAutoMatchSelect.IsChecked;
                Properties.Settings.Default["home_server"] = CmbbxHomeServerSelection.SelectedValue;
                Properties.Settings.Default.Save();
                AutoMatchSetActiveMatch();
            }
            // If home-server is set go to BL selection
            else if (CmbbxHomeServerSelection.SelectedValue != null)
            {
                Properties.Settings.Default["home_server"] = CmbbxHomeServerSelection.SelectedValue;
                Properties.Settings.Default.Save();
                AutoMatchSetActiveMatch();
            }
            else if (LstbxMatchSelection.SelectedItem != null)
            {
                WvwMatch.Options.active_match = (string)LstbxMatchSelection.SelectedValue;
                RtvMatchDetails(null, null);
            }
            BuildMenu();
            cnvsMatchSelection.Visibility = Visibility.Hidden;
            GetBorderlandSelection();
        }

        private void CmbbxMatchSelection_Change(object sender, SelectionChangedEventArgs e)
        {
            if (CmbbxHomeServerSelection.SelectedItem == null)
                return;

            var selection = (World_Names_)CmbbxHomeServerSelection.SelectedItem;

            Console.WriteLine(selection.name);

            CnvsMatchUp.Visibility = Visibility.Hidden;
            cnvsMatchSelection.Height = 100;
        }

        private void lblSelectionReset_Click(object sender, MouseButtonEventArgs e)
        {
            cnvsMatchSelection.Height = 400;
            CnvsMatchUp.Visibility = Visibility.Visible;
            CmbbxHomeServerSelection.SelectedItem = null;
            LstbxMatchSelection.SelectedItem = null;
        }
    }
}
