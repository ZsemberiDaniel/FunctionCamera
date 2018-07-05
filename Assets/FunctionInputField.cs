using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(TextMeshProUGUI))]
public class FunctionInputField : MonoBehaviour {

    private TextMeshProUGUI textInput;

	private void Start () {
        textInput = GetComponent<TextMeshProUGUI>();

        var a = GetTokens("3 + sin(5) - -5");
        var b = FromInfixToPostfix(a);
        Debug.Log((int) '1');
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

            // x
            if (chars[i] == 'x' && nmbrFunc.Length == 0) {
                tokens.Add(new X());
                continue;
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
                double nmb; // Used for out

                // Try to check whether it is a number or not
                if (double.TryParse(nmbrFunc, out nmb)) {
                    tokens.Add(new Number(nmb));
                } else {
                    i++; // With this we go to the first character of the first parameter

                    string parameters = "";
                    // Go till the end of the function
                    while (chars[i] != ')') {
                        parameters += chars[i].ToString();
                        i++;
                    }
                    // Cast parameters to int
                    double[] doubleParams = parameters.Replace(" ", string.Empty).Split(new string[] { "," }, System.StringSplitOptions.None)
                        .Select(s => double.Parse(s)).ToArray();

                    // Add new function
                    tokens.Add(new Function(nmbrFunc, doubleParams));

                    i++;
                }

                nmbrFunc = "";
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
                    // while top operator is not left bracket (something else or right bracket)
                    while (!(operators.Peek() is Parenthesis) || (operators.Peek() is Parenthesis && !((Parenthesis) operators.Peek()).open)) {
                        output.Enqueue(operators.Pop());
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

    private static void InfixToExpressionTree(List<Token> tokens) {
        Stack<Token> expressionTree = new Stack<Token>();

        for (int i = 0; i < tokens.Count; i++) {

        }
    }

    internal static bool isOperator(char c) {
        return isOperator(c.ToString());
    }
    internal static bool isOperator(string s) {
        return s == Operators.ADD.mark || s == Operators.SUB.mark || s == Operators.DIV.mark ||
            s == Operators.MULTI.mark || s == Operators.POW.mark;
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

    public Number(double number) {
        this.number = number;
    }
}
internal class X : Token {
    internal string x = "x";
}
internal class Function : Token {
    internal string func;
    internal double[] parameters;

    public Function(string func, params double[] parameters) {
        this.func = func;
        this.parameters = parameters;
    }
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

internal class InvalidTokenException : System.Exception {
    public InvalidTokenException() { }
    public InvalidTokenException(string msg) : base(msg) { }
    public InvalidTokenException(string msg, System.Exception inner) : base(msg, inner) { }
}
