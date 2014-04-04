using System;
using System.IO;
using System.IO.Compression;

namespace iFSA.Service
{
	public sealed class CompressionHelper
	{
		private readonly byte[] _buffer;

		public event EventHandler<decimal> PercentProgress;
		private void OnPercentProgress(decimal e)
		{
			var handler = PercentProgress;
			if (handler != null) handler(this, e);
		}

		public CompressionHelper(byte[] buffer)
		{
			if (buffer == null) throw new ArgumentNullException("buffer");
			if (buffer.Length == 0) throw new ArgumentOutOfRangeException("buffer");

			_buffer = buffer;
		}

		public byte[] Compress(byte[] data)
		{
			if (data == null) throw new ArgumentNullException("data");
			if (data.Length == 0) throw new ArgumentOutOfRangeException("data");

			using (var input = new MemoryStream(data))
			{
				using (var output = new MemoryStream())
				{
					using (var zipStream = new GZipStream(output, CompressionMode.Compress))
					{
						this.Process(input, zipStream, data.Length);
					}
					return output.ToArray();
				}
			}
		}

		public byte[] Decompress(byte[] data)
		{
			if (data == null) throw new ArgumentNullException("data");
			if (data.Length == 0) throw new ArgumentOutOfRangeException("data");

			using (var input = new MemoryStream(data))
			{
				using (var output = new MemoryStream())
				{
					using (var zipStream = new GZipStream(input, CompressionMode.Decompress))
					{
						this.Process(zipStream, output, data.Length);
					}
					return output.ToArray();
				}
			}
		}

		private void Process(Stream input, Stream output, int totalBytes)
		{
			this.OnPercentProgress(0);

			int readBytes;
			int totalReadBytes = 0;
			while ((readBytes = input.Read(_buffer, 0, _buffer.Length)) != 0)
			{
				output.Write(_buffer, 0, readBytes);

				totalReadBytes += readBytes;
				this.OnPercentProgress(Utilities.GetProgressPercent(totalBytes, totalReadBytes));
			}
		}
	}
}