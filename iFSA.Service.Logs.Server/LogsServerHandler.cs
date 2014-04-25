using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using iFSA.Service.Core;
using iFSA.Service.Logs.Client;

namespace iFSA.Service.Logs.Server
{
	public sealed class LogsServerHandler : ServerHandlerBase
	{
		public static readonly string ConfigName = @"logs.cfg";

		private static readonly byte[] ZeroBytes = { 0, 0, 0, 0 };
		private static readonly byte[] OneBytes = { 1, 0, 0, 0 };

		private readonly string[] _contexts;
		private readonly string[] _dbFolders = new string[Constants.SupportedPlatforms];
		private readonly string[] _logFolders = new string[Constants.SupportedPlatforms];
		private readonly string[] _filesFolders = new string[Constants.SupportedPlatforms];

		public LogsServerHandler(byte id)
			: base(id)
		{
			_contexts = Enum.GetNames(typeof(LogMethod));

			for (var i = 0; i < Constants.SupportedPlatforms; i++)
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
				var folders = new string[3 * Constants.SupportedPlatforms];
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
					offset += Constants.SupportedPlatforms;
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

		private async Task GetConfigsAsync(ITransferHandler handler, LogMethod method)
		{
			using (var ms = new MemoryStream())
			{
				this.Write(ms, _dbFolders, LogMethod.UploadDatabase);
				this.Write(ms, _logFolders, LogMethod.UploadLogs);
				this.Write(ms, _filesFolders, LogMethod.UploadFiles);

				var data = ms.ToArray();
				this.LogResponse(data, _contexts[(int)method]);
				await handler.WriteAsync(data);
			}
		}

		private async Task ConfigureLogsAsync(ITransferHandler handler, LogMethod method)
		{
			var data = await handler.ReadAsync();
			this.LogRequest(data, _contexts[(int)method]);
			await this.ConfigureAsync(data, method);
		}

		private async Task ConfigureFilesAsync(ITransferHandler handler, LogMethod method)
		{
			await this.ConfigureAsync(await handler.ReadAsync(), method);
		}

		private async Task ConfigureDatabaseAsync(ITransferHandler handler, LogMethod method)
		{
			await this.ConfigureAsync(await handler.ReadAsync(), method);
		}

		private async Task UploadLogsAsync(ITransferHandler handler, LogMethod method)
		{
			var data = await UploadAsync(await handler.ReadAsync(), _logFolders, method, true);
			this.LogResponse(data, _contexts[(int)method]);
			await handler.WriteAsync(data);
		}

		private async Task UploadFilesAsync(ITransferHandler handler, LogMethod method)
		{
			var data = await UploadAsync(await handler.ReadAsync(), _filesFolders, method, false);
			this.LogResponse(data, _contexts[(int)method]);
			await handler.WriteAsync(data);
		}

		private async Task UploadDatabaseAsync(ITransferHandler handler, LogMethod method)
		{
			var data = await UploadAsync(await handler.ReadAsync(), _dbFolders, method, false);
			this.LogResponse(data, _contexts[(int)method]);
			await handler.WriteAsync(data);
		}

		private async Task ConfigureAsync(byte[] data, LogMethod method)
		{
			this.LogResponse(data, _contexts[(int)method]);

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
			this.LogRequest(data, _contexts[(int)method]);

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
						await new ServerPackageHelper(buffer).UnpackAsync(ms, userFolder, append);
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