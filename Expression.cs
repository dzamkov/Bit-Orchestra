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
        /// Gets an evaluator for this expression, checking first if it is in the cache.
        /// </summary>
        public Evaluator GetEvaluator(Dictionary<Expression, Evaluator> Cache, int BufferSize, int Resolution)
        {
            Evaluator eval;
            if (!Cache.TryGetValue(this, out eval))
            {
                eval = this.CreateEvaluator(Cache, BufferSize, Resolution);
                Cache[this] = eval;
            }
            return eval;
        }

        /// <summary>
        /// Gets an evaluator for this expression.
        /// </summary>
        protected abstract Evaluator CreateEvaluator(Dictionary<Expression, Evaluator> Cache, int BufferSize, int Resolution);
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

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;

            ConstantExpression ce = obj as ConstantExpression;
            return
                ce != null &&
                this.Value == ce.Value;
        }

        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
        }

        protected override Evaluator CreateEvaluator(Dictionary<Expression,Evaluator> Cache, int BufferSize, int Resolution)
        {
            return new ConstantEvaluator(BufferSize, this.Value);
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

        public override bool Equals(object obj)
        {
            return obj == Instance;    
        }

        public override int GetHashCode()
        {
            return 0x61C459A1;
        }

        protected override Evaluator CreateEvaluator(Dictionary<Expression,Evaluator> Cache, int BufferSize, int Resolution)
        {
            return new IdentityEvaluator(BufferSize);
        }
    }

    /// <summary>
    /// An expression that returns the current resolution.
    /// </summary>
    public sealed class ResolutionExpression : Expression
    {
        private ResolutionExpression()
        {

        }

        /// <summary>
        /// The only instance of this class.
        /// </summary>
        public static readonly ResolutionExpression Instance = new ResolutionExpression();

        public override bool Equals(object obj)
        {
            return obj == Instance;
        }

        public override int GetHashCode()
        {
            return 0x363AD79B;
        }

        protected override Evaluator CreateEvaluator(Dictionary<Expression, Evaluator> Cache, int BufferSize, int Resolution)
        {
            return new ConstantEvaluator(BufferSize, Resolution);
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

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;

            BinaryExpression be = obj as BinaryExpression;
            return
                be != null &&
                this.Operation == be.Operation &&
                be.Left.Equals(this.Left) &&
                be.Right.Equals(this.Right);
        }

        public override int GetHashCode()
        {
            int hash = 0x73AB95F3;
            hash += this.Left.GetHashCode();
            hash += (hash << 3) ^ (hash >> 3);
            hash += this.Right.GetHashCode();
            hash += (hash << 7) ^ (hash >> 7);
            hash += this.Operation.GetHashCode();
            hash += (hash << 11) ^ (hash >> 1);
            return hash;
        }

        protected override Evaluator CreateEvaluator(Dictionary<Expression,Evaluator> Cache, int BufferSize, int Resolution)
        {
            Evaluator lefteval = this.Left.GetEvaluator(Cache, BufferSize, Resolution);
            Evaluator righteval = this.Right.GetEvaluator(Cache, BufferSize, Resolution);

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
                        return new ConstantEvaluator(BufferSize, leftval + rightval);
                    if (constleft)
                        return new AddConstantEvaluator(BufferSize, righteval, leftval);
                    if (constright)
                        return new AddConstantEvaluator(BufferSize, lefteval, rightval);
                    return new AddEvaluator(BufferSize, lefteval, righteval);

                case BinaryOperation.Subtract:
                    if (constleft && constright)
                        return new ConstantEvaluator(BufferSize, leftval - rightval);
                    if (constleft)
                        return new AddConstantEvaluator(BufferSize, righteval, -leftval);
                    if (constright)
                        return new AddConstantEvaluator(BufferSize, lefteval, -rightval);
                    return new SubtractEvaluator(BufferSize, lefteval, righteval);

                case BinaryOperation.Multiply:
                    if (constleft && constright)
                        return new ConstantEvaluator(BufferSize, leftval * rightval);
                    if (constleft)
                        return new MultiplyConstantEvaluator(BufferSize, righteval, leftval);
                    if (constright)
                        return new MultiplyConstantEvaluator(BufferSize, lefteval, rightval);
                    return new MultiplyEvaluator(BufferSize, lefteval, righteval);

                case BinaryOperation.Divide:
                    if (constleft && constright)
                        return new ConstantEvaluator(BufferSize, leftval / rightval);
                    if (constleft)
                        return new DivideEvaluator(BufferSize, lefteval, righteval);
                    if (constright)
                        return new DivideConstantEvaluator(BufferSize, lefteval, rightval);
                    return new DivideEvaluator(BufferSize, lefteval, righteval);

                case BinaryOperation.Modulus:
                    if (constleft && constright)
                        return new ConstantEvaluator(BufferSize, leftval % rightval);
                    if (constleft)
                        return new ModulusEvaluator(BufferSize, lefteval, righteval);
                    if (constright)
                        return new ModulusConstantEvaluator(BufferSize, lefteval, rightval);
                    return new ModulusEvaluator(BufferSize, lefteval, righteval);

                case BinaryOperation.Or:
                    if (constleft && constright)
                        return new ConstantEvaluator(BufferSize, leftval | rightval);
                    if (constleft)
                        return new OrEvaluator(BufferSize, lefteval, righteval);
                    if (constright)
                        return new OrEvaluator(BufferSize, lefteval, righteval);
                    return new OrEvaluator(BufferSize, lefteval, righteval);
                    
                case BinaryOperation.And:
                    if (constleft && constright)
                        return new ConstantEvaluator(BufferSize, leftval & rightval);
                    if (constleft)
                        return new AndEvaluator(BufferSize, lefteval, righteval);
                    if (constright)
                        return new AndEvaluator(BufferSize, lefteval, righteval);
                    return new AndEvaluator(BufferSize, lefteval, righteval);

                case BinaryOperation.Xor:
                    if (constleft && constright)
                        return new ConstantEvaluator(BufferSize, leftval ^ rightval);
                    if (constleft)
                        return new XorEvaluator(BufferSize, lefteval, righteval);
                    if (constright)
                        return new XorEvaluator(BufferSize, lefteval, righteval);
                    return new XorEvaluator(BufferSize, lefteval, righteval);

                case BinaryOperation.LeftShift:
                    if (constleft && constright)
                        return new ConstantEvaluator(BufferSize, leftval << rightval);
                    if (constleft)
                        return new LeftShiftEvaluator(BufferSize, lefteval, righteval);
                    if (constright)
                        return new LeftShiftConstantEvaluator(BufferSize, lefteval, rightval);
                    return new LeftShiftEvaluator(BufferSize, lefteval, righteval);

                case BinaryOperation.RightShift:
                    if (constleft && constright)
                        return new ConstantEvaluator(BufferSize, leftval >> rightval);
                    if (constleft)
                        return new RightShiftEvaluator(BufferSize, lefteval, righteval);
                    if (constright)
                        return new RightShiftConstantEvaluator(BufferSize, lefteval, rightval);
                    return new RightShiftEvaluator(BufferSize, lefteval, righteval);

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

    /// <summary>
    /// An expression that accesses values from a sequence.
    /// </summary>
    public sealed class SequencerExpression : Expression
    {
        public SequencerExpression(List<Expression> Items, Expression Parameter)
        {
            this.Items = Items;
            this.Parameter = Parameter;
        }

        /// <summary>
        /// The items in the sequence.
        /// </summary>
        public readonly List<Expression> Items;

        /// <summary>
        /// The parameter used for lookup in the sequence.
        /// </summary>
        public readonly Expression Parameter;

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;

            SequencerExpression se = obj as SequencerExpression;
            if (se != null)
            {
                if (se.Items.Count != this.Items.Count)
                    return false;
                if (!se.Parameter.Equals(this.Parameter))
                    return false;
                for (int t = 0; t < this.Items.Count; t++)
                {
                    if (!se.Items[t].Equals(this.Items[t]))
                        return false;
                }
                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            int hash = 0x139F4BA3;
            hash += this.Parameter.GetHashCode();
            hash += (hash << 17) ^ (hash >> 17);
            for (int t = 0; t < this.Items.Count; t++)
            {
                hash += this.Items[t].GetHashCode();
                hash += (hash << 13) ^ (hash >> 13);
            }
            hash *= 0x253A35F1;
            return hash;
        }

        protected override Evaluator CreateEvaluator(Dictionary<Expression, Evaluator> Cache, int BufferSize, int Resolution)
        {
            ConstantEvaluator constparam = this.Parameter.GetEvaluator(Cache, BufferSize, Resolution) as ConstantEvaluator;
            if (constparam != null)
            {
                return this.Items[(int)((uint)constparam.Value % (uint)this.Items.Count)].GetEvaluator(Cache, BufferSize, Resolution);
            }

            Evaluator[] items = new Evaluator[this.Items.Count];
            int[] constitems = new int[this.Items.Count];
            bool constseq = true;
            for (int t = 0; t < items.Length; t++)
            {
                Evaluator itemeval = this.Items[t].GetEvaluator(Cache, BufferSize, Resolution);
                items[t] = itemeval;
                if (constseq)
                {
                    ConstantEvaluator constitem = itemeval as ConstantEvaluator;
                    if (constitem != null)
                    {
                        constitems[t] = constitem.Value;
                    }
                    else
                    {
                        constseq = false;
                    }
                }
            }

            if (constseq)
            {
                return new SequencerConstantEvaluator(BufferSize, constitems, this.Parameter.GetEvaluator(Cache, BufferSize, Resolution));
            }
            else
            {
                return new SequencerEvaluator(BufferSize, items, this.Parameter.GetEvaluator(Cache, BufferSize, Resolution));
            }
        }
    }
}