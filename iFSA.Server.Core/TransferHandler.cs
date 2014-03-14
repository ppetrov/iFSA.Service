using System;
using System.IO;
using System.IO.Compression;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace iFSA.Server.Core
{
	public class TransferHandler
	{
		private readonly byte[] _buffer = new byte[16 * 1024];
		private readonly byte[] _byteBuffer = new byte[1];
		private readonly byte[] _sizeBuffer = BitConverter.GetBytes(Convert.ToInt32(0));

		public bool EnableCompression { get; set; }

		public event EventHandler<decimal> WriteProgress;
		protected virtual void OnWriteProgress(decimal e)
		{
			var handler = WriteProgress;
			if (handler != null) handler(this, e);
		}

		public event EventHandler<decimal> ReadProgress;
		protected virtual void OnReadProgress(decimal e)
		{
			var handler = ReadProgress;
			if (handler != null) handler(this, e);
		}

		public TransferHandler()
		{
			this.EnableCompression = true;
		}

		public void Write(NetworkStream stream, byte handlerId)
		{
			if (stream == null) throw new ArgumentNullException("stream");
			if (handlerId <= 0) throw new ArgumentOutOfRangeException("stream");

			stream.WriteByte(handlerId);
		}

		public void WriteClose(NetworkStream stream)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			this.Write(stream, byte.MaxValue);
		}

		public void WriteData(NetworkStream stream, byte[] input)
		{
			if (stream == null) throw new ArgumentNullException("stream");
			if (input == null) throw new ArgumentNullException("input");
			if (input.Length == 0) throw new ArgumentOutOfRangeException("input");

			this.OnWriteProgress(this.GetProgressPercent(0));

			var data = this.Compress(input);

			// Write packet size
			var dataSize = data.Length;
			var size = BitConverter.GetBytes(Convert.ToInt32(dataSize));
			stream.Write(size, 0, size.Length);

			// Write data
			var bufferLength = _buffer.Length;
			var parts = (dataSize / bufferLength) * bufferLength;
			for (var i = 0; i < parts; i += bufferLength)
			{
				stream.Write(data, i, bufferLength);
				this.OnWriteProgress(this.GetProgressPercent(dataSize, i + bufferLength));
			}

			stream.Write(data, parts, dataSize % bufferLength);
			this.OnWriteProgress(this.GetProgressPercent(dataSize, dataSize));
		}

		public async Task WriteDataAsync(NetworkStream stream, byte[] input)
		{
			if (stream == null) throw new ArgumentNullException("stream");
			if (input == null) throw new ArgumentNullException("input");
			if (input.Length == 0) throw new ArgumentOutOfRangeException("input");

			this.OnWriteProgress(this.GetProgressPercent(0));

			var data = await this.CompressAsync(input);

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

		public byte[] ReadData(NetworkStream stream)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			this.OnReadProgress(this.GetProgressPercent(0));

			// Read size
			var index = 0;
			while (index != _sizeBuffer.Length && (stream.Read(_byteBuffer, 0, _byteBuffer.Length)) != 0)
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
					this.ReadData(stream, _buffer, length);
					ms.Write(_buffer, 0, length);
					this.OnReadProgress(this.GetProgressPercent(dataSize, i + length));
				}
				var remaining = dataSize % length;
				if (remaining != 0)
				{
					this.ReadData(stream, _buffer, remaining);
					ms.Write(_buffer, 0, remaining);
					this.OnReadProgress(this.GetProgressPercent(dataSize, dataSize));
				}
				return this.Decompress(ms.GetBuffer());
			}
		}

		public async Task<byte[]> ReadDataAsync(NetworkStream stream)
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
				return await this.DecompressAsync(ms.GetBuffer());
			}
		}

		private void ReadData(Stream stream, byte[] buffer, int count)
		{
			var offset = 0;

			int readBytes;
			while ((readBytes = stream.Read(buffer, offset, count)) != 0)
			{
				count -= readBytes;
				if (count == 0)
				{
					return;
				}
				offset += readBytes;
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

		private byte[] Compress(byte[] data)
		{
			if (!this.EnableCompression)
			{
				return data;
			}
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
		}

		private Task<byte[]> CompressAsync(byte[] data)
		{
			return !this.EnableCompression ? Task.FromResult(data) : Task.Run(() => this.Compress(data));
		}

		private byte[] Decompress(byte[] data)
		{
			if (!this.EnableCompression)
			{
				return data;
			}
			using (var input = new MemoryStream(data))
			{
				using (var output = new MemoryStream())
				{
					using (var zipStream = new GZipStream(input, CompressionMode.Decompress))
					{
						int readBytes;
						while ((readBytes = zipStream.Read(_buffer, 0, _buffer.Length)) > 0)
						{
							output.Write(_buffer, 0, readBytes);
						}
					}
					return output.ToArray();
				}
			}
		}

		private Task<byte[]> DecompressAsync(byte[] data)
		{
			return !this.EnableCompression ? Task.FromResult(data) : Task.Run(() => this.Decompress(data));
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