using System;
using System.IO;

namespace iFSA.Service
{
	public sealed class RequestPackage : INetworkTransferable<RequestPackage>
	{
		public RequestHeader RequestHeader { get; private set; }
		public byte[] Package { get; private set; }
		public byte[] NetworkBuffer { get; private set; }

		public RequestPackage() { }

		public RequestPackage(RequestHeader requestHeader, byte[] package)
		{
			if (requestHeader == null) throw new ArgumentNullException("requestHeader");
			if (package == null) throw new ArgumentNullException("package");

			this.RequestHeader = requestHeader;
			this.Package = package;

			var headerBuffer = requestHeader.NetworkBuffer;
			using (var ms = new MemoryStream(headerBuffer.Length + package.Length))
			{
				ms.Write(headerBuffer, 0, headerBuffer.Length);
				ms.Write(package, 0, package.Length);

				this.NetworkBuffer = ms.GetBuffer();
			}
		}

		public RequestPackage Setup(MemoryStream stream)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			this.NetworkBuffer = stream.GetBuffer();
			this.RequestHeader = new RequestHeader().Setup(stream);
			this.Package = new byte[stream.Length - stream.Position];
			Array.Copy(this.NetworkBuffer, stream.Position, this.Package, 0, this.Package.Length);

			return this;
		}
	}
}