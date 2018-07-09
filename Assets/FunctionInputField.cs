using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using static ExpressionTree;
using System;

[RequireComponent(typeof(TextMeshProUGUI))]
public class FunctionInputField : MonoBehaviour {

    private TextMeshProUGUI textInput;

	private void Start () {
        textInput = GetComponent<TextMeshProUGUI>();

        var a = GetTokens("(3 + 4 + x) * (3 + 4i) ^ 2 - Sin(5)");
        var b = FromInfixToPostfix(a);
        var c = InfixToExpressionTree(b);
        Debug.Log(c.Evaluate(new Number(1, 1)));
    }

    /// <summary>
    /// Makes a list of tokens from a mathematical expression.
    /// </summary>
    /// <param name="expression">The mathematical expression</param>
    /// <returns>A list of tokens</returns>
    private static List<Token> GetTokens(string expression) {
        List<Token> tokens = new List<Token>();
        var chars = expression.Replace(" ", string.Empty).ToLower().ToCharArray();

        string nmbrFunc = "";
        int i = -1;
        while (i < chars.Length - 1) {
            i++;

            // For these tokens we need to not be in the process of a number or function
            if (nmbrFunc.Length == 0) {
                if (chars[i] == 'x') {
                    tokens.Add(new X());
                    continue;
                }

                // Special case for whn 1 * i is not written out simply i is written
                if (chars[i] == 'i') {
                    tokens.Add(new Number(0, 1));
                    continue;
                }
            }

            // This checks whether the number has a minus sign before it (the current char is minus sign)
            // Explanation: Has a non-letter or digit before it and has a number after it
            if ((chars[i] == '-' && 
                (i == 0 || !char.IsLetterOrDigit(chars[i - 1])) &&
                (i == chars.Length - 1 || char.IsDigit(chars[i + 1])))) {

                nmbrFunc += chars[i].ToString();
                i++; // skip a character because we don't want to add this '-' to operators
            }

            // The numbers and functions can be more than one digit so we need to add
            // those to a string and add it to tokens at the end of the number or function
            if (char.IsLetterOrDigit(chars[i]) || chars[i] == '.') {
                nmbrFunc += chars[i].ToString();
            } else if (nmbrFunc != "") {
                double nmb = 0; // Used for out

                // Try to check whether it is a number or not
                if (double.TryParse(nmbrFunc, out nmb)) {
                    // Check if it is an imaginary number or not (has *i at end)
                    if (i >= chars.Length + i && chars[i + 1] == '*' && chars[i + 1] == 'i') {
                        tokens.Add(new Number(0, nmb));
                        i += 2; // We don't need to look at the *i
                    } else { // Not imaginary number
                        tokens.Add(new Number(nmb));
                    }
                // We have in i at the end of the number
                } else if (nmbrFunc.EndsWith("i") && double.TryParse(nmbrFunc.Substring(0, nmbrFunc.Length - 1), out nmb)) {
                    tokens.Add(new Number(0, nmb));
                } else {
                    i++; // With this we go to the first character of the first parameter

                    string parameters = "";
                    // Go till the end of the function
                    while (chars[i] != ')') {
                        parameters += chars[i].ToString();
                        i++;
                    }
                    if (i == chars.Length) throw new InputException("Missing a closing bracket for function " + nmbrFunc, i);

                    // Cast parameters to int
                    Token[] doubleParams = parameters.Replace(" ", string.Empty).Split(new string[] { "," }, StringSplitOptions.None)
                        .Select(s => {
                            var list = GetTokens(s);
                            if (list.Count > 1) throw new InputException("Problem with the parameters of " + nmbrFunc + " function!", i);

                            return list[0];
                        }).ToArray();

                    // Add new function
                    try { 
                        tokens.Add(new Function(nmbrFunc, doubleParams));
                    } catch (InvalidTokenException e) {
                        throw new InputException("No known function with the name " + nmbrFunc, e, i);
                    }

                    // Skip )
                    i++;
                }

                nmbrFunc = "";

                if (i >= chars.Length) break; // Special case for when a function is at the end
            }

            // One character operator (needs to be seperate if)
            if (isOperator(chars[i])) {
                tokens.Add(new Operator(chars[i].ToString()));
            // Parenthesis
            } else if (chars[i] == '(' || chars[i] == ')') {
                tokens.Add(new Parenthesis(chars[i].ToString()));
            }
        }

        // If at the end we still have something in nmbrFunc (which contains a longer number or function)
        if (nmbrFunc != "") {
            double nmb;

            // Try to see whether it is a number or not
            if (double.TryParse(nmbrFunc, out nmb)) {
                tokens.Add(new Number(nmb));
            } else {
                tokens.Add(new Function(nmbrFunc));
            }
        }

        return tokens;
    }

