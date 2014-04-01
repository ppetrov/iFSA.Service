using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace iFSA.Service.Logs
{
	public sealed class LogsClientHandler : ClientHandlerBase
	{
		public LogsClientHandler(byte id)
			: base(id, new TransferHandler { EnableCompression = true })
		{
#if DEBUG
			this.TransferHandler.WriteProgress += (sender, _) => Console.WriteLine("Uploading ... " + _.ToString(@"F2") + "%");
			this.TransferHandler.ReadProgress += (sender, _) => Console.WriteLine("Downloading ... " + _.ToString(@"F2") + "%");
#endif
		}

		public async Task<LogConfig[]> GetConfigsAsync(TcpClient client)
		{
			if (client == null) throw new ArgumentNullException("client");

			using (var s = client.GetStream())
			{
				await this.TransferHandler.WriteMethodAsync(s, this.Id, (byte)LogMethod.GetConfigs);

				var data = await this.TransferHandler.ReadDataAsync(s);

				await this.TransferHandler.WriteCloseAsync(s);

				if (data.Length != TransferHandler.NoData.Length)
				{
					var configs = new List<LogConfig>();

					using (var ms = new MemoryStream(data))
					{
						while (ms.Position != ms.Length)
						{
							configs.Add(new LogConfig().Setup(ms));
						}
					}

					return configs.ToArray();
				}
			}

			return null;
		}

		public async Task ConfigureAsync(TcpClient client, LogConfig logConfig)
		{
			if (client == null) throw new ArgumentNullException("client");
			if (logConfig == null) throw new ArgumentNullException("logConfig");

			using (var s = client.GetStream())
			{
				await this.TransferHandler.WriteMethodAsync(s, this.Id, (byte)logConfig.LogMethod);
				await this.TransferHandler.WriteDataAsync(s, logConfig.NetworkBuffer);
				await this.TransferHandler.WriteCloseAsync(s);
			}
		}

		public async Task<bool> UploadLogsAsync(TcpClient client, RequestHeader header, ClientFile[] logs)
		{
			if (client == null) throw new ArgumentNullException("client");
			if (header == null) throw new ArgumentNullException("header");
			if (logs == null) throw new ArgumentNullException("logs");

			return await Upload(client, header, logs, LogMethod.UploadLogs);
		}

		public async Task<bool> UploadFilesAsync(TcpClient client, RequestHeader header, ClientFile[] files)
		{
			if (client == null) throw new ArgumentNullException("client");
			if (header == null) throw new ArgumentNullException("header");
			if (files == null) throw new ArgumentNullException("files");

			return await Upload(client, header, files, LogMethod.UploadFiles);
		}

		public async Task<bool> UploadDatabaseAsync(TcpClient client, RequestHeader header, ClientFile database)
		{
			if (client == null) throw new ArgumentNullException("client");
			if (header == null) throw new ArgumentNullException("header");
			if (database == null) throw new ArgumentNullException("database");

			return await Upload(client, header, new[] { database }, LogMethod.UploadDatabase);
		}

		private async Task<bool> Upload(TcpClient client, RequestHeader header, ClientFile[] files, LogMethod method)
		{
			bool uploaded;

			var package = await new PackageHandler(this.TransferHandler.Buffer).PackAsync(files);

			using (var s = client.GetStream())
			{
				await this.TransferHandler.WriteMethodAsync(s, this.Id, (byte)method);
				await this.TransferHandler.WriteDataAsync(s, new RequestPackage(header, package).NetworkBuffer);

				var data = await this.TransferHandler.ReadDataAsync(s);
				uploaded = Convert.ToBoolean(BitConverter.ToInt32(data, 0));

				await this.TransferHandler.WriteCloseAsync(s);
			}

			return uploaded;
		}
	}
}