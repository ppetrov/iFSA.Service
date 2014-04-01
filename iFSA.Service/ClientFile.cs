using System;
using System.IO;

namespace iFSA.Service
{
	public sealed class ClientFile
	{
		public FileInfo File { get; private set; }

		public ClientFile(FileInfo file)
		{
			if (file == null) throw new ArgumentNullException("file");

			this.File = file;
		}
	}
}