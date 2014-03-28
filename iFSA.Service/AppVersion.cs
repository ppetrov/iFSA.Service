using System;
using System.IO;
using System.Threading.Tasks;

namespace iFSA.Service
{
	public class AppVersion
	{
		public ClientPlatform ClientPlatform { get; private set; }
		public Version Version { get; private set; }

		public AppVersion(ClientPlatform clientPlatform, Version version)
		{
			if (version == null) throw new ArgumentNullException("version");

			this.ClientPlatform = clientPlatform;
			this.Version = version;
		}

		public void Setup(byte[] input)
		{
			if (input == null) throw new ArgumentNullException("input");

			this.ClientPlatform = (ClientPlatform)BitConverter.ToInt32(input, 0);
			this.Version = new Version(BitConverter.ToInt32(input, 4), BitConverter.ToInt32(input, 8), BitConverter.ToInt32(input, 12), BitConverter.ToInt32(input, 16));
		}

		public async Task WriteAsync(Stream stream)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			var buffer = BitConverter.GetBytes((int)this.ClientPlatform);
			await stream.WriteAsync(buffer, 0, buffer.Length);

			buffer = BitConverter.GetBytes(this.Version.Major);
			await stream.WriteAsync(buffer, 0, buffer.Length);

			buffer = BitConverter.GetBytes(this.Version.Minor);
			await stream.WriteAsync(buffer, 0, buffer.Length);

			buffer = BitConverter.GetBytes(this.Version.Build);
			await stream.WriteAsync(buffer, 0, buffer.Length);

			buffer = BitConverter.GetBytes(this.Version.Revision);
			await stream.WriteAsync(buffer, 0, buffer.Length);
		}
	}
}