    /// <summary>
    /// Creates an infix notation (3 4 2 * 1 5 - 2 3 ^ ^ / +) from a postfix notation (3 + 4 * 2 / ( 1 / 5 ) ^ 2 ^ 3)
    /// </summary>
    /// <param name="tokens">Tokens from which to create the infix</param>
    /// <returns>An infix notation created from the given postfix</returns>
    private static List<Token> FromInfixToPostfix(List<Token> tokens) {
        Queue<Token> output = new Queue<Token>();
        Stack<Token> operators = new Stack<Token>();

        for (int i = 0; i < tokens.Count; i++) {
            // these need to be pushed directly in the output queue
            if (tokens[i] is Number || tokens[i] is X || tokens[i] is Function) {
                output.Enqueue(tokens[i]);
            } else if (tokens[i] is Operator) {
                Operator currOp = (Operator) tokens[i];

                // while...
                while (operators.Count > 0 &&
                    (operators.Peek() is Operator && // The top is an operator (needed for the two lines below)
                            (((Operator) operators.Peek()) > currOp || // with a greater precedence
                             // OR with the same precedence and is left associative
                             ((Operator) operators.Peek()) == currOp && !((Operator) operators.Peek()).attribute.rightAssociative))) {
                    output.Enqueue(operators.Pop());
                }
                operators.Push(currOp); // push operator to the operators stack
            } else if (tokens[i] is Parenthesis) {
                Parenthesis currPar = (Parenthesis) tokens[i];

                // left/open parenthesis need to be pushed on the operator stack
                if (currPar.open) {
                    operators.Push(currPar);
                } else { // right/closing parenthesis
                    try { 
                        // while top operator is not left bracket (something else or right bracket)
                        while (!(operators.Peek() is Parenthesis) || (operators.Peek() is Parenthesis && !((Parenthesis) operators.Peek()).open)) {
                            output.Enqueue(operators.Pop());
                        }
                    } catch (InvalidOperationException e) {
                        throw new InputException("No closing parenthesis!", e, -1);
                    }
                    operators.Pop();
                }
            }
        }

        // while we have operators on the stack add it to the queue
        while (operators.Count > 0) {
            output.Enqueue(operators.Pop());
        }

        return output.ToList();
    }

    /// <summary>
    /// Creates an ExpressionTree from infix notation (3 4 2 * 1 5 - 2 3 ^ ^ / +)
    /// </summary>
    private static ExpressionTree InfixToExpressionTree(List<Token> tokens) {
        Stack<ExpressionNode> expressionTree = new Stack<ExpressionNode>();

        for (int i = 0; i < tokens.Count; i++) {
            Token curr = tokens[i];

            if (curr is Number || curr is X || curr is Function) {
                // Simply add it to the stack
                expressionTree.Push(new ExpressionNode(curr));
            } else if (curr is Operator) {
                // We need to pop the top two nodes and make them the child of current operator
                var node = new ExpressionNode(curr);

                try { 
                    node.ChildLeft = expressionTree.Pop();
                    node.ChildRight = expressionTree.Pop();
                } catch (InvalidOperationException e) {
                    throw new InputException("No number for operator " + ((Operator) curr).Op, e, -1);
                }

                expressionTree.Push(node);
            }
        }

        var tree = new ExpressionTree();
        tree.BuildTree(expressionTree.Pop());

        return tree;
    }

    internal static bool isOperator(char c) {
        return isOperator(c.ToString());
    }
    internal static bool isOperator(string s) {
        return s == Operators.ADD.mark || s == Operators.SUB.mark || s == Operators.DIV.mark ||
            s == Operators.MULTI.mark || s == Operators.POW.mark;
    }
}

internal class ExpressionTree {

    private ExpressionNode[] nodes = new ExpressionNode[7];

    /// <summary>
    /// Evaluates this tree with the given x
    /// </summary>
    public Number Evaluate(Number x) {
        return EvaluateRecursive(x);
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
                return (EvaluateRecursive(x, at * 2) ^ EvaluateRecursive(x, at * 2 + 1).number);
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

        public bool IsLeaf { get { return !HasChildLeft && !HasChildRight;  } }

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

internal static class Operators {
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
internal class OperatorAttr {
    internal string mark;
    internal int precedence;
    internal bool rightAssociative;

    public OperatorAttr(string mark, int precedence, bool rightAssociative) {
        this.mark = mark;
        this.precedence = precedence;
        this.rightAssociative = rightAssociative;
    }
}

internal interface Token { }

internal class Operator : Token {
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
}
internal class Number : Token {
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

    public static Number operator /(Number left, Number right) {
        double div = right.number * right.number + right.i * right.i;

        return new Number((left.number * right.number + left.i * right.i) / div, (left.i * right.number - left.number * right.i) / div);
    }
    
    public static Number operator ^(Number left, double right) {
        double r = Math.Pow(Math.Sqrt(left.number * left.number + left.i * left.i), right);
        double theta = Math.Atan(left.i / left.number);

        return new Number(r * Math.Cos(right * theta), Math.Sin(right * theta));
    }

    public override string ToString() {
        return number + " + " + i + "i";
    }
}
internal class X : Token {
    internal string x = "x";
}

internal class Function : Token {
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
}
internal static class FunctionExtension {
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
internal enum FunctionTypes {
    SIN, COS
}

internal class Parenthesis : Token {
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
}

internal class InvalidTokenException : Exception {
    public InvalidTokenException() { }
    public InvalidTokenException(string msg) : base(msg) { }
    public InvalidTokenException(string msg, Exception inner) : base(msg, inner) { }
}
internal class InputException : Exception {
    public InputException() { }
    public InputException(string msg, int at) : base(msg + " (At character " + at) { }
    public InputException(string msg, Exception inner, int at) : base(msg + " (At character " + at, inner) { }
}
