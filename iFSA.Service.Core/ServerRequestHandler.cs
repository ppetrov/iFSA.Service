using System.Net.Sockets;

namespace iFSA.Service.Core
{
	public abstract class ServerRequestHandler : ClientRequestHandler
	{
		protected ServerRequestHandler(byte id)
			: base(id)
		{
		}

		public abstract void Handle(NetworkStream stream);
	}
}