using System;
using System.IO;

namespace iFSA.Service.Logs
{
	public sealed class LogConfig : INetworkTransferable<LogConfig>
	{
		public RequestHeader RequestHeader { get; private set; }
		public LogMethod LogMethod { get; private set; }
		public string Folder { get; private set; }
		public byte[] NetworkBuffer { get; private set; }

		public LogConfig() { }

		public LogConfig(RequestHeader requestHeader, LogMethod logMethod, string folder)
		{
			if (requestHeader == null) throw new ArgumentNullException("requestHeader");
			if (folder == null) throw new ArgumentNullException("folder");

			this.RequestHeader = requestHeader;
			this.LogMethod = logMethod;
			this.Folder = folder;
			this.NetworkBuffer = this.GetNetworkBuffer();
		}

		public LogConfig Setup(MemoryStream stream)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			var buffer = BitConverter.GetBytes(0);
			this.RequestHeader = new RequestHeader().Setup(stream);
			this.LogMethod = (LogMethod)NetworkHelper.ReadInt32(stream, buffer);
			this.Folder = NetworkHelper.ReadString(stream, buffer);
			this.NetworkBuffer = this.GetNetworkBuffer();

			return this;
		}

		private byte[] GetNetworkBuffer()
		{
			var buffer = this.RequestHeader.NetworkBuffer;
			var updateMethod = (int)this.LogMethod;
			var folderBuffer = NetworkHelper.GetBytes(this.Folder);

			using (var ms = new MemoryStream(
				NetworkHelper.GetRawSize(buffer) +
				NetworkHelper.GetBytesSize(updateMethod) +
				NetworkHelper.GetBytesSize(folderBuffer)))
			{
				NetworkHelper.WriteRaw(ms, buffer);
				NetworkHelper.Write(ms, updateMethod);
				NetworkHelper.Write(ms, folderBuffer);

				return ms.GetBuffer();
			}
		}
	}
}