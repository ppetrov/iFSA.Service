using System;

namespace iFSA.Service
{
	public sealed class ClientFile
	{
		public string Path { get; private set; }
		public byte[] Data { get; private set; }

		public ClientFile(string path, byte[] data)
		{
			if (path == null) throw new ArgumentNullException("path");
			if (data == null) throw new ArgumentNullException("data");

			this.Path = path;
			this.Data = data;
		}
	}
}