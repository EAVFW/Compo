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

    public ExpressionParser()
    {
        // TODO: Figure out of to remove the where clause
        // assignees: thygesteffensen
        function =
            Map((functionName, args, _) => (Node)new FunctionNode(functionName, args.Where(x => x != null!).ToList()),
                AnyCharExcept('(').ManyString().Before(_openParen),
                Try(Terminal)
                    .Or(Try(Rec(() => function)))
                    .Or(SkipWhitespaces.Select(_ => (Node)null!))
                    .Between(Whitespaces.IgnoreResult())
                    .Separated(_comma),
                _closeParen);

        expression = Map((_, func) => func, Tok('@'), function);
    }

    // TODO: Consider just returning the
    /// <summary>
    /// Build a Function AST from the given string
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public Node BuildAst(string input)
    {
        var parseResult = expression.Parse(input);

        if (!parseResult.Success)
        {
            throw new Exception(parseResult.Error!.RenderErrorMessage());
        }

        return parseResult.Value;
    }
}
