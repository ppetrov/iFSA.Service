using System;
using System.IO;

namespace iFSA.Server.AutoUpdate
{
	public sealed class ServerVersion
	{
		private byte[] _data;

		public Version Version { get; private set; }
		public FileInfo Package { get; private set; }

		public ServerVersion(Version version, FileInfo package)
		{
			if (version == null) throw new ArgumentNullException("version");
			if (package == null) throw new ArgumentNullException("package");

			this.Version = version;
			this.Package = package;
		}

		public byte[] GetPackageBuffer()
		{
			if (_data == null)
			{
				using (var ms = new MemoryStream((int)this.Package.Length))
				{
					using (var fs = this.Package.OpenRead())
					{
						var buffer = new byte[16 * 4 * 1024];

						int readBytes;
						while ((readBytes = fs.Read(buffer, 0, buffer.Length)) != 0)
						{
							ms.Write(buffer, 0, readBytes);
						}
					}
					_data = ms.GetBuffer();
				}
			}
			return _data;
		}
	}
}