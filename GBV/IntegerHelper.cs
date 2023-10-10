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
}