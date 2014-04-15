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
			var networkBuffer = TransferHandler.NoDataBytes;

			var context = UpdateMethod.GetVersion.ToString();
			var input = await handler.ReadDataAsync(stream);
			this.LogRequest(input, context);
			var package = _packages[BitConverter.ToInt32(input, 0)];
			if (package != null)
			{
				networkBuffer = package.Header.NetworkBuffer;
			}

			var data = networkBuffer;
			this.LogResponse(data, context);
			await handler.WriteAsync(stream, data);
		}

		private async Task GetVersionsAsync(Stream stream, TransferHandler handler)
		{
			var networkBuffer = TransferHandler.NoDataBytes;

			using (var ms = new MemoryStream())
			{
				foreach (var v in _packages)
				{
					if (v != null)
					{
						var buffer = v.Header.NetworkBuffer;
						ms.Capacity += buffer.Length;
						ms.Write(buffer, 0, buffer.Length);
					}
				}
				if (ms.Length != 0)
				{
					networkBuffer = ms.GetBuffer();
				}
			}

			var data = networkBuffer;
			this.LogResponse(data, UpdateMethod.GetVersions.ToString());
			await handler.WriteAsync(stream, data);
		}

		private async Task UploadPackageAsync(Stream stream, TransferHandler handler)
		{
			var input = await handler.ReadDataAsync(stream);
			this.LogRequest(input, UpdateMethod.UploadPackage.ToString());

			using (var ms = new MemoryStream(input))
			{
				var header = new RequestHeader().Setup(ms);
				var packageBytes = new byte[stream.Length - stream.Position];
				ms.Read(packageBytes, 0, packageBytes.Length);

				var package = new RequestPackage(header, packageBytes);
				_packages[(int)package.Header.ClientPlatform] = package;
			}
		}

		private async Task DownloadPackageAsync(Stream stream, TransferHandler handler)
		{
			var networkBuffer = TransferHandler.NoDataBytes;

			var input = await handler.ReadDataAsync(stream);
			this.LogRequest(input, UpdateMethod.DownloadPackage.ToString());

			var header = new RequestHeader().Setup(new MemoryStream(input));
			var package = _packages[(int)header.ClientPlatform];
			if (package != null && package.Header.Version > header.Version)
			{
				networkBuffer = package.Data;
			}

			var data = networkBuffer;
			this.LogResponse(data, UpdateMethod.DownloadPackage.ToString());
			await handler.WriteAsync(stream, data);
		}
	}
}