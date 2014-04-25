using System;
using System.IO;
using System.Threading.Tasks;
using iFSA.Service.Core;
using iFSA.Service.Update.Client;

namespace iFSA.Service.Update.Server
{
	public sealed class UpdateServerHandler : ServerHandlerBase
	{
		public static readonly string ConfigName = @"update.cfg";

		private readonly string[] _contexts;
		private readonly string[] _paths;
		private readonly RequestPackage[] _packages = new RequestPackage[Constants.SupportedPlatforms];

		public UpdateServerHandler(byte id)
			: base(id)
		{
			_contexts = Enum.GetNames(typeof(UpdateMethod));
			_paths = new string[_contexts.Length];
			for (var i = 0; i < _paths.Length; i++)
			{
				_paths[i] = _contexts + @".dat";
			}
		}

		public override async Task InitializeAsync()
		{
			try
			{
				var length = _packages.Length;
				var versions = new string[length];
				for (var i = 0; i < length; i++)
				{
					versions[i] = string.Empty;
				}

				using (var sr = new StreamReader(ConfigName))
				{
					var index = 0;
					string line;
					while ((line = await sr.ReadLineAsync()) != null)
					{
						if (index < length)
						{
							versions[index++] = line;
						}
					}
				}

				for (var i = 0; i < length; i++)
				{
					var version = versions[i];
					if (version != string.Empty)
					{
						_packages[i] = await LoadPackageAsync((ClientPlatform)i, version);
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
				var method = (UpdateMethod)methodId;
				switch (method)
				{
					case UpdateMethod.GetVersion:
						await this.GetVersionAsync(h, method);
						break;
					case UpdateMethod.GetVersions:
						await this.GetVersionsAsync(h, method);
						break;
					case UpdateMethod.UploadPackage:
						await this.UploadPackageAsync(h, method);
						break;
					case UpdateMethod.DownloadPackage:
						await this.DownloadPackageAsync(h, method);
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

		private async Task GetVersionAsync(ITransferHandler handler, UpdateMethod method)
		{
			var networkBuffer = Constants.NoDataBytes;

			var context = _contexts[(int)method];
			var input = await handler.ReadAsync();
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

		private async Task GetVersionsAsync(ITransferHandler handler, UpdateMethod method)
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
							await ms.WriteAsync(buffer, 0, buffer.Length);
						}
					}
					networkBuffer = ms.GetBuffer();
				}
			}

			var data = networkBuffer;
			this.LogResponse(data, _contexts[(int)method]);
			await handler.WriteAsync(data);
		}

		private async Task UploadPackageAsync(ITransferHandler handler, UpdateMethod method)
		{
			var input = await handler.ReadAsync();
			this.LogRequest(input, _contexts[(int)method]);

			using (var ms = new MemoryStream(input))
			{
				var header = new RequestHeader().Setup(ms);
				var packageBytes = new byte[ms.Length - ms.Position];
				await ms.ReadAsync(packageBytes, 0, packageBytes.Length);

				var package = new RequestPackage(header, packageBytes);
				_packages[(int)package.Header.ClientPlatform] = package;

				await this.SavePackageAsync(package.Header.ClientPlatform, package);
			}
		}

		private async Task DownloadPackageAsync(ITransferHandler handler, UpdateMethod method)
		{
			var networkBuffer = Constants.NoDataBytes;

			var input = await handler.ReadAsync();
			var context = _contexts[(int)method];
			this.LogRequest(input, context);

			var header = new RequestHeader().Setup(new MemoryStream(input));
			var package = _packages[(int)header.ClientPlatform];
			if (package != null && package.Header.Version > header.Version)
			{
				networkBuffer = package.Data;
			}

			var data = networkBuffer;
			this.LogResponse(data, context);
			await handler.WriteAsync(data);
		}

		private async Task<RequestPackage> LoadPackageAsync(ClientPlatform platform, string version)
		{
			using (var fs = File.OpenRead(_paths[(int)platform]))
			{
				var buffer = MemoryPool.Get80KBuffer();
				try
				{
					using (var ms = new MemoryStream((int)new FileInfo(fs.Name).Length))
					{
						int readBytes;
						while ((readBytes = await fs.ReadAsync(buffer, 0, buffer.Length)) != 0)
						{
							await ms.WriteAsync(buffer, 0, readBytes);
						}
						return new RequestPackage(new RequestHeader(platform, Version.Parse(version), string.Empty, string.Empty),
							ms.GetBuffer());
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
	}
}