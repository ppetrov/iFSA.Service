using System;
using System.IO;
using System.Threading.Tasks;
using iFSA.Service.Core;

namespace iFSA.Service.Logs.Server
{
	public class ServerPackageHelper
	{
		private readonly byte[] _buffer;

		public ServerPackageHelper(byte[] buffer)
		{
			if (buffer == null) throw new ArgumentNullException("buffer");
			if (buffer.Length == 0) throw new ArgumentOutOfRangeException("buffer");

			_buffer = buffer;
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
			foreach (var fileHeader in NetworkHelper.ReadString(input, _buffer).Split(PackageHelper.FileSeparator))
			{
				var name = fileHeader.Substring(0, fileHeader.IndexOf(PackageHelper.SizeSeparator));

				var filePath = Path.Combine(folder.FullName, name);
				var folderPath = Path.GetDirectoryName(filePath);
				if (!Directory.Exists(folderPath))
				{
					Directory.CreateDirectory(folderPath);
				}

				using (var output = new FileStream(filePath, mode))
				{
					int readBytes;
					while ((readBytes = await input.ReadAsync(_buffer, 0, _buffer.Length)) != 0)
					{
						await output.WriteAsync(_buffer, 0, readBytes);
					}
				}
			}
		}
	}
}