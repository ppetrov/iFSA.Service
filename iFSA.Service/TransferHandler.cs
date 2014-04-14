using System;
using System.IO;
using System.Threading.Tasks;

namespace iFSA.Service
{
	public sealed class TransferHandler
	{
		public static readonly byte[] NoDataBytes = { 255, 255, 255, 255 };

		private readonly byte[] _buffer = new byte[16 * 1024];

		public bool EnableCompression { get; private set; }

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
			: this(true)
		{
		}

		public TransferHandler(bool enableCompression)
		{
			this.EnableCompression = enableCompression;
		}

		public async Task WriteAsync(Stream stream, byte handlerId, byte methodId)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			_buffer[0] = handlerId;
			_buffer[1] = methodId;
			await stream.WriteAsync(_buffer, 0, 2);
		}

		public async Task WriteAsync(Stream stream, byte[] input)
		{
			if (stream == null) throw new ArgumentNullException("stream");
			if (input == null) throw new ArgumentNullException("input");
			if (input.Length == 0) throw new ArgumentOutOfRangeException("input");

			this.OnWriteProgress(0);

			var data = input;
			if (this.EnableCompression)
			{
#if ASYNC
				data = await new CompressionHelper(_buffer).CompressAsync(input);
#else
				data = await Task.Run(() => new CompressionHelper(_buffer).Compress(input)).ConfigureAwait(false);
#endif
			}

			// Write size
			var totalBytes = data.Length;
			NetworkHelper.FillBytes(totalBytes, _buffer);
			await stream.WriteAsync(_buffer, 0, NetworkHelper.GetBytesSize(totalBytes));

			// Write data
			var bufferLength = _buffer.Length;
			var chunks = (totalBytes / bufferLength) * bufferLength;
			for (var i = 0; i < chunks; i += bufferLength)
			{
				await stream.WriteAsync(data, i, bufferLength);
				this.OnWriteProgress(Utilities.GetProgressPercent(totalBytes, i + bufferLength));
			}
			var remaining = totalBytes % bufferLength;
			if (remaining != 0)
			{
				await stream.WriteAsync(data, chunks, remaining);
				this.OnWriteProgress(100);
			}
		}

		public void WriteClose(Stream stream)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			_buffer[0] = byte.MaxValue;
			stream.Write(_buffer, 0, 1);
		}

		public async Task<byte[]> ReadDataAsync(Stream stream)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			this.OnReadProgress(0);

			// Read size
			await ReadDataAsync(stream, _buffer, NetworkHelper.GetBytesSize(0));
			var dataSize = BitConverter.ToInt32(_buffer, 0);

			// Read data
			using (var output = new MemoryStream(dataSize))
			{
				var bufferLength = _buffer.Length;
				var chunks = (dataSize / bufferLength) * bufferLength;
				for (var i = 0; i < chunks; i += bufferLength)
				{
					await this.ReadDataAsync(stream, _buffer, bufferLength);
					await output.WriteAsync(_buffer, 0, bufferLength);
					this.OnReadProgress(Utilities.GetProgressPercent(dataSize, i + bufferLength));
				}
				var remaining = dataSize % bufferLength;
				if (remaining != 0)
				{
					await this.ReadDataAsync(stream, _buffer, remaining);
					await output.WriteAsync(_buffer, 0, remaining);
					this.OnReadProgress(100);
				}
				var input = output.GetBuffer();
				if (this.EnableCompression)
				{
#if ASYNC
					return await new CompressionHelper(_buffer).DecompressAsync(input);
#else
					return await Task.Run(() => new CompressionHelper(_buffer).Decompress(input)).ConfigureAwait(false);
#endif
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
	}
}