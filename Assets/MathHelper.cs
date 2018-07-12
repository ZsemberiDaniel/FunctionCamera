using System;
using System.Collections.Generic;
using UnityEngine;
using static MathHelper;

/// <summary>
/// Helper class for the mathematical/information technology part of the processing
/// of functions
/// </summary>
public static class MathHelper {
    public class ExpressionTree {

        private ExpressionNode[] nodes = new ExpressionNode[7];

        /// <summary>
        /// Evaluates this tree with the given x
        /// </summary>
        public Number Evaluate(Number x) {
            return EvaluateRecursive(x);
        }

        /// <summary>
        /// Evaluates this tree with the given x
        /// </summary>
        public Number Evaluate(double realPart, double imaginaryPart) {
            return Evaluate(new Number(realPart, imaginaryPart));
        }

        /// <summary>
        /// Helper function for the public one. Recursively calculates what this tree
        /// returns when x is given.
        /// </summary>
        private Number EvaluateRecursive(Number x, int at = 1) {
            if (nodes[at].IsLeaf) {
                if (nodes[at].Token is Number) return ((Number) nodes[at].Token);
                else if (nodes[at].Token is X) return x;
                else if (nodes[at].Token is Function) return ((Function) nodes[at].Token).Evaluate(x);
            } else if (nodes[at].Token is Operator) {
                var oper = (Operator) nodes[at].Token;

                // +
                if (oper.Op == Operators.ADD.mark) {
                    return (EvaluateRecursive(x, at * 2) + EvaluateRecursive(x, at * 2 + 1));
                    // -
                } else if (oper.Op == Operators.SUB.mark) {
                    return (EvaluateRecursive(x, at * 2) - EvaluateRecursive(x, at * 2 + 1));
                    // *
                } else if (oper.Op == Operators.MULTI.mark) {
                    return (EvaluateRecursive(x, at * 2) * EvaluateRecursive(x, at * 2 + 1));
                    // /
                } else if (oper.Op == Operators.DIV.mark) {
                    return (EvaluateRecursive(x, at * 2) / EvaluateRecursive(x, at * 2 + 1));
                    // ^
                } else if (oper.Op == Operators.POW.mark) {
                    return (EvaluateRecursive(x, at * 2) ^ EvaluateRecursive(x, at * 2 + 1));
                }
            }

            // This should never occur
            throw new Exception("You done fucked up my man. This should never be seen in production");
        }

        /// <summary>
        /// Builds a tree form a given node. The node needs to have children in order for this function
        /// to work. Otherwise it will simply add that node to a tree and return the tree.
        /// </summary>
        public void BuildTree(ExpressionNode node) {
            nodes[1] = node;

            for (int i = 1; i < nodes.Length; i++) {
                if (nodes[i] == null) continue;

                // If the current node has a child then we need to add that to the nodes list.
                // Later when we get ther we'll look at that node as well and add it's children
                if (nodes[i].HasChildLeft) {
                    if (nodes.Length - 1 < i * 2) ExpandTree();

                    nodes[i * 2] = nodes[i].ChildLeft;
                }
                if (nodes[i].HasChildRight) {
                    if (nodes.Length - 2 < i * 2) ExpandTree();

                    nodes[i * 2 + 1] = nodes[i].ChildRight;
                }
            }
        }

        /// <summary>
        /// Creates a token at the given position in this tree
        /// </summary>
        private void AddToken(int at, Token token) {
            if (nodes[at] != null) {
                throw new InvalidOperationException("Token cannot be added in place of other token! Use replace instead!");
            }

            // It can only be a children to something if it's between these two
            if (at >= 2 && at <= nodes.Length * 2) {
                if (nodes[at / 2] == null) {
                    throw new InvalidOperationException("Token has no parent!");
                }

                if (at > nodes.Length) ExpandTree();

                //Create new node
                nodes[at] = new ExpressionNode(token, nodes[at / 2]);
                // Set the correct child
                if (at % 2 == 1) {
                    nodes[at / 2].ChildLeft = nodes[at];
                } else {
                    nodes[at / 2].ChildRight = nodes[at];
                }
            } else if (at == 1) { // The root token
                nodes[1] = new ExpressionNode(token);
            } else {
                throw new InvalidOperationException("Token cannot be created at " + at + " because it cannot be a childrean of something");
            }
        }

