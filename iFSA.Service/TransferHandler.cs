using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace iFSA.Service
{
	public sealed class TransferHandler
	{
		public static readonly byte[] NoData = BitConverter.GetBytes(-1);

		private readonly byte[] _buffer = new byte[16 * 1024];
		private readonly byte[] _byteBuffer = new byte[1];
		private readonly byte[] _sizeBuffer = BitConverter.GetBytes(Convert.ToInt32(0));

		internal byte[] Buffer { get { return _buffer; } }
		public bool EnableCompression { get; set; }

		public event EventHandler<decimal> WriteProgress;
		private void OnWriteProgress(decimal e)
		{
			var handler = WriteProgress;
			if (handler != null) handler(this, e);
		}

		public event EventHandler<decimal> ReadProgress;
		private void OnReadProgress(decimal e)
		{
			var handler = ReadProgress;
			if (handler != null) handler(this, e);
		}

		public TransferHandler()
		{
			this.EnableCompression = true;
		}

		public async Task<byte[]> CompressAsync(byte[] data)
		{
			if (data == null) throw new ArgumentNullException("data");

			return await Task.Run(() =>
			{
				using (var input = new MemoryStream(data))
				{
					using (var output = new MemoryStream())
					{
						using (var zipStream = new GZipStream(output, CompressionMode.Compress))
						{
							int readBytes;
							while ((readBytes = input.Read(_buffer, 0, _buffer.Length)) != 0)
							{
								zipStream.Write(_buffer, 0, readBytes);
							}
						}
						return output.ToArray();
					}
				}
			}).ConfigureAwait(false);
		}

		public async Task<byte[]> DecompressAsync(byte[] data)
		{
			if (data == null) throw new ArgumentNullException("data");

			return await Task.Run(() =>
			{
				using (var input = new MemoryStream(data))
				{
					using (var output = new MemoryStream())
					{
						using (var zipStream = new GZipStream(input, CompressionMode.Decompress))
						{
							int readBytes;
							while ((readBytes = zipStream.Read(_buffer, 0, _buffer.Length)) != 0)
							{
								output.Write(_buffer, 0, readBytes);
							}
						}
						return output.ToArray();
					}
				}
			}).ConfigureAwait(false);
		}

		public async Task WriteMethodAsync(Stream stream, byte handlerId, byte methodId)
		{
			if (stream == null) throw new ArgumentNullException("stream");
			if (handlerId <= 0) throw new ArgumentOutOfRangeException("stream");

			_byteBuffer[0] = handlerId;
			await stream.WriteAsync(_byteBuffer, 0, _byteBuffer.Length);

			_byteBuffer[0] = methodId;
			await stream.WriteAsync(_byteBuffer, 0, _byteBuffer.Length);
		}

		public async Task WriteCloseAsync(Stream stream)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			_byteBuffer[0] = byte.MaxValue;
			await stream.WriteAsync(_byteBuffer, 0, _byteBuffer.Length);
		}

		public async Task WriteDataAsync(Stream stream, byte[] input)
		{
			if (stream == null) throw new ArgumentNullException("stream");
			if (input == null) throw new ArgumentNullException("input");
			if (input.Length == 0) throw new ArgumentOutOfRangeException("input");

			this.OnWriteProgress(this.GetProgressPercent(0));

			var data = input;
			if (this.EnableCompression)
			{
				data = await this.CompressAsync(data);
			}

			// Write packet size
			var dataSize = data.Length;
			var size = BitConverter.GetBytes(Convert.ToInt32(dataSize));
			await stream.WriteAsync(size, 0, size.Length);

			// Write data
			var bufferLength = _buffer.Length;
			var parts = (dataSize / bufferLength) * bufferLength;
			for (var i = 0; i < parts; i += bufferLength)
			{
				await stream.WriteAsync(data, i, bufferLength);
				this.OnWriteProgress(this.GetProgressPercent(dataSize, i + bufferLength));
			}

			await stream.WriteAsync(data, parts, dataSize % bufferLength);
			this.OnWriteProgress(this.GetProgressPercent(dataSize, dataSize));
		}

		public async Task<byte[]> ReadDataAsync(Stream stream)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			this.OnReadProgress(this.GetProgressPercent(0));

			// Read size
			var index = 0;
			while (index != _sizeBuffer.Length && (await stream.ReadAsync(_byteBuffer, 0, _byteBuffer.Length)) != 0)
			{
				_sizeBuffer[index++] = _byteBuffer[0];
			}
			var dataSize = BitConverter.ToInt32(_sizeBuffer, 0);

			// Read data
			using (var ms = new MemoryStream(dataSize))
			{
				var length = _buffer.Length;
				for (var i = 0; i < (dataSize / length) * length; i += length)
				{
					await this.ReadDataAsync(stream, _buffer, length);
					await ms.WriteAsync(_buffer, 0, length);
					this.OnReadProgress(this.GetProgressPercent(dataSize, i + length));
				}
				var remaining = dataSize % length;
				if (remaining != 0)
				{
					await this.ReadDataAsync(stream, _buffer, remaining);
					await ms.WriteAsync(_buffer, 0, remaining);
					this.OnReadProgress(this.GetProgressPercent(dataSize, dataSize));
				}
				var input = ms.GetBuffer();
				if (this.EnableCompression)
				{
					return await this.DecompressAsync(input);
				}
				return input;
			}
		}

		private async Task ReadDataAsync(Stream stream, byte[] buffer, int count)
		{
			var offset = 0;

			int readBytes;
			while ((readBytes = await stream.ReadAsync(buffer, offset, count)) != 0)
			{
				count -= readBytes;
				if (count == 0)
				{
					return;
				}
				offset += readBytes;
			}
		}

		private decimal GetProgressPercent(decimal value)
		{
			return value;
		}

		private decimal GetProgressPercent(int totalBytes, int readBytes)
		{
			return readBytes / (totalBytes / 100.0M);
		}
	}
}