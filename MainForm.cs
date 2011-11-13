﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

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
            this._Saved = true;

            try
            {
                this.Icon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
            catch
            {

            }

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
            menu.Items.Add(new ToolStripMenuItem("Save", null, this._SaveClick, Keys.Control | Keys.S));
            menu.Items.Add(new ToolStripMenuItem("Load", null, this._LoadClick, Keys.Control | Keys.L));
            menu.Items.Add(new ToolStripMenuItem("Export", null, this._ExportClick, Keys.Control | Keys.E));
            menu.Items.Add(this._PlayStop = new ToolStripMenuItem("Play", null, this._PlayStopClick, Keys.F5));
            this._SetPlayStopState(PlayStopState.Play);
            this.Controls.Add(menu);
            this._Menu = menu;
        }

        /// <summary>
        /// The caption for message boxes created by this form.
        /// </summary>
        public static readonly string MessageBoxCaption = "Bit Orchestra says";

        void IDisposable.Dispose()
        {
            this._Sound.Dispose();
        }

        private void _Play()
        {
            Expression expr;
            SoundOptions opts;
            if (this._Parse(out expr, out opts))
            {
                if (this._Sound.Play(new EvaluatorStream(4096, expr, opts, false)))
                {
                    this._SetPlayStopState(PlayStopState.Stop);
                }
                else
                {
                    MessageBox.Show("Could not create sound output, check values of \"#rate\" and \"#resolution\".", MessageBoxCaption, MessageBoxButtons.OK);
                    this._SetPlayStopState(PlayStopState.Play);
                }
            }
            else
            {
                this._SetPlayStopState(PlayStopState.Play);
            }
        }

        /// <summary>
        /// Tries parsing the contents of this form, displaying the appropriate messages on failure.
        /// </summary>
        private bool _Parse(out Expression Expression, out SoundOptions Options)
        {
            int errorindex;
            if (Parser.Parse(this._Text.Text, out Expression, out Options, out errorindex))
            {
                if (Expression != null)
                {
                    return true;
                }
                else
                {
                    MessageBox.Show("The result variable \"" + Parser.Result + "\" must be defined", MessageBoxCaption, MessageBoxButtons.OK);
                    return false;
                }
            }
            else
            {
                this._Text.Select(errorindex, 0);
                return false;
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
            this._Saved = false;
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

        /// <summary>
        /// Checks if the work is saved and prompts the user to save it if not.
        /// </summary>
        private void _CheckSaved()
        {
            if (!this._Saved)
            {
                if (MessageBox.Show("Would you like to save your work?", MessageBoxCaption, MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    this._Save();
                }
            }
        }

        /// <summary>
        /// The filter string to use for files.
        /// </summary>
        private static string _FileFilter = "Bit Orchestra script (*.bos)|*.bos|C file (*.c)|*.c|All files (*.*)|*.*";

        /// <summary>
        /// Opens a save file dialog for saving the current text.
        /// </summary>
        private void _Save()
        {
            SaveFileDialog sfg = new SaveFileDialog();
            sfg.Filter = _FileFilter;
            sfg.RestoreDirectory = true;

            if (sfg.ShowDialog() == DialogResult.OK)
            {
                using (TextWriter tw = new StreamWriter(sfg.FileName))
                {
                    tw.Write(this._Text.Text);
                    tw.Close();
                }
                this._Saved = true;
            }
        }

        /// <summary>
        /// Opens an open file dialog for loading text.
        /// </summary>
        private void _Load()
        {
            OpenFileDialog ofg = new OpenFileDialog();
            ofg.Filter = _FileFilter;
            ofg.RestoreDirectory = true;

            if (ofg.ShowDialog() == DialogResult.OK)
            {
                using (TextReader tr = new StreamReader(ofg.FileName))
                {
                    this._Text.Text = tr.ReadToEnd();
                    tr.Close();
                }
                this._Saved = true;
            }
        }
        
        /// <summary>
        /// Opens a save file dialog for exporting as a wave file.
        /// </summary>
        private void _Export()
        {
            Expression expr;
            SoundOptions opts;
            if (this._Parse(out expr, out opts))
            {
                if (opts.Length != 0)
                {
                    SaveFileDialog sfg = new SaveFileDialog();
                    sfg.Filter = "Wave file (*.wav)|*.wav|All files (*.*)|*.*";
                    sfg.RestoreDirectory = true;

                    if (sfg.ShowDialog() == DialogResult.OK)
                    {
                        if (!Sound.Export(sfg.FileName, new EvaluatorStream(4096, expr, opts, true)))
                        {
                            MessageBox.Show("Something went wrong.", MessageBoxCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("The \"#length\" option must be set to export.", MessageBoxCaption, MessageBoxButtons.OK);
                }
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            this._CheckSaved();
            this._Sound.Dispose();
        }

        private void _SaveClick(object sender, EventArgs e)
        {
            this._Save();
        }

        private void _LoadClick(object sender, EventArgs e)
        {
            this._CheckSaved();
            this._Load();
        }

        private void _ExportClick(object sender, EventArgs e)
        {
            this._Export();
        }

        private bool _Saved;
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