using System;

namespace iFSA.Service.Core
{
	public sealed class ClientFile
	{
		public string Name { get; private set; }
		public byte[] Data { get; private set; }

		public ClientFile(string name, byte[] data)
		{
			if (name == null) throw new ArgumentNullException("name");
			if (data == null) throw new ArgumentNullException("data");

			this.Name = name;
			this.Data = data;
		}
	}
}