using System;
using System.IO;

namespace iFSA.Service
{
	public sealed class RequestPackage : INetworkTransferable<RequestPackage>
	{
		private RequestHeader _requestHeader;
		private byte[] _package;
		private byte[] _networkBuffer;

		public RequestHeader RequestHeader
		{
			get
			{
				if (_requestHeader == null)
				{
					this.SetupHeaderAndPackage();
				}
				return _requestHeader;
			}
		}
		public byte[] Package
		{
			get
			{
				if (_package == null)
				{
					this.SetupHeaderAndPackage();
				}
				return _package;
			}
		}
		public byte[] NetworkBuffer
		{
			get { return _networkBuffer ?? (_networkBuffer = Utilities.Concat(_requestHeader.NetworkBuffer, _package)); }
		}

		public RequestPackage() { }

		public RequestPackage(RequestHeader requestHeader, byte[] package)
		{
			if (requestHeader == null) throw new ArgumentNullException("requestHeader");
			if (package == null) throw new ArgumentNullException("package");

			_requestHeader = requestHeader;
			_package = package;
		}

		public RequestPackage Setup(MemoryStream stream)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			_networkBuffer = stream.GetBuffer();

			return this;
		}

		private void SetupHeaderAndPackage()
		{
			using (var stream = new MemoryStream(_networkBuffer))
			{
				_requestHeader = new RequestHeader().Setup(stream);
				_package = new byte[stream.Length - stream.Position];
				Array.Copy(this.NetworkBuffer, stream.Position, this.Package, 0, this.Package.Length);
			}
		}
	}
}