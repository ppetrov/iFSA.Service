using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

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

			return Process(data, CompressionMode.Compress);
		}

		public Task<byte[]> CompressAsync(byte[] data)
		{
			if (data == null) throw new ArgumentNullException("data");
			if (data.Length == 0) throw new ArgumentOutOfRangeException("data");

			return ProcessAsync(data, CompressionMode.Compress);
		}

		public byte[] Decompress(byte[] data)
		{
			if (data == null) throw new ArgumentNullException("data");
			if (data.Length == 0) throw new ArgumentOutOfRangeException("data");

			return Process(data, CompressionMode.Decompress);
		}

		public Task<byte[]> DecompressAsync(byte[] data)
		{
			if (data == null) throw new ArgumentNullException("data");
			if (data.Length == 0) throw new ArgumentOutOfRangeException("data");

			return ProcessAsync(data, CompressionMode.Decompress);
		}

		private byte[] Process(byte[] data, CompressionMode mode)
		{
			using (var inStream = new MemoryStream(data))
			{
				using (var outStream = new MemoryStream())
				{
					Stream zipInput;
					switch (mode)
					{
						case CompressionMode.Decompress:
							zipInput = inStream;
							break;
						case CompressionMode.Compress:
							zipInput = outStream;
							break;
						default:
							throw new ArgumentOutOfRangeException("mode");
					}
					using (var zipStream = new GZipStream(zipInput, mode))
					{
						Stream input = inStream;
						Stream output = zipStream;
						if (mode == CompressionMode.Decompress)
						{
							input = zipStream;
							output = outStream;
						}

						var totalBytes = data.Length;
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
					return outStream.ToArray();
				}
			}
		}

		private async Task<byte[]> ProcessAsync(byte[] data, CompressionMode mode)
		{
			using (var inStream = new MemoryStream(data))
			{
				using (var outStream = new MemoryStream())
				{
					Stream zipInput;
					switch (mode)
					{
						case CompressionMode.Decompress:
							zipInput = inStream;
							break;
						case CompressionMode.Compress:
							zipInput = outStream;
							break;
						default:
							throw new ArgumentOutOfRangeException("mode");
					}
					using (var zipStream = new GZipStream(zipInput, mode))
					{
						Stream input = inStream;
						Stream output = zipStream;
						if (mode == CompressionMode.Decompress)
						{
							input = zipStream;
							output = outStream;
						}

						var totalBytes = data.Length;
						this.OnPercentProgress(0);

						int readBytes;
						int totalReadBytes = 0;
						while ((readBytes = await input.ReadAsync(_buffer, 0, _buffer.Length)) != 0)
						{
							await output.WriteAsync(_buffer, 0, readBytes);

							totalReadBytes += readBytes;
							this.OnPercentProgress(Utilities.GetProgressPercent(totalBytes, totalReadBytes));
						}
					}
					return outStream.ToArray();
				}
			}
		}
	}
}