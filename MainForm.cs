using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;

namespace BitOrchestra
{
    /// <summary>
    /// The main form for the application.
    /// </summary>
    public class MainForm : Form
    {
        public MainForm()
        {
            this.Text = "Bit Orchestra";
            this.Width = 640;
            this.Height = 480;

            this._Sound = new Sound();

            // Text area
            TextBox text = new TextBox();
            text.Dock = DockStyle.Fill;
            text.Font = new Font(FontFamily.GenericMonospace, 12.0f);
            text.Multiline = true;
            text.ScrollBars = ScrollBars.Vertical;
            this.Controls.Add(text);
            this._Text = text;

            // Menu
            MenuStrip menu = new MenuStrip();
            menu.Items.Add("Save");
            menu.Items.Add("Load");
            menu.Items.Add("Export");
            menu.Items.Add(this._PlayStop = new ToolStripMenuItem("Play", null, this._PlayStopClick, Keys.F5));
            this.Controls.Add(menu);
            this._Menu = menu;
        }

        private void _Play()
        {
            Expression expr;
            SoundOptions opts;
            int errorindex;
            if (Parser.Parse(this._Text.Text, out expr, out opts, out errorindex))
            {
                this._Sound.Play(4096, expr, opts);
                this._PlayStop.Text = "Stop";
            }
            else
            {
                this._Text.Select(errorindex, 0);
            }
        }

        private void _Stop()
        {
            this._Sound.Stop();
            this._PlayStop.Text = "Play";
        }

        private void _PlayStopClick(object sender, EventArgs e)
        {
            if (this._Sound.IsPlaying)
                this._Stop();
            else
                this._Play();
        }

        private ToolStripMenuItem _PlayStop;
        private TextBox _Text;
        private MenuStrip _Menu;
        private Sound _Sound;
    }
}