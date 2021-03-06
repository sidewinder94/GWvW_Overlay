﻿using GWvW_Overlay.DataModel;
using GWvW_Overlay.Resources.Lang;
using Logitech_LCD;
using Logitech_LCD.Applets;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ArenaNET;
using System.Windows.Forms;

namespace GWvW_Overlay
{
    public partial class ColorDisplayApplet : BaseApplet
    {
        private static String[] mapNames = new String[4] { "GreenHome", "BlueHome", "RedHome", "Center" };
        private MainWindow parent;
        private String _bl;
        private Label[] lines;
        private int currentLine;
        public WvwMatchup match { get; set; }

        public ColorDisplayApplet(MainWindow parent, WvwMatchup match) :
            this(LcdType.Color, parent, match)
        {

        }


        public ColorDisplayApplet(LcdType lcdType, MainWindow parent, WvwMatchup match)
            : base(lcdType)
        {
            InitializeComponent();

            this.parent = parent;

            lines = new Label[6] {line1,
                                  line2,
                                  line3,
                                  line4,
                                  line5,
                                  line6};

            this.match = match;
            _bl = match.Options.active_bl;
            LcdColorLeftButtonPressed += ColorDisplayApplet_OnLcdColorLeftButtonPressed;
            LcdColorRightButtonPressed += ColorDisplayApplet_OnLcdColorRightButtonPressed;
            LcdColorUpButtonPressed += ColorDisplayApplet_LcdColorUpButtonPressed;
            LcdColorDownButtonPressed += ColorDisplayApplet_LcdColorDownButtonPressed;
            BuildTabControlTabs();
        }

        private int GetCurrentMap()
        {
            for (int i = 0; i < mapNames.Length; i++)
            {
                if (mapNames[i].Equals(match.Options.active_bl))
                {
                    return i;
                }
            }
            return 0;
        }

        void ColorDisplayApplet_LcdColorDownButtonPressed(object sender, EventArgs e)
        {
            if (this.parent.CnvsBlSelection.Visibility == System.Windows.Visibility.Hidden &&
                this.parent.cnvsMatchSelection.Visibility == System.Windows.Visibility.Hidden)
            {
                String newMap;
                if (GetCurrentMap() < mapNames.Length - 1)
                {
                    newMap = mapNames[GetCurrentMap() + 1];
                }
                else
                {
                    newMap = mapNames[0];
                }
                this.parent.BorderlandSelected(newMap, EventArgs.Empty);
            }
        }

        void ColorDisplayApplet_LcdColorUpButtonPressed(object sender, EventArgs e)
        {
            if (this.parent.CnvsBlSelection.Visibility == System.Windows.Visibility.Hidden &&
                this.parent.cnvsMatchSelection.Visibility == System.Windows.Visibility.Hidden)
            {
                String newMap;
                if (GetCurrentMap() > 0)
                {
                    newMap = mapNames[GetCurrentMap() - 1];
                }
                else
                {
                    newMap = mapNames[mapNames.Length - 1];
                }
                this.parent.BorderlandSelected(newMap, EventArgs.Empty);
            }
        }

        void ColorDisplayApplet_OnLcdColorRightButtonPressed(object sender, EventArgs e)
        {
            if (tabs.SelectedIndex < tabs.TabCount - 1)
            {
                tabs.SelectedIndex++;
            }
            else
            {
                tabs.SelectedIndex = 0;
            }
        }

        void ColorDisplayApplet_OnLcdColorLeftButtonPressed(object sender, EventArgs e)
        {
            if (tabs.SelectedIndex > 0)
            {
                tabs.SelectedIndex--;
            }
            else
            {
                tabs.SelectedIndex = tabs.TabCount - 1;
            }
        }



