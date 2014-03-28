using System;
using System.IO;
using System.Threading.Tasks;

namespace iFSA.Service.Update
{
	public sealed class UpdateServerHandler : ServerHandlerBase
	{
		private readonly UpdateVersion[] _versions = new UpdateVersion[3];

		public UpdateServerHandler(byte id)
			: base(id)
		{
		}

		public override async Task ProcessAsync(Stream stream, byte methodId)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			var h = new TransferHandler { EnableCompression = false };
			switch ((UpdateMethods)methodId)
			{
				case UpdateMethods.GetVersion:
					await this.GetVersionAsync(stream, h);
					break;
				case UpdateMethods.GetVersions:
					await this.GetVersionsAsync(stream, h);
					break;
				case UpdateMethods.UploadVersion:
					await this.UploadVersionAsync(stream, h);
					break;
				case UpdateMethods.DownloadVersion:
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
				using (var ms = new MemoryStream(versions * UpdateVersion.VersionNetworkBufferSize))
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
			this.Setup(new UpdateVersion(data));
		}

		private async Task DownloadVersionAsync(Stream stream, TransferHandler handler)
		{
			var networkBuffer = TransferHandler.NoData;

			var clientVersion = new ClientVersion(await handler.ReadDataAsync(stream));
			var serverVersion = _versions[(int)clientVersion.ClientPlatform];
			if (serverVersion != null && serverVersion.Version > clientVersion.Version)
			{
				networkBuffer = serverVersion.Package;
			}

			await handler.WriteDataAsync(stream, networkBuffer);
		}

		private void Setup(UpdateVersion updateVersion)
		{
			_versions[(int)updateVersion.ClientPlatform] = updateVersion;
		}
	}
}