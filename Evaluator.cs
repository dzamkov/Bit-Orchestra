using System;
using System.Collections.Generic;
using System.Linq;

using Value = System.Int32;
using UValue = System.UInt32;

namespace BitOrchestra
{

    /// <summary>
    /// An evaluator for an expression that operates on a fixed buffer size.
    /// </summary>
    public abstract class Evaluator
    {
        /// <summary>
        /// Gets a buffered form of this evaluator. If this is already buffered, this is returned.
        /// </summary>
        public BufferedEvaluator GetBuffered(int BufferSize)
        {
            if (this._Buffered == null)
            {
                BufferedEvaluator be = this as BufferedEvaluator;
                if (be != null)
                {
                    return this._Buffered = be;
                }
                else
                {
                    return this._Buffered = new BufferedEvaluator(BufferSize, this);
                }
            }
            return this._Buffered;
        }

        /// <summary>
        /// Indicates wether this evaluator has data for the previous Generate call. This can be made false with
        /// Invalidate.
        /// </summary>
        public bool Ready;

        /// <summary>
        /// Discards all intermediate results for the last Generate call and prepares the evaluator for another
        /// iteration.
        /// </summary>
        public virtual void Invalidate()
        {
            this.Ready = false;
        }

        /// <summary>
        /// Generates the values of the evaluator starting at the given offset and writes them to the
        /// given buffer.
        /// </summary>
        public abstract void Generate(Value Start, Value[] Buffer);

        /// <summary>
        /// A buffered version of this evaluator.
        /// </summary>
        private BufferedEvaluator _Buffered;
    }

    /// <summary>
    /// An evaluator that stores results from a source evaluator in a buffer.
    /// </summary>
    public sealed class BufferedEvaluator : Evaluator
    {
        public BufferedEvaluator(int BufferSize, Evaluator Source)
        {
            this.Buffer = new Value[BufferSize];
            this.Source = Source;
        }

        /// <summary>
        /// The buffer for this evaluator.
        /// </summary>
        public readonly Value[] Buffer;

        /// <summary>
        /// The source evaluator for this buffered evaluator.
        /// </summary>
        public readonly Evaluator Source;

        public override void Invalidate()
        {
            base.Invalidate();
            this.Source.Invalidate();
        }

        public override void Generate(Value Start, Value[] Buffer)
        {
            Value[] src = this.Generate(Start);
            for (int t = 0; t < src.Length; t++)
            {
                Buffer[t] = src[t];
            }
        }

        /// <summary>
        /// Generates the values of the evaluator starting at the given offset and returns them as a buffer.
        /// </summary>
        public Value[] Generate(Value Start)
        {
            if (!this.Ready)
            {
                this.Source.Generate(Start, this.Buffer);
                this.Ready = true;
            }
            return this.Buffer;
        }
    }

    /// <summary>
    /// An evaluator for a constant value.
    /// </summary>
    public sealed class ConstantEvaluator : Evaluator
    {
        public ConstantEvaluator(Value Value)
        {
            this.Value = Value;
        }

        /// <summary>
        /// The value of the evaluator.
        /// </summary>
        public readonly Value Value;

        public override void Invalidate()
        {
            // Since the value of this evaluator never changes, there is no need to invalidate.
        }

        public override void Generate(Value Start, Value[] Buffer)
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

        public override void Generate(Value Start, Value[] Buffer)
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
        public BinaryEvaluator(Evaluator Left, BufferedEvaluator Right)
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
        public readonly BufferedEvaluator Right;

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
        public UnaryEvaluator(Evaluator Source)
        {
            this.Source = Source;
        }

        /// <summary>
        /// The source input for this evaluator.
        /// </summary>
        public readonly Evaluator Source;

        public override void Invalidate()
        {
            this.Source.Invalidate();
        }
    }

    /// <summary>
    /// An evaluator that adds two values together.
    /// </summary>
    public sealed class AddEvaluator : BinaryEvaluator
    {
        public AddEvaluator(Evaluator Left, BufferedEvaluator Right)
            : base(Left, Right)
        {

        }

