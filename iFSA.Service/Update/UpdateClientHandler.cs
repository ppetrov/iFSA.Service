using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace iFSA.Service.Update
{
	public sealed class UpdateClientHandler : ClientHandlerBase
	{
		private readonly CompressionHelper _compressionHelper;

		public CompressionHelper CompressionHelper
		{
			get { return _compressionHelper; }
		}

		public UpdateClientHandler(byte id, string hostname, int port)
			: base(id, hostname, port)
		{
#if DEBUG
			this.TransferHandler.WriteProgress += (sender, _) => Console.WriteLine("Uploading ... " + _.ToString(@"F2") + "%");
			this.TransferHandler.ReadProgress += (sender, _) => Console.WriteLine("Downloading ... " + _.ToString(@"F2") + "%");
#endif
			_compressionHelper = new CompressionHelper(new byte[80 * 1024]);
		}

		public async Task<RequestHeader> GetPackageAsync(Stream stream, ClientPlatform platform)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			await this.TransferHandler.WriteAsync(stream, this.Id, (byte)UpdateMethod.GetVersion);
			await this.TransferHandler.WriteAsync(stream, BitConverter.GetBytes((int)platform));
			var data = await this.TransferHandler.ReadDataAsync(stream);

			if (data.Length != TransferHandler.NoDataBytes.Length)
			{
				return new RequestHeader().Setup(new MemoryStream(data));
			}

			return null;
		}

		public async Task<RequestHeader[]> GetPackagesAsync(Stream stream)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			await this.TransferHandler.WriteAsync(stream, this.Id, (byte)UpdateMethod.GetVersions);
			var data = await this.TransferHandler.ReadDataAsync(stream);

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

		public async Task UploadPackageAsync(Stream stream, RequestPackage package)
		{
			if (stream == null) throw new ArgumentNullException("stream");
			if (package == null) throw new ArgumentNullException("package");

			var data = Utilities.Concat(package.Header.NetworkBuffer, package.Data);
			var input = data;
			if (this.TransferHandler.EnableCompression)
			{
				input = await Task.Run(() => _compressionHelper.Compress(data)).ConfigureAwait(false);
			}
			await this.TransferHandler.WriteAsync(stream, this.Id, (byte)UpdateMethod.UploadPackage);
			await this.TransferHandler.WriteAsync(stream, input);
		}

		public async Task<byte[]> DownloadPackageAsync(Stream stream, RequestHeader header)
		{
			if (stream == null) throw new ArgumentNullException("stream");
			if (header == null) throw new ArgumentNullException("header");

			await this.TransferHandler.WriteAsync(stream, this.Id, (byte)UpdateMethod.DownloadPackage);
			await this.TransferHandler.WriteAsync(stream, header.NetworkBuffer);
			var data = await this.TransferHandler.ReadDataAsync(stream);

			if (data.Length != TransferHandler.NoDataBytes.Length)
			{
				return data;
			}

			return null;
		}
	}
}