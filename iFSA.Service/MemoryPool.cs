using System;

namespace iFSA.Service
{
	public static class MemoryPool
	{
		private static readonly object Sync = new object();

		public const int Size16K = 16 * 1024;
		private static readonly bool[] Used16KBuffers = { false, false, false };
		private static readonly byte[][] Buffers16K = { null, null, null };

		public const int Size80K = 80 * 1024;
		private static readonly bool[] Used80KBuffers = { false, false, false };
		private static readonly byte[][] Buffers80K = { null, null, null };

		public static byte[] Get16KBuffer()
		{
			lock (Sync)
			{
				for (var i = 0; i < Used16KBuffers.Length; i++)
				{
					if (!Used16KBuffers[i])
					{
						Used16KBuffers[i] = true;
						return Buffers16K[i] = Buffers16K[i] ?? new byte[Size16K];
					}
				}
			}

			return new byte[Size16K];
		}

		public static void Return16KBuffer(byte[] buffer)
		{
			if (buffer == null) throw new ArgumentNullException("buffer");
			if (buffer.Length != Size16K) throw new ArgumentOutOfRangeException("buffer");

			lock (Sync)
			{
				for (var i = 0; i < Buffers16K.Length; i++)
				{
					if (Buffers16K[i] == buffer)
					{
						Used16KBuffers[i] = false;
					}
				}
			}
		}

		public static byte[] Get80KBuffer()
		{
			lock (Sync)
			{
				for (var i = 0; i < Used80KBuffers.Length; i++)
				{
					if (!Used80KBuffers[i])
					{
						Used80KBuffers[i] = true;
						return Buffers80K[i] = Buffers80K[i] ?? new byte[Size80K];
					}
				}
			}

			return new byte[Size80K];
		}

		public static void Return80KBuffer(byte[] buffer)
		{
			if (buffer == null) throw new ArgumentNullException("buffer");

			lock (Sync)
			{
				for (var i = 0; i < Buffers80K.Length; i++)
				{
					if (Buffers80K[i] == buffer)
					{
						Used80KBuffers[i] = false;
					}
				}
			}
		}
	}
}