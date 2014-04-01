using System;
using System.IO;
using System.Threading.Tasks;
using iFSA.Service.Update;

namespace iFSA.Service.Logs
{
	public sealed class ClientLog
	{
		public AppVersion AppVersion { get; private set; }
		public DirectoryInfo Folder { get; private set; }
		public string SearchPattern { get; private set; }

		public ClientLog(AppVersion appVersion, DirectoryInfo folder, string searchPattern = @"*.txt")
		{
			if (appVersion == null) throw new ArgumentNullException("appVersion");
			if (folder == null) throw new ArgumentNullException("folder");
			if (searchPattern == null) throw new ArgumentNullException("searchPattern");

			this.AppVersion = appVersion;
			this.Folder = folder;
			this.SearchPattern = searchPattern;
		}

		public async Task<byte[]> GetNetworkBufferAsync(byte[] buffer)
		{
			if (buffer == null) throw new ArgumentNullException("buffer");

			return await new PackageHandler(buffer).PackAsync(this.AppVersion, this.Folder, this.SearchPattern);
		}
	}


	public sealed class LogConfig
	{
		public UpdateMethod UpdateMethod { get; private set; }
		public ClientPlatform Platform { get; private set; }
		public DirectoryInfo Folder { get; private set; }


	}
}