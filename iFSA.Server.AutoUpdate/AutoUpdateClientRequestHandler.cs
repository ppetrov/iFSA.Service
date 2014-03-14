using System;
using System.Net.Sockets;
using iFSA.Server.Core;

namespace iFSA.Server.AutoUpdate
{
	public sealed class AutoUpdateClientRequestHandler : ClientRequestHandler
	{
		public AutoUpdateClientRequestHandler(byte id, TransferHandler transferHandler)
			: base(id, transferHandler)
		{
#if DEBUG
			this.WriteProgress += (sender, _) => Console.WriteLine("Uploading ... " + _.ToString(@"F2") + "%");
			this.ReadProgress += (sender, _) => Console.WriteLine("Downloading ... " + _.ToString(@"F2") + "%");
#endif
		}

		public byte[] DownloadServerVersion(TcpClient client, ClientVersion version)
		{
			if (client == null) throw new ArgumentNullException("client");
			if (version == null) throw new ArgumentNullException("version");

			byte[] data;

			using (var s = client.GetStream())
			{
				this.TransferHandler.Write(s, this.Id);
				this.TransferHandler.WriteData(s, version.GetNetworkBuffer());

				data = this.TransferHandler.ReadData(s);

				this.TransferHandler.WriteClose(s);
			}



			return data;
		}
	}
}