namespace JoyReactor.Accordion.Logic.Extensions;

public static class TaskTyped
{
    public static async Task<(T1, T2)> WhenAll<T1, T2>(Task<T1> t1, Task<T2> t2)
    {
        await Task.WhenAll(t1, t2);
        return (t1.Result, t2.Result);
    }

    public static async Task<(T1, T2, T3)> WhenAll<T1, T2, T3>(Task<T1> t1, Task<T2> t2, Task<T3> t3)
    {
        await Task.WhenAll(t1, t2, t3);
        return (t1.Result, t2.Result, t3.Result);
    }

    public static async Task<(T1, T2, T3, T4)> WhenAll<T1, T2, T3, T4>(Task<T1> t1, Task<T2> t2, Task<T3> t3, Task<T4> t4)
    {
        await Task.WhenAll(t1, t2, t3, t4);
        return (t1.Result, t2.Result, t3.Result, t4.Result);
    }

    public static async Task<(T1, T2, T3, T4, T5)> WhenAll<T1, T2, T3, T4, T5>(Task<T1> t1, Task<T2> t2, Task<T3> t3, Task<T4> t4, Task<T5> t5)
    {
        await Task.WhenAll(t1, t2, t3, t4, t5);
        return (t1.Result, t2.Result, t3.Result, t4.Result, t5.Result);
    }

    public static async Task<(T1, T2, T3, T4, T5, T6)> WhenAll<T1, T2, T3, T4, T5, T6>(Task<T1> t1, Task<T2> t2, Task<T3> t3, Task<T4> t4, Task<T5> t5, Task<T6> t6)
    {
        await Task.WhenAll(t1, t2, t3, t4, t5, t6);
        return (t1.Result, t2.Result, t3.Result, t4.Result, t5.Result, t6.Result);
    }

    public static async Task<(T1, T2, T3, T4, T5, T6, T7)> WhenAll<T1, T2, T3, T4, T5, T6, T7>(Task<T1> t1, Task<T2> t2, Task<T3> t3, Task<T4> t4, Task<T5> t5, Task<T6> t6, Task<T7> t7)
    {
        await Task.WhenAll(t1, t2, t3, t4, t5, t6, t7);
        return (t1.Result, t2.Result, t3.Result, t4.Result, t5.Result, t6.Result, t7.Result);
    }

    public static async Task<(T1, T2, T3, T4, T5, T6, T7, T8)> WhenAll<T1, T2, T3, T4, T5, T6, T7, T8>(Task<T1> t1, Task<T2> t2, Task<T3> t3, Task<T4> t4, Task<T5> t5, Task<T6> t6, Task<T7> t7, Task<T8> t8)
    {
        await Task.WhenAll(t1, t2, t3, t4, t5, t6, t7, t8);
        return (t1.Result, t2.Result, t3.Result, t4.Result, t5.Result, t6.Result, t7.Result, t8.Result);
    }

    public static async Task<(T1, T2, T3, T4, T5, T6, T7, T8, T9)> WhenAll<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Task<T1> t1, Task<T2> t2, Task<T3> t3, Task<T4> t4, Task<T5> t5, Task<T6> t6, Task<T7> t7, Task<T8> t8, Task<T9> t9)
    {
        await Task.WhenAll(t1, t2, t3, t4, t5, t6, t7, t8, t9);
        return (t1.Result, t2.Result, t3.Result, t4.Result, t5.Result, t6.Result, t7.Result, t8.Result, t9.Result);
    }

    public static async Task<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)> WhenAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Task<T1> t1, Task<T2> t2, Task<T3> t3, Task<T4> t4, Task<T5> t5, Task<T6> t6, Task<T7> t7, Task<T8> t8, Task<T9> t9, Task<T10> t10)
    {
        await Task.WhenAll(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
        return (t1.Result, t2.Result, t3.Result, t4.Result, t5.Result, t6.Result, t7.Result, t8.Result, t9.Result, t10.Result);
    }

    public static async Task<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)> WhenAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Task<T1> t1, Task<T2> t2, Task<T3> t3, Task<T4> t4, Task<T5> t5, Task<T6> t6, Task<T7> t7, Task<T8> t8, Task<T9> t9, Task<T10> t10, Task<T11> t11)
    {
        await Task.WhenAll(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
        return (t1.Result, t2.Result, t3.Result, t4.Result, t5.Result, t6.Result, t7.Result, t8.Result, t9.Result, t10.Result, t11.Result);
    }

    public static async Task<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)> WhenAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Task<T1> t1, Task<T2> t2, Task<T3> t4, Task<T4> t5, Task<T5> t6, Task<T6> t7, Task<T7> t8, Task<T8> t9, Task<T9> t10, Task<T10> t11, Task<T11> t12, Task<T12> t13)
    {
        await Task.WhenAll(t1, t2, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13);
        return (t1.Result, t2.Result, t4.Result, t5.Result, t6.Result, t7.Result, t8.Result, t9.Result, t10.Result, t11.Result, t12.Result, t13.Result);
    }

    public static async Task<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)> WhenAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Task<T1> t1, Task<T2> t2, Task<T3> t3, Task<T4> t4, Task<T5> t5, Task<T6> t6, Task<T7> t7, Task<T8> t8, Task<T9> t9, Task<T10> t10, Task<T11> t11, Task<T12> t12, Task<T13> t13)
    {
        await Task.WhenAll(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13);
        return (t1.Result, t2.Result, t3.Result, t4.Result, t5.Result, t6.Result, t7.Result, t8.Result, t9.Result, t10.Result, t11.Result, t12.Result, t13.Result);
    }

    public static async Task<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)> WhenAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Task<T1> t1, Task<T2> t2, Task<T3> t3, Task<T4> t4, Task<T5> t5, Task<T6> t6, Task<T7> t7, Task<T8> t8, Task<T9> t9, Task<T10> t10, Task<T11> t11, Task<T12> t12, Task<T13> t13, Task<T14> t14)
    {
        await Task.WhenAll(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14);
        return (t1.Result, t2.Result, t3.Result, t4.Result, t5.Result, t6.Result, t7.Result, t8.Result, t9.Result, t10.Result, t11.Result, t12.Result, t13.Result, t14.Result);
    }

    public static async Task<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)> WhenAll<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Task<T1> t1, Task<T2> t2, Task<T3> t3, Task<T4> t4, Task<T5> t5, Task<T6> t6, Task<T7> t7, Task<T8> t8, Task<T9> t9, Task<T10> t10, Task<T11> t11, Task<T12> t12, Task<T13> t13, Task<T14> t14, Task<T15> t15)
    {
        await Task.WhenAll(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15);
        return (t1.Result, t2.Result, t3.Result, t4.Result, t5.Result, t6.Result, t7.Result, t8.Result, t9.Result, t10.Result, t11.Result, t12.Result, t13.Result, t14.Result, t15.Result);
    }
}