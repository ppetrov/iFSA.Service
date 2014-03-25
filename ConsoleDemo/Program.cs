using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using iFSA.Service;
using iFSA.Service.AutoUpdate;

namespace ConsoleDemo
{
	class Program
	{
		static void Main(string[] args)
		{
			var hostname = @"127.0.0.1";
			var port = 11111;

			Console.WriteLine(@"Start new server");
			ThreadPool.QueueUserWorkItem(async _ =>
			{
				try
				{
					var s = new Server(IPAddress.Parse(hostname), port);
					s.Register(new ServerHandler(1));
					await s.StartAsync();
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
				}
			});

			Thread.Sleep(1000);
			var platform = Platform.Metro;
			Console.WriteLine(@"Get version for " + platform);
			var h = new ClientHandler(1);
			using (var c = new TcpClient(hostname, port))
			{
				var package = h.GetVersionAsync(c, platform).Result;
				if (package != null)
				{
					Console.WriteLine(package);
				}
				else
				{
					Console.WriteLine(@"No version is available");
				}
			}

			Thread.Sleep(1000);
			using (var c = new TcpClient(hostname, port))
			{
				var package = h.GetVersionsAsync(c).Result;
				if (package != null)
				{
					Console.WriteLine(package);
				}
				else
				{
					Console.WriteLine(@"No versions is available");
				}
			}


			Thread.Sleep(1000);
			Console.WriteLine(@"Upload version for " + platform);
			using (var c = new TcpClient(hostname, port))
			{
				h.UploadVersionAsync(c, new ServerVersion(platform, new Version(2, 2, 2, 2), GetPackage())).Wait();
			}

			Thread.Sleep(1000);
			Console.WriteLine(@"Upload version for " + platform);
			using (var c = new TcpClient(hostname, port))
			{
				h.UploadVersionAsync(c, new ServerVersion(Platform.WindowsMobile, new Version(3, 3, 3, 3), GetPackage())).Wait();
			}


			Thread.Sleep(1000);
			Console.WriteLine(@"Get version for " + platform);
			using (var c = new TcpClient(hostname, port))
			{
				Console.WriteLine(h.GetVersionAsync(c, platform).Result);
			}

			Thread.Sleep(1000);
			using (var c = new TcpClient(hostname, port))
			{
				var package = h.GetVersionsAsync(c).Result;
				if (package != null)
				{
					Console.WriteLine(package);
				}
				else
				{
					Console.WriteLine(@"No versions is available");
				}
			}

			Thread.Sleep(1000);
			Console.WriteLine(@"Download version for " + platform + " latest");
			using (var c = new TcpClient(hostname, port))
			{
				var package = h.DownloadVersionAsync(c, new ClientVersion(platform, new Version(3, 0, 12, 2317), @"ppetrov", @"sc1f1r3hack03")).Result;
				if (package != null)
				{
					Console.WriteLine("Client" + package.Length);
				}
				else
				{
					Console.WriteLine("Client at latest");
				}
			}

			Thread.Sleep(1000);
			Console.WriteLine(@"Download version for " + platform + " old");
			using (var c = new TcpClient(hostname, port))
			{
				var package = h.DownloadVersionAsync(c, new ClientVersion(platform, new Version(2, 0, 12, 2317), @"ppetrov", @"sc1f1r3hack03")).Result;
				Console.WriteLine("Client" + package.Length);
			}

			var w = Stopwatch.StartNew();
			var thread = 7;
			;
			using (var ce = new CountdownEvent(thread))
			{
				for (var i = 0; i < thread; i++)
				{
					ThreadPool.QueueUserWorkItem(_ =>
												 {
													 var e = _ as CountdownEvent;
													 try
													 {
														 var th = new ClientHandler(1);
														 var v = new ClientVersion(Platform.Metro, new Version(1, 0, 12, 2317), @"ppetrov", @"sc1f1r3hack03");
														 for (int j = 0; j < 23; j++)
														 {
															 using (var c = new TcpClient(hostname, port))
															 {
																 var package = th.DownloadVersionAsync(c, v).Result;
																 //Console.WriteLine(local + " Client bye bye " + package.Length);
																 //Thread.Sleep(TimeSpan.FromSeconds(1));
															 }
														 }
													 }
													 finally
													 {
														 e.Signal();
													 }
												 }, ce);
				}
				ce.Wait();
			}
			Console.WriteLine(w.ElapsedMilliseconds);

			//WaitHandle.WaitAll(new WaitHandle[] { new ManualResetEvent(false) });
		}

		private static byte[] GetPackage()
		{
			byte[] package;

			var f = new FileInfo(@"C:\temp\pack.dat");
			var size = (int)f.Length;
			Console.WriteLine(size);
			using (var ms = new MemoryStream(size))
			{
				using (var fs = f.OpenRead())
				{
					var buffer = new byte[16 * 1024];

					int readBytes;
					while ((readBytes = fs.Read(buffer, 0, buffer.Length)) != 0)
					{
						ms.Write(buffer, 0, readBytes);
					}
				}
				package = ms.GetBuffer();
			}
			return package;
		}
	}
}
