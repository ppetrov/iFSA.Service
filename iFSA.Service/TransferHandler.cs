using System;
using System.IO;
using System.Threading.Tasks;

namespace iFSA.Service
{
	public sealed class TransferHandler
	{
		public static readonly byte[] NoDataBytes = { 255, 255, 255, 255 };

		private readonly Stream _stream;
		private readonly byte[] _buffer;

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

		public TransferHandler(Stream stream, byte[] buffer)
		{
			if (stream == null) throw new ArgumentNullException("stream");
			if (buffer == null) throw new ArgumentNullException("buffer");
			if (buffer.Length == 0) throw new ArgumentOutOfRangeException("buffer");

			_stream = stream;
			_buffer = buffer;
		}

		public async Task WriteAsync(byte handlerId, byte methodId)
		{
			_buffer[0] = handlerId;
			_buffer[1] = methodId;
			await _stream.WriteAsync(_buffer, 0, 2);
		}

		public async Task WriteAsync(byte[] data)
		{
			if (data == null) throw new ArgumentNullException("data");
			if (data.Length == 0) throw new ArgumentOutOfRangeException("data");

			this.OnWriteProgress(0);

			data = await new CompressionHelper(_buffer).CompressAsync(data);

			// Write size
			var totalBytes = data.Length;
			NetworkHelper.FillBytes(totalBytes, _buffer);
			await _stream.WriteAsync(_buffer, 0, NetworkHelper.GetBytesSize(totalBytes));

			// Write data
			var bufferLength = _buffer.Length;
			var chunks = (totalBytes / bufferLength) * bufferLength;
			for (var i = 0; i < chunks; i += bufferLength)
			{
				await _stream.WriteAsync(data, i, bufferLength);
				this.OnWriteProgress(Utilities.GetProgressPercent(totalBytes, i + bufferLength));
			}
			var remaining = totalBytes % bufferLength;
			if (remaining != 0)
			{
				await _stream.WriteAsync(data, chunks, remaining);
				this.OnWriteProgress(100);
			}
		}

		public async Task CloseAsync()
		{
			_buffer[0] = byte.MaxValue;
			await _stream.WriteAsync(_buffer, 0, 1);
		}

		public async Task<byte[]> ReadDataAsync()
		{
			this.OnReadProgress(0);

			// Read size
			await ReadDataAsync(_buffer, NetworkHelper.GetBytesSize(0));
			var dataSize = BitConverter.ToInt32(_buffer, 0);

			// Read data
			using (var output = new MemoryStream(dataSize))
			{
				var bufferLength = _buffer.Length;
				var chunks = (dataSize / bufferLength) * bufferLength;
				for (var i = 0; i < chunks; i += bufferLength)
				{
					await this.ReadDataAsync(_buffer, bufferLength);
					await output.WriteAsync(_buffer, 0, bufferLength);
					this.OnReadProgress(Utilities.GetProgressPercent(dataSize, i + bufferLength));
				}
				var remaining = dataSize % bufferLength;
				if (remaining != 0)
				{
					await this.ReadDataAsync(_buffer, remaining);
					await output.WriteAsync(_buffer, 0, remaining);
					this.OnReadProgress(100);
				}
				return await new CompressionHelper(_buffer).DecompressAsync(output.GetBuffer());
			}
		}

		private async Task ReadDataAsync(byte[] buffer, int count)
		{
			var offset = 0;

			int readBytes;
			while ((readBytes = await _stream.ReadAsync(buffer, offset, count)) != 0)
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