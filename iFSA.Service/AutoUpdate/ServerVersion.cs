using System;
using System.IO;
using System.Threading.Tasks;

namespace iFSA.Service.AutoUpdate
{
	public sealed class ServerVersion : AppVersion
	{
		public const int VersionNetworkBufferSize = 20;

		private byte[] _versionNetworkBuffer;
		private readonly byte[] _package;

		public byte[] Package { get { return _package; } }

		public ServerVersion(Platform platform, Version version, byte[] package)
			: base(platform, version)
		{
			if (package == null) throw new ArgumentNullException("package");

			_package = package;
		}

		public ServerVersion(byte[] input)
			: base(Platform.Ipad, new Version())
		{
			if (input == null) throw new ArgumentNullException("input");

			this.Setup(input);

			_package = new byte[input.Length - VersionNetworkBufferSize];
			Array.Copy(input, VersionNetworkBufferSize, _package, 0, _package.Length);
		}

		public async Task<byte[]> GetVersionNetworkBufferAsync()
		{
			if (_versionNetworkBuffer == null)
			{
				_versionNetworkBuffer = new byte[VersionNetworkBufferSize];

				using (var ms = new MemoryStream(_versionNetworkBuffer))
				{
					await this.WriteAsync(ms);
				}
			}

			return _versionNetworkBuffer;
		}

		public async Task<byte[]> GetNetworkBufferAsync()
		{
			using (var ms = new MemoryStream(VersionNetworkBufferSize + _package.Length))
			{
				await this.WriteAsync(ms);
				await ms.WriteAsync(_package, 0, _package.Length);
				return ms.GetBuffer();
			}
		}
	}
}