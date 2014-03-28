using System;
using System.IO;
using System.Text;

namespace iFSA.Service
{
	public sealed class ClientVersion : AppVersion
	{
		public string Username { get; private set; }
		public string Password { get; private set; }

		public ClientVersion(ClientPlatform clientPlatform, Version version, string username, string password)
			: base(clientPlatform, version)
		{
			if (username == null) throw new ArgumentNullException("username");
			if (password == null) throw new ArgumentNullException("password");

			this.Username = username;
			this.Password = password;
		}

		public ClientVersion(byte[] input)
			: base(ClientPlatform.IPad, new Version())
		{
			this.Setup(input);
			var userDataLength = BitConverter.ToInt32(input, 20);
			this.Username = Encoding.Unicode.GetString(input, 24, userDataLength);
			var passDataLength = BitConverter.ToInt32(input, 24 + userDataLength);
			this.Password = Encoding.Unicode.GetString(input, 28 + userDataLength, passDataLength);
		}

		public byte[] GetNetworkBuffer()
		{
			using (var ms = new MemoryStream(256))
			{
				var buffer = BitConverter.GetBytes((int)this.ClientPlatform);
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