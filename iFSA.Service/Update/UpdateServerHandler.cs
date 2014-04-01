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
			switch ((UpdateMethod)methodId)
			{
				case UpdateMethod.GetVersion:
					await this.GetVersionAsync(stream, h);
					break;
				case UpdateMethod.GetVersions:
					await this.GetVersionsAsync(stream, h);
					break;
				case UpdateMethod.UploadVersion:
					await this.UploadVersionAsync(stream, h);
					break;
				case UpdateMethod.DownloadVersion:
					await this.DownloadVersionAsync(stream, h);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private async Task GetVersionAsync(Stream stream, TransferHandler handler)
		{
			var networkBuffer = TransferHandler.NoData;

			var version = _versions[BitConverter.ToInt32(await handler.ReadDataAsync(stream), 0)];
			if (version != null)
			{
				networkBuffer = version.AppVersion.NetworkBuffer;
			}

			await handler.WriteDataAsync(stream, networkBuffer);
		}

		private async Task GetVersionsAsync(Stream stream, TransferHandler handler)
		{
			var networkBuffer = TransferHandler.NoData;

			using (var ms = new MemoryStream())
			{
				foreach (var v in _versions)
				{
					if (v != null)
					{
						var buffer = v.AppVersion.NetworkBuffer;
						ms.Capacity += buffer.Length;
						ms.Write(buffer, 0, buffer.Length);
					}
				}
				if (ms.Length != 0)
				{
					networkBuffer = ms.GetBuffer();
				}
			}

			await handler.WriteDataAsync(stream, networkBuffer);
		}

		private async Task UploadVersionAsync(Stream stream, TransferHandler handler)
		{
			var updateVersion = new UpdateVersion(await handler.DecompressAsync(await handler.ReadDataAsync(stream)));
			_versions[(int)updateVersion.AppVersion.ClientPlatform] = updateVersion;
		}

		private async Task DownloadVersionAsync(Stream stream, TransferHandler handler)
		{
			var networkBuffer = TransferHandler.NoData;

			var clientVersion = AppVersion.Create(await handler.ReadDataAsync(stream));
			var updateVersion = _versions[(int)clientVersion.ClientPlatform];
			if (updateVersion != null && updateVersion.AppVersion.Version > clientVersion.Version)
			{
				networkBuffer = updateVersion.Package;
			}

			await handler.WriteDataAsync(stream, networkBuffer);
		}
	}
}