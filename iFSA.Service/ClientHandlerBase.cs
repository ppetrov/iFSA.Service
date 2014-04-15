using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;

namespace iFSA.Service
{
	public abstract class ClientHandlerBase : IDisposable
	{
		private readonly TcpClient _client;
		private readonly Stream _stream;

		public byte Id { get; private set; }
		public abstract string Name { get; }

		public TransferHandler TransferHandler { get; private set; }

		protected Stream Stream { get { return _stream; } }

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
			this.TransferHandler.WriteClose(_stream);

			using (_stream) { }
			using (_client) { }
		}

		protected void LogRequest(string method)
		{
			Trace.WriteLine(string.Format(@"Request to {0}({1})", this.Name, method));
		}

		protected void LogRequest(byte[] data, string method)
		{
			Trace.WriteLine(string.Format(@"Send {0} bytes to {1}({2})", data.Length, this.Name, method));
		}

		protected void LogResponse(byte[] data, string method)
		{
			Trace.WriteLine(string.Format(@"Read {0} bytes from '{1}'({2})", data.Length, this.Name, method));
		}
	}
}