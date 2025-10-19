//// See https://aka.ms/new-console-template for more information

//using Microsoft.Extensions.DependencyInjection;
//using Compo;
//using Compo.Functions.Math;
//using Pidgin;

//var services = new ServiceCollection();

//services.AddTransient<IFunction, AbsFunction>();
//services.AddTransient<IFunction, MultiplicationFunction>();

//services.AddSingleton(new FunctionRegistration
//{
//    FunctionName = "abs",
//    FunctionType = typeof(AbsFunction)
//});

//services.AddSingleton(new FunctionRegistration
//{
//    FunctionName = "mult",
//    FunctionType = typeof(MultiplicationFunction)
//});

//var sp = services.BuildServiceProvider();

//var functions = sp.GetServices<FunctionRegistration>();

//var multFunction = functions.FirstOrDefault(x => x.FunctionName == "mult");

//var invokable = sp.GetServices<IFunction>().FirstOrDefault(x => x.GetType() == multFunction!.FunctionType)!;

//var p1 = 2d;
//var p2 = 3d;


//object result = null;

//// Determine the type of IFunction interface
//// result = FunctionAuxiliary.Executer(invokable, p1, p2);

//if (result != null)
//{
//    Console.WriteLine($"Result: {result}");
//}
//else
//{
//    Console.WriteLine("Unable to invoke the function.");
//}


//var abs = sp.GetServices<IFunction>().FirstOrDefault(x => x.GetType() == typeof(AbsFunction))!;

//result = FunctionAuxiliary.FunctionInvoker(abs, [1.23f]);

//if (result != null)
//{
//    Console.WriteLine($"Result: {result}");
//}
//else
//{
//    Console.WriteLine("Unable to invoke the function.");
//}

//var a = Parser.Char('a');
//var b = Parser.Char('b');

//Parser<char, Node> m = null!;

//m = Parser.Map(
//    (arg1, arg2)
//        => (Node)new AccessNode(arg1, new ValueNode<char>(arg2)),
//    Parser.Rec(() => m).Or(a.Select(x => (Node)new ValueNode<char>(x))),
//    b);

//Console.WriteLine(m.Parse("aaab"));

//// var objs = new object[]{2d, 2d};

//// (invokable as IFunction<double, double, double>).Execute((dynamic)p1, (dynamic)p2);

//// var result = (invokable as IFunction<double, double, double>)?.Execute(objs[1]);

//// multFunction.FunctionType.

//// var functionType = typeof(IFunction<,>).MakeGenericType(p1.GetType(), typeof(double));
