using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iFSA.Service.Core
{
	public sealed class PackageHelper
	{
		public static readonly char[] FileSeparator = { '*' };
		public static readonly char SizeSeparator = '|';

		public async Task<byte[]> PackAsync(ICollection<ClientFile> files)
		{
			if (files == null) throw new ArgumentNullException("files");
			if (files.Count == 0) throw new ArgumentOutOfRangeException("files");

			var header = new StringBuilder();

			var totalBuffer = new byte[files.Select(f => f.Data.Length).Sum()];

			using (var output = new MemoryStream(totalBuffer))
			{
				foreach (var f in files)
				{
					var data = f.Data;
					await output.WriteAsync(data, 0, data.Length);

					if (header.Length > 0)
					{
						header.Append(FileSeparator);
					}
					header.Append(f.Name);
					header.Append(SizeSeparator);
					header.Append(data.Length);
				}

				var headerData = Encoding.Unicode.GetBytes(header.ToString());
				var headerSize = BitConverter.GetBytes(headerData.Length);

				return Utilities.Concat(headerSize, headerData, totalBuffer);
			}
		}
	}
}