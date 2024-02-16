using Pidgin;
using static Pidgin.Parser;
using Char = System.Char;

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
        // TODO: Figure out of to remove the where clause
        // assignees: thygesteffensen

        /*indexedFunction = Map((_, _) => (Node)new AccessNode(null!, null!), Rec(() =>
                   Terminal.IgnoreResult().Or(AnyCharExcept('(', ']', '[').ManyString().Then(_openParen).Then(_closeParen)
                       .IgnoreResult())),
                   Lookahead(Char(']').IgnoreResult()).Or(Try(Parser<char>.End).IgnoreResult()
                   .Or(Try(
                       Try(Terminal).Or(Rec(() => indexedFunction)).Between(_openBracket, _closeBracket).IgnoreResult())))
           )*/

        /*indexedFunction = Map((_, _) => (Node)new AccessNode(null!, null!), Rec(() =>
                Terminal.IgnoreResult().Or(Rec(() => function).IgnoreResult())),
                Lookahead(Char(']').IgnoreResult()).Or(Try(Parser<char>.End).IgnoreResult()
                .Or(Try(
                    Try(Terminal).Or(Rec(() => indexedFunction)).Between(_openBracket, _closeBracket).IgnoreResult())))
        );

        function =
            Map((functionName, args, _) => (Node)new FunctionNode(functionName, args.Where(x => x != null!).ToList()),
                AnyCharExcept('(', ']', '[').ManyString().Before(_openParen),
                Try(Terminal)
                    .Or(Try(Rec(() => indexedFunction)))
                    .Or(SkipWhitespaces.Select(_ => (Node)null!))
                    .Between(Whitespaces.IgnoreResult())
                    .Separated(_comma),
                _closeParen);

        expression = Map((_, func, _) => func, Tok('@'), indexedFunction, Parser<char>.End);*/

        function =
            Map(
                (functionName, args, access) =>
                    /*(Node)new AccessNode(new FunctionNode(functionName, args.Where(x => x != null!).ToList()),
                        access.FirstOrDefault()!)*/
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

    // TODO: Consider just returning the
    /// <summary>
    /// Build a Function AST from the given string
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public Node BuildAst(string input)
    {
        var parseResult = expression.Parse(input);
        var t = Parser<char>.End;

        if (!parseResult.Success)
        {
            throw new Exception(parseResult.Error!.RenderErrorMessage());
        }

        return parseResult.Value;
    }
}
