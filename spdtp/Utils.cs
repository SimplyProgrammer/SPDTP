/**
 *	Helper utilities
*/
public class Utils 
{
	public static byte[] getBytes(short value)
	{
		return new byte[] {
			(byte) ((value >> 8) & 0xFF),
			(byte) (value & 0xFF)
		};
	}

	public static short getShort(byte[] bytes, int from = 0)
	{
		return (short) ((bytes[from] << 8) | bytes[from+1]);
	}

	public static byte[] getBytes(int value)
	{
		return new byte[] {
			(byte) ((value >> 24) & 0xFF),
			(byte) ((value >> 16) & 0xFF),
			(byte) ((value >> 8) & 0xFF),
			(byte) (value & 0xFF)
		};
	}

	public static int getInt(byte[] bytes, int from = 0)
	{
		return (bytes[from] << 24) | (bytes[from+1] << 16) | (bytes[from+2] << 8) | bytes[from+3];
	}

	public static void printHeader(byte[] bytes, int cols = 4)
	{
		String[] bits = bytes.Select( x => Convert.ToString( x, 2 ).PadLeft( 8, '0' ) ).ToArray();
		for (int i = 0; i < bits.Length; i++)
		{
			String str = bits[i];
			Console.Write(i > 0 ? "|" + str : str);
			if (i >= cols && i % cols == 0)
				Console.WriteLine();
		}
		Console.WriteLine();
	}
}