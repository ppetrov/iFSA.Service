using System;
using System.IO;

namespace iFSA.Service
{
	public static class Utilities
	{
		public static byte[] Concat(byte[] x, byte[] y)
		{
			if (x == null) throw new ArgumentNullException("x");
			if (y == null) throw new ArgumentNullException("y");
			if (x.Length == 0) throw new ArgumentOutOfRangeException("x");
			if (y.Length == 0) throw new ArgumentOutOfRangeException("y");

			using (var ms = new MemoryStream(x.Length + y.Length))
			{
				ms.Write(x, 0, x.Length);
				ms.Write(y, 0, y.Length);

				return ms.GetBuffer();
			}
		}

		public static byte[] Concat(byte[] x, byte[] y, byte[] z)
		{
			if (x == null) throw new ArgumentNullException("x");
			if (y == null) throw new ArgumentNullException("y");
			if (z == null) throw new ArgumentNullException("z");
			if (x.Length == 0) throw new ArgumentOutOfRangeException("x");
			if (y.Length == 0) throw new ArgumentOutOfRangeException("y");
			if (z.Length == 0) throw new ArgumentOutOfRangeException("z");

			using (var ms = new MemoryStream(x.Length + y.Length + z.Length))
			{
				ms.Write(x, 0, x.Length);
				ms.Write(y, 0, y.Length);
				ms.Write(z, 0, z.Length);

				return ms.GetBuffer();
			}
		}

		public static decimal GetProgressPercent(int totalBytes, int readBytes)
		{
			return (readBytes * 100.0M) / totalBytes;
		}
	}
}