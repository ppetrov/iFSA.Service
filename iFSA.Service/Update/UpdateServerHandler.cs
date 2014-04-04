using System;
using System.IO;
using System.Threading.Tasks;

namespace iFSA.Service.Update
{
	public sealed class UpdateServerHandler : ServerHandlerBase
	{
		private readonly RequestPackage[] _packages = new RequestPackage[3];

		public UpdateServerHandler(byte id)
			: base(id)
		{
		}

		public override async Task ProcessAsync(Stream stream, byte methodId)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			var h = new TransferHandler(false);
			switch ((UpdateMethod)methodId)
			{
				case UpdateMethod.GetVersion:
					await this.GetVersionAsync(stream, h);
					break;
				case UpdateMethod.GetVersions:
					await this.GetVersionsAsync(stream, h);
					break;
				case UpdateMethod.UploadPackage:
					await this.UploadPackageAsync(stream, h);
					break;
				case UpdateMethod.DownloadPackage:
					await this.DownloadPackageAsync(stream, h);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private async Task GetVersionAsync(Stream stream, TransferHandler handler)
		{
			var networkBuffer = TransferHandler.NoData;

			var package = _packages[BitConverter.ToInt32(await handler.ReadDataAsync(stream), 0)];
			if (package != null)
			{
				networkBuffer = package.RequestHeader.NetworkBuffer;
			}

			await handler.WriteAsync(stream, networkBuffer);
		}

		private async Task GetVersionsAsync(Stream stream, TransferHandler handler)
		{
			var networkBuffer = TransferHandler.NoData;

			using (var ms = new MemoryStream())
			{
				foreach (var v in _packages)
				{
					if (v != null)
					{
						var buffer = v.RequestHeader.NetworkBuffer;
						ms.Capacity += buffer.Length;
						ms.Write(buffer, 0, buffer.Length);
					}
				}
				if (ms.Length != 0)
				{
					networkBuffer = ms.GetBuffer();
				}
			}

			await handler.WriteAsync(stream, networkBuffer);
		}

		private async Task UploadPackageAsync(Stream stream, TransferHandler handler)
		{
			using (var ms = new MemoryStream())
			{
				var input = await handler.ReadDataAsync(stream);
				var data = await Task.Run(() => new CompressionHelper(handler.Buffer).Decompress(input)).ConfigureAwait(false);
				ms.Write(data, 0, data.Length);
				ms.Position = 0;

				var package = new RequestPackage().Setup(ms);
				_packages[(int)package.RequestHeader.ClientPlatform] = package;
			}
		}

		private async Task DownloadPackageAsync(Stream stream, TransferHandler handler)
		{
			var networkBuffer = TransferHandler.NoData;

			var header = new RequestHeader().Setup(new MemoryStream(await handler.ReadDataAsync(stream)));
			var package = _packages[(int)header.ClientPlatform];
			if (package != null && package.RequestHeader.Version > header.Version)
			{
				networkBuffer = package.Package;
			}

			await handler.WriteAsync(stream, networkBuffer);
		}
	}
}