using System;
using System.IO;
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

		public override async Task ProcessAsync(Stream stream, byte methodId)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			var h = new TransferHandler { EnableCompression = false };
			switch ((Method)methodId)
			{
				case Method.GetVersion:
					await this.GetVersionAsync(stream, h);
					break;
				case Method.GetVersions:
					await this.GetVersionsAsync(stream, h);
					break;
				case Method.UploadVersion:
					await this.UploadVersionAsync(stream, h);
					break;
				case Method.DownloadVersion:
					await this.DownloadVersionAsync(stream, h);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private async Task GetVersionAsync(Stream stream, TransferHandler handler)
		{
			var networkBuffer = TransferHandler.NoData;

			var platform = BitConverter.ToInt32(await handler.ReadDataAsync(stream), 0);
			if (0 <= platform && platform < _versions.Length)
			{
				var serverVersion = _versions[platform];
				if (serverVersion != null)
				{
					networkBuffer = await serverVersion.GetVersionNetworkBufferAsync();
				}
			}

			await handler.WriteDataAsync(stream, networkBuffer);
		}

		private async Task GetVersionsAsync(Stream stream, TransferHandler handler)
		{
			var networkBuffer = TransferHandler.NoData;

			var versions = 0;
			foreach (var v in _versions)
			{
				if (v != null)
				{
					versions++;
				}
			}
			if (versions > 0)
			{
				using (var ms = new MemoryStream(versions * ServerVersion.VersionNetworkBufferSize))
				{
					foreach (var v in _versions)
					{
						if (v != null)
						{
							var buffer = await v.GetVersionNetworkBufferAsync();
							await ms.WriteAsync(buffer, 0, buffer.Length);
						}
					}
					networkBuffer = ms.GetBuffer();
				}
			}

			await handler.WriteDataAsync(stream, networkBuffer);
		}

		private async Task UploadVersionAsync(Stream stream, TransferHandler handler)
		{
			var data = await handler.DecompressAsync(await handler.ReadDataAsync(stream));
			this.Setup(new ServerVersion(data));
		}

		private async Task DownloadVersionAsync(Stream stream, TransferHandler handler)
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

		private void Setup(ServerVersion serverVersion)
		{
			_versions[(int)serverVersion.Platform] = serverVersion;
		}
	}
}