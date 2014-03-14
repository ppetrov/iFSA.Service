using System.Net.Sockets;

namespace iFSA.Server.Core
{
	public abstract class ServerRequestHandler : RequestHandler
	{
		protected ServerRequestHandler(byte id)
			: base(id)
		{
		}

		public abstract void Handle(NetworkStream stream);
	}
}