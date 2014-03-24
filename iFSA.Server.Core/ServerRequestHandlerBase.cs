using System;
using System.Net.Sockets;

namespace iFSA.Server
{
	public abstract class ServerRequestHandlerBase
	{
		public byte Id { get; private set; }

		protected ServerRequestHandlerBase(byte id)
		{
			if (id <= 0) throw new ArgumentOutOfRangeException("id");

			this.Id = id;
		}

		public abstract void Process(NetworkStream stream, byte functionId);
	}
}