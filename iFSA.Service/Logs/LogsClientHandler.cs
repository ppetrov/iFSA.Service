using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace iFSA.Service.Logs
{
	public sealed class LogsClientHandler : ClientHandlerBase
	{
		public LogsClientHandler(byte id)
			: base(id, new TransferHandler { EnableCompression = true })
		{
#if DEBUG
			this.TransferHandler.WriteProgress += (sender, _) => Console.WriteLine("Uploading ... " + _.ToString(@"F2") + "%");
			this.TransferHandler.ReadProgress += (sender, _) => Console.WriteLine("Downloading ... " + _.ToString(@"F2") + "%");
#endif
		}

		public async Task UploadLogsAsync(TcpClient client, ClientVersion clientVersion, ClientLog log)
		{
			if (client == null) throw new ArgumentNullException("client");
			if (clientVersion == null) throw new ArgumentNullException("clientVersion");
			if (log == null) throw new ArgumentNullException("log");

			var clientVersionBuffer = clientVersion.GetNetworkBuffer();
			var buffer = await log.GetNetworkBufferAsync(clientVersionBuffer);

			using (var s = client.GetStream())
			{
				await this.TransferHandler.WriteMethodAsync(s, this.Id, (byte)LogMethods.UploadLogs);
				await this.TransferHandler.WriteDataAsync(s, buffer);
				await this.TransferHandler.WriteCloseAsync(s);
			}
		}
	}
}