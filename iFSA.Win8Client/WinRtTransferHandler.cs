using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using iFSA.Service.Core;

namespace iFSA.Win8Client
{
	public class WinRtTransferHandler : ITransferHandler
	{
		private readonly byte[] _buffer = new byte[16 * 1024];

		private readonly DataReader _reader;
		private readonly DataWriter _writer;

		public WinRtTransferHandler(StreamSocket socket)
		{
			if (socket == null) throw new ArgumentNullException("socket");

			_reader = new DataReader(socket.InputStream);
			_writer = new DataWriter(socket.OutputStream);
		}

		public async Task WriteAsync(byte handlerId, byte methodId)
		{
			_writer.WriteByte(handlerId);
			_writer.WriteByte(methodId);

			await _writer.FlushAsync();
			await _writer.StoreAsync();
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
			_writer.WriteBuffer(_buffer.AsBuffer(), 0, (uint)NetworkHelper.GetBytesSize(totalBytes));
			await _writer.FlushAsync();
			await _writer.StoreAsync();

			// Write data
			var bufferLength = _buffer.Length;
			var chunks = (totalBytes / bufferLength) * bufferLength;
			for (var i = 0; i < chunks; i += bufferLength)
			{
				_writer.WriteBuffer(data.AsBuffer(), (uint)i, (uint)bufferLength);
				await _writer.FlushAsync();
				await _writer.StoreAsync();

				this.OnWriteProgress(Utilities.GetProgressPercent(totalBytes, i + bufferLength));
			}
			var remaining = totalBytes % bufferLength;
			if (remaining != 0)
			{
				_writer.WriteBuffer(data.AsBuffer(), (uint)chunks, (uint)remaining);
				await _writer.FlushAsync();
				await _writer.StoreAsync();

				this.OnWriteProgress(100);
			}
		}

		public async Task CloseAsync()
		{
			_writer.WriteByte(byte.MaxValue);
			await _writer.FlushAsync();
			await _writer.StoreAsync();
		}

		public async Task<byte[]> ReadAsync()
		{
			this.OnReadProgress(0);

			// Read size
			await ReadAsync(_buffer, NetworkHelper.GetBytesSize(0));
			var dataSize = BitConverter.ToInt32(_buffer, 0);

			// Read data
			var buffer = new byte[dataSize];
			using (var output = new MemoryStream(buffer))
			{
				var bufferLength = _buffer.Length;
				var chunks = (dataSize / bufferLength) * bufferLength;
				for (var i = 0; i < chunks; i += bufferLength)
				{
					await this.ReadAsync(_buffer, bufferLength);
					await output.WriteAsync(_buffer, 0, bufferLength);
					this.OnReadProgress(Utilities.GetProgressPercent(dataSize, i + bufferLength));
				}
				var remaining = dataSize % bufferLength;
				if (remaining != 0)
				{
					await this.ReadAsync(_buffer, remaining);
					await output.WriteAsync(_buffer, 0, remaining);
					this.OnReadProgress(100);
				}
				return await new CompressionHelper(_buffer).DecompressAsync(buffer);
			}
		}

		private async Task ReadAsync(byte[] buffer, int count)
		{
			await _reader.LoadAsync((uint)count);
			_reader.ReadBuffer((uint)count).CopyTo(buffer);
		}

		private void OnReadProgress(decimal percent)
		{

		}

		private void OnWriteProgress(decimal percent)
		{

		}
	}
}