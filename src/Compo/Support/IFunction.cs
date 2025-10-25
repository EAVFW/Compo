namespace Compo;

public interface IFunction;

public interface IFunction<out TR> : IFunction
{
    public TR Execute();
}

public interface IFunction<in T, out TR> : IFunction
   // where T : IConvertible
{
    public TR Execute(T t);
}

public interface IFunctionParams<in T, out TR> : IFunction
  //  where T : IConvertible
{
    public TR Execute(params T[] ts);
}

public interface IFunction<in T1, in T2, out TR> : IFunction
  //  where T1 : IConvertible
   // where T2 : IConvertible
{
    public TR Execute(T1 t1, T2 t2);
}

public interface IFunction<in T1, in T2, in T3, out TR> : IFunction
  //  where T1 : IConvertible
  //  where T2 : IConvertible
  //  where T3 : IConvertible
{
    public TR Execute(T1 t1, T2 t2, T3 t3);
}

public interface IFunction<in T1, in T2, in T3, in T4, out TR> : IFunction
{
    public TR Execute(T1 t1, T2 t2, T3 t3, T4 t4);
}

public interface IFunction<in T1, in T2, in T3, in T4, in T5, out TR> : IFunction
{
    public TR Execute(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5);
}

public interface IFunction<in T1, in T2, in T3, in T4, in T5, in T6, out TR> : IFunction
{
    public TR Execute(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6);
}
public interface IFunction<in T1, in T2, in T3, in T4, in T5, in T6, in T7, out TR> : IFunction
{
    public TR Execute(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7);
}
public interface IFunction<in T1, in T2, in T3, in T4, in T5, in T6, in T7, in T8, out TR> : IFunction
{
    public TR Execute(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8);
}

