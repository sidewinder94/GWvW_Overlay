using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Logitech_LCD;
using Logitech_LCD.Applets;
using GWvW_Overlay.Resources.Lang;

namespace GWvW_Overlay
{
    public partial class MonoDisplayApplet : BaseApplet
    {
        //private String _bl;
        //private Label[] lines;
        //private int currentLine;
        public WvwMatch_ match { get; set; }

        public MonoDisplayApplet(ref WvwMatch_ match)
            : this(LcdType.Mono, ref match)
        {

        }

        public MonoDisplayApplet(LcdType lcdType, ref WvwMatch_ match)
            : base(lcdType)
        {
            InitializeComponent();
        }

        protected override void OnDataUpdate(object sender, EventArgs e)
        {

        }
    }
}
