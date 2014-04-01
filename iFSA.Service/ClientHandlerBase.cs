using System;
using System.IO;
using System.Net.Sockets;

namespace iFSA.Service
{
	public abstract class ClientHandlerBase : IDisposable
	{
		private readonly TcpClient _client;
		private readonly Stream _stream;

		public byte Id { get; private set; }
		public TransferHandler TransferHandler { get; private set; }

		protected Stream Stream
		{
			get { return _stream; }
		}

		protected ClientHandlerBase(byte id, string hostname, int port)
		{
			if (hostname == null) throw new ArgumentNullException("hostname");

			this.Id = id;
			this.TransferHandler = new TransferHandler();

			_client = new TcpClient(hostname, port);
			_stream = _client.GetStream();
		}

		public void Dispose()
		{
			this.TransferHandler.WriteClose(Stream);

			using (_stream) { }
			using (_client) { }
		}
	}
}