using System;
using System.Diagnostics;

namespace iFSA.Service
{
	public abstract class ClientHandler
	{
		private readonly TransferHandler _transferHandler;

		public byte Id { get; private set; }
		public abstract string Name { get; }

		protected TransferHandler TransferHandler { get { return _transferHandler; } }

		protected ClientHandler(byte id, TransferHandler transferHandler)
		{
			if (transferHandler == null) throw new ArgumentNullException("transferHandler");

			this.Id = id;
			_transferHandler = transferHandler;
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