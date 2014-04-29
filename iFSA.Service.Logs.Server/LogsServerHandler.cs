using System;
using System.IO;
using System.Text;
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

		private static readonly int Categories = Enum.GetValues(typeof(LogCategory)).Length;
		private static readonly int Platforms = Enum.GetValues(typeof(ClientPlatform)).Length;

		private readonly string[] _folders = new string[Categories * Platforms];

		public LogsServerHandler(byte id)
			: base(id)
		{
			for (var i = 0; i < _folders.Length; i++)
			{
				_folders[i] = string.Empty;
			}
		}

		public override async Task InitializeAsync()
		{
			try
			{
				var index = 0;
				using (var sr = new StreamReader(ConfigName))
				{
					string line;
					while ((line = await sr.ReadLineAsync()) != null)
					{
						_folders[index++] = line;
					}
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
				switch ((LogMethod)methodId)
				{
					case LogMethod.GetConfigs:
						await this.GetConfigsAsync(h);
						break;
					case LogMethod.ConfigureLogs:
						await this.ConfigureAsync(h, LogCategory.Logs);
						break;
					case LogMethod.ConfigureFiles:
						await this.ConfigureAsync(h, LogCategory.Files);
						break;
					case LogMethod.ConfigureDatabase:
						await this.ConfigureAsync(h, LogCategory.Database);
						break;
					case LogMethod.UploadLogs:
						await this.UploadAsync(h, LogCategory.Logs, true);
						break;
					case LogMethod.UploadFiles:
						await this.UploadAsync(h, LogCategory.Files);
						break;
					case LogMethod.UploadDatabase:
						await this.UploadAsync(h, LogCategory.Database);
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

		private async Task GetConfigsAsync(ITransferHandler handler)
		{
			using (var ms = new MemoryStream())
			{
				for (var i = 0; i < _folders.Length; i++)
				{
					var folder = _folders[i];
					var platform = (ClientPlatform)(i / Categories);
					var category = (LogCategory)(i % Platforms);

					var config = new LogConfig(new RequestHeader(platform, RequestHeader.EmptyVersion, string.Empty, string.Empty), category, folder);
					var buffer = config.NetworkBuffer;
					ms.Write(buffer, 0, buffer.Length);
				}

				await handler.WriteAsync(ms.ToArray());
			}
		}

		private async Task ConfigureAsync(ITransferHandler handler, LogCategory category)
		{
			using (var ms = new MemoryStream(await handler.ReadAsync()))
			{
				var logConfig = new LogConfig().Setup(ms);
				_folders[FolderIndex(logConfig.RequestHeader.ClientPlatform, category)] = logConfig.Folder;
			}

			var buffer = new StringBuilder();
			foreach (var folder in _folders)
			{
				buffer.AppendLine(folder);
			}
			using (var sw = new StreamWriter(ConfigName))
			{
				await sw.WriteAsync(buffer.ToString());
			}
		}

		private async Task<byte[]> UploadAsync(ITransferHandler handler, LogCategory category, bool append = false)
		{
			using (var ms = new MemoryStream(await handler.ReadAsync()))
			{
				var header = new RequestHeader().Setup(ms);
				var folder = _folders[FolderIndex(header.ClientPlatform, category)];
				if (folder != string.Empty)
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
						return OneBytes;
					}
					finally
					{
						MemoryPool.Return80KBuffer(buffer);
					}
				}
			}

			return ZeroBytes;
		}

		private static int FolderIndex(ClientPlatform platform, LogCategory category)
		{
			return ((int)platform * Categories) + (int)category;
		}
	}
}