using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace iFSA.Service.AutoUpdate
{
	public sealed class ServerHandler : ServerHandlerBase
	{
		private readonly ServerVersion[] _versions = new ServerVersion[3];

		public ServerHandler(byte id)
			: base(id)
		{
		}

		public override async Task ProcessAsync(NetworkStream stream, byte functionId)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			var h = new TransferHandler { EnableCompression = false };
			switch ((Function)functionId)
			{
				case Function.PublishVersion:
					await this.PublishVersionAsync(stream, h);
					break;
				case Function.DownloadVersion:
					await this.DownloadVersionAsync(stream, h);
					break;
				case Function.GetVersion:
					await this.GetVersionAsync(stream, h);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private async Task PublishVersionAsync(NetworkStream stream, TransferHandler handler)
		{
			var data = await handler.DecompressAsync(await handler.ReadDataAsync(stream));
			this.SetupVersion(new ServerVersion(data));
		}

		private async Task DownloadVersionAsync(NetworkStream stream, TransferHandler handler)
		{
			var networkBuffer = TransferHandler.NoData;

			var clientVersion = new ClientVersion(await handler.ReadDataAsync(stream));
			var serverVersion = _versions[(int)clientVersion.Platform];
			if (serverVersion != null && serverVersion.Version > clientVersion.Version)
			{
				networkBuffer = serverVersion.Package;
			}

			await handler.WriteDataAsync(stream, networkBuffer);
		}

		private async Task GetVersionAsync(NetworkStream stream, TransferHandler handler)
		{
			var networkBuffer = TransferHandler.NoData;

			var platform = BitConverter.ToInt32(await handler.ReadDataAsync(stream), 0);
			if (0 <= platform && platform < _versions.Length)
			{
				var serverVersion = _versions[platform];
				if (serverVersion != null)
				{
					networkBuffer = await serverVersion.GetNetworkBufferAsync(false);
				}
			}

			await handler.WriteDataAsync(stream, networkBuffer);
		}

		private void SetupVersion(ServerVersion serverVersion)
		{
			_versions[(int)serverVersion.Platform] = serverVersion;
		}
	}
}