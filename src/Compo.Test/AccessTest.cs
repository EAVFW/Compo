using Microsoft.Extensions.DependencyInjection;
using Compo.Functions.Math;

namespace Compo.Test;

public class AccessTest
{
    public record FunctionOutout(int value) : IConvertible
    {
        public TypeCode GetTypeCode()
        {
            return TypeCode.Object;
        }

        public bool ToBoolean(IFormatProvider? provider)
        {
            throw new NotImplementedException("ToBoolean");
        }

        public byte ToByte(IFormatProvider? provider)
        {
            throw new NotImplementedException("ToByte");
        }

        public char ToChar(IFormatProvider? provider)
        {
            throw new NotImplementedException("ToChar");
        }

        public DateTime ToDateTime(IFormatProvider? provider)
        {
            throw new NotImplementedException("ToDateTime");
        }

        public decimal ToDecimal(IFormatProvider? provider)
        {
            throw new NotImplementedException("ToDecimal");
        }

        public double ToDouble(IFormatProvider? provider)
        {
            throw new NotImplementedException("ToDouble");
        }

        public short ToInt16(IFormatProvider? provider)
        {
            throw new NotImplementedException("ToInt16");
        }

        public int ToInt32(IFormatProvider? provider)
        {
            throw new NotImplementedException("ToInt32");
        }

        public long ToInt64(IFormatProvider? provider)
        {
            throw new NotImplementedException("ToInt64");
        }

        public sbyte ToSByte(IFormatProvider? provider)
        {
            throw new NotImplementedException("ToSByte");
        }

        public float ToSingle(IFormatProvider? provider)
        {
            throw new NotImplementedException("ToSingle");
        }

        public string ToString(IFormatProvider? provider)
        {
            throw new NotImplementedException("ToString");
        }

        public object ToType(Type conversionType, IFormatProvider? provider)
        {
            if (conversionType == typeof(FunctionOutout))
                return this;

            throw new NotImplementedException("ToType:" + conversionType.FullName);
        }

        public ushort ToUInt16(IFormatProvider? provider)
        {
            throw new NotImplementedException("ToUInt16");
        }

        public uint ToUInt32(IFormatProvider? provider)
        {
            throw new NotImplementedException("ToUInt32");
        }

        public ulong ToUInt64(IFormatProvider? provider)
        {
            throw new NotImplementedException("ToUInt64");
        }
    }

    public class Function1 : IFunction<int, FunctionOutout>
    {
        

        public FunctionOutout Execute(int test)
        {
            return new FunctionOutout(test);
        }
    }

    public class Function2 : IFunction<int,int, FunctionOutout>
    {
        
        public FunctionOutout Execute(int teest, int test2)
        {
            return new FunctionOutout(test2);
        }
    }

    public class Function3 : IFunction<FunctionOutout, int>,IFunction<FunctionOutout, FunctionOutout, int>
    {

        public int Execute(FunctionOutout teest, FunctionOutout test2)
        {
            return test2.value;
        }

        public int Execute(FunctionOutout test1)
        {
            return test1.value;
        }
    }
    public class OutputsFunction : IFunction<IDictionary<string, object>>
    {
        private readonly IDictionary<string, object> _outputs = new Dictionary<string, object>
        {
            { "value1", 3 },
        };

        public IDictionary<string, object> Execute()
        {
            return _outputs;
        }
    }

    [Theory]
    [InlineData("@outputs()['value1']", 3)]
    public void Tester(string expression, object result)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.RegisterFunction<MultiplicationFunction>("mult");
        services.RegisterFunction<OutputsFunction>("outputs");
      

        services.DiscoverFunctions();

        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();

        var serviceProvider = services.BuildServiceProvider();

        var expressionEvaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();

        var engine = new ExpressionParser();
        var actual = expressionEvaluator.Evaluate(engine.BuildAst(expression).Value!);

        actual.Should().BeEquivalentTo(result);
    }

    [Theory]
    [InlineData("@func(outputs()['value1'])", 3)]
    [InlineData("@func(4,outputs()['value1'])", 3)]
    public void TestOverloads(string expression, object result)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.RegisterFunction<MultiplicationFunction>("mult");
        services.RegisterFunction<OutputsFunction>("outputs");
        services.RegisterFunction<Function1>("func");
        services.RegisterFunction<Function2>("func");

        services.DiscoverFunctions();

        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();

        var serviceProvider = services.BuildServiceProvider();

        var expressionEvaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();

        var engine = new ExpressionParser();
        var ast = engine.BuildAst(expression);
        var actual = expressionEvaluator.Evaluate(ast.Value!);
       
        if(actual is FunctionOutout fo)
        {
            actual = fo.value;
        }
        actual.Should().BeEquivalentTo(result);
    }

    [Theory]
    [InlineData("@func(func1(outputs()['value1']))", 3)]
    [InlineData("@func(func2(4,4),func1(outputs()['value1']))", 3)]
    public void TestOverloadsInSameFunction(string expression, object result)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.RegisterFunction<MultiplicationFunction>("mult");
        services.RegisterFunction<OutputsFunction>("outputs");
        services.RegisterFunction<Function1>("func1");
        services.RegisterFunction<Function2>("func2");
        services.RegisterFunction<Function3>("func");

        services.DiscoverFunctions();

        services.AddSingleton<IExpressionEvaluator, ExpressionEvaluator>();

        var serviceProvider = services.BuildServiceProvider();

        var expressionEvaluator = serviceProvider.GetRequiredService<IExpressionEvaluator>();

        var engine = new ExpressionParser();
        var ast = engine.BuildAst(expression);
        var actual = expressionEvaluator.Evaluate(ast.Value!);

        if (actual is FunctionOutout fo)
        {
            actual = fo.value;
        }
        actual.Should().BeEquivalentTo(result);
    }
}
