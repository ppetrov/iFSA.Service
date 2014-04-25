using System;
using System.Net.Sockets;
using iFSA.Service.Core;
using iFSA.Service.Logs.Client;

namespace iFSA.ConsoleClient
{
	class Program
	{
		static void Main(string[] args)
		{
			var v = 0;
			var limit = 10;

			using (var client = new TcpClient(@"127.0.0.1", 11111))
			{
				//var package = new RequestPackage(new RequestHeader(ClientPlatform.WinRT, new Version(2, 2, 2, 2), string.Empty, string.Empty), new byte[] { 1, 2, 3, 4, 5 });

				var handler = new TransferHandler(client.GetStream(), new byte[16 * 1024]);
				//var updateHandler = new UpdateClientHandler(1, handler);
				var logsHandler = new LogsClientHandler(2, handler);

				for (var i = 0; i < limit; i++)
				{
					//updateHandler.UploadPackageAsync(package).Wait();
					var tmp = logsHandler.GetConfigsAsync().Result;
					v += tmp.Length;

					logsHandler.ConfigureAsync(new LogConfig(new RequestHeader(ClientPlatform.WinRT, new Version(1, 1, i, 1),
						string.Empty, string.Empty), LogMethod.ConfigureDatabase, @"C:\temp\db")).Wait();
				}

				handler.CloseAsync().Wait();
			}
			Console.WriteLine(@"Done");
			Console.WriteLine(v);
			Console.ReadLine();
		}
	}
}
