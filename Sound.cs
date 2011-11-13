using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace BitOrchestra
{
    /// <summary>
    /// Contains options for playing a sound.
    /// </summary>
    public class SoundOptions
    {
        /// <summary>
        /// The sample rate to play at.
        /// </summary>
        public int Rate = 8000;

        /// <summary>
        /// The parameter value to start playing at.
        /// </summary>
        public int Offset = 0;

        /// <summary>
        /// The length of the sound in samples, used for exporting.
        /// </summary>
        public int Length = 0;
    }

    /// <summary>
    /// An interface to a sound output for evaluators.
    /// </summary>
    public class Sound : IDisposable
    {
        public Sound()
        {

        }

        /// <summary>
        /// Tries creating a waveout interface.
        /// </summary>
        private static bool _CreateWaveout(out IWavePlayer Player)
        {
            try
            {
                Player = new WaveOut();
                return true;
            }
            catch
            {
                Player = null;
                return false;
            }
        }

        /// <summary>
        /// Tries creating a wasapi interface.
        /// </summary>
        private static bool _CreateWasapi(out IWavePlayer Player)
        {
            try
            {
                Player = new WasapiOut(AudioClientShareMode.Shared, 300);
                return true;
            }
            catch
            {
                Player = null;
                return false;
            }
        }

        /// <summary>
        /// Tries creating a directsound interface.
        /// </summary>
        private static bool _CreateDirectSound(out IWavePlayer Player)
        {
            try
            {
                Player = new DirectSoundOut();
                return true;
            }
            catch
            {
                Player = null;
                return false;
            }
        }

        /// <summary>
        /// Tries creating an asio interface.
        /// </summary>
        private static bool _CreateAsio(out IWavePlayer Player)
        {
            try
            {
                Player = new AsioOut();
                return true;
            }
            catch
            {
                Player = null;
                return false;
            }
        }

        /// <summary>
        /// Tries creating a wave player of some sort.
        /// </summary>
        private static bool _Create(out IWavePlayer Player)
        {
            return
                _CreateWasapi(out Player) ||
                _CreateWaveout(out Player) ||
                _CreateDirectSound(out Player) ||
                _CreateAsio(out Player);
        }

        /// <summary>
        /// Gets if this sound is currently playing.
        /// </summary>
        public bool IsPlaying
        {
            get
            {
                return this._Player.PlaybackState == PlaybackState.Playing;
            }
        }

        /// <summary>
        /// Tries playing the sound from the given evaluator stream.
        /// </summary>
        public bool Play(WaveStream Stream)
        {
            try
            {
                if (this._Player == null)
                {
                    if (_Create(out this._Player))
                    {
                        this._Player.Init(this._Stream = Stream);
                        this._Player.Play();
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                this._Stream = null;
                return false;
            }
        }

        /// <summary>
        /// Tries exporting the given wavestream as a wave file.
        /// </summary>
        public static bool Export(string File, WaveStream Stream)
        {
            try
            {
                WaveFileWriter.CreateWaveFile(File, Stream);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Stops playing this sound.
        /// </summary>
        public void Stop()
        {
            if (this._Stream != null)
            {
                // NAudio needs to be persuaded to make sure the stream stays stopped. (Something about threading issues?)
                ((IDisposable)(this._Stream)).Dispose();
                this._Stream = null;
            }
            this._Player.Stop();
            this._Player.Dispose();
            this._Player = null;
        }

        public void Dispose()
        {
            if (this._Player != null)
            {
                this.Stop();
            }
        }

        private IWavePlayer _Player;
        private WaveStream _Stream;
    }

    /// <summary>
    /// A sound stream produced by an evaluator.
    /// </summary>
    public class EvaluatorStream : WaveStream, IDisposable
    {
        public EvaluatorStream(int BufferSize, Evaluator Evaluator, SoundOptions Options, bool Exporting)
        {
            this._Exporting = Exporting;
            this._Options = Options;
            this._Evaluator = Evaluator;
            this._Buffer = new int[BufferSize];
            this._Offset = BufferSize;
            this._Parameter = Options.Offset;
        }

        public EvaluatorStream(int BufferSize, Expression Expression, SoundOptions Options, bool Exporting)
        {
            this._Exporting = Exporting;
            this._Options = Options;
            this._Evaluator = Expression.GetEvaluator(BufferSize);
            this._Buffer = new int[BufferSize];
            this._Offset = BufferSize;
            this._Parameter = Options.Offset;
        }

        /// <summary>
        /// Gets the wave format for the given sound options.
        /// </summary>
        public static WaveFormat GetFormat(SoundOptions Options)
        {
            return new WaveFormat(Options.Rate, 8, 1);
        }

        public override WaveFormat WaveFormat
        {
            get
            {
                return GetFormat(this._Options);
            }
        }

        public override long Length
        {
            get
            {
                return this._Options.Length;
            }
        }

        public override long Position
        {
            get
            {
                return this._Parameter - this._Options.Offset;
            }
            set
            {
                this._Parameter = this._Options.Offset + (int)value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this._Evaluator == null)
            {
                return 0;
            }

            // If exporting, make sure only to write "Options.Length" samples
            if (this._Exporting)
                count = Math.Min(count, this._Options.Length - this._Parameter);

            int ocount = count;
            while (true)
            {
                int sampsleft = this._Buffer.Length - this._Offset;
                if (count < sampsleft)
                {
                    int ofs = this._Offset;
                    for (int t = 0; t < count; t++)
                    {
                        buffer[offset] = (byte)this._Buffer[ofs];
                        offset++;
                        ofs++;
                    }
                    this._Offset = ofs;
                    break;
                }
                else
                {
                    int ofs = this._Offset;
                    for (int t = 0; t < sampsleft; t++)
                    {
                        buffer[offset] = (byte)this._Buffer[ofs];
                        offset++;
                        ofs++;
                    }
                    count -= sampsleft;

                    this._Evaluator.Generate(this._Parameter, this._Buffer);
                    this._Parameter += this._Buffer.Length;
                    this._Offset = 0;
                    continue;
                }
            }
            return ocount;
        }

        void IDisposable.Dispose()
        {
            this._Evaluator = null;
        }

        private bool _Exporting;
        private SoundOptions _Options;
        private Evaluator _Evaluator;
        private int[] _Buffer;
        private int _Offset;
        private int _Parameter;
    }
}