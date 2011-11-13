using System;
using System.Collections.Generic;
using System.Linq;

namespace BitOrchestra
{
    /// <summary>
    /// An evaluator for an expression that operates on a fixed buffer size.
    /// </summary>
    public abstract class Evaluator
    {
        /// <summary>
        /// Generates the values of the evaluator starting at the given offset and writes them to the given
        /// buffer.
        /// </summary>
        public abstract void Generate(int Start, int[] Buffer);
    }
    
    /// <summary>
    /// An evaluator for a constant value.
    /// </summary>
    public sealed class ConstantEvaluator : Evaluator
    {
        public ConstantEvaluator(int Value)
        {
            this.Value = Value;
        }

        /// <summary>
        /// The value of the evaluator.
        /// </summary>
        public readonly int Value;

        public override void Generate(int Start, int[] Buffer)
        {
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] = Value;
            }
        }
    }

    /// <summary>
    /// An evaluator that returns the value of the parameter.
    /// </summary>
    public sealed class IdentityEvaluator : Evaluator
    {
        private IdentityEvaluator()
        {

        }

        /// <summary>
        /// The only instance of this class.
        /// </summary>
        public static readonly IdentityEvaluator Instance = new IdentityEvaluator();

        public override void Generate(int Start, int[] Buffer)
        {
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] = Start + t;
            }
        }
    }

    /// <summary>
    /// An evaluator that combines two input values.
    /// </summary>
    public abstract class BinaryEvaluator : Evaluator
    {
        public BinaryEvaluator(int BufferSize, Evaluator Left, Evaluator Right)
        {
            this.TempBuffer = new int[BufferSize];
            this.Left = Left;
            this.Right = Right;
        }

        /// <summary>
        /// The left input for the evaluator.
        /// </summary>
        public readonly Evaluator Left;

        /// <summary>
        /// The right input for the evaluator.
        /// </summary>
        public readonly Evaluator Right;

        /// <summary>
        /// The temporary buffer for this evaluator.
        /// </summary>
        public readonly int[] TempBuffer;
    }

    /// <summary>
    /// An evaluator that modifies an input value.
    /// </summary>
    public abstract class UnaryEvaluator : Evaluator
    {
        public UnaryEvaluator(Evaluator Source)
        {
            this.Source = Source;
        }

        /// <summary>
        /// The source input for this evaluator.
        /// </summary>
        public readonly Evaluator Source;
    }

    /// <summary>
    /// An evaluator that adds two values together.
    /// </summary>
    public sealed class AddEvaluator : BinaryEvaluator
    {
        public AddEvaluator(int BufferSize, Evaluator Left, Evaluator Right)
            : base(BufferSize, Left, Right)
        {

        }

        public override void Generate(int Start, int[] Buffer)
        {
            this.Left.Generate(Start, Buffer);
            this.Right.Generate(Start, this.TempBuffer);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] += this.TempBuffer[t];
            }
        }
    }

    /// <summary>
    /// An evaluator that adds a constant value to a source evaluator.
    /// </summary>
    public sealed class AddConstantEvaluator : UnaryEvaluator
    {
        public AddConstantEvaluator(Evaluator Source, int Amount)
            : base(Source)
        {
            this.Amount = Amount;
        }

        /// <summary>
        /// The amount that is added to the source.
        /// </summary>
        public readonly int Amount;

        public override void Generate(int Start, int[] Buffer)
        {
            this.Source.Generate(Start, Buffer);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] += Amount;
            }
        }
    }

    /// <summary>
    /// An evaluator that subtracts one value from another.
    /// </summary>
    public sealed class SubtractEvaluator : BinaryEvaluator
    {
        public SubtractEvaluator(int BufferSize, Evaluator Left, Evaluator Right)
            : base(BufferSize, Left, Right)
        {

        }

        public override void Generate(int Start, int[] Buffer)
        {
            this.Left.Generate(Start, Buffer);
            this.Right.Generate(Start, this.TempBuffer);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] -= this.TempBuffer[t];
            }
        }
    }

    /// <summary>
    /// An evaluator that multiplies two values together.
    /// </summary>
    public sealed class MultiplyEvaluator : BinaryEvaluator
    {
        public MultiplyEvaluator(int BufferSize, Evaluator Left, Evaluator Right)
            : base(BufferSize, Left, Right)
        {

        }

        public override void Generate(int Start, int[] Buffer)
        {
            this.Left.Generate(Start, Buffer);
            this.Right.Generate(Start, this.TempBuffer);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] *= this.TempBuffer[t];
            }
        }
    }

    /// <summary>
    /// An evaluator that multiplies a constant value with a source evaluator.
    /// </summary>
    public sealed class MultiplyConstantEvaluator : UnaryEvaluator
    {
        public MultiplyConstantEvaluator(Evaluator Source, int Amount)
            : base(Source)
        {
            this.Amount = Amount;
        }

        /// <summary>
        /// The amount that the source is multiplied by.
        /// </summary>
        public readonly int Amount;

        public override void Generate(int Start, int[] Buffer)
        {
            this.Source.Generate(Start, Buffer);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] *= Amount;
            }
        }
    }

    /// <summary>
    /// An evaluator that divides one value by another.
    /// </summary>
    public sealed class DivideEvaluator : BinaryEvaluator
    {
        public DivideEvaluator(int BufferSize, Evaluator Left, Evaluator Right)
            : base(BufferSize, Left, Right)
        {

        }

        public override void Generate(int Start, int[] Buffer)
        {

            this.Left.Generate(Start, Buffer);
            this.Right.Generate(Start, this.TempBuffer);
            unchecked
            {
                for (int t = 0; t < Buffer.Length; t++)
                {
                    Buffer[t] /= this.TempBuffer[t];
                }
            }
        }
    }

    /// <summary>
    /// An evaluator that divides a source evaluator by a constant value.
    /// </summary>
    public sealed class DivideConstantEvaluator : UnaryEvaluator
    {
        public DivideConstantEvaluator(Evaluator Source, int Amount)
            : base(Source)
        {
            this.Amount = Amount;
        }

        /// <summary>
        /// The amount that the source is divided by.
        /// </summary>
        public readonly int Amount;

        public override void Generate(int Start, int[] Buffer)
        {

            this.Source.Generate(Start, Buffer);
            unchecked
            {
                for (int t = 0; t < Buffer.Length; t++)
                {
                    Buffer[t] /= Amount;
                }
            }
        }
    }

    /// <summary>
    /// An evaluator that finds the remainder of one value divided by another.
    /// </summary>
    public sealed class ModulusEvaluator : BinaryEvaluator
    {
        public ModulusEvaluator(int BufferSize, Evaluator Left, Evaluator Right)
            : base(BufferSize, Left, Right)
        {

        }

        public override void Generate(int Start, int[] Buffer)
        {

            this.Left.Generate(Start, Buffer);
            this.Right.Generate(Start, this.TempBuffer);
            unchecked
            {
                for (int t = 0; t < Buffer.Length; t++)
                {
                    Buffer[t] %= this.TempBuffer[t];
                }
            }
        }
    }

    /// <summary>
    /// An evaluator that finds the remainder of one value divided by a constant.
    /// </summary>
    public sealed class ModulusConstantEvaluator : UnaryEvaluator
    {
        public ModulusConstantEvaluator(Evaluator Source, int Amount)
            : base(Source)
        {
            this.Amount = Amount;
        }

        /// <summary>
        /// The base of the modulus operation.
        /// </summary>
        public readonly int Amount;

        public override void Generate(int Start, int[] Buffer)
        {

            this.Source.Generate(Start, Buffer);
            unchecked
            {
                for (int t = 0; t < Buffer.Length; t++)
                {
                    Buffer[t] %= Amount;
                }
            }
        }
    }

    /// <summary>
    /// An evaluator that finds the bitwise or of two values.
    /// </summary>
    public sealed class OrEvaluator : BinaryEvaluator
    {
        public OrEvaluator(int BufferSize, Evaluator Left, Evaluator Right)
            : base(BufferSize, Left, Right)
        {

        }

        public override void Generate(int Start, int[] Buffer)
        {
            this.Left.Generate(Start, Buffer);
            this.Right.Generate(Start, this.TempBuffer);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] |= this.TempBuffer[t];
            }
        }
    }

    /// <summary>
    /// An evaluator that finds the bitwise and of two values.
    /// </summary>
    public sealed class AndEvaluator : BinaryEvaluator
    {
        public AndEvaluator(int BufferSize, Evaluator Left, Evaluator Right)
            : base(BufferSize, Left, Right)
        {

        }

        public override void Generate(int Start, int[] Buffer)
        {
            this.Left.Generate(Start, Buffer);
            this.Right.Generate(Start, this.TempBuffer);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] &= this.TempBuffer[t];
            }
        }
    }

    /// <summary>
    /// An evaluator that finds the bitwise xor of two values.
    /// </summary>
    public sealed class XorEvaluator : BinaryEvaluator
    {
        public XorEvaluator(int BufferSize, Evaluator Left, Evaluator Right)
            : base(BufferSize, Left, Right)
        {

        }

        public override void Generate(int Start, int[] Buffer)
        {
            this.Left.Generate(Start, Buffer);
            this.Right.Generate(Start, this.TempBuffer);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] ^= this.TempBuffer[t];
            }
        }
    }

    /// <summary>
    /// An evaluator that finds a value left-shifted by another.
    /// </summary>
    public sealed class LeftShiftEvaluator : BinaryEvaluator
    {
        public LeftShiftEvaluator(int BufferSize, Evaluator Left, Evaluator Right)
            : base(BufferSize, Left, Right)
        {

        }

        public override void Generate(int Start, int[] Buffer)
        {
            this.Left.Generate(Start, Buffer);
            this.Right.Generate(Start, this.TempBuffer);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] <<= this.TempBuffer[t];
            }
        }
    }

    /// <summary>
    /// An evaluator that finds a value left-shifted by a constant.
    /// </summary>
    public sealed class LeftShiftConstantEvaluator : UnaryEvaluator
    {
        public LeftShiftConstantEvaluator(Evaluator Source, int Amount)
            : base(Source)
        {
            this.Amount = Amount;
        }

        /// <summary>
        /// The amount to left-shift by.
        /// </summary>
        public readonly int Amount;

        public override void Generate(int Start, int[] Buffer)
        {
            this.Source.Generate(Start, Buffer);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] <<= Amount;
            }
        }
    }

    /// <summary>
    /// An evaluator that finds a value right-shifted by another.
    /// </summary>
    public sealed class RightShiftEvaluator : BinaryEvaluator
    {
        public RightShiftEvaluator(int BufferSize, Evaluator Left, Evaluator Right)
            : base(BufferSize, Left, Right)
        {

        }

        public override void Generate(int Start, int[] Buffer)
        {
            this.Left.Generate(Start, Buffer);
            this.Right.Generate(Start, this.TempBuffer);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] >>= this.TempBuffer[t];
            }
        }
    }

    /// <summary>
    /// An evaluator that finds a value right-shifted by a constant.
    /// </summary>
    public sealed class RightShiftConstantEvaluator : UnaryEvaluator
    {
        public RightShiftConstantEvaluator(Evaluator Source, int Amount)
            : base(Source)
        {
            this.Amount = Amount;
        }

        /// <summary>
        /// The amount to right-shift by.
        /// </summary>
        public readonly int Amount;

        public override void Generate(int Start, int[] Buffer)
        {
            this.Source.Generate(Start, Buffer);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] >>= Amount;
            }
        }
    }

    /// <summary>
    /// An evaluator for a sequencer.
    /// </summary>
    public sealed class SequencerEvaluator : Evaluator
    {
        public SequencerEvaluator(int BufferSize, Evaluator[] Items, Evaluator Parameter)
        {
            this.Items = Items;
            this.Parameter = Parameter;

            this.ItemBuffers = new int[Items.Length][];
            for (int t = 0; t < Items.Length; t++)
            {
                this.ItemBuffers[t] = new int[BufferSize];
            }
        }

        /// <summary>
        /// The evaluator for the items of the sequence.
        /// </summary>
        public readonly Evaluator[] Items;

        /// <summary>
        /// The evaluator for the parameter of the sequence.
        /// </summary>
        public readonly Evaluator Parameter;

        /// <summary>
        /// The buffers for the items of this sequencer.
        /// </summary>
        public readonly int[][] ItemBuffers;

        public override void Generate(int Start, int[] Buffer)
        {
            // Keep track of what items are generated
            bool[] generated = new bool[Items.Length];

            this.Parameter.Generate(Start, Buffer);
            for (int t = 0; t < Buffer.Length; t++)
            {
                uint param = (uint)Buffer[t] % (uint)this.Items.Length;
                if (!generated[param])
                {
                    this.Items[param].Generate(Start, this.ItemBuffers[param]);
                    generated[param] = true;
                }
                Buffer[t] = this.ItemBuffers[param][t];
            }
        }
    }
}