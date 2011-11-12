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
    public class MainForm : Form, IDisposable
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
            text.TextChanged += this._TextChanged;
            this._Text = text;

            // Menu
            MenuStrip menu = new MenuStrip();
            menu.Items.Add("Save");
            menu.Items.Add("Load");
            menu.Items.Add("Export");
            menu.Items.Add(this._PlayStop = new ToolStripMenuItem("Play", null, this._PlayStopClick, Keys.F5));
            this._SetPlayStopState(PlayStopState.Play);
            this.Controls.Add(menu);
            this._Menu = menu;
        }

        public void Dispose()
        {
            this._Sound.Dispose();
        }

        private void _Play()
        {
            Expression expr;
            SoundOptions opts;
            int errorindex;
            if (Parser.Parse(this._Text.Text, out expr, out opts, out errorindex))
            {
                this._Sound.Play(4096, expr, opts);
                this._SetPlayStopState(PlayStopState.Stop);
            }
            else
            {
                this._Text.Select(errorindex, 0);
                this._SetPlayStopState(PlayStopState.Play);
            }
        }

        private void _PlayStopClick(object sender, EventArgs e)
        {
            switch (this._PlayStopState)
            {
                case PlayStopState.Play:
                    this._Play();
                    break;
                case PlayStopState.Stop:
                    this._Sound.Stop();
                    this._SetPlayStopState(PlayStopState.Play);
                    break;
                case PlayStopState.Update:
                    this._Sound.Stop();
                    this._Play();
                    break;
            }
        }

        private void _TextChanged(object sender, EventArgs e)
        {
            if (this._PlayStopState == PlayStopState.Stop)
                this._SetPlayStopState(PlayStopState.Update);
        }

        private void _SetPlayStopState(PlayStopState State)
        {
            this._PlayStopState = State;
            switch (State)
            {
                case PlayStopState.Play:
                    this._PlayStop.Text = "Play";
                    break;
                case PlayStopState.Stop:
                    this._PlayStop.Text = "Stop";
                    break;
                case PlayStopState.Update:
                    this._PlayStop.Text = "Update";
                    break;
            }
        }

        private PlayStopState _PlayStopState;
        private ToolStripMenuItem _PlayStop;
        private TextBox _Text;
        private MenuStrip _Menu;
        private Sound _Sound;
    }

    /// <summary>
    /// Indicates a possible state for the play/stop button.
    /// </summary>
    public enum PlayStopState
    {
        Stop,
        Play,
        Update
    }
}