namespace Compo;

public interface IFunction;

public interface IFunction<in T, out TR> : IFunction
    where T : IConvertible
    where TR : IConvertible
{
    public TR Execute(T t);
}

public interface IFunctionParams<in T, out TR> : IFunction
    where T : IConvertible
    where TR : IConvertible
{
    public TR Execute(params T[] ts);
}

public interface IFunction<in T1, in T2, out TR> : IFunction
    where T1 : IConvertible
    where T2 : IConvertible
    where TR : IConvertible
{
    public TR Execute(T1 t1, T2 t2);
}

public interface IFunction<in T1, in T2, in T3, out TR> : IFunction
    where T1 : IConvertible
    where T2 : IConvertible
    where T3 : IConvertible
    where TR : IConvertible
{
    public TR Execute(T1 t1, T2 t2, T3 t3);
}