        /// <summary>
        /// Doubles the amount of place available for the nodes
        /// </summary>
        private void ExpandTree() {
            ExpressionNode[] newArray = new ExpressionNode[nodes.Length * 2];
            Array.Copy(nodes, newArray, nodes.Length);

            nodes = newArray;
        }

        public class ExpressionNode {
            private ExpressionNode parent;

            public ExpressionNode ChildLeft { get; set; }
            public bool HasChildLeft { get { return ChildLeft != null; } }

            public ExpressionNode ChildRight { get; set; }
            public bool HasChildRight { get { return ChildRight != null; } }

            public bool IsLeaf { get { return !HasChildLeft && !HasChildRight; } }

            private Token token;
            public Token Token { get { return token; } }

            /// <summary>
            /// Creates and ExpressionNode. It has no parent or child if created this way!
            /// </summary>
            public ExpressionNode(Token token) {
                this.token = token;
            }

            /// <summary>
            /// Creates an ExpressionNode. The parent and data is set this way.
            /// </summary>
            public ExpressionNode(Token token, ExpressionNode parent) : this(token) {
                this.parent = parent;
            }
        }
    }

    public static class Operators {
        internal static Dictionary<string, OperatorAttr> ATTRIBUTES;

        internal static OperatorAttr POW;
        internal static OperatorAttr MULTI;
        internal static OperatorAttr DIV;
        internal static OperatorAttr SUB;
        internal static OperatorAttr ADD;

        static Operators() {
            POW = new OperatorAttr("^", 4, true);
            MULTI = new OperatorAttr("*", 3, false);
            DIV = new OperatorAttr("/", 3, false);
            SUB = new OperatorAttr("-", 2, false);
            ADD = new OperatorAttr("+", 2, false);

            ATTRIBUTES = new Dictionary<string, OperatorAttr>() {
            { "^", POW },
            { "*", MULTI },
            { "/", DIV },
            { "-", SUB },
            { "+", ADD }
        };
        }
    }
    public class OperatorAttr {
        internal string mark;
        internal int precedence;
        internal bool rightAssociative;

        public OperatorAttr(string mark, int precedence, bool rightAssociative) {
            this.mark = mark;
            this.precedence = precedence;
            this.rightAssociative = rightAssociative;
        }
    }

    public interface Token { }

    public class Operator : Token {
        private string op;
        internal string Op {
            get { return op; }
            set {
                op = value;

                // We also need to set the attribute of this operator
                attribute = Operators.ATTRIBUTES[op];
            }
        }
        internal OperatorAttr attribute;

        /// <summary>
        /// Makes an operator token
        /// </summary>
        /// <exception cref="InvalidTokenException">If the given op is not an operator</exception>
        public Operator(string op) {
            if (!FunctionInputField.isOperator(op)) throw new InvalidTokenException("Given op " + op + " is not an operator!");

            this.Op = op;
        }

        /// <summary>
        /// Compares which one has precenedence over the other
        /// </summary>
        public static int Compare(Operator left, Operator right) {
            if (left.attribute.precedence > right.attribute.precedence) return 1;
            else if (left.attribute.precedence == right.attribute.precedence) return 0;
            else return -1;
        }

        /// <summary>
        /// True when the left has precedence over the right one
        /// </summary>
        public static bool operator >(Operator left, Operator right) {
            return Compare(left, right) > 0;
        }

        /// <summary>
        /// True when the right has precenedence over the left one
        /// </summary>
        public static bool operator <(Operator left, Operator right) {
            return Compare(left, right) < 0;
        }

        /// <summary>
        /// Returns  whether they have the same precendence
        /// </summary>
        public static bool operator ==(Operator left, Operator right) {
            return Compare(left, right) == 0;
        }

        /// <summary>
        /// Returns true if they do not have the same precendence
        /// </summary>
        public static bool operator !=(Operator left, Operator right) {
            return Compare(left, right) != 0;
        }

        public override int GetHashCode() {
            return op.GetHashCode();
        }

        public override bool Equals(object obj) {
            if (obj is Operator) {
                return Compare(this, (Operator) obj) > 0;
            }

            return false;
        }

        public override string ToString() {
            return op;
        }
    }
    public class Number : Token {
        internal double number;
        internal double i;

        public Number(double number) {
            this.number = number;
        }

        public Number(double number, double i) : this(number) {
            this.i = i;
        }

