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

		protected void LogRequest(byte[] data, string method)
		{
			// TODO : !!!
			//Trace.WriteLine(string.Format(@"Read {0} bytes from client ({1})", data.Length, method));
		}

		protected void LogResponse(byte[] data, string method)
		{
			// TODO : !!!
			//Trace.WriteLine(string.Format(@"Send {0} bytes to client ({1})", data.Length, method));
		}
	}
}