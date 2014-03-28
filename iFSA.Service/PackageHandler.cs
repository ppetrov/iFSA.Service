using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace iFSA.Service
{
	public sealed class PackageHandler
	{
		private static readonly char[] FileSeparator = { '*' };
		private static readonly char SizeSeparator = '|';

		private readonly byte[] _headerSize = new byte[4];
		private readonly byte[] _buffer = new byte[16 * 4 * 1024];
		private readonly Encoding _encoding = Encoding.Unicode;

		private int _readBytes;
		private decimal _totalBytes;

		public event EventHandler<string> FileProgress;
		private void OnFileProgress(string e)
		{
			var handler = FileProgress;
			if (handler != null) handler(this, e);
		}

		public event EventHandler<double> PercentProgress;
		private void OnPercentProgress(double e)
		{
			var handler = PercentProgress;
			if (handler != null) handler(this, e);
		}

		public async Task<byte[]> PackAsync(byte[] clientBuffer, DirectoryInfo folder, string searchPattern)
		{
			if (clientBuffer == null) throw new ArgumentNullException("clientBuffer");
			if (folder == null) throw new ArgumentNullException("folder");
			if (searchPattern == null) throw new ArgumentNullException("searchPattern");

			var package = await ReadFolderAsync(folder, searchPattern);
			using (var output = new MemoryStream(clientBuffer.Length + package.Item1.Length + package.Item2.Length + package.Item3.Length))
			{
				await output.WriteAsync(clientBuffer, 0, clientBuffer.Length);

				await PackAsync(output, package.Item1, package.Item2, package.Item3);
				return output.GetBuffer();
			}
		}

		public async Task PackAsync(DirectoryInfo folder, Stream output)
		{
			if (folder == null) throw new ArgumentNullException("folder");
			if (output == null) throw new ArgumentNullException("output");

			var package = await ReadFolderAsync(folder, @"*");
			await PackAsync(output, package.Item1, package.Item2, package.Item3);
		}

		public async Task UnpackAsync(Stream input, DirectoryInfo folder)
		{
			if (input == null) throw new ArgumentNullException("input");
			if (folder == null) throw new ArgumentNullException("folder");

			await input.ReadAsync(_headerSize, 0, _headerSize.Length);

			var headerBuffer = new byte[BitConverter.ToInt32(_headerSize, 0)];
			await input.ReadAsync(headerBuffer, 0, headerBuffer.Length);

			foreach (var fileHeader in _encoding.GetString(headerBuffer, 0, headerBuffer.Length).Split(FileSeparator))
			{
				var fileName = fileHeader.Substring(0, fileHeader.IndexOf(SizeSeparator));
				var size = int.Parse(fileHeader.Substring(fileName.Length + 1));

				this.OnFileProgress(fileName);

				var filePath = Path.Combine(folder.FullName, fileName);
				var folderPath = Path.GetDirectoryName(filePath);
				if (!Directory.Exists(folderPath))
				{
					Directory.CreateDirectory(folderPath);
				}
				using (var output = File.OpenWrite(filePath))
				{
					await CopyAsync(input, output, size);
				}
			}
		}

		private async Task<Tuple<byte[], byte[], byte[]>> ReadFolderAsync(FileSystemInfo folder, string searchPattern)
		{
			using (var output = new MemoryStream())
			{
				var header = new StringBuilder();
				var offset = folder.FullName.Length + 1;

				foreach (var fileName in Directory.EnumerateFiles(folder.FullName, searchPattern, SearchOption.AllDirectories))
				{
					this.OnFileProgress(fileName);

					var file = new FileInfo(fileName);
					var size = Convert.ToInt32(file.Length);
					output.Capacity += size;

					var data = await this.ReadFileAsync(file, size);
					await output.WriteAsync(data, 0, data.Length);

					if (header.Length > 0)
					{
						header.Append(FileSeparator);
					}
					header.Append(fileName.Substring(offset));
					header.Append(SizeSeparator);
					header.Append(size);
				}

				var headerData = _encoding.GetBytes(header.ToString());
				var headerSize = BitConverter.GetBytes(headerData.Length);

				return Tuple.Create(headerSize, headerData, output.GetBuffer());
			}
		}

		private async Task PackAsync(Stream output, byte[] headerSize, byte[] headerData, byte[] data)
		{
			await output.WriteAsync(headerSize, 0, headerSize.Length);
			await output.WriteAsync(headerData, 0, headerData.Length);

			using (var input = new MemoryStream(data))
			{
				await this.CopyAsync(input, output, data.Length);
			}
		}

		private async Task<byte[]> ReadFileAsync(FileInfo file, int size)
		{
			var buffer = new byte[size];

			using (var output = new MemoryStream(buffer))
			{
				using (var input = file.OpenRead())
				{
					this.InitPercentProgress(buffer.Length);

					int readBytes;
					while ((readBytes = await input.ReadAsync(_buffer, 0, _buffer.Length)) != 0)
					{
						await output.WriteAsync(_buffer, 0, readBytes);
						this.ReportPercentProgress(readBytes);
					}
				}
			}

			return buffer;
		}

		private async Task CopyAsync(Stream input, Stream output, int size)
		{
			this.InitPercentProgress(size);

			var bufferSize = _buffer.Length;
			for (var i = 0; i < size / bufferSize; i++)
			{
				await CopyDataAsync(input, output, bufferSize);
			}
			bufferSize = (size % bufferSize);
			if (bufferSize != 0)
			{
				await CopyDataAsync(input, output, bufferSize);
			}
		}

		private async Task CopyDataAsync(Stream input, Stream output, int bufferSize)
		{
			await input.ReadAsync(_buffer, 0, bufferSize);
			await output.WriteAsync(_buffer, 0, bufferSize);

			this.ReportPercentProgress(bufferSize);
		}

		private void InitPercentProgress(int bytes)
		{
			_readBytes = 0;
			_totalBytes = bytes / 100.0M;

			this.ReportPercentProgress(0);
		}

		private void ReportPercentProgress(int readBytes)
		{
			_readBytes += readBytes;
			this.OnPercentProgress(Convert.ToDouble(_readBytes / _totalBytes));
		}
	}
}