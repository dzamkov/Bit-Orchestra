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
                        return new ConstantEvaluator(BufferSize, leftval << (int)rightval);
                    if (constleft)
                        return new LeftShiftEvaluator(BufferSize, lefteval, righteval);
                    if (constright)
                        return new LeftShiftConstantEvaluator(BufferSize, lefteval, (int)rightval);
                    return new LeftShiftEvaluator(BufferSize, lefteval, righteval);

                case BinaryOperation.RightShift:
                    if (constleft && constright)
                        return new ConstantEvaluator(BufferSize, leftval >> (int)rightval);
                    if (constleft)
                        return new RightShiftEvaluator(BufferSize, lefteval, righteval);
                    if (constright)
                        return new RightShiftConstantEvaluator(BufferSize, lefteval, (int)rightval);
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
                        return new ConstantEvaluator(BufferSize, -srcval);
                    return new NegateEvaluator(BufferSize, srceval);
                case UnaryOperation.Complement:
                    if (constsrc)
                        return new ConstantEvaluator(BufferSize, ~srcval);
                    return new ComplementEvaluator(BufferSize, srceval);
                case UnaryOperation.Saw:
                    return new SawEvaluator(BufferSize, srceval, period, scale);
                case UnaryOperation.Sine:
                    return new SineEvaluator(BufferSize, srceval, period, scale);
                case UnaryOperation.Square:
                    return new SquareEvaluator(BufferSize, srceval, period, scale);
                case UnaryOperation.Triangle:
                    return new TriangleEvaluator(BufferSize, srceval, period, scale);
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

            Evaluator[] items = new Evaluator[this.Items.Count];
            Value[] constitems = new Value[this.Items.Count];
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