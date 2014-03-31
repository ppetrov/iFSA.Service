﻿using System;
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
				await this.TransferHandler.WriteMethodAsync(s, this.Id, (byte)UpdateMethods.GetVersion);
				await this.TransferHandler.WriteDataAsync(s, BitConverter.GetBytes((int)platform));

				var data = await this.TransferHandler.ReadDataAsync(s);
				await this.TransferHandler.WriteCloseAsync(s);

				if (data.Length != TransferHandler.NoData.Length)
				{
					return AppVersion.Create(data).Version;
				}
			}

			return null;
		}

		public async Task<AppVersion[]> GetVersionsAsync(TcpClient client)
		{
			if (client == null) throw new ArgumentNullException("client");

			using (var s = client.GetStream())
			{
				await this.TransferHandler.WriteMethodAsync(s, this.Id, (byte)UpdateMethods.GetVersions);

				var data = await this.TransferHandler.ReadDataAsync(s);
				await this.TransferHandler.WriteCloseAsync(s);

				if (data.Length != TransferHandler.NoData.Length)
				{
					var versions = new List<AppVersion>();

					using (var ms = new MemoryStream(data))
					{
						versions.Add(AppVersion.Create(ms));
					}

					return versions.ToArray();
				}
			}

			return null;
		}

		public async Task UploadVersionAsync(TcpClient client, UpdateVersion version)
		{
			if (client == null) throw new ArgumentNullException("client");
			if (version == null) throw new ArgumentNullException("version");

			using (var s = client.GetStream())
			{
				await this.TransferHandler.WriteMethodAsync(s, this.Id, (byte)UpdateMethods.UploadVersion);
				await this.TransferHandler.WriteDataAsync(s, await this.TransferHandler.CompressAsync(version.NetworkBuffer));
				await this.TransferHandler.WriteCloseAsync(s);
			}
		}

		public async Task<byte[]> DownloadVersionAsync(TcpClient client, AppVersion version)
		{
			if (client == null) throw new ArgumentNullException("client");
			if (version == null) throw new ArgumentNullException("version");

			using (var s = client.GetStream())
			{
				await this.TransferHandler.WriteMethodAsync(s, this.Id, (byte)UpdateMethods.DownloadVersion);
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