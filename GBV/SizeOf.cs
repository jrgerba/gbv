using System.Reflection.Emit;

namespace GBV;

public static class TypeInfo<T>
{
    public static readonly int Size;

    static TypeInfo()
    {
        var dm = new DynamicMethod("SizeOfType", typeof(int), Array.Empty<Type>());
        ILGenerator il = dm.GetILGenerator();
        il.Emit(OpCodes.Sizeof, typeof(T));
        il.Emit(OpCodes.Ret);
        Size = (int)dm.Invoke(null, null);
    }
}