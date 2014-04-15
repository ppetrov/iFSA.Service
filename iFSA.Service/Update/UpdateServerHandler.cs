using System;
using System.Diagnostics;
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

			var data = networkBuffer;
			Trace.WriteLine(string.Format(@"Send {0} bytes to client ({1})", data.Length, UpdateMethod.GetVersion));
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
			Trace.WriteLine(string.Format(@"Send {0} bytes to client ({1})", data.Length, UpdateMethod.GetVersions));
			await handler.WriteAsync(stream, data);
		}

		private async Task UploadPackageAsync(Stream stream, TransferHandler handler)
		{
			var input = await handler.ReadDataAsync(stream);

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

			var header = new RequestHeader().Setup(new MemoryStream(await handler.ReadDataAsync(stream)));
			var package = _packages[(int)header.ClientPlatform];
			if (package != null && package.Header.Version > header.Version)
			{
				networkBuffer = package.Data;
			}

			var data = networkBuffer;
			Trace.WriteLine(string.Format(@"Send {0} bytes to client ({1})", data.Length, UpdateMethod.DownloadPackage));
			await handler.WriteAsync(stream, data);
		}
	}
}