using System;
using System.IO;
using System.Threading.Tasks;
using iFSA.Service;
using iFSA.Service.Update;

namespace iFSA.Update
{
	public sealed class UpdateServerHandler : ServerHandlerBase
	{
		public static readonly string ConfigName = @"update.cfg";

		private readonly RequestPackage[] _packages = new RequestPackage[Server.SupportedPlatforms];

		public UpdateServerHandler(byte id)
			: base(id)
		{
		}

		public override async Task InitializeAsync()
		{
			try
			{
				var versions = new string[_packages.Length];
				for (var i = 0; i < versions.Length; i++)
				{
					versions[i] = string.Empty;
				}

				using (var sr = new StreamReader(ConfigName))
				{
					var index = 0;
					string line;
					while ((line = await sr.ReadLineAsync()) != null)
					{
						if (index < versions.Length)
						{
							versions[index++] = line;
						}
					}
				}

				for (var i = 0; i < versions.Length; i++)
				{
					var version = versions[i];
					if (version != string.Empty)
					{
						var platform = (ClientPlatform)i;
						var header = new RequestHeader(platform, Version.Parse(version), string.Empty, string.Empty);
						var data = File.ReadAllBytes(GetPackagePath(platform));
						_packages[i] = new RequestPackage(header, data);
					}
				}
			}
			catch (FileNotFoundException) { }
		}

		public override async Task ProcessAsync(Stream stream, byte methodId)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			var h = new TransferHandler(stream);
			switch ((UpdateMethod)methodId)
			{
				case UpdateMethod.GetVersion:
					await this.GetVersionAsync(h);
					break;
				case UpdateMethod.GetVersions:
					await this.GetVersionsAsync(h);
					break;
				case UpdateMethod.UploadPackage:
					await this.UploadPackageAsync(h);
					break;
				case UpdateMethod.DownloadPackage:
					await this.DownloadPackageAsync(h);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private async Task GetVersionAsync(TransferHandler handler)
		{
			var networkBuffer = TransferHandler.NoDataBytes;

			var context = UpdateMethod.GetVersion.ToString();
			var input = await handler.ReadDataAsync();
			this.LogRequest(input, context);
			var package = _packages[BitConverter.ToInt32(input, 0)];
			if (package != null)
			{
				networkBuffer = package.Header.NetworkBuffer;
			}

			var data = networkBuffer;
			this.LogResponse(data, context);
			await handler.WriteAsync(data);
		}

		private async Task GetVersionsAsync(TransferHandler handler)
		{
			var networkBuffer = TransferHandler.NoDataBytes;

			using (var ms = new MemoryStream())
			{
				foreach (var v in _packages)
				{
					if (v != null)
					{
						var buffer = v.Header.NetworkBuffer;
						ms.Capacity += buffer.Length;
						ms.Write(buffer, 0, buffer.Length);
					}
				}
				if (ms.Length != 0)
				{
					networkBuffer = ms.GetBuffer();
				}
			}

			var data = networkBuffer;
			this.LogResponse(data, UpdateMethod.GetVersions.ToString());
			await handler.WriteAsync(data);
		}

		private async Task UploadPackageAsync(TransferHandler handler)
		{
			var input = await handler.ReadDataAsync();
			this.LogRequest(input, UpdateMethod.UploadPackage.ToString());

			using (var ms = new MemoryStream(input))
			{
				var header = new RequestHeader().Setup(ms);
				var packageBytes = new byte[ms.Length - ms.Position];
				ms.Read(packageBytes, 0, packageBytes.Length);

				var package = new RequestPackage(header, packageBytes);
				_packages[(int)package.Header.ClientPlatform] = package;

				await this.SavePackageAsync(package.Header.ClientPlatform, package);
			}
		}

		private async Task DownloadPackageAsync(TransferHandler handler)
		{
			var networkBuffer = TransferHandler.NoDataBytes;

			var input = await handler.ReadDataAsync();
			this.LogRequest(input, UpdateMethod.DownloadPackage.ToString());

			var header = new RequestHeader().Setup(new MemoryStream(input));
			var package = _packages[(int)header.ClientPlatform];
			if (package != null && package.Header.Version > header.Version)
			{
				networkBuffer = package.Data;
			}

			var data = networkBuffer;
			this.LogResponse(data, UpdateMethod.DownloadPackage.ToString());
			await handler.WriteAsync(data);
		}

		private async Task SavePackageAsync(ClientPlatform platform, RequestPackage package)
		{
			using (var fs = File.OpenWrite(GetPackagePath(platform)))
			{
				var data = package.Data;
				await fs.WriteAsync(data, 0, data.Length);
			}
			await this.SavePackageConfigAsync();
		}

		private async Task SavePackageConfigAsync()
		{
			using (var sw = new StreamWriter(ConfigName))
			{
				foreach (var package in _packages)
				{
					var value = string.Empty;

					if (package != null)
					{
						value = package.Header.Version.ToString();
					}

					await sw.WriteLineAsync(value);
				}
			}
		}

		private static string GetPackagePath(ClientPlatform platform)
		{
			return platform + @".dat";
		}
	}
}