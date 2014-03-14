using System;
using System.Net.Sockets;
using iFSA.Service.Core;

namespace iFSA.Service.AutoUpdate
{
	public sealed class AutoUpdateServerRequestHandler : ServerRequestHandler
	{
		private readonly ServerVersion[] _versions = new ServerVersion[3];
		private readonly byte[] _noPackage = BitConverter.GetBytes(-1);

		public AutoUpdateServerRequestHandler(byte id)
			: base(id)
		{
		}

		public void Publish(Platform platform, ServerVersion serverVersion)
		{
			if (serverVersion == null) throw new ArgumentNullException("serverVersion");

			_versions[(int)platform] = serverVersion;
		}

		public override void Handle(NetworkStream stream)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			var h = new TransferHandler();
			var clientVersion = new ClientVersion(h.ReadData(stream));
			var updatePackage = this.GetUpdatePackage(clientVersion);
			h.WriteData(stream, updatePackage);
		}

		private byte[] GetUpdatePackage(ClientVersion clientVersion)
		{
			var package = _noPackage;
			var serverVersion = _versions[(int)clientVersion.Platform];
			if (serverVersion != null && serverVersion.Version > clientVersion.Version)
			{
				package = serverVersion.GetPackageBuffer();
			}
			return package;
		}
	}
}