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

			var package = _packages[BitConverter.ToInt32(await handler.ReadDataAsync(stream), 0)];
			if (package != null)
			{
				networkBuffer = package.Header.NetworkBuffer;
			}

			await handler.WriteAsync(stream, networkBuffer);
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

			await handler.WriteAsync(stream, networkBuffer);
		}

		private async Task UploadPackageAsync(Stream stream, TransferHandler handler)
		{
			var input = await handler.ReadDataAsync(stream);
			var data = await Task.Run(() => new CompressionHelper(handler.Buffer).Decompress(input)).ConfigureAwait(false);

			RequestHeader header;
			byte[] packageBytes;
			using (var ms = new MemoryStream(data))
			{
				header = new RequestHeader().Setup(ms);
				packageBytes = new byte[stream.Length - stream.Position];
				Array.Copy(data, stream.Position, packageBytes, 0, packageBytes.Length);
			}

			var package = new RequestPackage(header, packageBytes);
			_packages[(int)package.Header.ClientPlatform] = package;
		}

		private async Task DownloadPackageAsync(Stream stream, TransferHandler handler)
		{
			var networkBuffer = TransferHandler.NoDataBytes;

			var header = new RequestHeader().Setup(new MemoryStream(await handler.ReadDataAsync(stream)));
			var package = _packages[(int)header.ClientPlatform];
			if (package != null && package.Header.Version > header.Version)
			{
				networkBuffer = package.Data;
			}

			await handler.WriteAsync(stream, networkBuffer);
		}
	}
}