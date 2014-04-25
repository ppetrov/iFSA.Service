using System;
using System.IO;

namespace iFSA.Service.Core
{
	public sealed class RequestHeader : INetworkTransferable<RequestHeader>
	{
		public static readonly Version EmptyVersion = new Version(0, 0, 0, 0);

		public ClientPlatform ClientPlatform { get; private set; }
		public Version Version { get; private set; }
		public string Username { get; private set; }
		public string Password { get; private set; }
		public byte[] NetworkBuffer { get; private set; }

		public RequestHeader() { }

		public RequestHeader(ClientPlatform clientPlatform, Version version, string username, string password)
		{
			if (version == null) throw new ArgumentNullException("version");
			if (username == null) throw new ArgumentNullException("username");
			if (password == null) throw new ArgumentNullException("password");

			this.ClientPlatform = clientPlatform;
			this.Version = version;
			this.Username = username;
			this.Password = password;
			this.NetworkBuffer = this.GetNetworkBuffer();
		}

		public RequestHeader Setup(MemoryStream stream)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			var buffer = new byte[NetworkHelper.IntBytes];
			this.ClientPlatform = (ClientPlatform)NetworkHelper.ReadInt32(stream, buffer);
			var major = NetworkHelper.ReadInt32(stream, buffer);
			var minor = NetworkHelper.ReadInt32(stream, buffer);
			var build = NetworkHelper.ReadInt32(stream, buffer);
			var revision = NetworkHelper.ReadInt32(stream, buffer);
			this.Version = new Version(major, minor, build, revision);
			this.Username = NetworkHelper.ReadString(stream, buffer);
			this.Password = NetworkHelper.ReadString(stream, buffer);
			this.NetworkBuffer = this.GetNetworkBuffer();

			return this;
		}

		private byte[] GetNetworkBuffer()
		{
			var platform = (int)this.ClientPlatform;
			var userBuffer = NetworkHelper.GetBytes(this.Username);
			var passBuffer = NetworkHelper.GetBytes(this.Password);

			var result = new byte[NetworkHelper.GetBytesSize(platform) +
								  NetworkHelper.GetBytesSize(this.Version) +
								  NetworkHelper.GetBytesSize(userBuffer) +
								  NetworkHelper.GetBytesSize(passBuffer)];
			using (var ms = new MemoryStream(result))
			{
				NetworkHelper.Write(ms, platform);
				NetworkHelper.Write(ms, this.Version);
				NetworkHelper.Write(ms, userBuffer);
				NetworkHelper.Write(ms, passBuffer);
			}

			return result;
		}
	}
}