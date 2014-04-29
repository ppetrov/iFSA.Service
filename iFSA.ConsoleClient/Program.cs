using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using iFSA.Service;
using iFSA.Service.Core;
using iFSA.Service.Logs.Client;

namespace iFSA.ConsoleClient
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.ReadLine();
			Console.WriteLine(@"Upload video");
			using (var client = new TcpClient(@"127.0.0.1", 11111))
			{
				var handler = new TransferHandler(client.GetStream(), new byte[16 * 1024]);

				//var config = new LogConfig(new RequestHeader(ClientPlatform.WinMobile, new Version(0, 0, 0, 0), string.Empty, string.Empty), Category.Logs, @"C:\Temp\");
				//new LogsClientHandler(2, handler).ConfigureAsync(config).Wait();

				var path = @"C:\Users\bg900343\Desktop\DEV-B318.wmv";
				var clientFile = new ClientFile(Path.GetFileName(path), File.ReadAllBytes(path));
				var requestHeader = new RequestHeader(ClientPlatform.WinMobile, new Version(0, 0, 0, 0), string.Empty, string.Empty);
				new LogsClientHandler(2, handler).UploadFilesAsync(requestHeader, new List<ClientFile> { clientFile }).Wait();

				handler.CloseAsync().Wait();
			}
			Console.WriteLine(@"Done");
			Console.ReadLine();
		}
	}
}
