using System;

namespace iFSA.Server.Core
{
	public abstract class RequestHandler
	{
		public byte Id { get; private set; }

		protected RequestHandler(byte id)
		{
			if (id <= 0) throw new ArgumentOutOfRangeException("id");

			this.Id = id;
		}
	}
}