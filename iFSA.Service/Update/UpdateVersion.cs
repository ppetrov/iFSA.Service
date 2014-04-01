using System;
using System.IO;

namespace iFSA.Service.Update
{
	public sealed class UpdateVersion
	{
		public AppVersion AppVersion { get; private set; }
		public byte[] Package { get; private set; }
		public byte[] NetworkBuffer { get; private set; }

		public UpdateVersion(byte[] networkBuffer)
		{
			if (networkBuffer == null) throw new ArgumentNullException("networkBuffer");

			this.NetworkBuffer = networkBuffer;

			using (var ms = new MemoryStream(networkBuffer))
			{
				this.AppVersion = AppVersion.Create(ms);
				this.Package = new byte[networkBuffer.Length - ms.Position];
				Array.Copy(networkBuffer, ms.Position, this.Package, 0, this.Package.Length);
			}
		}

		public UpdateVersion(AppVersion appVersion, byte[] package)
		{
			if (appVersion == null) throw new ArgumentNullException("appVersion");
			if (package == null) throw new ArgumentNullException("package");

			this.AppVersion = appVersion;
			this.Package = package;

			var buffer = appVersion.NetworkBuffer;
			using (var ms = new MemoryStream(buffer.Length + package.Length))
			{
				ms.Write(buffer, 0, buffer.Length);
				ms.Write(package, 0, package.Length);
				this.NetworkBuffer = ms.GetBuffer();
			}
		}
	}
}