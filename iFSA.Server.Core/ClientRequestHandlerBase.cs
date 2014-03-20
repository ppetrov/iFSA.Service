using System;

namespace iFSA.Server.Core
{
	public abstract class ClientRequestHandlerBase : RequestHandler
	{
		public TransferHandler TransferHandler { get; private set; }

		protected ClientRequestHandlerBase(byte id, TransferHandler transferHandler)
			: base(id)
		{
			if (transferHandler == null) throw new ArgumentNullException("transferHandler");

			this.TransferHandler = transferHandler;
		}
	}
}