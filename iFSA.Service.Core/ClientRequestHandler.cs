using System;

namespace iFSA.Service.Core
{
	public abstract class ClientRequestHandler : RequestHandler
	{
		public TransferHandler TransferHandler { get; private set; }

		protected ClientRequestHandler(byte id, TransferHandler transferHandler)
			: base(id)
		{
			if (transferHandler == null) throw new ArgumentNullException("transferHandler");

			this.TransferHandler = transferHandler;
		}
	}
}