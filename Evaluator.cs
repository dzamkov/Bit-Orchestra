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
}