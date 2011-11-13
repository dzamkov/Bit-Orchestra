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

        /// <summary>
        /// The amount of bits of resolution in the output of the sound.
        /// </summary>
        public int Resolution = 8;
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
                this._Player = null;
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
            this._Offset = BufferSize;
            this._Parameter = Options.Offset;

            // Calculate shift and sample size
            int res = Options.Resolution;
            int sampsize = (res + 7) / 8;
            int shift = sampsize * 8 - res;
            this._SampleSize = sampsize;
            this._Shift = shift;

            // Generate initial buffer
            this._Advance();
        }

        public EvaluatorStream(int BufferSize, Expression Expression, SoundOptions Options, bool Exporting)
            : this(BufferSize, Expression.GetEvaluator(new Dictionary<Expression,Evaluator>(), BufferSize, Options.Resolution), Options, Exporting)
        {

        }

        public override WaveFormat WaveFormat
        {
            get
            {
                return new WaveFormat(this._Options.Rate, this._SampleSize * 8, 1);
            }
        }

        public override long Length
        {
            get
            {
                return this._Options.Length * this._SampleSize;
            }
        }

        public override long Position
        {
            get
            {
                return (this._Parameter - this._Options.Offset) * this._SampleSize;
            }
            set
            {
                this._Parameter = (this._Options.Offset + (int)value) / this._SampleSize;
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

            // Find sample size and shift amount
            int sampsize = this._SampleSize;
            int shift = this._Shift;

            int ocount = count;
            count /= this._SampleSize; // Its more useful to treat count as a sample count rather than a byte count.

            while (true)
            {
                int sampsleft = this._Buffer.Length - this._Offset;
                int toread = Math.Min(count, sampsleft);
                int ofs = this._Offset;
                for (int t = 0; t < toread; t++)
                {
                    int val = this._Buffer[ofs];

                    if (shift == 0 && this._SampleSize == 1)
                    {
                        // Direct byte copying is faster
                        buffer[offset] = (byte)val;
                        offset++;
                    }
                    else
                    {
                        // Copy the first byte manually, since a unique leftshift must be applied.
                        buffer[offset] = (byte)(val << shift);
                        offset++;

                        int sf = 8 - shift;
                        for (int i = 1; i < sampsize; i++)
                        {
                            buffer[offset] = (byte)(val >> sf);
                            sf += 8;
                            offset++;
                        }
                    }

                    ofs++;
                }
                this._Offset = ofs;
                count -= toread;

                if (count < sampsleft)
                {
                    break;
                }
                else
                {
                    this._Advance();
                    continue;
                }
            }
            return ocount;
        }

        /// <summary>
        /// Generates the next buffer and advances the parameter.
        /// </summary>
        private void _Advance()
        {
            this._Evaluator.Invalidate();
            this._Buffer = this._Evaluator.Generate(this._Parameter);
            this._Parameter += this._Buffer.Length;
            this._Offset = 0;
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

        private int _SampleSize;
        private int _Shift;
    }
}