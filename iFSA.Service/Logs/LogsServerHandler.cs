using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace iFSA.Service.Logs
{
	public sealed class LogsServerHandler : ServerHandlerBase
	{
		private static readonly byte[] ZeroBytes = { 0, 0, 0, 0 };
		private static readonly byte[] OneBytes = { 1, 0, 0, 0 };

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
				Trace.WriteLine(string.Format(@"Send {0} bytes to client ({1})", data.Length, method));
				await handler.WriteAsync(stream, data);
			}
		}

		private async Task ConfigureLogsAsync(Stream stream, TransferHandler handler, LogMethod method)
		{
			var data = await handler.ReadDataAsync(stream);
			Trace.WriteLine(string.Format(@"Read {0} bytes from client ({1})", data.Length, method));
			this.Configure(data, method);
		}

		private async Task ConfigureFilesAsync(Stream stream, TransferHandler handler, LogMethod method)
		{
			this.Configure(await handler.ReadDataAsync(stream), method);
		}

		private async Task ConfigureDatabaseAsync(Stream stream, TransferHandler handler, LogMethod method)
		{
			this.Configure(await handler.ReadDataAsync(stream), method);
		}

		private async Task UploadLogsAsync(Stream stream, TransferHandler handler, LogMethod method)
		{
			var data = await Upload(await handler.ReadDataAsync(stream), _logFolders, true);
			Trace.WriteLine(string.Format(@"Send {0} bytes to client ({1})", data.Length, method));
			await handler.WriteAsync(stream, data);
		}

		private async Task UploadFilesAsync(Stream stream, TransferHandler handler, LogMethod method)
		{
			var data = await Upload(await handler.ReadDataAsync(stream), _filesFolders, false);
			Trace.WriteLine(string.Format(@"Send {0} bytes to client ({1})", data.Length, method));
			await handler.WriteAsync(stream, data);
		}

		private async Task UploadDatabaseAsync(Stream stream, TransferHandler handler, LogMethod method)
		{
			var data = await Upload(await handler.ReadDataAsync(stream), _dbFolders, false);
			Trace.WriteLine(string.Format(@"Send {0} bytes to client ({1})", data.Length, method));
			await handler.WriteAsync(stream, data);
		}

		private void Configure(byte[] data, LogMethod method)
		{
			Trace.WriteLine(string.Format(@"Send {0} bytes to client ({1})", data.Length, method));

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

		private async Task<byte[]> Upload(byte[] data, string[] folders, bool append)
		{
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
	}
}