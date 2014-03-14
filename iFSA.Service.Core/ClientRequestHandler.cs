using System;

namespace iFSA.Service.Core
{
	public abstract class ClientRequestHandler : TransferHandler
	{
		public byte Id { get; private set; }

		protected ClientRequestHandler(byte id)
		{
			if (id <= 0) throw new ArgumentOutOfRangeException("id");

			this.Id = id;
		}
	}
}