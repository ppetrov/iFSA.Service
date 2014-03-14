using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using iFSA.Server.AutoUpdate;
using iFSA.Server.Core;

namespace ConsoleDemo
{
	class Program
	{
		static void Main(string[] args)
		{
			var hostname = @"127.0.0.1";
			var port = 11111;

			ThreadPool.QueueUserWorkItem(_ =>
			{
				try
				{
					var s = new Server(IPAddress.Parse(hostname), port);
					var handler = new AutoUpdateServerRequestHandler(1);
					handler.Publish(Platform.Metro, new ServerVersion(new Version(2, 2, 2, 2), new FileInfo(@"C:\temp\pack.dat")));
					s.Add(handler);
					s.Start();
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
				}
			});

			Thread.Sleep(50);

			for (var i = 0; i < 4; i++)
			{
				var local = i;
				ThreadPool.QueueUserWorkItem(_ =>
				{
					var h = new AutoUpdateClientRequestHandler(1, new TransferHandler());
					var v = new ClientVersion(Platform.Metro, new Version(1, 0, 12, 2317), @"ppetrov", @"sc1f1r3hack03");
					while (true)
					{
						using (var c = new TcpClient(hostname, port))
						{
							var package = h.DownloadServerVersion(c, v);
							//Console.WriteLine(local + " Client bye bye " + package.Length);
							Thread.Sleep(TimeSpan.FromSeconds(1));
						}
					}
				});
			}

			WaitHandle.WaitAll(new WaitHandle[] { new ManualResetEvent(false) });
		}
	}
}
