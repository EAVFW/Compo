using System.Globalization;
using Pidgin;
using static Pidgin.Parser;

namespace Compo;

public class ExpressionParser
{
    static Parser<char, T> Tok<T>(Parser<char, T> p)
        => Try(p).Before(SkipWhitespaces);

    static Parser<char, char> Tok(char value) => Tok(Char(value));
    static Parser<char, string> Tok(string value) => Tok(String(value));

    private static readonly Parser<char, char> Comma = Tok(',');
    private static readonly Parser<char, char> OpenParen = Tok('(');
    private static readonly Parser<char, char> CloseParen = Tok(')');
    private static readonly Parser<char, char> OpenBracket = Tok('[');
    private static readonly Parser<char, char> CloseBracket = Tok(']');
    private static readonly Parser<char, char> Dot = Tok('.');
    private static readonly Parser<char, char> Quote = Tok('\'');

    #region PidingPaste

    private static readonly Parser<char, string> SignString
        = Char('-').ThenReturn("-")
            .Or(Char('+').ThenReturn("+"))
            .Or(Parser<char>.Return(""));

    private static readonly Parser<char, Unit> FractionalPart
        = Char('.').Then(Digit.SkipAtLeastOnce());

    private static readonly Parser<char, Unit> OptionalFractionalPart
        = FractionalPart.Or(Parser<char>.Return(Unit.Value));

    private static Parser<char, Node> MyReal { get; }
        = SignString
            .Then(
                FractionalPart
                    .Or(Digit.SkipAtLeastOnce()
                        .Then(OptionalFractionalPart)) // if we saw an integral part, the fractional part is optional
            )
            .Then(
                CIChar('e').Then(SignString).Then(Digit.SkipAtLeastOnce())
                    .Or(Parser<char>.Return(Unit.Value))
            )
            .MapWithInput<Node?>((span, _) =>
            {
                if (int.TryParse(span.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture,
                        out var intResult))
                {
                    return new ValueNode<int>(intResult);
                }

                if (decimal.TryParse(span.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture,
                        out var decimalResult))
                {
                    return new ValueNode<decimal>(decimalResult);
                }

                return null;
            })
            .Assert(x => x != null, "Couldn't parse a number")
            .Select(x => x!)
            .Labelled($"real number");

    #endregion

    private static readonly Parser<char, Node> IntegerValue =
        Num.Select(x => (Node)new ValueNode<int>(x));

    private static readonly Parser<char, Node> DoubleValue = Real.Select(x => (Node)new ValueNode<decimal>((decimal)x));

    private static readonly Parser<char, Node> BooleanValue =
        String("true").Or(String("false")).Select(x => (Node)new ValueNode<bool>(x == "true"));

    private static readonly Parser<char, Node> StringValue = AnyCharExcept('\'').ManyString()
        .Between(Quote).Select(x => (Node)new ValueNode<string>(x));

    private static readonly Parser<char, Node> Terminal =
        MyReal.Or(BooleanValue).Or(StringValue);
    
    // Feature flag
    /*private static readonly Parser<char, Node> Infix =
        Map((lhs, op, rhs) => (Node)new FunctionNode("add", [lhs, rhs]),
        Try(Terminal),
        Tok('+'),
        Try(Terminal));*/

    private static Parser<char, Node> _function = null!;
    private static Parser<char, Node> _expression = null!;
    private static Parser<char, Node> _indexedFunction = null!;


    public ExpressionParser()
    {
        _function =
            Map(
                (functionName, args, access) =>
                    access.Aggregate((Node)new FunctionNode(functionName, args.Where(x => x != null!).ToList()),
                        (node, node1) => new AccessNode(node, node1)),
                AnyCharExcept('(', ']', '[').ManyString().Before(OpenParen),
                Try(Terminal)
                    .Or(Try(Rec(() => _function)))
                    .Or(SkipWhitespaces.Select(_ => (Node)null!))
                    .Between(Whitespaces.IgnoreResult())
                    .Separated(Comma),
                CloseParen.Then( /*Try(Parser<char>.End.IgnoreResult()).Or*/
                    (Try(Terminal).Or(Rec(() => _function)).Between(OpenBracket, CloseBracket).Or(
                        Dot.Then(AnyCharExcept('[', ')', '.').ManyString().Select(x => (Node)new ValueNode<string>(x)))))
                    .Many()));

        _expression = Map((_, func, _) => func, Tok('@'), _function, Parser<char>.End);
    }

    /// <summary>
    /// Build a Function AST from the given string
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public ParseResult BuildAst(string input)
    {
        var parseResult = _expression.Parse(input);
        _ = Parser<char>.End;

        if (!parseResult.Success)
        {
            throw new Exception(parseResult.Error!.RenderErrorMessage());
        }

        return new ParseResult(parseResult);
    }

    public string BuildExpression(Node node)
    {
        return node switch
        {
            ValueNode<int> valueNode => valueNode.Value.ToString(),
            ValueNode<decimal> valueNode => valueNode.Value.ToString(CultureInfo.InvariantCulture),
            ValueNode<string> valueNode => $"'{valueNode.Value}'",
            FunctionNode functionNode => $"{functionNode.Function}({string.Join(", ", functionNode.Arguments.Select(BuildExpression))})",
            AccessNode accessNode => $"{BuildExpression(accessNode.Node)}[{BuildExpression(accessNode.Index)}]",
            _ => throw new ArgumentOutOfRangeException(nameof(node))
        };
    }
}
