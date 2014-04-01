using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace iFSA.Service.Update
{
	public sealed class UpdateClientHandler : ClientHandlerBase
	{
		public UpdateClientHandler(byte id, string hostname, int port)
			: base(id, hostname, port)
		{
#if DEBUG
			this.TransferHandler.WriteProgress += (sender, _) => Console.WriteLine("Uploading ... " + _.ToString(@"F2") + "%");
			this.TransferHandler.ReadProgress += (sender, _) => Console.WriteLine("Downloading ... " + _.ToString(@"F2") + "%");
#endif
		}

		public async Task<RequestHeader> GetPackageAsync(Stream stream, ClientPlatform platform)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			await this.TransferHandler.WriteMethodAsync(stream, this.Id, (byte)UpdateMethod.GetVersion);
			await this.TransferHandler.WriteDataAsync(stream, BitConverter.GetBytes((int)platform));
			var data = await this.TransferHandler.ReadDataAsync(stream);

			if (data.Length != TransferHandler.NoData.Length)
			{
				return new RequestHeader().Setup(new MemoryStream(data));
			}

			return null;
		}

		public async Task<RequestHeader[]> GetPackagesAsync(Stream stream)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			await this.TransferHandler.WriteMethodAsync(stream, this.Id, (byte)UpdateMethod.GetVersions);
			var data = await this.TransferHandler.ReadDataAsync(stream);

			if (data.Length != TransferHandler.NoData.Length)
			{
				var headers = new List<RequestHeader>();

				using (var ms = new MemoryStream(data))
				{
					while (ms.Position != ms.Length)
					{
						headers.Add(new RequestHeader().Setup(ms));
					}
				}

				return headers.ToArray();
			}

			return null;
		}

		public async Task UploadPackageAsync(Stream stream, RequestPackage package)
		{
			if (stream == null) throw new ArgumentNullException("stream");
			if (package == null) throw new ArgumentNullException("package");

			await this.TransferHandler.WriteMethodAsync(stream, this.Id, (byte)UpdateMethod.UploadPackage);
			await this.TransferHandler.WriteDataAsync(stream, await this.TransferHandler.CompressAsync(package.NetworkBuffer));
		}

		public async Task<byte[]> DownloadPackageAsync(Stream stream, RequestHeader header)
		{
			if (stream == null) throw new ArgumentNullException("stream");
			if (header == null) throw new ArgumentNullException("header");

			await this.TransferHandler.WriteMethodAsync(stream, this.Id, (byte)UpdateMethod.DownloadPackage);
			await this.TransferHandler.WriteDataAsync(stream, header.NetworkBuffer);
			var data = await this.TransferHandler.ReadDataAsync(stream);

			if (data.Length != TransferHandler.NoData.Length)
			{
				return data;
			}

			return null;
		}
	}
}