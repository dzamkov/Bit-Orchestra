using System;
using System.Collections.Generic;
using System.Linq;

namespace BitOrchestra
{
    /// <summary>
    /// Represents an expression that can produce integer values from an integer parameter.
    /// </summary>
    public abstract class Expression
    {
        /// <summary>
        /// Gets an evaluator for this expression.
        /// </summary>
        public abstract Evaluator GetEvaluator(int BufferSize);
    }

    /// <summary>
    /// An expression with a constant value.
    /// </summary>
    public sealed class ConstantExpression : Expression
    {
        public ConstantExpression(int Value)
        {
            this.Value = Value;
        }

        /// <summary>
        /// The value of this expression.
        /// </summary>
        public readonly int Value;

        public override Evaluator GetEvaluator(int BufferSize)
        {
            return new ConstantEvaluator(this.Value);
        }
    }

    /// <summary>
    /// An expression that returns the value of the parameter.
    /// </summary>
    public sealed class IdentityExpression : Expression
    {
        private IdentityExpression()
        {

        }

        /// <summary>
        /// The only instance of this class.
        /// </summary>
        public static readonly IdentityExpression Instance = new IdentityExpression();

        public override Evaluator GetEvaluator(int BufferSize)
        {
            return IdentityEvaluator.Instance;
        }
    }

    /// <summary>
    /// An expression that relates two component expressions.
    /// </summary>
    public sealed class BinaryExpression : Expression
    {
        public BinaryExpression(Expression Left, Expression Right, BinaryOperation Operation)
        {
            this.Left = Left;
            this.Right = Right;
            this.Operation = Operation;
        }

        /// <summary>
        /// The expression on the left of this binary expression.
        /// </summary>
        public readonly Expression Left;

        /// <summary>
        /// The expression on the right of this binary expression.
        /// </summary>
        public readonly Expression Right;

        /// <summary>
        /// The operation used by this expression.
        /// </summary>
        public readonly BinaryOperation Operation;

        public override Evaluator GetEvaluator(int BufferSize)
        {
            Evaluator lefteval = this.Left.GetEvaluator(BufferSize);
            Evaluator righteval = this.Right.GetEvaluator(BufferSize);

            bool constleft = false;
            int leftval = 0;

            bool constright = false;
            int rightval = 0;

            ConstantEvaluator consteval = lefteval as ConstantEvaluator;
            if (consteval != null)
            {
                constleft = true;
                leftval = consteval.Value;
            }

            consteval = righteval as ConstantEvaluator;
            if (consteval != null)
            {
                constright = true;
                rightval = consteval.Value;
            }

            switch (this.Operation)
            {
                case BinaryOperation.Add:
                    if (constleft && constright)
                        return new ConstantEvaluator(leftval + rightval);
                    if (constleft)
                        return new AddConstantEvaluator(righteval, leftval);
                    if (constright)
                        return new AddConstantEvaluator(lefteval, rightval);
                    return new AddEvaluator(BufferSize, lefteval, righteval);
                case BinaryOperation.Multiply:
                    if (constleft && constright)
                        return new ConstantEvaluator(leftval * rightval);
                    if (constleft)
                        return new MultiplyConstantEvaluator(righteval, leftval);
                    if (constright)
                        return new MultiplyConstantEvaluator(lefteval, rightval);
                    return new MultiplyEvaluator(BufferSize, lefteval, righteval);
                default:
                    throw new NotImplementedException();
            }
        }
    }

    /// <summary>
    /// Identifies a possible binary operation.
    /// </summary>
    public enum BinaryOperation
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulus,
        Or,
        And,
        Xor,
        LeftShift,
        RightShift
    }
}