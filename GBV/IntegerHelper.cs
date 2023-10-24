using System.Numerics;

namespace GBV;

public static class IntegerHelper
{
    public static (byte high, byte low) SplitShort(ushort value)
    {
        (byte high, byte low) bytes;

        bytes.low = (byte)value;
        bytes.high = (byte)(value >> 8);

        return bytes;
    }

    public static ushort JoinBytes(byte high, byte low)
    {
        return (ushort)((high << 8) | low);
    }

    public static int GetBitCount<T>(T value) where T : IBinaryNumber<T>, IShiftOperators<T, T, T>
    {
        int i = 0;
        while (value != T.Zero)
        {
            value <<= T.One;
            i++;
        }

        return i;
    }
}