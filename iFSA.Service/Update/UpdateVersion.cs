using System;
using System.IO;

namespace iFSA.Service.Update
{
	public sealed class UpdateVersion
	{
		public AppVersion AppVersion { get; private set; }
		public byte[] Package { get; private set; }
		public byte[] NetworkBuffer { get; private set; }

		public UpdateVersion(AppVersion appVersion, byte[] package)
		{
			if (appVersion == null) throw new ArgumentNullException("appVersion");
			if (package == null) throw new ArgumentNullException("package");

			this.AppVersion = appVersion;
			this.Package = package;

			var buffer = appVersion.NetworkBuffer;
			using (var ms = new MemoryStream(buffer.Length + package.Length))
			{
				ms.Write(buffer, 0, buffer.Length);
				ms.Write(package, 0, package.Length);
				this.NetworkBuffer = ms.GetBuffer();
			}
		}

		//public async Task<byte[]> GetNetworkBufferAsync()
		//{
		//	using (var ms = new MemoryStream(VersionNetworkBufferSize + _package.Length))
		//	{
		//		await this.WriteAsync(ms);
		//		await ms.WriteAsync(_package, 0, _package.Length);
		//		return ms.GetBuffer();
		//	}
		//}

		//public static UpdateVersion Create(byte[] input)
		//{
		//	if (input == null) throw new ArgumentNullException("input");

		//	using (var ms = new MemoryStream(input))
		//	{
		//		return Create(ms);
		//	}
		//}

		//public static UpdateVersion Create(MemoryStream stream)
		//{
		//	if (stream == null) throw new ArgumentNullException("stream");

		//	var buffer = BitConverter.GetBytes(0);

		//	//var platform = (ClientPlatform)ReadInt32(stream, buffer);
		//	//var version = new Version(ReadInt32(stream, buffer), ReadInt32(stream, buffer), ReadInt32(stream, buffer), ReadInt32(stream, buffer));
		//	//var username = ReadString(stream, new byte[ReadInt32(stream, buffer)]);
		//	//var password = ReadString(stream, new byte[ReadInt32(stream, buffer)]);

		//	//return new ClientVersion(platform, version, username, password);
		//	return null;
		//}

		//public UpdateVersion(byte[] input)
		//{
		//	if (input == null) throw new ArgumentNullException("input");

		//	this.Setup(input);

		//	_package = new byte[input.Length - VersionNetworkBufferSize];
		//	Array.Copy(input, VersionNetworkBufferSize, _package, 0, _package.Length);
		//}


	}
}