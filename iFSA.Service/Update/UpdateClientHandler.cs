using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace iFSA.Service.Update
{
	public sealed class UpdateClientHandler : ClientHandlerBase
	{
		public UpdateClientHandler(byte id)
			: base(id, new TransferHandler { EnableCompression = false })
		{
#if DEBUG
			this.TransferHandler.WriteProgress += (sender, _) => Console.WriteLine("Uploading ... " + _.ToString(@"F2") + "%");
			this.TransferHandler.ReadProgress += (sender, _) => Console.WriteLine("Downloading ... " + _.ToString(@"F2") + "%");
#endif
		}

		public async Task<Version> GetVersionAsync(TcpClient client, ClientPlatform platform)
		{
			if (client == null) throw new ArgumentNullException("client");

			using (var s = client.GetStream())
			{
				await this.TransferHandler.WriteMethodAsync(s, this.Id, (byte)UpdateMethod.GetVersion);
				await this.TransferHandler.WriteDataAsync(s, BitConverter.GetBytes((int)platform));

				var data = await this.TransferHandler.ReadDataAsync(s);
				await this.TransferHandler.WriteCloseAsync(s);

				if (data.Length != TransferHandler.NoData.Length)
				{
					return new RequestHeader().Setup(new MemoryStream(data)).Version;
				}
			}

			return null;
		}

		public async Task<RequestHeader[]> GetVersionsAsync(TcpClient client)
		{
			if (client == null) throw new ArgumentNullException("client");

			using (var s = client.GetStream())
			{
				await this.TransferHandler.WriteMethodAsync(s, this.Id, (byte)UpdateMethod.GetVersions);

				var data = await this.TransferHandler.ReadDataAsync(s);
				await this.TransferHandler.WriteCloseAsync(s);

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
			}

			return null;
		}

		public async Task UploadVersionAsync(TcpClient client, RequestPackage package)
		{
			if (client == null) throw new ArgumentNullException("client");
			if (package == null) throw new ArgumentNullException("package");

			using (var s = client.GetStream())
			{
				await this.TransferHandler.WriteMethodAsync(s, this.Id, (byte)UpdateMethod.UploadVersion);
				await this.TransferHandler.WriteDataAsync(s, await this.TransferHandler.CompressAsync(package.NetworkBuffer));
				await this.TransferHandler.WriteCloseAsync(s);
			}
		}

		public async Task<byte[]> DownloadVersionAsync(TcpClient client, RequestHeader version)
		{
			if (client == null) throw new ArgumentNullException("client");
			if (version == null) throw new ArgumentNullException("version");

			using (var s = client.GetStream())
			{
				await this.TransferHandler.WriteMethodAsync(s, this.Id, (byte)UpdateMethod.DownloadVersion);
				await this.TransferHandler.WriteDataAsync(s, version.NetworkBuffer);

				var data = await this.TransferHandler.ReadDataAsync(s);
				await this.TransferHandler.WriteCloseAsync(s);

				if (data.Length != TransferHandler.NoData.Length)
				{
					return data;
				}
			}

			return null;
		}
	}
}