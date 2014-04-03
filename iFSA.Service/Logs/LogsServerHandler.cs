using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace iFSA.Service.Logs
{
	public sealed class LogsServerHandler : ServerHandlerBase
	{
		private readonly string[] _dbFolders = new string[3];
		private readonly string[] _logFolders = new string[3];
		private readonly string[] _filesFolders = new string[3];

		public LogsServerHandler(byte id)
			: base(id)
		{
		}

		public override async Task ProcessAsync(Stream stream, byte methodId)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			var h = new TransferHandler();
			switch ((LogMethod)methodId)
			{
				case LogMethod.GetConfigs:
					await this.GetConfigsAsync(stream, h);
					break;
				case LogMethod.ConfigureLogs:
					await this.ConfigureLogsAsync(stream, h);
					break;
				case LogMethod.ConfigureFiles:
					await this.ConfigureFilesAsync(stream, h);
					break;
				case LogMethod.ConfigureDatabase:
					await this.ConfigureDatabaseAsync(stream, h);
					break;
				case LogMethod.UploadLogs:
					await this.UploadLogsAsync(stream, h);
					break;
				case LogMethod.UploadFiles:
					await this.UploadFilesAsync(stream, h);
					break;
				case LogMethod.UploadDatabase:
					await this.UploadDatabaseAsync(stream, h);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private async Task GetConfigsAsync(Stream stream, TransferHandler handler)
		{
			using (var ms = new MemoryStream())
			{
				this.Write(ms, _dbFolders, LogMethod.UploadDatabase);
				this.Write(ms, _logFolders, LogMethod.UploadLogs);
				this.Write(ms, _filesFolders, LogMethod.UploadFiles);

				await handler.WriteAsync(stream, ms.ToArray());
			}
		}

		private async Task ConfigureLogsAsync(Stream stream, TransferHandler handler)
		{
			this.Configure(await handler.ReadDataAsync(stream));
		}

		private async Task ConfigureFilesAsync(Stream stream, TransferHandler handler)
		{
			this.Configure(await handler.ReadDataAsync(stream));
		}

		private async Task ConfigureDatabaseAsync(Stream stream, TransferHandler handler)
		{
			this.Configure(await handler.ReadDataAsync(stream));
		}

		private async Task UploadLogsAsync(Stream stream, TransferHandler handler)
		{
			var success = await Upload(handler, await handler.ReadDataAsync(stream), _logFolders, true);
			await handler.WriteAsync(stream, BitConverter.GetBytes(Convert.ToInt32(success)));
		}

		private async Task UploadFilesAsync(Stream stream, TransferHandler handler)
		{
			var success = await Upload(handler, await handler.ReadDataAsync(stream), _filesFolders, false);
			await handler.WriteAsync(stream, BitConverter.GetBytes(Convert.ToInt32(success)));
		}

		private async Task UploadDatabaseAsync(Stream stream, TransferHandler handler)
		{
			var success = await Upload(handler, await handler.ReadDataAsync(stream), _dbFolders, false);
			await handler.WriteAsync(stream, BitConverter.GetBytes(Convert.ToInt32(success)));
		}

		private void Configure(byte[] data)
		{
			using (var ms = new MemoryStream(data))
			{
				var logConfig = new LogConfig().Setup(ms);
				var folders = GetConfigFolders(logConfig);
				folders[(int)logConfig.RequestHeader.ClientPlatform] = logConfig.Folder;
			}
		}

		private string[] GetConfigFolders(LogConfig logConfig)
		{
			if (logConfig.LogMethod == LogMethod.ConfigureLogs)
			{
				return _logFolders;
			}
			if (logConfig.LogMethod == LogMethod.ConfigureFiles)
			{
				return _filesFolders;
			}
			if (logConfig.LogMethod == LogMethod.ConfigureDatabase)
			{
				return _dbFolders;
			}
			throw new ArgumentOutOfRangeException();
		}

		private void Write(Stream stream, IList<string> folders, LogMethod method)
		{
			for (var i = 0; i < folders.Count; i++)
			{
				NetworkHelper.WriteRaw(stream, new LogConfig(new RequestHeader((ClientPlatform)i, RequestHeader.EmptyVersion, string.Empty, string.Empty), method, folders[i] ?? string.Empty).NetworkBuffer);
			}
		}

		private static async Task<bool> Upload(TransferHandler handler, byte[] data, string[] folders, bool append)
		{
			using (var ms = new MemoryStream(data))
			{
				var header = new RequestHeader().Setup(ms);
				var folder = folders[(int)header.ClientPlatform];
				if (folder != null)
				{
					var userFolder = new DirectoryInfo(Path.Combine(folder, header.Username));
					if (!userFolder.Exists)
					{
						userFolder.Create();
					}
					await new PackageHandler(handler.Buffer).UnpackAsync(ms, userFolder, append);
					return true;
				}
			}

			return false;
		}
	}
}