        public static Number operator +(Number left, Number right) {
            return new Number(left.number + right.number, left.i + right.i);
        }

        public static Number operator -(Number left, Number right) {
            return new Number(left.number - right.number, left.i - right.i);
        }

        public static Number operator *(Number left, Number right) {
            return new Number(left.number * right.number - left.i * right.i, left.i * right.number + left.number * right.i);
        }

        public static Number operator *(double left, Number right) {
            return new Number(right.number * left, right.i * left);
        }

        public static Number operator *(Number right, double left) {
            return left * right;
        }

        public static Number operator /(Number left, Number right) {
            double div = right.number * right.number + right.i * right.i;

            return new Number((left.number * right.number + left.i * right.i) / div, (left.i * right.number - left.number * right.i) / div);
        }

        public static Number operator ^(Number left, Number right) {
            double r = Math.Sqrt(left.number * left.number + left.i * left.i);
            double theta = Math.Atan(left.i / left.number);

            double logR = Math.Log(r, Math.E);
            Number exp = new Number(logR * right.number - theta * right.i, logR * right.i + theta * right.number);

            return Math.Pow(Math.E, exp.number) * new Number(Math.Cos(exp.i), Math.Sin(exp.i));
        }

        public override string ToString() {
            return number + " + " + i + "i";
        }
    }
    public class X : Token {
        internal string x = "x";
    }

    public class Function : Token {
        internal FunctionTypes type;
        internal Token[] parameters;

        /// <summary>
        /// If the parameters have x in them then it is a so called changin function.
        /// We always need to recalculate a changing function
        /// </summary>
        internal bool changing = false;
        internal Number constNumber;

        public Function(string func, params Token[] parameters) {
            if (!Enum.TryParse(func, true, out type)) throw new InvalidTokenException(func + " is not a function!");

            this.parameters = parameters;
            // If parameters have x in them then it is a changin function
            for (int i = 0; i < parameters.Length; i++) {
                if (parameters[i] is X) {
                    changing = true;
                    break;
                }
            }

            // If not changing this calculated value should never change
            if (!changing) {
                constNumber = Evaluate(null);
            }
        }

        /// <summary>
        /// Evaluate this function
        /// </summary>
        public Number Evaluate(Number x) {
            // Not a changing function
            if (!changing && constNumber != null) return constNumber;

            return type.Evaluate(x, this);
        }

        /// <summary>
        /// Checks whether the given string corresponds to a pre defined function
        /// </summary>
        public static bool IsItAFunction(string f) {
            FunctionTypes _;

            return !Enum.TryParse(f, true, out _);
        }

        public override string ToString() {
            return type.ToString();
        }
    }
    public enum FunctionTypes {
        SIN, COS
    }

    public class Parenthesis : Token {
        internal bool open;

        /// <summary>
        /// Makes a new parenthesis token
        /// </summary>
        /// <param name="par">What parenthesis to use</param>
        /// <exception cref="InvalidTokenException">If the given par is not an exception</exception>
        public Parenthesis(string par) {
            if (par == "(") {
                open = true;
            } else if (par == ")") {
                open = false;
            } else {
                throw new InvalidTokenException("It's not a parenthesis!");
            }
        }

        public override string ToString() {
            return open ? "(" : ")";
        }
    }
}

public static class FunctionExtension {
    /// <summary>
    /// Evaluates a function with the given x
    /// </summary>
    public static Number Evaluate(this FunctionTypes type, Number x, Function func) {
        // Store the parameters in this new array (so we can insert x as well)
        Number[] parameters = new Number[func.parameters.Length];

        for (int i = 0; i < parameters.Length; i++) {
            if (func.parameters[i] is Number) parameters[i] = (Number) func.parameters[i];
            else if (func.parameters[i] is X) parameters[i] = x;
        }

        switch (type) {
            case FunctionTypes.SIN:
                return new Number(Math.Sin(parameters[0].number) * Math.Cosh(parameters[0].i),
                    Math.Cos(parameters[0].number) * Math.Sinh(parameters[0].i));
            case FunctionTypes.COS:
                return new Number(Math.Cos(parameters[0].number) * Math.Cosh(parameters[0].i),
                    -1 * Math.Sin(parameters[0].number) * Math.Sinh(parameters[0].i));
        }

        throw new InputException("This should never ever be reached when evaluation comes!", -1);
    }
}