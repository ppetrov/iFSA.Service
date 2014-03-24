using System;
using System.Net.Sockets;

namespace iFSA.Service
{
	public abstract class ServerHandlerBase
	{
		public byte Id { get; private set; }

		protected ServerHandlerBase(byte id)
		{
			if (id <= 0) throw new ArgumentOutOfRangeException("id");

			this.Id = id;
		}

		public abstract void Process(NetworkStream stream, byte functionId);
	}
}