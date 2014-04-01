using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace iFSA.Service.Logs
{
	public sealed class LogsServerHandler : ServerHandlerBase
	{
		private readonly DirectoryInfo[] _dbFolders = new DirectoryInfo[3];
		private readonly DirectoryInfo[] _logFolders = new DirectoryInfo[3];

		public LogsServerHandler(byte id)
			: base(id)
		{
		}

		public override async Task ProcessAsync(Stream stream, byte methodId)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			var h = new TransferHandler { EnableCompression = true };
			switch ((LogMethod)methodId)
			{
				case LogMethod.GetConfigs:
					await this.GetConfigsAsync(stream, h);
					break;
				case LogMethod.ConfigureLogs:
					await this.ConfigureLogsAsync(stream, h);
					break;
				case LogMethod.ConfigureDatabase:
					await this.ConfigureDatabaseAsync(stream, h);
					break;
				case LogMethod.UploadLogs:
					await this.UploadLogsAsync(stream, h);
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
				var emptyVersion = new Version();

				this.Write(ms, _logFolders, emptyVersion, LogMethod.UploadLogs);
				this.Write(ms, _dbFolders, emptyVersion, LogMethod.UploadDatabase);

				await handler.WriteDataAsync(stream, ms.ToArray());
			}
		}



		private async Task ConfigureLogsAsync(Stream stream, TransferHandler handler)
		{
			this.Setup(_logFolders, await handler.ReadDataAsync(stream));
		}

		private async Task ConfigureDatabaseAsync(Stream stream, TransferHandler handler)
		{
			this.Setup(_dbFolders, await handler.ReadDataAsync(stream));
		}

		private async Task UploadLogsAsync(Stream stream, TransferHandler handler)
		{
			var data = await handler.ReadDataAsync(stream);

			using (var ms = new MemoryStream(data))
			{
				var appVersion = AppVersion.Create(ms);
				var folder = _logFolders[(int)appVersion.ClientPlatform];
#if DEBUG
				folder = folder ?? new DirectoryInfo(@"C:\temp\Logs2");
#endif
				if (folder != null)
				{
					var userFolder = new DirectoryInfo(Path.Combine(folder.FullName, appVersion.Username));
					if (!userFolder.Exists)
					{
						userFolder.Create();
					}
					await new PackageHandler(handler.Buffer).UnpackAsync(ms, userFolder);
				}
			}
		}

		private async Task UploadDatabaseAsync(Stream stream, TransferHandler handler)
		{
			//var data = await handler.DecompressAsync(await handler.ReadDataAsync(stream));
			//this.Setup(new UpdateVersion(data));
		}

		private void Setup(DirectoryInfo[] folders, byte[] data)
		{
			using (var ms = new MemoryStream(data))
			{
				var appVersion = AppVersion.Create(ms);

				var buffer = new byte[data.Length - ms.Position];
				ms.Read(buffer, 0, buffer.Length);

				folders[(int)appVersion.ClientPlatform] = new DirectoryInfo(Encoding.Unicode.GetString(buffer));
			}
		}

		private void Write(Stream stream, IList<DirectoryInfo> folders, Version emptyVersion, LogMethod method)
		{
			//for (var i = 0; i < folders.Count; i++)
			//{
			//	var folder = folders[i];
			//	var version = new AppVersion((ClientPlatform)i, emptyVersion, string.Empty, string.Empty);
			//	var buffer = new ClientLog(version, folder).GetVersionNetworkBuffer(methods);
			//	stream.Write(buffer, 0, buffer.Length);
			//}
		}

		//public byte[] GetVersionNetworkBuffer(LogMethods methods)
		//{
		//	var path = this.Folder.FullName;
		//	var methodBuffer = BitConverter.GetBytes((int)methods);
		//	var platformBuffer = BitConverter.GetBytes((int)this.AppVersion.ClientPlatform);
		//	var pathLengthBuffer = BitConverter.GetBytes(path.Length);
		//	var pathDataBuffer = Encoding.Unicode.GetBytes(path);

		//	using (var ms = new MemoryStream(methodBuffer.Length + platformBuffer.Length + pathLengthBuffer.Length + pathDataBuffer.Length))
		//	{
		//		ms.Write(methodBuffer, 0, methodBuffer.Length);
		//		ms.Write(platformBuffer, 0, platformBuffer.Length);
		//		ms.Write(pathLengthBuffer, 0, pathLengthBuffer.Length);
		//		ms.Write(pathDataBuffer, 0, pathDataBuffer.Length);

		//		return ms.GetBuffer();
		//	}
		//}
	}
}