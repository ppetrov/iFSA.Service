using System;

namespace iFSA.Service.AutoUpdate
{
	public abstract class AppVersion
	{
		public Platform Platform { get; private set; }
		public Version Version { get; private set; }

		protected AppVersion(Platform platform, Version version)
		{
			if (version == null) throw new ArgumentNullException("version");

			this.Platform = platform;
			this.Version = version;
		}

		protected void Setup(byte[] input)
		{
			if (input == null) throw new ArgumentNullException("input");

			this.Platform = (Platform)BitConverter.ToInt32(input, 0);
			this.Version = new Version(BitConverter.ToInt32(input, 4), BitConverter.ToInt32(input, 8), BitConverter.ToInt32(input, 12), BitConverter.ToInt32(input, 16));
		}
	}
}