        public override void Generate(Value Start, Value[] Buffer)
        {
            this.Left.Generate(Start, Buffer);
            Value[] right = this.Right.Generate(Start);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] += right[t];
            }
        }
    }

    /// <summary>
    /// An evaluator that adds a constant value to a source evaluator.
    /// </summary>
    public sealed class AddConstantEvaluator : UnaryEvaluator
    {
        public AddConstantEvaluator(Evaluator Source, Value Amount)
            : base(Source)
        {
            this.Amount = Amount;
        }

        /// <summary>
        /// The amount that is added to the source.
        /// </summary>
        public readonly Value Amount;

        public override void Generate(Value Start, Value[] Buffer)
        {
            this.Source.Generate(Start, Buffer);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] += this.Amount;
            }
        }
    }

    /// <summary>
    /// An evaluator that subtracts one value from another.
    /// </summary>
    public sealed class SubtractEvaluator : BinaryEvaluator
    {
        public SubtractEvaluator(Evaluator Left, BufferedEvaluator Right)
            : base(Left, Right)
        {

        }

        public override void Generate(Value Start, Value[] Buffer)
        {
            this.Left.Generate(Start, Buffer);
            Value[] right = this.Right.Generate(Start);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] -= right[t];
            }
        }
    }

    /// <summary>
    /// An evaluator that multiplies two values together.
    /// </summary>
    public sealed class MultiplyEvaluator : BinaryEvaluator
    {
        public MultiplyEvaluator(Evaluator Left, BufferedEvaluator Right)
            : base(Left, Right)
        {

        }

        public override void Generate(Value Start, Value[] Buffer)
        {
            this.Left.Generate(Start, Buffer);
            Value[] right = this.Right.Generate(Start);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] *= right[t];
            }
        }
    }

    /// <summary>
    /// An evaluator that multiplies a constant value with a source evaluator.
    /// </summary>
    public sealed class MultiplyConstantEvaluator : UnaryEvaluator
    {
        public MultiplyConstantEvaluator(Evaluator Source, Value Amount)
            : base(Source)
        {
            this.Amount = Amount;
        }

        /// <summary>
        /// The amount that the source is multiplied by.
        /// </summary>
        public readonly Value Amount;

        public override void Generate(Value Start, Value[] Buffer)
        {
            this.Source.Generate(Start, Buffer);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] *= this.Amount;
            }
        }
    }

    /// <summary>
    /// An evaluator that divides one value by another.
    /// </summary>
    public sealed class DivideEvaluator : BinaryEvaluator
    {
        public DivideEvaluator(Evaluator Left, BufferedEvaluator Right)
            : base(Left, Right)
        {

        }

        public override void Generate(Value Start, Value[] Buffer)
        {
            this.Left.Generate(Start, Buffer);
            Value[] right = this.Right.Generate(Start);
            for (int t = 0; t < Buffer.Length; t++)
            {
                if (right[t] == 0)
                    Buffer[t] = 0;
                else
                    Buffer[t] /= right[t];
            }
        }
    }

    /// <summary>
    /// An evaluator that divides a source evaluator by a constant value.
    /// </summary>
    public sealed class DivideConstantEvaluator : UnaryEvaluator
    {
        public DivideConstantEvaluator(Evaluator Source, Value Amount)
            : base(Source)
        {
            this.Amount = Amount;
        }

        /// <summary>
        /// The amount that the source is divided by.
        /// </summary>
        public readonly Value Amount;

        public override void Generate(Value Start, Value[] Buffer)
        {
            this.Source.Generate(Start, Buffer);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] /= this.Amount;
            }
        }
    }

    /// <summary>
    /// An evaluator that finds the remainder of one value divided by another.
    /// </summary>
    public sealed class ModulusEvaluator : BinaryEvaluator
    {
        public ModulusEvaluator(Evaluator Left, BufferedEvaluator Right)
            : base(Left, Right)
        {

        }

        public override void Generate(Value Start, Value[] Buffer)
        {
            this.Left.Generate(Start, Buffer);
            Value[] right = this.Right.Generate(Start);
            unchecked
            {
                for (int t = 0; t < Buffer.Length; t++)
                {
                    if (right[t] == 0)
                        Buffer[t] = 0;
                    else
                        Buffer[t] %= right[t];
                }
            }
        }
    }

    /// <summary>
    /// An evaluator that finds the remainder of one value divided by a constant.
    /// </summary>
    public sealed class ModulusConstantEvaluator : UnaryEvaluator
    {
        public ModulusConstantEvaluator(Evaluator Source, Value Amount)
            : base(Source)
        {
            this.Amount = Amount;
        }

        /// <summary>
        /// The base of the modulus operation.
        /// </summary>
        public readonly Value Amount;

        public override void Generate(Value Start, Value[] Buffer)
        {
            this.Source.Generate(Start, Buffer);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] %= this.Amount;
            }
        }
    }

    /// <summary>
    /// An evaluator that finds the bitwise or of two values.
    /// </summary>
    public sealed class OrEvaluator : BinaryEvaluator
    {
        public OrEvaluator(Evaluator Left, BufferedEvaluator Right)
            : base(Left, Right)
        {

        }

        public override void Generate(Value Start, Value[] Buffer)
        {
            this.Left.Generate(Start, Buffer);
            Value[] right = this.Right.Generate(Start);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] |= right[t];
            }
        }
    }

    /// <summary>
    /// An evaluator that finds the bitwise and of two values.
    /// </summary>
    public sealed class AndEvaluator : BinaryEvaluator
    {
        public AndEvaluator(Evaluator Left, BufferedEvaluator Right)
            : base(Left, Right)
        {

        }

        public override void Generate(Value Start, Value[] Buffer)
        {
            this.Left.Generate(Start, Buffer);
            Value[] right = this.Right.Generate(Start);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] &= right[t];
            }
        }
    }

    /// <summary>
    /// An evaluator that finds the bitwise xor of two values.
    /// </summary>
    public sealed class XorEvaluator : BinaryEvaluator
    {
        public XorEvaluator(Evaluator Left, BufferedEvaluator Right)
            : base(Left, Right)
        {

        }

        public override void Generate(Value Start, Value[] Buffer)
        {
            this.Left.Generate(Start, Buffer);
            Value[] right = this.Right.Generate(Start);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] ^= right[t];
            }
        }
    }

    /// <summary>
    /// An evaluator that finds a value left-shifted by another.
    /// </summary>
    public sealed class LeftShiftEvaluator : BinaryEvaluator
    {
        public LeftShiftEvaluator(Evaluator Left, BufferedEvaluator Right)
            : base(Left, Right)
        {

        }

        public override void Generate(Value Start, Value[] Buffer)
        {
            this.Left.Generate(Start, Buffer);
            Value[] right = this.Right.Generate(Start);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] <<= (int)right[t];
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

        public override void Generate(Value Start, Value[] Buffer)
        {
            this.Source.Generate(Start, Buffer);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] <<= this.Amount;
            }
        }
    }

    /// <summary>
    /// An evaluator that finds a value right-shifted by another.
    /// </summary>
    public sealed class RightShiftEvaluator : BinaryEvaluator
    {
        public RightShiftEvaluator(Evaluator Left, BufferedEvaluator Right)
            : base(Left, Right)
        {

        }

        public override void Generate(Value Start, Value[] Buffer)
        {
            this.Left.Generate(Start, Buffer);
            Value[] right = this.Right.Generate(Start);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] >>= (int)right[t];
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

        public override void Generate(Value Start, Value[] Buffer)
        {
            this.Source.Generate(Start, Buffer);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] >>= this.Amount;
            }
        }
    }

    /// <summary>
    /// An evaluator that negates the source.
    /// </summary>
    public sealed class NegateEvaluator : UnaryEvaluator
    {
        public NegateEvaluator(Evaluator Source)
            : base(Source)
        {

        }

        public override void Generate(Value Start, Value[] Buffer)
        {
            this.Source.Generate(Start, Buffer);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] = -Buffer[t];
            }
        }
    }

    /// <summary>
    /// An evaluator that complements the source.
    /// </summary>
    public sealed class ComplementEvaluator : UnaryEvaluator
    {
        public ComplementEvaluator(Evaluator Source)
            : base(Source)
        {

        }

        public override void Generate(Value Start, Value[] Buffer)
        {
            this.Source.Generate(Start, Buffer);
            for (int t = 0; t < Buffer.Length; t++)
            {
                Buffer[t] = ~Buffer[t];
            }
        }
    }

    /// <summary>
    /// An evaluator for a generator function.
    /// </summary>
    public abstract class GeneratorEvaluator : UnaryEvaluator
    {
        public GeneratorEvaluator(Evaluator Source, double Period, double Scale)
            : base(Source)
        {
            this.Period = Period;
            this.Scale = Scale;
        }

        /// <summary>
        /// The length of a period for this generator.
        /// </summary>
        public readonly double Period;

        /// <summary>
        /// The amount the output value is scaled by.
        /// </summary>
        public readonly double Scale;
    }

    /// <summary>
    /// An evaluator for a saw generator.
    /// </summary>
    public sealed class SawEvaluator : GeneratorEvaluator
    {
        public SawEvaluator(Evaluator Source, double Period, double Scale)
            : base(Source, Period, Scale)
        {

        }

        public override void Generate(Value Start, Value[] Buffer)
        {
            this.Source.Generate(Start, Buffer);
            for (int t = 0; t < Buffer.Length; t++)
            {
                double input = ((Buffer[t] / this.Period) % 1.0 + 1.0) % 1.0;
                double output = input * 2.0 - 1.0;
                Buffer[t] = (Value)(output * Scale);
            }
        }
    }

    /// <summary>
    /// An evaluator for a sine generator.
    /// </summary>
    public sealed class SineEvaluator : GeneratorEvaluator
    {
        public SineEvaluator(Evaluator Source, double Period, double Scale)
            : base(Source, Period, Scale)
        {

        }

        public override void Generate(Value Start, Value[] Buffer)
        {
            this.Source.Generate(Start, Buffer);
            for (int t = 0; t < Buffer.Length; t++)
            {
                double input = Buffer[t] / this.Period;
                double output = Math.Sin(input * 2.0 * Math.PI);
                Buffer[t] = (Value)(output * Scale);
            }
        }
    }

    /// <summary>
    /// An evaluator for a square generator.
    /// </summary>
    public sealed class SquareEvaluator : GeneratorEvaluator
    {
        public SquareEvaluator(Evaluator Source, double Period, double Scale)
            : base(Source, Period, Scale)
        {

        }

        public override void Generate(Value Start, Value[] Buffer)
        {
            this.Source.Generate(Start, Buffer);
            for (int t = 0; t < Buffer.Length; t++)
            {
                double input = ((Buffer[t] / this.Period) % 1.0 + 1.0) % 1.0;
                double output = input > 0.5 ? 1.0 : -1.0;
                Buffer[t] = (Value)(output * Scale);
            }
        }
    }

    /// <summary>
    /// An evaluator for a triangle generator.
    /// </summary>
    public sealed class TriangleEvaluator : GeneratorEvaluator
    {
        public TriangleEvaluator(Evaluator Source, double Period, double Scale)
            : base(Source, Period, Scale)
        {

        }

        public override void Generate(Value Start, Value[] Buffer)
        {
            this.Source.Generate(Start, Buffer);
            for (int t = 0; t < Buffer.Length; t++)
            {
                double input = ((Buffer[t] / this.Period) % 1.0 + 1.0) % 1.0;
                double output = input < 0.5 ? input * 4.0 - 1.0 : input * -4.0 + 3.0;
                Buffer[t] = (Value)(output * Scale);
            }
        }
    }

    /// <summary>
    /// An evaluator for a sequencer.
    /// </summary>
    public sealed class SequencerEvaluator : Evaluator
    {
        public SequencerEvaluator(BufferedEvaluator[] Items, Evaluator Parameter)
        {
            this.Items = Items;
            this.Parameter = Parameter;
        }

        /// <summary>
        /// The evaluator for the items of the sequence.
        /// </summary>
        public readonly BufferedEvaluator[] Items;

        /// <summary>
        /// The evaluator for the parameter of the sequence.
        /// </summary>
        public readonly Evaluator Parameter;

        public override void Invalidate()
        {
            base.Invalidate();
            this.Parameter.Invalidate();
            for (int t = 0; t < this.Items.Length; t++)
            {
                this.Items[t].Invalidate();
            }
        }

        public override void Generate(Value Start, Value[] Buffer)
        {
            this.Parameter.Generate(Start, Buffer);
            for (int t = 0; t < Buffer.Length; t++)
            {
                uint param = (uint)((UValue)Buffer[t] % (UValue)this.Items.Length);
                Buffer[t] = this.Items[param].Generate(Start)[t];
            }
        }
    }

    /// <summary>
    /// A sequencer evaluator for items that are constants.
    /// </summary>
    public sealed class SequencerConstantEvaluator : Evaluator
    {
        public SequencerConstantEvaluator(Value[] Items, Evaluator Parameter)
        {
            this.Items = Items;
            this.Parameter = Parameter;
        }

        /// <summary>
        /// The items of the sequence.
        /// </summary>
        public readonly Value[] Items;

        /// <summary>
        /// The evaluator for the parameter of the sequence.
        /// </summary>
        public readonly Evaluator Parameter;

        public override void Invalidate()
        {
            base.Invalidate();
            this.Parameter.Invalidate();
        }

        public override void Generate(Value Start, Value[] Buffer)
        {
            this.Parameter.Generate(Start, Buffer);
            for (int t = 0; t < Buffer.Length; t++)
            {
                uint param = (uint)((UValue)Buffer[t] % (UValue)this.Items.Length);
                Buffer[t] = this.Items[param];
            }
        }
    }
}