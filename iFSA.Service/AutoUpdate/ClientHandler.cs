using System;
using System.Net.Sockets;

namespace iFSA.Service.AutoUpdate
{
	public sealed class ClientHandler : ClientHandlerBase
	{
		public ClientHandler(byte id, TransferHandler transferHandler)
			: base(id, transferHandler)
		{
#if DEBUG
			this.TransferHandler.WriteProgress += (sender, _) => Console.WriteLine("Uploading ... " + _.ToString(@"F2") + "%");
			this.TransferHandler.ReadProgress += (sender, _) => Console.WriteLine("Downloading ... " + _.ToString(@"F2") + "%");
#endif
		}

		public byte[] DownloadVersion(TcpClient client, ClientVersion version)
		{
			if (client == null) throw new ArgumentNullException("client");
			if (version == null) throw new ArgumentNullException("version");

			using (var s = client.GetStream())
			{
				try
				{
					this.TransferHandler.Write(s, this.Id, (byte)Function.DownloadVersion);
					this.TransferHandler.WriteData(s, version.GetNetworkBuffer());

					var data = this.TransferHandler.ReadData(s);
					if (data.Length == TransferHandler.NoData.Length)
					{
						data = null;
					}
					return data;
				}
				finally
				{
					this.TransferHandler.WriteClose(s);
				}
			}
		}

		public void PublishVersion(TcpClient client, ServerVersion version)
		{
			if (client == null) throw new ArgumentNullException("client");
			if (version == null) throw new ArgumentNullException("version");

			using (var s = client.GetStream())
			{
				try
				{
					this.TransferHandler.Write(s, this.Id, (byte)Function.PublishVersion);
					this.TransferHandler.WriteData(s, version.GetNetworkBuffer());
				}
				finally
				{
					this.TransferHandler.WriteClose(s);
				}
			}
		}

		public Version GetVersion(TcpClient client, Platform platform)
		{
			if (client == null) throw new ArgumentNullException("client");

			using (var s = client.GetStream())
			{
				try
				{
					this.TransferHandler.Write(s, this.Id, (byte)Function.GetVersion);
					this.TransferHandler.WriteData(s, BitConverter.GetBytes((int)platform));

					var data = this.TransferHandler.ReadData(s);
					if (data.Length != TransferHandler.NoData.Length)
					{
						return new ServerVersion(data).Version;
					}
				}
				finally
				{
					this.TransferHandler.WriteClose(s);
				}
			}

			return null;
		}
	}
}