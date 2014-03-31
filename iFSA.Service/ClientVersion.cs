using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace iFSA.Service
{
	public class AppVersion
	{
		public ClientPlatform ClientPlatform { get; private set; }
		public Version Version { get; private set; }
		public string Username { get; private set; }
		public string Password { get; private set; }

		private readonly byte[] _networkBuffer;
		public byte[] NetworkBuffer
		{
			get { return _networkBuffer; }
		}

		public AppVersion(ClientPlatform clientPlatform, Version version, string username, string password)
		{
			if (version == null) throw new ArgumentNullException("version");
			if (username == null) throw new ArgumentNullException("username");
			if (password == null) throw new ArgumentNullException("password");

			this.ClientPlatform = clientPlatform;
			this.Version = version;
			this.Username = username;
			this.Password = password;

			var encoding = Encoding.Unicode;
			var userBuffer = encoding.GetBytes(this.Username);
			var passBuffer = encoding.GetBytes(this.Password);

			using (var ms = new MemoryStream((4 * 5) + (4 + userBuffer.Length) + (4 + passBuffer.Length)))
			{
				Write(ms, (int)this.ClientPlatform);
				Write(ms, this.Version.Major);
				Write(ms, this.Version.Minor);
				Write(ms, this.Version.Build);
				Write(ms, this.Version.Revision);
				Write(ms, userBuffer);
				Write(ms, passBuffer);

				_networkBuffer = ms.GetBuffer();
			}
		}

		//public void Write(Stream stream)
		//{
		//	if (stream == null) throw new ArgumentNullException("stream");

		//	var buffer = BitConverter.GetBytes((int)this.ClientPlatform);
		//	stream.Write(buffer, 0, buffer.Length);

		//	buffer = BitConverter.GetBytes(this.Version.Major);
		//	stream.Write(buffer, 0, buffer.Length);

		//	buffer = BitConverter.GetBytes(this.Version.Minor);
		//	stream.Write(buffer, 0, buffer.Length);

		//	buffer = BitConverter.GetBytes(this.Version.Build);
		//	stream.Write(buffer, 0, buffer.Length);

		//	buffer = BitConverter.GetBytes(this.Version.Revision);
		//	stream.Write(buffer, 0, buffer.Length);
		//}

		public async Task WriteAsync(Stream stream)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			var buffer = BitConverter.GetBytes((int)this.ClientPlatform);
			await stream.WriteAsync(buffer, 0, buffer.Length);

			buffer = BitConverter.GetBytes(this.Version.Major);
			await stream.WriteAsync(buffer, 0, buffer.Length);

			buffer = BitConverter.GetBytes(this.Version.Minor);
			await stream.WriteAsync(buffer, 0, buffer.Length);

			buffer = BitConverter.GetBytes(this.Version.Build);
			await stream.WriteAsync(buffer, 0, buffer.Length);

			buffer = BitConverter.GetBytes(this.Version.Revision);
			await stream.WriteAsync(buffer, 0, buffer.Length);
		}

		public static AppVersion Create(byte[] input)
		{
			if (input == null) throw new ArgumentNullException("input");

			using (var ms = new MemoryStream(input))
			{
				return Create(ms);
			}
		}

		public static AppVersion Create(MemoryStream stream)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			var buffer = BitConverter.GetBytes(0);

			var platform = (ClientPlatform)ReadInt32(stream, buffer);
			var version = new Version(ReadInt32(stream, buffer), ReadInt32(stream, buffer), ReadInt32(stream, buffer), ReadInt32(stream, buffer));
			var username = ReadString(stream, new byte[ReadInt32(stream, buffer)]);
			var password = ReadString(stream, new byte[ReadInt32(stream, buffer)]);

			return new AppVersion(platform, version, username, password);
		}

		private static int ReadInt32(Stream stream, byte[] buffer)
		{
			if (stream == null) throw new ArgumentNullException("stream");
			if (buffer == null) throw new ArgumentNullException("buffer");

			stream.Read(buffer, 0, buffer.Length);
			return BitConverter.ToInt32(buffer, 0);
		}

		private static string ReadString(Stream stream, byte[] buffer)
		{
			if (stream == null) throw new ArgumentNullException("stream");
			if (buffer == null) throw new ArgumentNullException("buffer");

			stream.Read(buffer, 0, buffer.Length);
			return Encoding.Unicode.GetString(buffer);
		}

		private static void Write(Stream stream, int value)
		{
			var buffer = BitConverter.GetBytes(value);
			stream.Write(buffer, 0, buffer.Length);
		}

		private static void Write(Stream stream, byte[] buffer)
		{
			Write(stream, buffer.Length);
			stream.Write(buffer, 0, buffer.Length);
		}
	}
}