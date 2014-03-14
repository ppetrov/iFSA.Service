using System;
using System.IO;
using System.Text;

namespace iFSA.Server.AutoUpdate
{
	public sealed class ClientVersion
	{
		public Platform Platform { get; private set; }
		public Version Version { get; private set; }
		public string Username { get; private set; }
		public string Password { get; private set; }

		public ClientVersion(Platform platform, Version version, string username, string password)
		{
			if (version == null) throw new ArgumentNullException("version");
			if (username == null) throw new ArgumentNullException("username");
			if (password == null) throw new ArgumentNullException("password");

			this.Platform = platform;
			this.Version = version;
			this.Username = username;
			this.Password = password;
		}

		public ClientVersion(byte[] bytes)
		{
			this.Platform = (Platform)BitConverter.ToInt32(bytes, 0);
			this.Version = new Version(
				BitConverter.ToInt32(bytes, 4),
				BitConverter.ToInt32(bytes, 8),
				BitConverter.ToInt32(bytes, 12),
				BitConverter.ToInt32(bytes, 16));
			var userDataLength = BitConverter.ToInt32(bytes, 20);
			this.Username = Encoding.Unicode.GetString(bytes, 24, userDataLength);
			var passDataLength = BitConverter.ToInt32(bytes, 24 + userDataLength);
			this.Password = Encoding.Unicode.GetString(bytes, 28 + userDataLength, passDataLength);
		}

		public byte[] GetNetworkBuffer()
		{
			using (var ms = new MemoryStream(256))
			{
				var buffer = BitConverter.GetBytes((int)this.Platform);
				ms.Write(buffer, 0, buffer.Length);

				var v = this.Version;
				buffer = BitConverter.GetBytes(v.Major);
				ms.Write(buffer, 0, buffer.Length);
				buffer = BitConverter.GetBytes(v.Minor);
				ms.Write(buffer, 0, buffer.Length);
				buffer = BitConverter.GetBytes(v.Build);
				ms.Write(buffer, 0, buffer.Length);
				buffer = BitConverter.GetBytes(v.Revision);
				ms.Write(buffer, 0, buffer.Length);

				var tmp = Encoding.Unicode.GetBytes(this.Username);
				buffer = BitConverter.GetBytes(tmp.Length);
				ms.Write(buffer, 0, buffer.Length);
				ms.Write(tmp, 0, tmp.Length);

				tmp = Encoding.Unicode.GetBytes(this.Password);
				buffer = BitConverter.GetBytes(tmp.Length);
				ms.Write(buffer, 0, buffer.Length);
				ms.Write(tmp, 0, tmp.Length);

				return ms.ToArray();
			}
		}
	}
}