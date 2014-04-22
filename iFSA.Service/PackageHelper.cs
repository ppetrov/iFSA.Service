using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace iFSA.Service
{
	public sealed class PackageHelper
	{
		private static readonly char[] FileSeparator = { '*' };
		private static readonly char SizeSeparator = '|';

		private readonly byte[] _buffer;

		public PackageHelper(byte[] buffer)
		{
			if (buffer == null) throw new ArgumentNullException("buffer");
			if (buffer.Length == 0) throw new ArgumentOutOfRangeException("buffer");

			_buffer = buffer;
		}

		public event EventHandler<string> FileProgress;
		private void OnFileProgress(string e)
		{
			var handler = FileProgress;
			if (handler != null) handler(this, e);
		}

		public event EventHandler<decimal> PercentProgress;
		private void OnPercentProgress(decimal e)
		{
			var handler = PercentProgress;
			if (handler != null) handler(this, e);
		}

		public async Task<byte[]> PackAsync(ClientFile[] files)
		{
			if (files == null) throw new ArgumentNullException("files");
			if (files.Length == 0) throw new ArgumentOutOfRangeException("files");

			using (var output = new MemoryStream())
			{
				var header = new StringBuilder();

				foreach (var f in files)
				{
					var file = f.File;
					var name = Path.GetFileName(file.FullName);
					this.OnFileProgress(name);

					var size = Convert.ToInt32(file.Length);
					output.Capacity += size;

					var fileBytes = await this.ReadFileAsync(file, size);
					await output.WriteAsync(fileBytes, 0, fileBytes.Length);

					if (header.Length > 0)
					{
						header.Append(FileSeparator);
					}
					header.Append(name);
					header.Append(SizeSeparator);
					header.Append(size);
				}

				var headerData = Encoding.Unicode.GetBytes(header.ToString());
				var headerSize = BitConverter.GetBytes(headerData.Length);

				return Utilities.Concat(headerSize, headerData, output.GetBuffer());
			}
		}

		public async Task UnpackAsync(Stream input, DirectoryInfo folder, bool append)
		{
			if (input == null) throw new ArgumentNullException("input");
			if (folder == null) throw new ArgumentNullException("folder");

			var mode = FileMode.Create;
			if (append)
			{
				mode = FileMode.Append;
			}
			foreach (var fileHeader in NetworkHelper.ReadString(input, _buffer).Split(FileSeparator))
			{
				var name = fileHeader.Substring(0, fileHeader.IndexOf(SizeSeparator));
				var size = int.Parse(fileHeader.Substring(name.Length + 1));

				this.OnFileProgress(name);

				var filePath = Path.Combine(folder.FullName, name);
				var folderPath = Path.GetDirectoryName(filePath);
				if (!Directory.Exists(folderPath))
				{
					Directory.CreateDirectory(folderPath);
				}

				using (var output = new FileStream(filePath, mode))
				{
					await CopyAsync(input, output, size);
				}
			}
		}

		private async Task<byte[]> ReadFileAsync(FileInfo file, int size)
		{
			var buffer = new byte[size];

			using (var output = new MemoryStream(buffer))
			{
				using (var input = file.OpenRead())
				{
					await CopyAsync(input, output, size);
				}
			}

			return buffer;
		}

		private async Task CopyAsync(Stream input, Stream output, int size)
		{
			this.OnPercentProgress(0);

			int readBytes;
			var totalReadBytes = 0;
			while ((readBytes = await input.ReadAsync(_buffer, 0, _buffer.Length)) != 0)
			{
				await output.WriteAsync(_buffer, 0, readBytes);

				totalReadBytes += readBytes;
				this.OnPercentProgress(Utilities.GetProgressPercent(size, totalReadBytes));
			}
		}
	}
}