using System;
using System.IO;
using System.Text;

namespace iFSA.Service
{
	public static class NetworkHelper
	{
		public static void Write(Stream stream, int value)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			var buffer = BitConverter.GetBytes(value);
			stream.Write(buffer, 0, buffer.Length);
		}

		public static void Write(Stream stream, string value)
		{
			if (stream == null) throw new ArgumentNullException("stream");
			if (value == null) throw new ArgumentNullException("value");

			// Length
			Write(stream, value.Length);

			// Data
			var buffer = Encoding.Unicode.GetBytes(value);
			stream.Write(buffer, 0, buffer.Length);
		}

		public static void Write(Stream stream, byte[] buffer)
		{
			if (stream == null) throw new ArgumentNullException("stream");
			if (buffer == null) throw new ArgumentNullException("buffer");

			// Length
			Write(stream, buffer.Length);

			// Data
			WriteRaw(stream, buffer);
		}

		public static void WriteRaw(Stream stream, byte[] buffer)
		{
			if (stream == null) throw new ArgumentNullException("stream");
			if (buffer == null) throw new ArgumentNullException("buffer");

			stream.Write(buffer, 0, buffer.Length);
		}

		public static void Write(MemoryStream stream, Version version)
		{
			if (version == null) throw new ArgumentNullException("version");

			Write(stream, Math.Max(version.Major, 0));
			Write(stream, Math.Max(version.Minor, 0));
			Write(stream, Math.Max(version.Build, 0));
			Write(stream, Math.Max(version.Revision, 0));
		}

		public static int ReadInt32(Stream stream, byte[] buffer)
		{
			if (stream == null) throw new ArgumentNullException("stream");
			if (buffer == null) throw new ArgumentNullException("buffer");

			stream.Read(buffer, 0, buffer.Length);
			return BitConverter.ToInt32(buffer, 0);
		}

		public static string ReadString(Stream stream, byte[] buffer)
		{
			if (stream == null) throw new ArgumentNullException("stream");
			if (buffer == null) throw new ArgumentNullException("buffer");

			var tmp = new byte[ReadInt32(stream, buffer)];
			stream.Read(tmp, 0, tmp.Length);
			return Encoding.Unicode.GetString(tmp);
		}

		public static int GetRawSize(byte[] buffer)
		{
			if (buffer == null) throw new ArgumentNullException("buffer");

			return buffer.Length;
		}

		public static int GetNetworkSize(int value)
		{
			return 4;
		}

		public static int GetNetworkSize(byte[] buffer)
		{
			if (buffer == null) throw new ArgumentNullException("buffer");

			return GetNetworkSize(buffer.Length) + buffer.Length;
		}

		public static int GetNetworkSize(Version version)
		{
			if (version == null) throw new ArgumentNullException("version");

			return GetNetworkSize(version.Major) + GetNetworkSize(version.Minor) + GetNetworkSize(version.Build) + GetNetworkSize(version.Revision);
		}

		public static byte[] GetNetworkBytes(string value)
		{
			if (value == null) throw new ArgumentNullException("value");

			return Encoding.Unicode.GetBytes(value);
		}
	}
}