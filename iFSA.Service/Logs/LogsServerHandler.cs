using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace iFSA.Service.Logs
{
	public sealed class LogsServerHandler : ServerHandlerBase
	{
		public static readonly string ConfigName = @"logs.cfg";

		private static readonly byte[] ZeroBytes = { 0, 0, 0, 0 };
		private static readonly byte[] OneBytes = { 1, 0, 0, 0 };

		private readonly string[] _dbFolders = new string[Server.SupportedPlatforms];
		private readonly string[] _logFolders = new string[Server.SupportedPlatforms];
		private readonly string[] _filesFolders = new string[Server.SupportedPlatforms];

		public LogsServerHandler(byte id)
			: base(id)
		{
			for (var i = 0; i < Server.SupportedPlatforms; i++)
			{
				_dbFolders[i] = string.Empty;
				_logFolders[i] = string.Empty;
				_filesFolders[i] = string.Empty;
			}
		}

		public override async Task InitializeAsync()
		{
			try
			{
				var folders = new string[3 * Server.SupportedPlatforms];
				for (var i = 0; i < folders.Length; i++)
				{
					folders[i] = string.Empty;
				}

				using (var sr = new StreamReader(ConfigName))
				{
					var index = 0;

					string line;
					while ((line = await sr.ReadLineAsync()) != null)
					{
						if (index < folders.Length)
						{
							folders[index++] = line;
						}
					}
				}

				var offset = 0;
				foreach (var f in new[] { _dbFolders, _logFolders, _filesFolders })
				{
					for (var i = 0; i < f.Length; i++)
					{
						f[i] = folders[i + offset];
					}
					offset += Server.SupportedPlatforms;
				}
			}
			catch (FileNotFoundException) { }
		}

		public override async Task ProcessAsync(Stream stream, byte methodId)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			var h = new TransferHandler();
			var method = (LogMethod)methodId;
			switch (method)
			{
				case LogMethod.GetConfigs:
					await this.GetConfigsAsync(stream, h, method);
					break;
				case LogMethod.ConfigureLogs:
					await this.ConfigureLogsAsync(stream, h, method);
					break;
				case LogMethod.ConfigureFiles:
					await this.ConfigureFilesAsync(stream, h, method);
					break;
				case LogMethod.ConfigureDatabase:
					await this.ConfigureDatabaseAsync(stream, h, method);
					break;
				case LogMethod.UploadLogs:
					await this.UploadLogsAsync(stream, h, method);
					break;
				case LogMethod.UploadFiles:
					await this.UploadFilesAsync(stream, h, method);
					break;
				case LogMethod.UploadDatabase:
					await this.UploadDatabaseAsync(stream, h, method);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private async Task GetConfigsAsync(Stream stream, TransferHandler handler, LogMethod method)
		{
			using (var ms = new MemoryStream())
			{
				this.Write(ms, _dbFolders, LogMethod.UploadDatabase);
				this.Write(ms, _logFolders, LogMethod.UploadLogs);
				this.Write(ms, _filesFolders, LogMethod.UploadFiles);

				var data = ms.ToArray();
				this.LogResponse(data, method.ToString());
				await handler.WriteAsync(stream, data);
			}
		}

		private async Task ConfigureLogsAsync(Stream stream, TransferHandler handler, LogMethod method)
		{
			var data = await handler.ReadDataAsync(stream);
			this.LogRequest(data, method.ToString());
			await this.ConfigureAsync(data, method);
		}

		private async Task ConfigureFilesAsync(Stream stream, TransferHandler handler, LogMethod method)
		{
			await this.ConfigureAsync(await handler.ReadDataAsync(stream), method);
		}

		private async Task ConfigureDatabaseAsync(Stream stream, TransferHandler handler, LogMethod method)
		{
			await this.ConfigureAsync(await handler.ReadDataAsync(stream), method);
		}

		private async Task UploadLogsAsync(Stream stream, TransferHandler handler, LogMethod method)
		{
			var data = await UploadAsync(await handler.ReadDataAsync(stream), _logFolders, method, true);
			this.LogResponse(data, method.ToString());
			await handler.WriteAsync(stream, data);
		}

		private async Task UploadFilesAsync(Stream stream, TransferHandler handler, LogMethod method)
		{
			var data = await UploadAsync(await handler.ReadDataAsync(stream), _filesFolders, method, false);
			this.LogResponse(data, method.ToString());
			await handler.WriteAsync(stream, data);
		}

		private async Task UploadDatabaseAsync(Stream stream, TransferHandler handler, LogMethod method)
		{
			var data = await UploadAsync(await handler.ReadDataAsync(stream), _dbFolders, method, false);
			this.LogResponse(data, method.ToString());
			await handler.WriteAsync(stream, data);
		}

		private async Task ConfigureAsync(byte[] data, LogMethod method)
		{
			this.LogResponse(data, method.ToString());

			using (var ms = new MemoryStream(data))
			{
				var logConfig = new LogConfig().Setup(ms);
				string[] folders = null;
				switch (method)
				{
					case LogMethod.ConfigureLogs:
						folders = _logFolders;
						break;
					case LogMethod.ConfigureFiles:
						folders = _filesFolders;
						break;
					case LogMethod.ConfigureDatabase:
						folders = _dbFolders;
						break;
				}
				folders[(int)logConfig.RequestHeader.ClientPlatform] = logConfig.Folder;

				await this.SaveFoldersAsync();
			}
		}

		private async Task<byte[]> UploadAsync(byte[] data, string[] folders, LogMethod method, bool append)
		{
			this.LogRequest(data, method.ToString());

			var success = false;

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
					await new PackageHelper().UnpackAsync(ms, userFolder, append);
					success = true;
				}
			}

			return success ? OneBytes : ZeroBytes;
		}

		private async Task SaveFoldersAsync()
		{
			using (var sw = new StreamWriter(ConfigName))
			{
				foreach (var folders in new[] { _dbFolders, _logFolders, _filesFolders })
				{
					foreach (var folder in folders)
					{
						await sw.WriteLineAsync(folder);
					}
				}
			}
		}

		private void Write(Stream stream, IList<string> folders, LogMethod method)
		{
			for (var i = 0; i < folders.Count; i++)
			{
				NetworkHelper.WriteRaw(stream, new LogConfig(new RequestHeader((ClientPlatform)i, RequestHeader.EmptyVersion, string.Empty, string.Empty), method, folders[i] ?? string.Empty).NetworkBuffer);
			}
		}
	}
}