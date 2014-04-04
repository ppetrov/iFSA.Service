using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace iFSA.Service.Logs
{
	public sealed class LogsClientHandler : ClientHandlerBase
	{
		private readonly PackageHelper _packageHelper;

		public PackageHelper PackageHelper
		{
			get { return _packageHelper; }
		}

		public LogsClientHandler(byte id, string hostname, int port)
			: base(id, hostname, port)
		{
#if DEBUG
			this.TransferHandler.WriteProgress += (sender, _) => Console.WriteLine("Uploading ... " + _.ToString(@"F2") + "%");
			this.TransferHandler.ReadProgress += (sender, _) => Console.WriteLine("Downloading ... " + _.ToString(@"F2") + "%");
#endif
			_packageHelper = new PackageHelper(new byte[80 * 1024]);
		}

		public async Task<LogConfig[]> GetConfigsAsync()
		{
			await this.TransferHandler.WriteAsync(this.Stream, this.Id, (byte)LogMethod.GetConfigs);
			var data = await this.TransferHandler.ReadDataAsync(this.Stream);

			if (data.Length != TransferHandler.NoDataBytes.Length)
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

			return null;
		}

		public async Task ConfigureAsync(LogConfig logConfig)
		{
			if (logConfig == null) throw new ArgumentNullException("logConfig");

			await this.TransferHandler.WriteAsync(this.Stream, this.Id, (byte)logConfig.LogMethod);
			await this.TransferHandler.WriteAsync(this.Stream, logConfig.NetworkBuffer);
		}

		public async Task<bool> UploadLogsAsync(RequestHeader header, ClientFile[] logs)
		{
			if (header == null) throw new ArgumentNullException("header");
			if (logs == null) throw new ArgumentNullException("logs");
			if (logs.Length == 0) throw new ArgumentOutOfRangeException("logs");

			return await Upload(header, logs, LogMethod.UploadLogs);
		}

		public async Task<bool> UploadFilesAsync(RequestHeader header, ClientFile[] files)
		{
			if (header == null) throw new ArgumentNullException("header");
			if (files == null) throw new ArgumentNullException("files");
			if (files.Length == 0) throw new ArgumentOutOfRangeException("files");

			return await Upload(header, files, LogMethod.UploadFiles);
		}

		public async Task<bool> UploadDatabaseAsync(RequestHeader header, ClientFile database)
		{
			if (header == null) throw new ArgumentNullException("header");
			if (database == null) throw new ArgumentNullException("database");

			return await Upload(header, new[] { database }, LogMethod.UploadDatabase);
		}

		private async Task<bool> Upload(RequestHeader header, ClientFile[] files, LogMethod method)
		{
			var package = await _packageHelper.PackAsync(files);

			await this.TransferHandler.WriteAsync(this.Stream, this.Id, (byte)method);
			await this.TransferHandler.WriteAsync(this.Stream, Utilities.Concat(header.NetworkBuffer, package));

			var data = await this.TransferHandler.ReadDataAsync(this.Stream);
			return Convert.ToBoolean(BitConverter.ToInt32(data, 0));
		}
	}
}