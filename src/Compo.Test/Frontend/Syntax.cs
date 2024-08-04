namespace Compo.Test.Frontend;

public class Trees
{
    private class FrontendTestInput : TheoryData<string, Node>
    {
        public FrontendTestInput()
        {
            Add("@empty()", new FunctionNode("empty", []));
            Add("@int(2)", new FunctionNode("int", [new ValueNode<int>(2)]));
            Add("@float(2.1)", new FunctionNode("float", [new ValueNode<decimal>(2.1M)]));
            Add("@bool(false)", new FunctionNode("bool", [new ValueNode<bool>(false)]));
            Add("@bool(true)", new FunctionNode("bool", [new ValueNode<bool>(true)]));
            Add("@string('Hello Compo!')", new FunctionNode("string", [new ValueNode<string>("Hello Compo!")]));

            Add("@ints(1, 2, 3)",
                new FunctionNode("ints", [new ValueNode<int>(1), new ValueNode<int>(2), new ValueNode<int>(3)]));
            Add("@floats(1.1, 2.2, 3.3)",
                new FunctionNode("floats",
                    [new ValueNode<decimal>(1.1M), new ValueNode<decimal>(2.2M), new ValueNode<decimal>(3.3M)]));
            Add("@bools(true, false, true)",
                new FunctionNode("bools",
                    [new ValueNode<bool>(true), new ValueNode<bool>(false), new ValueNode<bool>(true)]));
            Add("@strings('Hello', 'Compo', '!')",
                new FunctionNode("strings",
                    [new ValueNode<string>("Hello"), new ValueNode<string>("Compo"), new ValueNode<string>("!")]));

            Add("@composite(function())", new FunctionNode("composite", [new FunctionNode("function", [])]));
            Add("@composite(function(1))",
                new FunctionNode("composite", [new FunctionNode("function", [new ValueNode<int>(1)])]));
            Add("@composite(function(1, 2))",
                new FunctionNode("composite",
                    [new FunctionNode("function", [new ValueNode<int>(1), new ValueNode<int>(2)])]));
            Add("@composite(function(1, empty()))",
                new FunctionNode("composite",
                    [new FunctionNode("function", [new ValueNode<int>(1), new FunctionNode("empty", [])])]));
            Add("@object()", new FunctionNode("object", []));
            Add("@object()[1]",
                new AccessNode(new FunctionNode("object", []), new ValueNode<int>(1)));
            Add("@object()[abc()]", new AccessNode(new FunctionNode("object", []), new FunctionNode("abc", [])));
            Add("@object()['abc']", new AccessNode(new FunctionNode("object", []), new ValueNode<string>("abc")));
            Add("@object()['abc']['abc']",
                new AccessNode(
                    new AccessNode(
                        new FunctionNode("object", []), new ValueNode<string>("abc")),
                    new ValueNode<string>("abc")));
            Add("@object()[abc()['abc']]", new AccessNode(
                new FunctionNode("object", []),
                new AccessNode(new FunctionNode("abc", []), new ValueNode<string>("abc"))));
        }
    }

    [Theory]
    [ClassData(typeof(FrontendTestInput))]
    // [InlineData("@int(2)", (Node)new FunctionNode("int", [new ValueNode<int>(2)]))]
    public void Test(string input, Node expected)
    {
        var actual = new ExpressionParser().BuildAst(input).Value;

        // actual.Should().BeEquivalentTo(expected, options => options.RespectingRuntimeTypes());
        Assert.Equal(expected, actual);
    }
}