        protected override void OnDataUpdate(object sender, EventArgs e)
        {
            if (_bl != match.Options.active_bl)
            {
                _bl = match.Options.active_bl;
                BuildTabControlTabs();
            }
            if (match.Details != null)
            {
                this.BackgroundImage = null;
                tabs.Visible = true;
                currentLine = 0;
                List<ArenaNET.Objective> result = new List<ArenaNET.Objective>();
                if (this.tabs.SelectedTab.Text == Strings.camps)
                {
                    result = match.Details.Maps.FirstOrDefault(map => map.Type == _bl).Objectives
                                                               .Where(obj => obj.Type == "Camp")
                                                               .Take(6)
                                                               .ToList();
                }
                else if ((this.tabs.SelectedTab.Text == Strings.towers) ||
                         (this.tabs.SelectedTab.Text == (Strings.towers + " 1")))
                {
                    result = match.Details.Maps.FirstOrDefault(map => map.Type == _bl).Objectives
                                                               .Where(obj => obj.Type == "Tower")
                                                               .Take(6)
                                                               .ToList();
                }
                else if (this.tabs.SelectedTab.Text == Strings.towers + " 2")
                {
                    result = match.Details.Maps.FirstOrDefault(map => map.Type == _bl).Objectives
                                                               .Where(obj => obj.Type == "Tower")
                                                               .Take(6)
                                                               .ToList();

                    result = match.Details.Maps.FirstOrDefault(map => map.Type == _bl).Objectives
                                                               .Where(obj => obj.Type == "Tower")
                                                               .Except(result)
                                                               .Take(6)
                                                               .ToList();
                }
                else if (this.tabs.SelectedTab.Text == Strings.keeps)
                {
                    result = match.Details.Maps.FirstOrDefault(map => map.Type == _bl).Objectives
                                                               .Where(obj => obj.Type == "Keep")
                                                               .Take(6)
                                                               .ToList();
                }
                else if (this.tabs.SelectedTab.Text == Strings.castles)
                {
                    result = match.Details.Maps.FirstOrDefault(map => map.Type == _bl).Objectives
                                                               .Where(obj => obj.Type == "Castle")
                                                               .Take(6)
                                                               .ToList();
                }
                result.ForEach(Format);
                for (; currentLine < lines.Length; currentLine++)
                {
                    lines[currentLine].Visible = false;
                }
            }
            else
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    lines[i].Visible = false;
                }
                tabs.Visible = false;

                this.BackgroundImage = Image.FromStream(
                    App.GetResourceStream(new Uri("/Resources/mapeb_normal.png", UriKind.Relative)).Stream);

                //Image.FromFile("Resources/mapeb_normal.png");
                this.BackgroundImageLayout = ImageLayout.Stretch;
            }

        }

        private void Format(ArenaNET.Objective obj)
        {
            if (obj.LastFlipped.HasValue)
            {

                TimeSpan diff = DateTime.Now.Subtract(obj.LastFlipped.Value);
                TimeSpan left = TimeSpan.FromMinutes(5) - diff;
                String time = diff < TimeSpan.FromMinutes(5) ? left.ToString(@"mm\:ss") : "N/A";
                lines[currentLine].Text = String.Format("{0} {1}",
                    time,
                    obj.Name);
                lines[currentLine].ForeColor = ownerColor(obj.Owner);
                lines[currentLine].Visible = true;
            }
            currentLine++;
        }

        private Color ownerColor(String owner)
        {
            switch (owner)
            {
                case "Green":
                    return Color.Green;
                case "Blue":
                    return Color.Blue;
                case "Red":
                    return Color.Red;
                default:
                    return Color.White;
            }
        }

        private void BuildTabControlTabs()
        {
            this.tabs.TabPages.Clear();
            this.tabs.TabPages.Add(new TabPage(Strings.camps));
            if (_bl == "Center")
            {
                this.tabs.TabPages.Add(new TabPage(Strings.towers + " 1"));
                this.tabs.TabPages.Add(new TabPage(Strings.towers + " 2"));
            }
            else
            {
                this.tabs.TabPages.Add(new TabPage(Strings.towers));
            }
            this.tabs.TabPages.Add(new TabPage(Strings.keeps));
            if (_bl == "Center")
                this.tabs.TabPages.Add(new TabPage(Strings.castles));
        }

    }
}
