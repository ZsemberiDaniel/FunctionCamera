using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System;
using static MathHelper;
using static MathHelper.ExpressionTree;

[RequireComponent(typeof(TextMeshProUGUI))]
public class FunctionInputField : MonoBehaviour {

    private TMP_InputField textInput;

    private void Start() {
        textInput = GetComponent<TMP_InputField>();
    }

    /// <summary>
    /// Evaluates the current input of this InputField then returns it's ExpressionTree.
    /// </summary>
    /// <returns>A class of MathHelper.ExpressionTree which then later can be evaluated based on x.</returns>
    /// <exception cref="InputException">
    /// If there is some problem with the input. The message contains the problem and may contain the character number.
    /// If the character number could not be identified then it is -1.
    /// </exception>
    public ExpressionTree EvaluateCurrentInput() {
        return FromInfixToExpressionTree(FromPostfixToInfix(GetTokens(textInput.text)));
    }

    /// <summary>
    /// Makes a list of tokens from a mathematical expression.
    /// </summary>
    /// <param name="expression">The mathematical expression</param>
    /// <returns>A list of tokens</returns>
    private static List<Token> GetTokens(string expression) {
        List<Token> tokens = new List<Token>();
        var chars = expression.Replace(" ", string.Empty).ToLower().ToCharArray();

        string nmbr = "", func = "";
        int i = -1;
        while (i < chars.Length - 1) {
            i++;

            // This checks whether something has a minus sign before it
            // Explanation: Has an operator or an open bracket before it and curr is -
            if (chars[i] == '-' &&
                // fuck this line and fuck me. Why would you want to read this shitty code?
                (i == 0 || (tokens.Count > 0 && (tokens[tokens.Count - 1] is Operator || (tokens[tokens.Count - 1] is Parenthesis && ((Parenthesis) tokens[tokens.Count - 1]).open))))) {

                tokens.Add(new Number(-1));
                tokens.Add(new Operator("*"));
                i++; // skip a character because we don't want to add this '-' to operators
            }

            // The numbers can be more than one digit so we need to add
            // those to a string and add it to tokens at the end of the number
            if (char.IsDigit(chars[i]) || chars[i] == '.') {
                nmbr += chars[i].ToString();
            } else if (nmbr != "") {
                double nmb = 0; // Used for out

                // Try to check whether it is a number or not
                if (double.TryParse(nmbr, out nmb)) {
                    // Check if it is an imaginary number or not (has *i at end)
                    if (i < chars.Length - 1 && chars[i] == '*' && chars[i + 1] == 'i') {
                        tokens.Add(new Number(0, nmb));
                        i += 2; // We don't need to look at the *i
                    // We have in i at the end of the number
                    } else if(chars[i] == 'i') {
                        tokens.Add(new Number(0, nmb));
                        i++; // We don't need to look at the *i
                    } else { // Not imaginary number
                        tokens.Add(new Number(nmb));
                    }

                }

                nmbr = "";

                // in case we overshot because of the i or *i
                if (i >= chars.Length) break;
            }

            // For these tokens we need to not be in the process of a function
            // TODO: here is a bug. If a function stars with an i then that i won't be detected because
            // this will catch it and add a number token instead (same with x)
            if (func == "") {
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

            // The functions can be more than one digit so we need to add
            // those to a string and add it to tokens at the end of the function
            if (char.IsLetter(chars[i])) {
                func += chars[i].ToString();
            } else if (func != "") {
                i++; // With this we go to the first character of the first parameter

                string parameters = "";
                // Go till the end of the function
                while (chars[i] != ')') {
                    parameters += chars[i].ToString();
                    i++;
                }
                if (i == chars.Length) throw new InputException("Missing a closing bracket for function " + func, i);

                // Cast parameters to int
                Token[] doubleParams = parameters.Replace(" ", string.Empty).Split(new string[] { "," }, StringSplitOptions.None)
                    .Select(s => {
                        var list = GetTokens(s);

                        return list[0];
                    }).ToArray();

                // Add new function
                try {
                    tokens.Add(new Function(func, doubleParams));
                } catch (InvalidTokenException e) {
                    throw new InputException("No known function with the name " + func, e, i);
                }

                func = "";

                if (i >= chars.Length) break; // Special case for when a function is at the end

                continue; // So the closing bracket is skipped
            } 

            // One character operator (needs to be seperate if)
            if (isOperator(chars[i])) {
                tokens.Add(new Operator(chars[i].ToString()));
                // Parenthesis
            } else if (chars[i] == '(' || chars[i] == ')') {
                tokens.Add(new Parenthesis(chars[i].ToString()));
            }
        }

        // If at the end we still have something in nmbr (which contains a longer number)
        if (nmbr != "") {
            double nmb;

            // Try to see whether it is a number or not
            if (double.TryParse(nmbr, out nmb)) {
                tokens.Add(new Number(nmb));
            } else {
                tokens.Add(new Function(nmbr));
            }
        }

        // Insert multiplication where it was omitted by the user for convinience
        i = 0;
        while (i < tokens.Count - 1) {
            i++;

            if (CanMultiplicationBeOmitted(tokens[i - 1], tokens[i])) {
                tokens.Insert(i, new Operator("*"));
                i++;
            }
        }


        return tokens;
    }

    /// <summary>
    /// Whether the tokens are ( ( , x , function) and ( ) , number , function) in any pairing
    /// </summary>
    /// <param name="beforeToken"></param>
    /// <param name="currToken"></param>
    /// <returns></returns>
    private static bool CanMultiplicationBeOmitted(Token beforeToken, Token currToken) =>
        ((currToken is Parenthesis && ((Parenthesis) currToken).open) ||
         (currToken is X) ||
         (currToken is Function))
        &&
        ((beforeToken is Parenthesis && !((Parenthesis) beforeToken).open) ||
         (beforeToken is Number) ||
         (beforeToken is X));

    /// <summary>
    /// Creates an infix notation (3 4 2 * 1 5 - 2 3 ^ ^ / +) from a postfix notation (3 + 4 * 2 / ( 1 / 5 ) ^ 2 ^ 3)
    /// </summary>
    /// <param name="tokens">Tokens from which to create the infix</param>
    /// <returns>An infix notation created from the given postfix</returns>
    private static List<Token> FromPostfixToInfix(List<Token> tokens) {
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
    private static ExpressionTree FromInfixToExpressionTree(List<Token> tokens) {
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

public class InvalidTokenException : Exception {
    public InvalidTokenException() { }
    public InvalidTokenException(string msg) : base(msg) { }
    public InvalidTokenException(string msg, Exception inner) : base(msg, inner) { }
}
public class InputException : Exception {
    public InputException() { }
    public InputException(string msg, int at) : base(msg + " (At character " + at) { }
    public InputException(string msg, Exception inner, int at) : base(msg + " (At character " + at, inner) { }
}
