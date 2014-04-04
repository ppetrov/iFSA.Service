using System;
using System.IO;

namespace iFSA.Service
{
	public class Utilities
	{
		public static byte[] Concat(byte[] x, byte[] y)
		{
			if (x == null) throw new ArgumentNullException("x");
			if (x.Length == 0) throw new ArgumentOutOfRangeException("x");
			if (y == null) throw new ArgumentNullException("y");
			if (y.Length == 0) throw new ArgumentOutOfRangeException("y");

			using (var ms = new MemoryStream(x.Length + y.Length))
			{
				ms.Write(x, 0, x.Length);
				ms.Write(y, 0, y.Length);

				return ms.GetBuffer();
			}
		}

		public static decimal GetProgressPercent(int totalBytes, int readBytes)
		{
			return (readBytes * 100.0M) / totalBytes;
		}
	}
}