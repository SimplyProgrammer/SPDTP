/**
 *	Helper utilities
*/
public class Utils 
{
	public static byte[] getBytes(ushort value)
	{
		return new byte[] {
			(byte) ((value >> 8) & 0xFF),
			(byte) (value & 0xFF)
		};
	}

	public static ushort getShort(byte[] bytes, int from = 0)
	{
		return (ushort) ((bytes[from] << 8) | bytes[from+1]);
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

	public static int getInt24(byte[] bytes, int from = 0)
	{
		return (bytes[from] << 16) | (bytes[from+1] << 8) | bytes[from+2];
	}

	public static int getHashIdentifier(String str, int hash = 7)
	{
		for (int i = 0, len = str.Length; i < len; i++)
			hash = hash*31 + str[i];
		return hash;
	}

	public static String formatHeader(byte[] bytes, int cols = 4) // TODO impl
	{
		String formatted = "";

		String[] binaries = bytes.Select( x => Convert.ToString( x, 2 ).PadLeft( 8, '0' ) ).ToArray();
		for (int i = 0; i < binaries.Length; i++)
		{
			String bits = binaries[i];
			formatted += (i > 0 && i % cols == 0) ? "\n" + bits : (i > 0 ? "|" + bits : bits);
		}

		return formatted;
	}

	public static byte[] introduceRandErrors(byte[] bytes, int errorCount = 1, int seed = 123)
	{
		Random rand = new Random(seed);

		while (errorCount-- > 0)
		{
			int octet = rand.Next(bytes.Length-1), bit = rand.Next(8);
			// Console.WriteLine(octet + ", " + bit);
			bytes[octet] ^= (byte) (0b1 << bit);
		}
		return bytes;
	}

	public static string truncString(String input, int maxLength, String substitute = "_")
	{
		if (input.Length <= maxLength)
			return input;

		return substitute + input.Substring(input.Length - maxLength + substitute.Length);
	}
}