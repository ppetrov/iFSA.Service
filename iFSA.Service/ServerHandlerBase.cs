using System;
using System.Net.Sockets;
using System.Threading.Tasks;

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

		public abstract Task ProcessAsync(NetworkStream networkStream, byte functionId);
	}
}