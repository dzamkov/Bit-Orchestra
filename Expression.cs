using System;
using System.Collections.Generic;
using System.Linq;

using Value = System.Int32;

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
            if (Cache.TryGetValue(this, out eval))
            {
                // Make sure the cached version is buffered, so its results can be reused.
                if (!(eval is ConstantEvaluator) && !(eval is IdentityEvaluator))
                {
                    BufferedEvaluator beval = eval.GetBuffered(BufferSize);
                    if (eval != beval)
                    {
                        eval = beval;
                        Cache[this] = beval;
                    }
                }
            }
            else
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
        public ConstantExpression(Value Value)
        {
            this.Value = Value;
        }

        /// <summary>
        /// The value of this expression.
        /// </summary>
        public readonly Value Value;

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
            return IdentityEvaluator.Instance;
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
            return new ConstantEvaluator(Resolution);
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
            Value leftval = 0;

            bool constright = false;
            Value rightval = 0;

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
                    return new AddEvaluator(lefteval, righteval.GetBuffered(BufferSize));

                case BinaryOperation.Subtract:
                    if (constleft && constright)
                        return new ConstantEvaluator(leftval - rightval);
                    if (constleft)
                        return new SubtractEvaluator(lefteval, righteval.GetBuffered(BufferSize));
                    if (constright)
                        return new AddConstantEvaluator(lefteval, -rightval);
                    return new SubtractEvaluator(lefteval, righteval.GetBuffered(BufferSize));

                case BinaryOperation.Multiply:
                    if (constleft && constright)
                        return new ConstantEvaluator(leftval * rightval);
                    if (constleft)
                        return new MultiplyConstantEvaluator(righteval, leftval);
                    if (constright)
                        return new MultiplyConstantEvaluator(lefteval, rightval);
                    return new MultiplyEvaluator(lefteval, righteval.GetBuffered(BufferSize));

                case BinaryOperation.Divide:
                    if (constleft && constright)
                    {
                        if (rightval == 0)
                            return new ConstantEvaluator(0);
                        else
                            return new ConstantEvaluator(leftval / rightval);
                    }
                    if (constleft)
                        return new DivideEvaluator(lefteval, righteval.GetBuffered(BufferSize));
                    if (constright)
                        return new DivideConstantEvaluator(lefteval, rightval);
                    return new DivideEvaluator(lefteval, righteval.GetBuffered(BufferSize));

                case BinaryOperation.Modulus:
                    if (constleft && constright)
                    {
                        if (rightval == 0)
                            return new ConstantEvaluator(0);
                        else
                            return new ConstantEvaluator(leftval % rightval);
                    }
                    if (constleft)
                        return new ModulusEvaluator(lefteval, righteval.GetBuffered(BufferSize));
                    if (constright)
                        return new ModulusConstantEvaluator(lefteval, rightval);
                    return new ModulusEvaluator(lefteval, righteval.GetBuffered(BufferSize));

                case BinaryOperation.Or:
                    if (constleft && constright)
                        return new ConstantEvaluator(leftval | rightval);
                    if (constleft)
                        return new OrEvaluator(lefteval, righteval.GetBuffered(BufferSize));
                    if (constright)
                        return new OrEvaluator(lefteval, righteval.GetBuffered(BufferSize));
                    return new OrEvaluator(lefteval, righteval.GetBuffered(BufferSize));
                    
                case BinaryOperation.And:
                    if (constleft && constright)
                        return new ConstantEvaluator(leftval & rightval);
                    if (constleft)
                        return new AndEvaluator(lefteval, righteval.GetBuffered(BufferSize));
                    if (constright)
                        return new AndEvaluator(lefteval, righteval.GetBuffered(BufferSize));
                    return new AndEvaluator(lefteval, righteval.GetBuffered(BufferSize));

                case BinaryOperation.Xor:
                    if (constleft && constright)
                        return new ConstantEvaluator(leftval ^ rightval);
                    if (constleft)
                        return new XorEvaluator(lefteval, righteval.GetBuffered(BufferSize));
                    if (constright)
                        return new XorEvaluator(lefteval, righteval.GetBuffered(BufferSize));
                    return new XorEvaluator(lefteval, righteval.GetBuffered(BufferSize));

                case BinaryOperation.LeftShift:
                    if (constleft && constright)
                        return new ConstantEvaluator(leftval << (int)rightval);
                    if (constleft)
                        return new LeftShiftEvaluator(lefteval, righteval.GetBuffered(BufferSize));
                    if (constright)
                        return new LeftShiftConstantEvaluator(lefteval, (int)rightval);
                    return new LeftShiftEvaluator(lefteval, righteval.GetBuffered(BufferSize));

                case BinaryOperation.RightShift:
                    if (constleft && constright)
                        return new ConstantEvaluator(leftval >> (int)rightval);
                    if (constleft)
                        return new RightShiftEvaluator(lefteval, righteval.GetBuffered(BufferSize));
                    if (constright)
                        return new RightShiftConstantEvaluator(lefteval, (int)rightval);
                    return new RightShiftEvaluator(lefteval, righteval.GetBuffered(BufferSize));

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
    /// Performs an operation on a source expression.
    /// </summary>
    public sealed class UnaryExpression : Expression
    {
        public UnaryExpression(Expression Source, UnaryOperation Operation)
        {
            this.Source = Source;
            this.Operation = Operation;
        }

        /// <summary>
        /// The source expression for this unary expression.
        /// </summary>
        public readonly Expression Source;

        /// <summary>
        /// The operation performed by this expression.
        /// </summary>
        public readonly UnaryOperation Operation;

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;

            UnaryExpression ue = obj as UnaryExpression;
            return
                ue != null &&
                this.Operation == ue.Operation &&
                ue.Source.Equals(this.Source);
        }

        public override int GetHashCode()
        {
            int hash = 0x539ACC23;
            hash += this.Source.GetHashCode();
            hash += (hash << 5) ^ (hash >> 5);
            hash += this.Operation.GetHashCode();
            hash += (hash << 3) ^ (hash >> 3);
            return hash;
        }

        protected override Evaluator CreateEvaluator(Dictionary<Expression, Evaluator> Cache, int BufferSize, int Resolution)
        {
            Evaluator srceval = this.Source.GetEvaluator(Cache, BufferSize, Resolution);

            bool constsrc = false;
            Value srcval = 0;

            ConstantEvaluator consteval = srceval as ConstantEvaluator;
            if (consteval != null)
            {
                constsrc = true;
                srcval = consteval.Value;
            }

            int resmag = 2 << (Resolution - 1);
            double period = resmag;
            double scale = (resmag - 3) * 0.5;

            switch (this.Operation)
            {
                case UnaryOperation.Negate:
                    if (constsrc)
                        return new ConstantEvaluator(-srcval);
                    return new NegateEvaluator(srceval);
                case UnaryOperation.Complement:
                    if (constsrc)
                        return new ConstantEvaluator(~srcval);
                    return new ComplementEvaluator(srceval);
                case UnaryOperation.Saw:
                    return new SawEvaluator(srceval, period, scale);
                case UnaryOperation.Sine:
                    return new SineEvaluator(srceval, period, scale);
                case UnaryOperation.Square:
                    return new SquareEvaluator(srceval, period, scale);
                case UnaryOperation.Triangle:
                    return new TriangleEvaluator(srceval, period, scale);
                default:
                    throw new NotImplementedException();
            }
        }
    }

    /// <summary>
    /// Identifies a possible unary operation.
    /// </summary>
    public enum UnaryOperation
    {
        Negate,
        Complement,
        Saw,
        Sine,
        Square,
        Triangle,
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

            BufferedEvaluator[] items = new BufferedEvaluator[this.Items.Count];
            Value[] constitems = new Value[this.Items.Count];
            bool constseq = true;
            for (int t = 0; t < items.Length; t++)
            {
                Evaluator itemeval = this.Items[t].GetEvaluator(Cache, BufferSize, Resolution);
                items[t] = itemeval.GetBuffered(BufferSize);
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
                return new SequencerConstantEvaluator(constitems, this.Parameter.GetEvaluator(Cache, BufferSize, Resolution));
            }
            else
            {
                return new SequencerEvaluator(items, this.Parameter.GetEvaluator(Cache, BufferSize, Resolution));
            }
        }
    }
}