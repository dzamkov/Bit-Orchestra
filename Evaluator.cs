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
        public Evaluator(int BufferSize)
        {
            this.Buffer = new int[BufferSize];
        }

        /// <summary>
        /// The buffer for this evaluator.
        /// </summary>
        public readonly int[] Buffer;

        /// <summary>
        /// Indicates wether the buffer for the evaluator is ready for the current parameter.
        /// </summary>
        public bool Ready;

        /// <summary>
        /// Gets a buffer containing the values of this evaluator starting at the given offset.
        /// </summary>
        public int[] Generate(int Start)
        {
            if (!this.Ready)
            {
                this.Generate(Start, this.Buffer);
                this.Ready = true;
            }
            return this.Buffer;
        }

        /// <summary>
        /// Sets "Ready" of this and all source evaluators to false.
        /// </summary>
        public virtual void Invalidate()
        {
            this.Ready = false;
        }

        /// <summary>
        /// Generates the values of the evaluator starting at the given offset and writes them to the
        /// given buffer.
        /// </summary>
        protected abstract void Generate(int Start, int[] Buffer);
    }
    
    /// <summary>
    /// An evaluator for a constant value.
    /// </summary>
    public sealed class ConstantEvaluator : Evaluator
    {
        public ConstantEvaluator(int BufferSize, int Value)
            : base(BufferSize)
        {
            this.Value = Value;
        }

        /// <summary>
        /// The value of the evaluator.
        /// </summary>
        public readonly int Value;

        public override void Invalidate()
        {
            // Since the buffer for this evaluator never changes, nothing needs to be invalidated.
        }

        protected override void Generate(int Start, int[] Buffer)
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
        public IdentityEvaluator(int BufferSize)
            : base(BufferSize)
        {

        }

        protected override void Generate(int Start, int[] Buffer)
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
            : base(BufferSize)
        {
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

        public override void Invalidate()
        {
            base.Invalidate();
            this.Left.Invalidate();
            this.Right.Invalidate();
        }
    }

    /// <summary>
    /// An evaluator that modifies an input value.
    /// </summary>
    public abstract class UnaryEvaluator : Evaluator
    {
        public UnaryEvaluator(int BufferSize, Evaluator Source)
            : base(BufferSize)
        {
            this.Source = Source;
        }

        /// <summary>
        /// The source input for this evaluator.
        /// </summary>
        public readonly Evaluator Source;

        public override void Invalidate()
        {
            base.Invalidate();
            this.Source.Invalidate();
        }
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

        protected override void Generate(int Start, int[] Buffer)
        {
            int[] left = this.Left.Generate(Start);
            int[] right = this.Right.Generate(Start);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] = left[t] + right[t];
            }
        }
    }

    /// <summary>
    /// An evaluator that adds a constant value to a source evaluator.
    /// </summary>
    public sealed class AddConstantEvaluator : UnaryEvaluator
    {
        public AddConstantEvaluator(int BufferSize, Evaluator Source, int Amount)
            : base(BufferSize, Source)
        {
            this.Amount = Amount;
        }

        /// <summary>
        /// The amount that is added to the source.
        /// </summary>
        public readonly int Amount;

        protected override void Generate(int Start, int[] Buffer)
        {
            int[] source = this.Source.Generate(Start);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] = source[t] + this.Amount;
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

        protected override void Generate(int Start, int[] Buffer)
        {
            int[] left = this.Left.Generate(Start);
            int[] right = this.Right.Generate(Start);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] = left[t] - right[t];
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

        protected override void Generate(int Start, int[] Buffer)
        {
            int[] left = this.Left.Generate(Start);
            int[] right = this.Right.Generate(Start);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] = left[t] * right[t];
            }
        }
    }

    /// <summary>
    /// An evaluator that multiplies a constant value with a source evaluator.
    /// </summary>
    public sealed class MultiplyConstantEvaluator : UnaryEvaluator
    {
        public MultiplyConstantEvaluator(int BufferSize, Evaluator Source, int Amount)
            : base(BufferSize, Source)
        {
            this.Amount = Amount;
        }

        /// <summary>
        /// The amount that the source is multiplied by.
        /// </summary>
        public readonly int Amount;

        protected override void Generate(int Start, int[] Buffer)
        {
            int[] source = this.Source.Generate(Start);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] = source[t] * this.Amount;
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

        protected override void Generate(int Start, int[] Buffer)
        {
            int[] left = this.Left.Generate(Start);
            int[] right = this.Right.Generate(Start);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] = unchecked(left[t] / right[t]);
            }
        }
    }

    /// <summary>
    /// An evaluator that divides a source evaluator by a constant value.
    /// </summary>
    public sealed class DivideConstantEvaluator : UnaryEvaluator
    {
        public DivideConstantEvaluator(int BufferSize, Evaluator Source, int Amount)
            : base(BufferSize, Source)
        {
            this.Amount = Amount;
        }

        /// <summary>
        /// The amount that the source is divided by.
        /// </summary>
        public readonly int Amount;

        protected override void Generate(int Start, int[] Buffer)
        {
            int[] source = this.Source.Generate(Start);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] = unchecked(source[t] / this.Amount);
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

        protected override void Generate(int Start, int[] Buffer)
        {
            int[] left = this.Left.Generate(Start);
            int[] right = this.Right.Generate(Start);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] = unchecked(left[t] % right[t]);
            }
        }
    }

    /// <summary>
    /// An evaluator that finds the remainder of one value divided by a constant.
    /// </summary>
    public sealed class ModulusConstantEvaluator : UnaryEvaluator
    {
        public ModulusConstantEvaluator(int BufferSize, Evaluator Source, int Amount)
            : base(BufferSize, Source)
        {
            this.Amount = Amount;
        }

        /// <summary>
        /// The base of the modulus operation.
        /// </summary>
        public readonly int Amount;

        protected override void Generate(int Start, int[] Buffer)
        {
            int[] source = this.Source.Generate(Start);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] = unchecked(source[t] % this.Amount);
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

        protected override void Generate(int Start, int[] Buffer)
        {
            int[] left = this.Left.Generate(Start);
            int[] right = this.Right.Generate(Start);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] = left[t] | right[t];
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

        protected override void Generate(int Start, int[] Buffer)
        {
            int[] left = this.Left.Generate(Start);
            int[] right = this.Right.Generate(Start);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] = left[t] & right[t];
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

        protected override void Generate(int Start, int[] Buffer)
        {
            int[] left = this.Left.Generate(Start);
            int[] right = this.Right.Generate(Start);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] = left[t] ^ right[t];
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

        protected override void Generate(int Start, int[] Buffer)
        {
            int[] left = this.Left.Generate(Start);
            int[] right = this.Right.Generate(Start);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] = left[t] << right[t];
            }
        }
    }

    /// <summary>
    /// An evaluator that finds a value left-shifted by a constant.
    /// </summary>
    public sealed class LeftShiftConstantEvaluator : UnaryEvaluator
    {
        public LeftShiftConstantEvaluator(int BufferSize, Evaluator Source, int Amount)
            : base(BufferSize, Source)
        {
            this.Amount = Amount;
        }

        /// <summary>
        /// The amount to left-shift by.
        /// </summary>
        public readonly int Amount;

        protected override void Generate(int Start, int[] Buffer)
        {
            int[] source = this.Source.Generate(Start);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] = source[t] << this.Amount;
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

        protected override void Generate(int Start, int[] Buffer)
        {
            int[] left = this.Left.Generate(Start);
            int[] right = this.Right.Generate(Start);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] = left[t] >> right[t];
            }
        }
    }

    /// <summary>
    /// An evaluator that finds a value right-shifted by a constant.
    /// </summary>
    public sealed class RightShiftConstantEvaluator : UnaryEvaluator
    {
        public RightShiftConstantEvaluator(int BufferSize, Evaluator Source, int Amount)
            : base(BufferSize, Source)
        {
            this.Amount = Amount;
        }

        /// <summary>
        /// The amount to right-shift by.
        /// </summary>
        public readonly int Amount;

        protected override void Generate(int Start, int[] Buffer)
        {
            int[] source = this.Source.Generate(Start);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] = source[t] >> this.Amount;
            }
        }
    }

    /// <summary>
    /// An evaluator for a sequencer.
    /// </summary>
    public sealed class SequencerEvaluator : Evaluator
    {
        public SequencerEvaluator(int BufferSize, Evaluator[] Items, Evaluator Parameter)
            : base(BufferSize)
        {
            this.Items = Items;
            this.Parameter = Parameter;
        }

        /// <summary>
        /// The evaluator for the items of the sequence.
        /// </summary>
        public readonly Evaluator[] Items;

        /// <summary>
        /// The evaluator for the parameter of the sequence.
        /// </summary>
        public readonly Evaluator Parameter;

        protected override void Generate(int Start, int[] Buffer)
        {
            int[] parambuf = this.Parameter.Generate(Start);
            for (int t = 0; t < Buffer.Length; t++)
            {
                uint param = (uint)parambuf[t] % (uint)this.Items.Length;
                Buffer[t] = this.Items[param].Generate(Start)[t];
            }
        }
    }

    /// <summary>
    /// A sequencer evaluator for items that are constants.
    /// </summary>
    public sealed class SequencerConstantEvaluator : Evaluator
    {
        public SequencerConstantEvaluator(int BufferSize, int[] Items, Evaluator Parameter)
            : base(BufferSize)
        {
            this.Items = Items;
            this.Parameter = Parameter;
        }

        /// <summary>
        /// The items of the sequence.
        /// </summary>
        public readonly int[] Items;

        /// <summary>
        /// The evaluator for the parameter of the sequence.
        /// </summary>
        public readonly Evaluator Parameter;

        protected override void Generate(int Start, int[] Buffer)
        {
            int[] parambuf = this.Parameter.Generate(Start);
            for (int t = 0; t < Buffer.Length; t++)
            {
                uint param = (uint)parambuf[t] % (uint)this.Items.Length;
                Buffer[t] = this.Items[param];
            }
        }
    }
}