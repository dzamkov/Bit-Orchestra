using System;
using System.Collections.Generic;
using System.Linq;

using Value = System.Int32;

namespace BitOrchestra
{
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
}