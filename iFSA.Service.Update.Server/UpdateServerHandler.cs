using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using iFSA.Service.Core;
using iFSA.Service.Update.Client;

namespace iFSA.Service.Update.Server
{
	public sealed class UpdateServerHandler : ServerHandlerBase
	{
		public static readonly string ConfigName = @"update.cfg";

		private readonly string[] _paths;
		private readonly RequestPackage[] _packages = new RequestPackage[Enum.GetValues(typeof(ClientPlatform)).Length];

		public UpdateServerHandler(byte id)
			: base(id)
		{
			_paths = Enum.GetNames(typeof(ClientPlatform));
			for (var i = 0; i < _paths.Length; i++)
			{
				_paths[i] = _paths[i] + @".dat";
			}
		}

		public override async Task InitializeAsync()
		{
			try
			{
				var index = 0;
				using (var sr = new StreamReader(ConfigName))
				{
					string line;
					while ((line = await sr.ReadLineAsync()) != null)
					{
						var package = default(RequestPackage);
						if (line != string.Empty)
						{
							package = await this.LoadPackageAsync((ClientPlatform)index, line);
						}
						_packages[index++] = package;
					}
				}
			}
			catch (FileNotFoundException) { }
		}

		public override async Task ProcessAsync(Stream stream, byte methodId)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			var buffer = MemoryPool.Get16KBuffer();
			try
			{
				var h = new TransferHandler(stream, buffer);
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
			finally
			{
				MemoryPool.Return16KBuffer(buffer);
			}
		}

		private async Task GetVersionAsync(ITransferHandler handler)
		{
			var networkBuffer = Constants.NoDataBytes;

			var data = await handler.ReadAsync();
			var package = _packages[BitConverter.ToInt32(data, 0)];
			if (package != null)
			{
				networkBuffer = package.Header.NetworkBuffer;
			}

			await handler.WriteAsync(networkBuffer);
		}

		private async Task GetVersionsAsync(ITransferHandler handler)
		{
			var networkBuffer = Constants.NoDataBytes;

			var totalBufferSize = 0;
			foreach (var p in _packages)
			{
				if (p != null)
				{
					totalBufferSize += p.Header.NetworkBuffer.Length;
				}
			}
			if (totalBufferSize > 0)
			{
				using (var ms = new MemoryStream(totalBufferSize))
				{
					foreach (var p in _packages)
					{
						if (p != null)
						{
							var buffer = p.Header.NetworkBuffer;
							ms.Write(buffer, 0, buffer.Length);
						}
					}
					networkBuffer = ms.GetBuffer();
				}
			}

			await handler.WriteAsync(networkBuffer);
		}

		private async Task UploadPackageAsync(ITransferHandler handler)
		{
			using (var ms = new MemoryStream(await handler.ReadAsync()))
			{
				var header = new RequestHeader().Setup(ms);
				var packageBytes = new byte[ms.Length - ms.Position];
				await ms.ReadAsync(packageBytes, 0, packageBytes.Length);

				var package = new RequestPackage(header, packageBytes);
				_packages[(int)package.Header.ClientPlatform] = package;

				await this.SavePackageAsync(package.Header.ClientPlatform, package);
			}
		}

		private async Task DownloadPackageAsync(ITransferHandler handler)
		{
			var networkBuffer = Constants.NoDataBytes;

			var header = new RequestHeader().Setup(new MemoryStream(await handler.ReadAsync()));
			var package = _packages[(int)header.ClientPlatform];
			if (package != null && package.Header.Version > header.Version)
			{
				networkBuffer = package.Data;
			}

			await handler.WriteAsync(networkBuffer);
		}

		private async Task<RequestPackage> LoadPackageAsync(ClientPlatform platform, string version)
		{
			var file = new FileInfo(_paths[(int)platform]);

			using (var fs = file.OpenRead())
			{
				var buffer = MemoryPool.Get80KBuffer();
				try
				{
					using (var ms = new MemoryStream((int)file.Length))
					{
						int readBytes;
						while ((readBytes = await fs.ReadAsync(buffer, 0, buffer.Length)) != 0)
						{
							await ms.WriteAsync(buffer, 0, readBytes);
						}
						return new RequestPackage(new RequestHeader(platform, Version.Parse(version), string.Empty, string.Empty), ms.GetBuffer());
					}
				}
				finally
				{
					MemoryPool.Return80KBuffer(buffer);
				}
			}
		}

		private async Task SavePackageAsync(ClientPlatform platform, RequestPackage package)
		{
			using (var fs = File.OpenWrite(_paths[(int)platform]))
			{
				var buffer = MemoryPool.Get80KBuffer();
				try
				{
					using (var ms = new MemoryStream(package.Data))
					{
						int readBytes;
						while ((readBytes = ms.Read(buffer, 0, buffer.Length)) != 0)
						{
							await fs.WriteAsync(buffer, 0, readBytes);
						}
					}
				}
				finally
				{
					MemoryPool.Return80KBuffer(buffer);
				}
			}

			await this.SavePackageConfigAsync();
		}

		private async Task SavePackageConfigAsync()
		{
			var buffer = new StringBuilder();

			foreach (var package in _packages)
			{
				var version = string.Empty;

				if (package != null)
				{
					version = package.Header.Version.ToString();
				}

				buffer.AppendLine(version);
			}

			using (var sw = new StreamWriter(ConfigName))
			{
				await sw.WriteAsync(buffer.ToString());
			}
		}
	}
}