﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace iFSA.Service.Logs
{
	public sealed class LogsClientHandler : ClientHandler
	{
		private readonly PackageHelper _packageHelper = new PackageHelper(new byte[80 * 1024]);

		public PackageHelper PackageHelper
		{
			get { return _packageHelper; }
		}

		public override string Name
		{
			get { return @"Logs"; }
		}

		public LogsClientHandler(byte id, TransferHandler transferHandler)
			: base(id, transferHandler)
		{
#if DEBUG
			this.TransferHandler.WriteProgress += (sender, _) => Console.WriteLine("Uploading ... " + _.ToString(@"F2") + "%");
			this.TransferHandler.ReadProgress += (sender, _) => Console.WriteLine("Downloading ... " + _.ToString(@"F2") + "%");
#endif
		}

		public async Task<LogConfig[]> GetConfigsAsync()
		{
			var method = LogMethod.GetConfigs;
			var context = method.ToString();
			this.LogRequest(context);
			await this.TransferHandler.WriteAsync(this.Id, (byte)method);

			var data = await this.TransferHandler.ReadDataAsync();
			this.LogResponse(data, context);

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

			var method = logConfig.LogMethod;
			var context = method.ToString();
			this.LogRequest(context);
			await this.TransferHandler.WriteAsync(this.Id, (byte)method);

			var data = logConfig.NetworkBuffer;
			this.LogRequest(data, context);
			await this.TransferHandler.WriteAsync(data);
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
			var context = method.ToString();
			this.LogRequest(context);
			await this.TransferHandler.WriteAsync(this.Id, (byte)method);

			var data = Utilities.Concat(header.NetworkBuffer, package);
			this.LogRequest(data, context);
			await this.TransferHandler.WriteAsync(data);

			var bytes = await this.TransferHandler.ReadDataAsync();
			this.LogResponse(bytes, context);
			return Convert.ToBoolean(BitConverter.ToInt32(bytes, 0));
		}
	}
}