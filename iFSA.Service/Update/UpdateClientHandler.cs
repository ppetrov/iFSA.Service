using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace iFSA.Service.Update
{
	public sealed class UpdateClientHandler : ClientHandlerBase
	{
		private readonly CompressionHelper _compressionHelper;

		public override string Name
		{
			get { return @"Update"; }
		}

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

			var method = UpdateMethod.GetVersion;
			var context = method.ToString();
			this.LogRequest(context);
			await this.TransferHandler.WriteAsync(this.Stream, this.Id, (byte)method);

			var data = BitConverter.GetBytes((int)platform);
			this.LogRequest(data, context);
			await this.TransferHandler.WriteAsync(stream, data);

			var bytes = await this.TransferHandler.ReadDataAsync(stream);
			this.LogResponse(bytes, context);
			if (bytes.Length != TransferHandler.NoDataBytes.Length)
			{
				return new RequestHeader().Setup(new MemoryStream(bytes));
			}

			return null;
		}

		public async Task<RequestHeader[]> GetPackagesAsync(Stream stream)
		{
			if (stream == null) throw new ArgumentNullException("stream");

			var method = UpdateMethod.GetVersions;
			var context = method.ToString();
			this.LogRequest(context);
			await this.TransferHandler.WriteAsync(this.Stream, this.Id, (byte)method);

			var data = await this.TransferHandler.ReadDataAsync(stream);
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

		public async Task UploadPackageAsync(Stream stream, RequestPackage package)
		{
			if (stream == null) throw new ArgumentNullException("stream");
			if (package == null) throw new ArgumentNullException("package");

			var data = Utilities.Concat(package.Header.NetworkBuffer, package.Data);
			if (this.TransferHandler.EnableCompression)
			{
#if ASYNC
				data = await _compressionHelper.CompressAsync(data);
#else
				input = await Task.Run(() => _compressionHelper.Compress(data)).ConfigureAwait(false);
#endif
			}

			var method = UpdateMethod.UploadPackage;
			var context = method.ToString();
			this.LogRequest(context);
			await this.TransferHandler.WriteAsync(this.Stream, this.Id, (byte)method);

			this.LogRequest(data, context);
			await this.TransferHandler.WriteAsync(stream, data);
		}

		public async Task<byte[]> DownloadPackageAsync(Stream stream, RequestHeader header)
		{
			if (stream == null) throw new ArgumentNullException("stream");
			if (header == null) throw new ArgumentNullException("header");

			var method = UpdateMethod.DownloadPackage;
			var context = method.ToString();
			this.LogRequest(context);
			await this.TransferHandler.WriteAsync(this.Stream, this.Id, (byte)method);

			var data = header.NetworkBuffer;
			this.LogRequest(data, context);
			await this.TransferHandler.WriteAsync(stream, data);

			var bytes = await this.TransferHandler.ReadDataAsync(stream);
			this.LogResponse(bytes, context);
			if (bytes.Length != TransferHandler.NoDataBytes.Length)
			{
				return bytes;
			}

			return null;
		}
	}
}