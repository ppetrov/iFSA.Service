using System;
using System.IO;
using System.Threading.Tasks;

namespace iFSA.Service.Logs
{
	public sealed class LogsServerHandler : ServerHandlerBase
	{
		private readonly DirectoryInfo[] _folders = new DirectoryInfo[3];

		public LogsServerHandler(byte id)
			: base(id)
		{
		}

		public override async Task ProcessAsync(Stream stream, byte methodId)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			var h = new TransferHandler { EnableCompression = true };
			switch ((LogMethods)methodId)
			{
				case LogMethods.UploadLogs:
					await this.UploadLogsAsync(stream, h);
					break;
				case LogMethods.UploadDatabase:
					await this.UploadDatabaseAsync(stream, h);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private async Task UploadLogsAsync(Stream stream, TransferHandler handler)
		{
			var data = await handler.ReadDataAsync(stream);

			using (var ms = new MemoryStream(data))
			{
				var appVersion = AppVersion.Create(ms);
				var f = _folders[(int)appVersion.ClientPlatform] ?? new DirectoryInfo(@"C:\temp\Logs2");

				await new PackageHandler(handler.Buffer).UnpackAsync(ms, f);
			}
		}

		private async Task UploadDatabaseAsync(Stream stream, TransferHandler handler)
		{
			var data = await handler.DecompressAsync(await handler.ReadDataAsync(stream));
			//this.Setup(new UpdateVersion(data));
		}
	}
}