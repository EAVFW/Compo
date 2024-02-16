using Pidgin;
using static Pidgin.Parser;

namespace Compo;

public class ExpressionParser
{
    static Parser<char, T> Tok<T>(Parser<char, T> p)
        => Try(p).Before(SkipWhitespaces);

    static Parser<char, char> Tok(char value) => Tok(Char(value));
    static Parser<char, string> Tok(string value) => Tok(String(value));

    static readonly Parser<char, char> _comma = Tok(',');
    static readonly Parser<char, char> _openParen = Tok('(');
    static readonly Parser<char, char> _closeParen = Tok(')');
    static readonly Parser<char, char> _openBracket = Tok('[');
    static readonly Parser<char, char> _closeBracket = Tok(']');
    static readonly Parser<char, char> _dot = Tok('.');
    static readonly Parser<char, char> _qoute = Tok('\'');

    private static readonly Parser<char, Node> IntegerValue =
        Num.Select(x => (Node)new ValueNode<int>(x));

    private static readonly Parser<char, Node> DoubleValue = Real.Select(x => (Node)new ValueNode<double>(x));

    private static readonly Parser<char, Node> BooleanValue =
        String("true").Or(String("false")).Select(x => (Node)new ValueNode<bool>(x == "true"));

    private static readonly Parser<char, Node> StringValue = AnyCharExcept('\'').ManyString()
        .Between(_qoute).Select(x => (Node)new ValueNode<string>(x));

    private static readonly Parser<char, Node> Terminal = DoubleValue.Or(IntegerValue).Or(BooleanValue).Or(StringValue);

    // Feature flag
    /*private static readonly Parser<char, Node> Infix =
        Map((lhs, op, rhs) => (Node)new FunctionNode("add", [lhs, rhs]),
        Try(Terminal),
        Tok('+'),
        Try(Terminal));*/

    private static Parser<char, Node> function = null!;
    private static Parser<char, Node> expression = null!;
    private static Parser<char, Node> indexedFunction = null!;


    public ExpressionParser()
    {
        function =
            Map(
                (functionName, args, access) =>
                    access.Aggregate((Node)new FunctionNode(functionName, args.Where(x => x != null!).ToList()),
                        (node, node1) => new AccessNode(node, node1)),
                AnyCharExcept('(', ']', '[').ManyString().Before(_openParen),
                Try(Terminal)
                    .Or(Try(Rec(() => function)))
                    .Or(SkipWhitespaces.Select(_ => (Node)null!))
                    .Between(Whitespaces.IgnoreResult())
                    .Separated(_comma),
                _closeParen.Then( /*Try(Parser<char>.End.IgnoreResult()).Or*/
                    (Try(Terminal).Or(Rec(() => function)).Between(_openBracket, _closeBracket).Many())));

        expression = Map((_, func, _) => func, Tok('@'), function, Parser<char>.End);
    }

    /// <summary>
    /// Build a Function AST from the given string
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public ParseResult BuildAst(string input)
    {
        var parseResult = expression.Parse(input);
        var t = Parser<char>.End;

        if (!parseResult.Success)
        {
            throw new Exception(parseResult.Error!.RenderErrorMessage());
        }

        return new ParseResult(parseResult);
    }
}
