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
            this._Player = new WaveOut();
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
        /// Tries playing the sound from the given evaluator.
        /// </summary>
        public bool Play(int BufferSize, Evaluator Evaluator, SoundOptions Options)
        {
            try
            {
                this._Player.Init(this._Stream = new _EvaluatorStream(BufferSize, Evaluator, Options.Rate, Options.Offset));
                this._Player.Play();
                return true;
            }
            catch
            {
                if (this._Stream != null)
                {
                    this._Stream.Stop();
                    this._Stream.Dispose();
                }
                return false;
            }
        }

        /// <summary>
        /// Plays sound based on the given expression.
        /// </summary>
        public bool Play(int BufferSize, Expression Expression, SoundOptions Options)
        {
            return this.Play(BufferSize, Expression.GetEvaluator(BufferSize), Options);
        }

        /// <summary>
        /// Stops playing this sound.
        /// </summary>
        public void Stop()
        {
            if (this._Stream != null)
            {
                // NAudio needs to be persuaded to make sure the stream stays stopped.
                this._Stream.Stop();
                this._Stream.Dispose();
                this._Stream = null;
            }
            this._Player.Stop();
        }

        public void Dispose()
        {
            this._Player.Dispose();
        }

        private class _EvaluatorStream : WaveStream
        {
            public _EvaluatorStream(int BufferSize, Evaluator Evaluator, int Rate, int Parameter)
            {
                this._Rate = Rate;
                this._Evaluator = Evaluator;
                this._Buffer = new int[BufferSize];
                this._Offset = BufferSize;
                this._Parameter = Parameter;
            }

            /// <summary>
            /// Prevents this stream from giving any more samples.
            /// </summary>
            public void Stop()
            {
                this._Evaluator = null;
            }

            public override WaveFormat WaveFormat
            {
                get
                {
                    return new WaveFormat(this._Rate, 8, 1);
                }
            }

            public override long Length
            {
                get { throw new NotImplementedException(); }
            }

            public override long Position
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (this._Evaluator == null)
                {
                    return 0;
                }

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

            private int _Rate;
            private Evaluator _Evaluator;
            private int[] _Buffer;
            private int _Offset;
            private int _Parameter;
            private int _Length;
        }

        private IWavePlayer _Player;
        private _EvaluatorStream _Stream;
    }
}