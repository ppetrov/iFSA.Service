using System;
using System.IO;
using System.Threading.Tasks;

namespace iFSA.Service.Logs
{
	public class ClientLog
	{
		public DirectoryInfo Folder { get; private set; }
		public string SearchPattern { get; private set; }

		public ClientLog(DirectoryInfo folder, string searchPattern = @"*.txt")
		{
			if (folder == null) throw new ArgumentNullException("folder");
			if (searchPattern == null) throw new ArgumentNullException("searchPattern");

			this.Folder = folder;
			this.SearchPattern = searchPattern;
		}

		public async Task<byte[]> GetNetworkBufferAsync(byte[] clientBuffer)
		{
			return await new PackageHandler().PackAsync(clientBuffer, this.Folder, this.SearchPattern);
		}
	}
}