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

		public async Task<LogConfig[]> GetConfigsAsync(TcpClient client)
		{
			if (client == null) throw new ArgumentNullException("client");

			using (var s = client.GetStream())
			{
				await this.TransferHandler.WriteMethodAsync(s, this.Id, (byte)LogMethod.GetConfigs);

				var data = await this.TransferHandler.ReadDataAsync(s);

				await this.TransferHandler.WriteCloseAsync(s);

				if (data.Length != TransferHandler.NoData.Length)
				{

				}
			}

			return null;
		}

		public async Task UploadLogsAsync(TcpClient client, ClientLog log)
		{
			if (client == null) throw new ArgumentNullException("client");
			if (log == null) throw new ArgumentNullException("log");

			using (var s = client.GetStream())
			{
				await this.TransferHandler.WriteMethodAsync(s, this.Id, (byte)LogMethod.UploadLogs);
				await this.TransferHandler.WriteDataAsync(s, await log.GetNetworkBufferAsync(this.TransferHandler.Buffer));
				await this.TransferHandler.WriteCloseAsync(s);
			}
		}
	}
}