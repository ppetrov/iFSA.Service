using System;

namespace iFSA.Service
{
	public abstract class ClientHandlerBase
	{
		public byte Id { get; private set; }
		public TransferHandler TransferHandler { get; private set; }

		protected ClientHandlerBase(byte id, TransferHandler transferHandler)
		{
			if (id <= 0) throw new ArgumentOutOfRangeException("id");
			if (transferHandler == null) throw new ArgumentNullException("transferHandler");

			this.Id = id;
			this.TransferHandler = transferHandler;
		}
	}
}