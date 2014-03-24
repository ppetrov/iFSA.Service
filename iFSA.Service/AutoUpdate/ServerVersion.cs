using System;
using System.IO;
using System.Threading.Tasks;

namespace iFSA.Service.AutoUpdate
{
	public sealed class ServerVersion : AppVersion
	{
		public byte[] Package { get; private set; }

		public ServerVersion(Platform platform, Version version, byte[] package)
			: base(platform, version)
		{
			if (package == null) throw new ArgumentNullException("package");

			this.Package = package;
		}

		public ServerVersion(byte[] input)
			: base(Platform.Ipad, new Version())
		{
			if (input == null) throw new ArgumentNullException("input");

			this.Setup(input);

			this.Package = new byte[input.Length - 20];
			Array.Copy(input, 20, this.Package, 0, this.Package.Length);
		}

		public async Task<byte[]> GetNetworkBufferAsync(bool includeData = true)
		{
			var capacity = 20;
			if (includeData)
			{
				capacity += this.Package.Length;
			}
			using (var ms = new MemoryStream(capacity))
			{
				var buffer = BitConverter.GetBytes((int)this.Platform);
				await ms.WriteAsync(buffer, 0, buffer.Length);

				buffer = BitConverter.GetBytes(this.Version.Major);
				await ms.WriteAsync(buffer, 0, buffer.Length);

				buffer = BitConverter.GetBytes(this.Version.Minor);
				await ms.WriteAsync(buffer, 0, buffer.Length);

				buffer = BitConverter.GetBytes(this.Version.Build);
				await ms.WriteAsync(buffer, 0, buffer.Length);

				buffer = BitConverter.GetBytes(this.Version.Revision);
				await ms.WriteAsync(buffer, 0, buffer.Length);

				if (includeData)
				{
					await ms.WriteAsync(this.Package, 0, this.Package.Length);
				}

				return ms.GetBuffer();
			}
		}
	}
}