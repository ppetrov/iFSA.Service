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

			var buffer = MemoryPool.Get16KBuffer();
			try
			{
				var h = new TransferHandler(stream, buffer);
				var method = (LogMethod)methodId;
				switch (method)
				{
					case LogMethod.GetConfigs:
						await this.GetConfigsAsync(h, method);
						break;
					case LogMethod.ConfigureLogs:
						await this.ConfigureLogsAsync(h, method);
						break;
					case LogMethod.ConfigureFiles:
						await this.ConfigureFilesAsync(h, method);
						break;
					case LogMethod.ConfigureDatabase:
						await this.ConfigureDatabaseAsync(h, method);
						break;
					case LogMethod.UploadLogs:
						await this.UploadLogsAsync(h, method);
						break;
					case LogMethod.UploadFiles:
						await this.UploadFilesAsync(h, method);
						break;
					case LogMethod.UploadDatabase:
						await this.UploadDatabaseAsync(h, method);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			finally
			{
				MemoryPool.Return16KBuffer(buffer);
			}
		}

		private async Task GetConfigsAsync(TransferHandler handler, LogMethod method)
		{
			using (var ms = new MemoryStream())
			{
				this.Write(ms, _dbFolders, LogMethod.UploadDatabase);
				this.Write(ms, _logFolders, LogMethod.UploadLogs);
				this.Write(ms, _filesFolders, LogMethod.UploadFiles);

				var data = ms.ToArray();
				this.LogResponse(data, method.ToString());
				await handler.WriteAsync(data);
			}
		}

		private async Task ConfigureLogsAsync(TransferHandler handler, LogMethod method)
		{
			var data = await handler.ReadDataAsync();
			this.LogRequest(data, method.ToString());
			await this.ConfigureAsync(data, method);
		}

		private async Task ConfigureFilesAsync(TransferHandler handler, LogMethod method)
		{
			await this.ConfigureAsync(await handler.ReadDataAsync(), method);
		}

		private async Task ConfigureDatabaseAsync(TransferHandler handler, LogMethod method)
		{
			await this.ConfigureAsync(await handler.ReadDataAsync(), method);
		}

		private async Task UploadLogsAsync(TransferHandler handler, LogMethod method)
		{
			var data = await UploadAsync(await handler.ReadDataAsync(), _logFolders, method, true);
			this.LogResponse(data, method.ToString());
			await handler.WriteAsync(data);
		}

		private async Task UploadFilesAsync(TransferHandler handler, LogMethod method)
		{
			var data = await UploadAsync(await handler.ReadDataAsync(), _filesFolders, method, false);
			this.LogResponse(data, method.ToString());
			await handler.WriteAsync(data);
		}

		private async Task UploadDatabaseAsync(TransferHandler handler, LogMethod method)
		{
			var data = await UploadAsync(await handler.ReadDataAsync(), _dbFolders, method, false);
			this.LogResponse(data, method.ToString());
			await handler.WriteAsync(data);
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
					var buffer = MemoryPool.Get80KBuffer();
					try
					{
						await new PackageHelper(buffer).UnpackAsync(ms, userFolder, append);
						success = true;
					}
					finally
					{
						MemoryPool.Return80KBuffer(buffer);
					}
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