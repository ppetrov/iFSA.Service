using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace iFSA.Service.Update
{
	public sealed class UpdateClientHandler : ClientHandler
	{
		private readonly CompressionHelper _compressionHelper = new CompressionHelper(new byte[80 * 1024]);

		public override string Name
		{
			get { return @"Update"; }
		}

		public CompressionHelper CompressionHelper
		{
			get { return _compressionHelper; }
		}

		public UpdateClientHandler(byte id, TransferHandler transferHandler)
			: base(id, transferHandler)
		{
#if DEBUG
			this.TransferHandler.WriteProgress += (sender, _) => Console.WriteLine("Uploading ... " + _.ToString(@"F2") + "%");
			this.TransferHandler.ReadProgress += (sender, _) => Console.WriteLine("Downloading ... " + _.ToString(@"F2") + "%");
#endif
		}

		public async Task<RequestHeader> GetPackageAsync(ClientPlatform platform)
		{
			var method = UpdateMethod.GetVersion;
			var context = method.ToString();
			this.LogRequest(context);
			await this.TransferHandler.WriteAsync(this.Id, (byte)method);

			var data = BitConverter.GetBytes((int)platform);
			this.LogRequest(data, context);
			await this.TransferHandler.WriteAsync(data);

			var bytes = await this.TransferHandler.ReadDataAsync();
			this.LogResponse(bytes, context);
			if (bytes.Length != TransferHandler.NoDataBytes.Length)
			{
				return new RequestHeader().Setup(new MemoryStream(bytes));
			}

			return null;
		}

		public async Task<RequestHeader[]> GetPackagesAsync()
		{
			var method = UpdateMethod.GetVersions;
			var context = method.ToString();
			this.LogRequest(context);
			await this.TransferHandler.WriteAsync(this.Id, (byte)method);

			var data = await this.TransferHandler.ReadDataAsync();
			this.LogResponse(data, context);

			if (data.Length != TransferHandler.NoDataBytes.Length)
			{
				var headers = new List<RequestHeader>();

				using (var ms = new MemoryStream(data))
				{
					while (ms.Position != ms.Length)
					{
						headers.Add(new RequestHeader().Setup(ms));
					}
				}

				return headers.ToArray();
			}

			return null;
		}

		public async Task UploadPackageAsync(RequestPackage package)
		{
			if (package == null) throw new ArgumentNullException("package");

			var method = UpdateMethod.UploadPackage;
			var context = method.ToString();
			this.LogRequest(context);
			await this.TransferHandler.WriteAsync(this.Id, (byte)method);

			var data = Utilities.Concat(package.Header.NetworkBuffer, package.Data);
			this.LogRequest(data, context);
			await this.TransferHandler.WriteAsync(data);
		}

		public async Task<byte[]> DownloadPackageAsync(Stream stream, RequestHeader header)
		{
			if (stream == null) throw new ArgumentNullException("stream");
			if (header == null) throw new ArgumentNullException("header");

			var method = UpdateMethod.DownloadPackage;
			var context = method.ToString();
			this.LogRequest(context);
			await this.TransferHandler.WriteAsync(this.Id, (byte)method);

			var data = header.NetworkBuffer;
			this.LogRequest(data, context);
			await this.TransferHandler.WriteAsync(data);

			var bytes = await this.TransferHandler.ReadDataAsync();
			this.LogResponse(bytes, context);
			if (bytes.Length != TransferHandler.NoDataBytes.Length)
			{
				return bytes;
			}

			return null;
		}
	}
}