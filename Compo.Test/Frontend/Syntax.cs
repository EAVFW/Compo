namespace Compo.Test.Frontend;

public class Trees
{
    private class FrontendTestInput : TheoryData<string, FunctionNode>
    {
        public FrontendTestInput()
        {
            Add("@empty()", new FunctionNode("empty", []));
            Add("@int(2)", new FunctionNode("int", [new ValueNode<int>(2)]));
            Add("@float(2.1)", new FunctionNode("float", [new ValueNode<double>(2.1)]));
            Add("@bool(false)", new FunctionNode("bool", [new ValueNode<bool>(false)]));
            Add("@bool(true)", new FunctionNode("bool", [new ValueNode<bool>(true)]));
            Add("@string('Hello Compo!')", new FunctionNode("string", [new ValueNode<string>("Hello Compo!")]));

            Add("@ints(1, 2, 3)",
                new FunctionNode("ints", [new ValueNode<int>(1), new ValueNode<int>(2), new ValueNode<int>(3)]));
            Add("@floats(1.1, 2.2, 3.3)",
                new FunctionNode("floats",
                    [new ValueNode<double>(1.1), new ValueNode<double>(2.2), new ValueNode<double>(3.3)]));
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
        }
    }

    [Theory]
    [ClassData(typeof(FrontendTestInput))]
    public void Test(string input, FunctionNode expected)
    {
        var actual = new ExpressionParser().BuildAst(input);
        actual.Should().BeEquivalentTo(expected);
    }
}
