using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Windows;
using GWvW_Overlay.Annotations;
using GWvW_Overlay.Properties;
using GWvW_Overlay.Resources.Lang;
using GWvW_Overlay.Service;
using GWvW_Overlay_Location_Server_Contracts;
using Newtonsoft.Json;

namespace GWvW_Overlay.DataModel
{
    public class WvwMatch_ : INotifyPropertyChanged
    {
        private List<int> _worldIds;
        private List<World_Names_> _worlds = new List<World_Names_>(51);
        private Visibility _markersVisibility = Visibility.Hidden;



        private readonly Positions _positions = new Positions();
        public Positions PlayerPositions
        {
            get { return _positions; }
        }

        public Visibility MarkersVisibility
        {
            get
            {
                return _markersVisibility;
            }
            set
            {
                _markersVisibility = value;
                OnPropertyChanged();
            }
        }

        public WvwMatch_()
        {

            Options = new Options_();
            CacheServerIDs();
        }

        public Options_ Options { get; set; }
        public List<Matches> Match { get; set; }

        public List<World_Names_> World
        {
            get
            {
                if (_worlds.Count == 0)
                {
                    _worlds = GetServerNames();
                }
                return _worlds;
            }
        }

        public Match_Details_ Details { get; set; }
        public List<WvwObjective> ObjectiveNames { get; set; }

        public string HomeServerColor
        {
            get { return Options.HomeServerColor ?? (Options.HomeServerColor = GetServerColor()); }
        }

        //Get BL IDs to Type
        public void GetBLID()
        {
            var dict = new Dictionary<string, int>();
            for (int i = 0; i < Details.Maps.Count; i++)
            {
                int map_id = i;
                dict.Add(Details.Maps[map_id].Type, map_id);
            }
            Options.blid = dict;
        }

        public void ListWorlds()
        {
            foreach (World_Names_ x in World)
            {
                Console.WriteLine("<ComboBoxItem Content=\"{0}\"/>", x.name);
            }
        }

        public Dictionary<string, string> GetMatchesList()
        {
            var ret = new Dictionary<string, string>();
            foreach (Matches x in Match)
            {
                string matchName = string.Format("{0}. {3} vs {2} vs {1}", x.wvw_match_id, GetServerName(x.red_world_id),
                    GetServerName(x.blue_world_id), GetServerName(x.green_world_id));
                x.wvw_match_string = matchName;
                ret.Add(x.wvw_match_id, matchName);
            }
            return ret.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
        }

        private void CacheServerIDs()
        {
            try
            {
                _worldIds =
                    JsonConvert.DeserializeObject<List<int>>(Utils.GetJson(@"https://api.guildwars2.com/v2/worlds"));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(@"https://api.guildwars2.com/v2/worlds" + " disabled/not accessible");
            }
        }

        private List<World_Names_> GetServerNames()
        {
            try
            {
                _worldIds.ForEach(id => _worlds.Add(JsonConvert.DeserializeObject<World_Names_>(
                    Utils.GetJson(String.Format(@"https://api.guildwars2.com/v2/worlds/{0}?lang={1}", id,
                        Strings.queryLanquage)))));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(@"https://api.guildwars2.com/v2/worlds/ unusable using fallback");
                _worlds.Clear();
            }


            if (_worlds.Count == 0)
            {
                List<World_Names_> worlds = null;
                try
                {
                    worlds =
                        JsonConvert.DeserializeObject<List<World_Names_>>(
                            Utils.GetJson(@"https://api.guildwars2.com/v1/world_names.json?lang=" +
                                          Strings.queryLanquage));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(@"https://api.guildwars2.com/v1/world_names.json?lang=" + Strings.queryLanquage +
                                      " disabled/not accessible");
                }

                if (worlds == null)
                {
                    try
                    {
                        worlds =
                            JsonConvert.DeserializeObject<List<World_Names_>>(
                                Utils.GetJson(
                                    String.Format(
                                        @"https://raw.githubusercontent.com/sidewinder94/GWvW_Overlay_Data/master/world_names/{0}.json",
                                        Strings.queryLanquage)));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine(
                            @"'https://raw.githubusercontent.com/sidewinder94/GWvW_Overlay_Data/master/world_names/{0}.json' disables/not accessible",
                            Strings.queryLanquage);
                    }
                }

                _worlds = worlds;
            }

            if (_worlds != null)
            {
                _worlds.Sort(
                    (x, y) =>
                        y.name != null
                            ? (x.name != null ? String.Compare(x.name, y.name, StringComparison.Ordinal) : 0)
                            : 0);
            }
            return _worlds;
        }

        public string GetServerName(string color)
        {
            foreach (Matches x in Match)
            {
                if (Options.active_match == x.wvw_match_id)
                {
                    switch (color.ToLower())
                    {
                        case "red":
                            return GetServerName(x.red_world_id);
                        case "blue":
                            return GetServerName(x.blue_world_id);
                        case "green":
                            return GetServerName(x.green_world_id);
                        case "neutral":
                            return "Neutral";
                    }
                }
            }
            return string.Format("MATCH_ID-{0}_NOT_FOUND", Options.active_match);
        }

        public string GetServerName(int ID)
        {
            foreach (World_Names_ x in World.Where(x => ID == x.id))
            {
                return x.name;
            }

            return string.Format("SERVER_{0}_NOT_FOUND", ID);
        }

        public string GetServerColor()
        {
            foreach (Matches x in Match)
            {
                if ((int)Settings.Default["home_server"] == x.red_world_id) // && x.red_world_id == id)
                    return "red";
                if ((int)Settings.Default["home_server"] == x.green_world_id) // && x.green_world_id == id)
                    return "green";
                if ((int)Settings.Default["home_server"] == x.blue_world_id) // && x.blue_world_id == id)
                    return "blue";
            }
            return "white";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
