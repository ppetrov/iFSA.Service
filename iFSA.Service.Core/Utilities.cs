using System;

namespace iFSA.Service.Core
{
	public static class Utilities
	{
		public static byte[] Concat(byte[] x, byte[] y)
		{
			if (x == null) throw new ArgumentNullException("x");
			if (y == null) throw new ArgumentNullException("y");
			if (x.Length == 0) throw new ArgumentOutOfRangeException("x");
			if (y.Length == 0) throw new ArgumentOutOfRangeException("y");

			var result = new byte[x.Length + y.Length];

			var offset = 0;
			Array.Copy(x, 0, result, offset, x.Length);
			offset += x.Length;
			Array.Copy(y, 0, result, offset, y.Length);

			return result;
		}

		public static byte[] Concat(byte[] x, byte[] y, byte[] z)
		{
			if (x == null) throw new ArgumentNullException("x");
			if (y == null) throw new ArgumentNullException("y");
			if (z == null) throw new ArgumentNullException("z");
			if (x.Length == 0) throw new ArgumentOutOfRangeException("x");
			if (y.Length == 0) throw new ArgumentOutOfRangeException("y");
			if (z.Length == 0) throw new ArgumentOutOfRangeException("z");

			var result = new byte[x.Length + y.Length + z.Length];

			var offset = 0;
			Array.Copy(x, 0, result, offset, x.Length);
			offset += x.Length;
			Array.Copy(y, 0, result, offset, y.Length);
			offset += y.Length;
			Array.Copy(z, 0, result, offset, z.Length);

			return result;
		}

		public static decimal GetProgressPercent(int totalBytes, int readBytes)
		{
			return (readBytes * 100.0M) / totalBytes;
		}
	}
}
