using System;

namespace iFSA.Service.Core
{
	public abstract class ClientHandler
	{
		private readonly ITransferHandler _transferHandler;

		public byte Id { get; private set; }
		public abstract string Name { get; }

		protected ITransferHandler TransferHandler { get { return _transferHandler; } }

		protected ClientHandler(byte id, ITransferHandler transferHandler)
		{
			if (transferHandler == null) throw new ArgumentNullException("transferHandler");

			this.Id = id;
			_transferHandler = transferHandler;
		}

		protected void LogRequest(string method)
		{
			// TODO : !!!
			//Trace.WriteLine(string.Format(@"Request to {0}({1})", this.Name, method));
		}

		protected void LogRequest(byte[] data, string method)
		{
			// TODO : !!!
			//Trace.WriteLine(string.Format(@"Send {0} bytes to {1}({2})", data.Length, this.Name, method));
		}

		protected void LogResponse(byte[] data, string method)
		{
			// TODO : !!!
			//Trace.WriteLine(string.Format(@"Read {0} bytes from '{1}'({2})", data.Length, this.Name, method));
		}
	}
}