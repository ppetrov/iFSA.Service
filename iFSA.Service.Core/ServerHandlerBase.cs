using System;
using System.IO;
using System.Threading.Tasks;

namespace iFSA.Service.Core
{
	public abstract class ServerHandlerBase
	{
		public byte Id { get; private set; }

		protected ServerHandlerBase(byte id)
		{
			if (id <= 0) throw new ArgumentOutOfRangeException("id");

			this.Id = id;
		}

		public abstract Task InitializeAsync();

		public abstract Task ProcessAsync(Stream stream, byte methodId);
	}
}