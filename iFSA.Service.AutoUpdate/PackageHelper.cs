using System;
using System.IO;
using System.Text;

namespace iFSA.Service.AutoUpdate
{
	public sealed class PackageHelper
	{
		private readonly byte[] _buffer = new byte[16 * 4 * 1024];
		private readonly char[] _fileSeparator = { '*' };
		private readonly char _nameSizeSeparator = '|';

		private long _readBytes;
		private long _bytes;

		public IProgress<string> FileProgress { get; set; }
		public IProgress<double> PercentProgress { get; set; }

		public void Pack(DirectoryInfo folder, Stream stream)
		{
			if (folder == null) throw new ArgumentNullException("folder");
			if (stream == null) throw new ArgumentNullException("stream");

			using (var ms = new MemoryStream())
			{
				var header = new StringBuilder();

				this.Read(folder, ms, header);
				this.Write(stream, ms, header);
			}
		}

		public void Unpack(Stream stream, DirectoryInfo folder)
		{
			if (stream == null) throw new ArgumentNullException("stream");
			if (folder == null) throw new ArgumentNullException("folder");

			var headerSize = new byte[4];
			stream.Read(headerSize, 0, headerSize.Length);

			var headerBuffer = new byte[BitConverter.ToInt32(headerSize, 0)];
			stream.Read(headerBuffer, 0, headerBuffer.Length);

			foreach (var line in Encoding.Unicode.GetString(headerBuffer, 0, headerBuffer.Length).Split(_fileSeparator))
			{
				var index = line.IndexOf(_nameSizeSeparator);
				var name = line.Substring(0, index);
				var size = int.Parse(line.Substring(index + 1));

				this.ReportStage(name);

				var filePath = Path.Combine(folder.FullName, name);
				var folderPath = Path.GetDirectoryName(filePath);
				if (!Directory.Exists(folderPath))
				{
					Directory.CreateDirectory(folderPath);
				}
				using (var s = File.OpenWrite(filePath))
				{
					CopyStreams(stream, s, size);
				}
			}
		}

		private void Read(DirectoryInfo folder, MemoryStream memoryStream, StringBuilder header)
		{
			var offset = folder.FullName.Length + 1;
			foreach (var file in Directory.EnumerateFiles(folder.FullName, @"*", SearchOption.AllDirectories))
			{
				var size = this.Read(file, memoryStream);
				if (header.Length > 0)
				{
					header.Append(_fileSeparator);
				}
				header.Append(file.Substring(offset));
				header.Append(_nameSizeSeparator);
				header.Append(size);
			}
		}

		private long Read(string fileName, MemoryStream memoryStream)
		{
			FileProgress.Report(fileName);

			var size = new FileInfo(fileName).Length;

			using (var s = File.OpenRead(fileName))
			{
				this.InitPercentProgress(size);

				int readBytes;
				while ((readBytes = s.Read(_buffer, 0, _buffer.Length)) != 0)
				{
					memoryStream.Write(_buffer, 0, readBytes);
					this.ReportPercentProgress(readBytes);
				}
			}

			return size;
		}

		private void Write(Stream stream, MemoryStream ms, StringBuilder header)
		{
			var headerDataBytes = Encoding.Unicode.GetBytes(header.ToString());
			var headerSizeBytes = BitConverter.GetBytes(headerDataBytes.Length);

			stream.Write(headerSizeBytes, 0, headerSizeBytes.Length);
			stream.Write(headerDataBytes, 0, headerDataBytes.Length);
			this.CopyStreams(ms, stream, ms.Length);
		}

		private void CopyStreams(Stream input, Stream output, long size)
		{
			this.InitPercentProgress(size);

			var bufferSize = _buffer.Length;
			for (var i = 0; i < size / bufferSize; i++)
			{
				CopyData(input, output, bufferSize);
				this.ReportPercentProgress(bufferSize);
			}

			bufferSize = (int)(size % bufferSize);
			CopyData(input, output, bufferSize);
			this.ReportPercentProgress(bufferSize);
		}

		private void CopyData(Stream input, Stream output, int bufferSize)
		{
			input.Read(_buffer, 0, bufferSize);
			output.Write(_buffer, 0, bufferSize);
		}

		private void InitPercentProgress(long bytes)
		{
			_bytes = bytes;
			_readBytes = 0;

			this.ReportPercentProgress(0);
		}

		private void ReportPercentProgress(int readBytes)
		{
			_readBytes += readBytes;

			if (this.PercentProgress != null)
			{
				this.PercentProgress.Report(Convert.ToDouble(_readBytes / (_bytes / 100.0M)));
			}
		}

		private void ReportStage(string stage)
		{
			if (this.FileProgress != null)
			{
				this.FileProgress.Report(stage);
			}
		}
	}
}