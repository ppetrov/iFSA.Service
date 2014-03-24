using System;

namespace iFSA.Server
{
	public abstract class ClientRequestHandlerBase
	{
		public byte Id { get; private set; }
		public TransferHandler TransferHandler { get; private set; }

		protected ClientRequestHandlerBase(byte id, TransferHandler transferHandler)
		{
			if (id <= 0) throw new ArgumentOutOfRangeException("id");
			if (transferHandler == null) throw new ArgumentNullException("transferHandler");

			this.Id = id;
			this.TransferHandler = transferHandler;
		}
	}
}