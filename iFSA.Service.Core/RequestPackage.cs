using System;

namespace iFSA.Service.Core
{
	public sealed class RequestPackage
	{
		public RequestHeader Header { get; private set; }
		public byte[] Data { get; private set; }

		public RequestPackage(RequestHeader header, byte[] data)
		{
			if (header == null) throw new ArgumentNullException("header");
			if (data == null) throw new ArgumentNullException("data");
			if (data.Length == 0) throw new ArgumentOutOfRangeException("data");

			this.Header = header;
			this.Data = data;
		}
	}
}