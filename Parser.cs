using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace BitOrchestra
{
    /// <summary>
    /// Contains parser-related functions.
    /// </summary>
    public static class Parser
    {
        /// <summary>
        /// Parses the given text and either returns true with an expression and sound options, or false with an error index.
        /// </summary>
        public static bool Parse(string Text, out Expression Expression, out SoundOptions Options, out int ErrorIndex)
        {
            Expression = null;
            Options = new SoundOptions();

            int index = 0;
            AcceptExtendedWhitespace(Text, ref index);

            // Parse options
            string optname = null;
            int optvalue = 0;
            while (AcceptOption(Text, ref index, ref optname, ref optvalue, out ErrorIndex))
            {
                switch (optname)
                {
                    case "rate":
                        Options.Rate = optvalue;
                        break;
                    case "offset":
                        Options.Offset = optvalue;
                        break;
                    case "length":
                        Options.Length = optvalue;
                        break;
                    default:
                        break;
                }

                AcceptExtendedWhitespace(Text, ref index);
            }
            
            if (AcceptExpression(Text, ref index, ref Expression, out ErrorIndex))
            {
                ErrorIndex = index;
                AcceptExtendedWhitespace(Text, ref index);
                if (ErrorIndex == Text.Length)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Tries parsing the target string in the given text.
        /// </summary>
        public static bool AcceptString(string Target, string Text, ref int Index)
        {
            if (Text.Length - Index < Target.Length)
                return false;
            for (int t = 0; t < Target.Length; t++)
            {
                if (Target[t] != Text[Index + t])
                    return false;
            }
            Index += Target.Length;
            return true;
        }

        /// <summary>
        /// Tries parsing a newline in the given text.
        /// </summary>
        public static bool AcceptNewline(string Text, ref int Index)
        {
            return AcceptString("\r\n", Text, ref Index);
        }

        /// <summary>
        /// Tries parsing whitespace (tabs and spaces) in the given text.
        /// </summary>
        public static bool AcceptWhitespace(string Text, ref int Index)
        {
            bool found = false;
            while (Index < Text.Length)
            {
                char c = Text[Index];
                if (c == ' ' || c == '\t')
                {
                    found = true;
                    Index++;
                    continue;
                }
                break;
            }
            return found;
        }

        /// <summary>
        /// Tries parsing an option.
        /// </summary>
        public static bool AcceptOption(string Text, ref int Index, ref string Name, ref int Value, out int ErrorIndex)
        {
            ErrorIndex = Index;
            int cur = Index;

            if (AcceptString("#", Text, ref cur))
            {
                ErrorIndex = Index;
                if (AcceptWord(Text, ref cur, ref Name))
                {
                    AcceptWhitespace(Text, ref cur);
                    if (AcceptInteger(Text, ref cur, ref Value, out ErrorIndex))
                    {
                        Index = cur;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Tries parsing a comment in the given text.
        /// </summary>
        public static bool AcceptComment(string Text, ref int Index, ref bool Multiline)
        {
            if (AcceptString("//", Text, ref Index))
            {
                Multiline = false;
                while (Index < Text.Length)
                {
                    char c = Text[Index];
                    switch (c)
                    {
                        case '\r':
                        case '\n':
                            return true;
                        default:
                            Index++;
                            continue;
                    }
                }
                return true;
            }

            if (AcceptString("/*", Text, ref Index))
            {
                Multiline = true;
                while (Index < Text.Length)
                {
                    char c = Text[Index];
                    switch (c)
                    {
                        case '*':
                            if (AcceptString("*/", Text, ref Index))
                                return true;
                            break;
                        default:
                            Index++;
                            continue;
                    }
                }
                return true;
            }


            return false;
        }

        /// <summary>
        /// Tries parsing extended whitespace (whitespace, comments, newlines) in the given text.
        /// </summary>
        public static bool AcceptExtendedWhitespace(string Text, ref int Index)
        {
            bool found = false;
            while (AcceptWhitespace(Text, ref Index))
            {
                found = true;
                bool multiline = false;
                if (AcceptComment(Text, ref Index, ref multiline) && multiline)
                    continue;
                break;
            }

            while (AcceptNewline(Text, ref Index))
            {
                bool multiline = false;
                found = true;
                AcceptWhitespace(Text, ref Index);
                while (AcceptComment(Text, ref Index, ref multiline) && multiline)
                {
                    AcceptWhitespace(Text, ref Index);
                }
            }

            return found;
        }

        /// <summary>
        /// Determines wether the given character is a decimal digit, if so, returns its value.
        /// </summary>
        public static bool IsDecimalDigit(char Char, ref int Value)
        {
            int i = (int)Char;
            if (i >= 48 && i <= 57)
            {
                Value = i - 48;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines wether the given character is a hexadecimal digit, if so, returns its value.
        /// </summary>
        public static bool IsHexadecimalDigit(char Char, ref int Value)
        {
            int i = (int)Char;
            if (i >= 48 && i <= 57)
            {
                Value = i - 48;
                return true;
            }
            if (i >= 65 && i <= 70)
            {
                Value = i - 65 + 10;
                return true;
            }
            if (i >= 97 && i <= 102)
            {
                Value = i - 97 + 10;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines wether the given character is valid in a word. Note that digits are not valid as the first character in a word.
        /// </summary>
        public static bool IsWordChar(char Char)
        {
            int i = (int)Char;
            if (i >= 95 && i <= 122) return true; // _ ` Lowercases
            if (i >= 65 && i <= 90) return true; // Uppercases
            if (i >= 48 && i <= 57) return true; // Digits
            return false;
        }

        /// <summary>
        /// Tries parsing a word in the given text.
        /// </summary>
        public static bool AcceptWord(string Text, ref int Index, ref string Word)
        {
            int cur = Index;
            while (cur < Text.Length)
            {
                char c = Text[cur];
                if (cur == Index)
                {
                    int dummy = 0;
                    if (Parser.IsDecimalDigit(c, ref dummy) || !Parser.IsWordChar(c))
                        break;
                }
                else
                {
                    if (!Parser.IsWordChar(c))
                        break;
                }
                cur++;
            }
            if (cur > Index)
            {
                Word = Text.Substring(Index, cur - Index);
                Index = cur;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Tries parsing an operator in the given text.
        /// </summary>
        public static bool AcceptOperator(string Text, ref int Index, ref Operator Operator)
        {
            bool found = false;
            int sindex = Index;
            int maxlen = Math.Min(Operator.MaxOperatorLength, Text.Length - Index);
            for (int t = 1; t <= maxlen; t++)
            {
                string optext = Text.Substring(sindex, t);
                Operator op;
                if (Operator.Map.TryGetValue(optext, out op))
                {
                    Index = sindex + t;
                    Operator = op;
                    found = true;
                }
            }
            return found;
        }

        /// <summary>
        /// Tries parsing an expression in the given text.
        /// </summary>
        public static bool AcceptExpression(string Text, ref int Index, ref Expression Expression, out int ErrorIndex)
        {
            ErrorIndex = Index;
            if (AcceptTerm(Text, ref Index, ref Expression, out ErrorIndex))
            {
                _OpTree curtree = new _OpTree(Expression);
                int cur = Index;
                while (true)
                {
                    AcceptExtendedWhitespace(Text, ref cur);
                    Operator op = null;
                    if (AcceptOperator(Text, ref cur, ref op))
                    {
                        AcceptExtendedWhitespace(Text, ref cur);
                        if (AcceptTerm(Text, ref cur, ref Expression, out ErrorIndex))
                        {
                            curtree = _OpTree.Combine(curtree, op, Expression);
                            Index = cur;
                            continue;
                        }
                        return false;
                    }
                    break;
                }

                Expression = curtree.Expression;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Represents a tree of operators and terms such that operators follow rules of precedence and associativity.
        /// </summary>
        private class _OpTree
        {
            public _OpTree(Expression Term)
            {
                this.Term = Term;
            }

            public _OpTree(Operator Operator, _OpTree Left, _OpTree Right)
            {
                this.Operator = Operator;
                this.Left = Left;
                this.Right = Right;
            }

            /// <summary>
            /// The value of this term, if applicable.
            /// </summary>
            public Expression Term;

            /// <summary>
            /// The operator of this optree, if applicable.
            /// </summary>
            public Operator Operator;

            /// <summary>
            /// The left child optree of the optree, if applicable.
            /// </summary>
            public _OpTree Left;

            /// <summary>
            /// The right child optree of the optree, if applicable.
            /// </summary>
            public _OpTree Right;

            /// <summary>
            /// Gets the expression of this optree.
            /// </summary>
            public Expression Expression
            {
                get
                {
                    return this.Term ?? new BinaryExpression(this.Left.Expression, this.Right.Expression, this.Operator.Operation);
                }
            }

            /// <summary>
            /// Combines an optree with a term using the given operator.
            /// </summary>
            public static _OpTree Combine(_OpTree Left, Operator Operator, Expression Right)
            {
                if (Left.Operator == null || Left.Operator.Precedence >= Operator.Precedence)
                    return new _OpTree(Operator, Left, new _OpTree(Right));
                else
                    return new _OpTree(Left.Operator, Left.Left, Combine(Left.Right, Operator, Right));
            }
        }

        /// <summary>
        /// Tries parsing an integer value given in binary, decimal or hexadecimal.
        /// </summary>
        public static bool AcceptInteger(string Text, ref int Index, ref int Value, out int ErrorIndex)
        {
            ErrorIndex = Index;
            int cur = Index;

            // Determine base, if any
            int b = 10;
            if (Text.Length - Index >= 2)
            {
                cur += 2;
                switch (Text.Substring(Index, 2))
                {
                    case "0d":
                        b = 10;
                        break;
                    case "0b":
                        b = 2;
                        break;
                    case "0h":
                    case "0x":
                        b = 16;
                        break;
                    default:
                        cur -= 2;
                        break;
                }
            }

            // Find digits
            List<int> digs = new List<int>();
            bool found = false;
            while (cur < Text.Length)
            {
                int dig = 0;
                if (IsHexadecimalDigit(Text[cur], ref dig) && dig < b)
                {
                    found = true;
                    digs.Add(dig);
                    cur++;
                    continue;
                }
                else
                {
                    ErrorIndex = cur;
                    break;
                }
            }

            if (found)
            {
                Value = 0;
                int r = 1;
                for (int t = digs.Count - 1; t >= 0; t--)
                {
                    Value += digs[t] * r;
                    r *= b;
                }

                Index = cur;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Tries parsing a literal term in the given text.
        /// </summary>
        public static bool AcceptLiteral(string Text, ref int Index, ref Expression Term, out int ErrorIndex)
        {
            int val = 0;
            if (AcceptInteger(Text, ref Index, ref val, out ErrorIndex))
            {
                Term = new ConstantExpression(val);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Tries parsing a term in the given text.
        /// </summary>
        public static bool AcceptTerm(string Text, ref int Index, ref Expression Term, out int ErrorIndex)
        {
            if (AcceptLiteral(Text, ref Index, ref Term, out ErrorIndex))
                return true;

            int cur = Index;
            string word = null;

            if (AcceptWord(Text, ref cur, ref word))
            {
                if (word == Parameter)
                {
                    Term = IdentityExpression.Instance;
                    Index = cur;
                    return true;
                }
            }

            if (AcceptString("(", Text, ref cur))
            {
                AcceptExtendedWhitespace(Text, ref cur);
                if (AcceptExpression(Text, ref cur, ref Term, out ErrorIndex))
                {
                    AcceptExtendedWhitespace(Text, ref cur);
                    if (AcceptString(")", Text, ref cur))
                    {
                        Index = cur;
                        return true;
                    }
                    ErrorIndex = cur;
                    return false;
                }
                return false;
            }

            return false;
        }

        /// <summary>
        /// The variable for the parameter of an expression.
        /// </summary>
        public static string Parameter = "t";
    }

    /// <summary>
    /// Gives information about a binary operator.
    /// </summary>
    public class Operator
    {
        public Operator(string Name, int Precedence, BinaryOperation Operation)
        {
            this.Name = Name;
            this.Precedence = Precedence;
            this.Operation = Operation;
        }

        /// <summary>
        /// A mapping of names to operators.
        /// </summary>
        public static readonly Dictionary<string, Operator> Map = new Dictionary<string,Operator>();

        /// <summary>
        /// The maximum string length of an operator.
        /// </summary>
        public static int MaxOperatorLength = 0;

        /// <summary>
        /// Adds an operator to the available set of operators.
        /// </summary>
        public static void Add(Operator Operator)
        {
            MaxOperatorLength = Math.Max(MaxOperatorLength, Operator.Name.Length);
            Map[Operator.Name] = Operator;
        }

        static Operator()
        {
            Add(new Operator("|", 0, BinaryOperation.Or));
            Add(new Operator("^", 1, BinaryOperation.Xor));
            Add(new Operator("&", 2, BinaryOperation.And));
            Add(new Operator("<<", 3, BinaryOperation.LeftShift));
            Add(new Operator(">>", 3, BinaryOperation.RightShift));
            Add(new Operator("+", 4, BinaryOperation.Add));
            Add(new Operator("-", 4, BinaryOperation.Subtract));
            Add(new Operator("*", 4, BinaryOperation.Multiply));
            Add(new Operator("/", 5, BinaryOperation.Divide));
            Add(new Operator("%", 5, BinaryOperation.Modulus));
        }

        /// <summary>
        /// The name of the operator.
        /// </summary>
        public string Name;

        /// <summary>
        /// The precedence of the operator.
        /// </summary>
        public int Precedence;

        /// <summary>
        /// The operation for the operator.
        /// </summary>
        public BinaryOperation Operation;
    }
}