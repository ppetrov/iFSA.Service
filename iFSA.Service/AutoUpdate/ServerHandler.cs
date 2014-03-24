using System;
using System.Net.Sockets;

namespace iFSA.Service.AutoUpdate
{
	public sealed class ServerHandler : ServerHandlerBase
	{
		private readonly ServerVersion[] _versions = new ServerVersion[3];

		public ServerHandler(byte id)
			: base(id)
		{
		}

		public override void Process(NetworkStream stream, byte functionId)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			var h = new TransferHandler();
			switch ((Function)functionId)
			{
				case Function.PublishVersion:
					this.PublishVersion(stream, h);
					break;
				case Function.DownloadVersion:
					this.DownloadVersion(stream, h);
					break;
				case Function.GetVersion:
					this.GetVersion(stream, h);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void PublishVersion(NetworkStream stream, TransferHandler handler)
		{
			var serverVersion = new ServerVersion(handler.ReadData(stream));
			_versions[(int)serverVersion.Platform] = serverVersion;
		}

		private void DownloadVersion(NetworkStream stream, TransferHandler handler)
		{
			var networkBuffer = TransferHandler.NoData;

			var clientVersion = new ClientVersion(handler.ReadData(stream));
			var serverVersion = _versions[(int)clientVersion.Platform];
			if (serverVersion != null && serverVersion.Version > clientVersion.Version)
			{
				networkBuffer = serverVersion.Package;
			}

			handler.WriteData(stream, networkBuffer);
		}

		private void GetVersion(NetworkStream stream, TransferHandler handler)
		{
			var networkBuffer = TransferHandler.NoData;

			var platform = BitConverter.ToInt32(handler.ReadData(stream), 0);
			if (0 <= platform && platform < _versions.Length)
			{
				var serverVersion = _versions[platform];
				if (serverVersion != null)
				{
					networkBuffer = serverVersion.GetNetworkBuffer(false);
				}
			}

			handler.WriteData(stream, networkBuffer);
		}
	}
}