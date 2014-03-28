using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using iFSA.Service;
using iFSA.Service.Logs;
using iFSA.Service.Update;

namespace ConsoleDemo
{
	class Program
	{
		static void Main(string[] args)
		{
			//var ph = new PackageHandler();
			//var dh = new TransferHandler();

			//var logsFolder = new DirectoryInfo(@"C:\temp\Logs");

			//using (var fs = File.OpenWrite(@"C:\temp\package.dat"))
			//{
			//	var f = new DirectoryInfo(@"C:\temp\arch");
			//	f = logsFolder;
			//	ph.PackAsync(f, fs).Wait();
			//}


			//var tmp = Stopwatch.StartNew();
			//var res = ph.PackAsync(logsFolder).Result;
			//var zip = dh.CompressAsync(res).Result;
			//tmp.Stop();
			//Console.WriteLine(tmp.ElapsedMilliseconds);
			//Console.WriteLine(res.Length);
			//Console.WriteLine(zip.Length);

			//File.WriteAllBytes(@"C:\temp\packLogs.dat", zip);


			//var unzip = dh.DecompressAsync(zip).Result;

			////using (var s = File.OpenRead(@"C:\temp\package.dat"))
			//using (var s = new MemoryStream(unzip))
			//{
			//	var f = new DirectoryInfo(@"C:\temp\Logs2");
			//	if (f.Exists)
			//	{
			//		f.Delete(true);
			//	}
			//	f.Create();
			//	ph.UnpackAsync(s, f).Wait();
			//}


			//Console.WriteLine(@"Done");


			var hostname = @"127.0.0.1";
			var port = 11111;

			Console.WriteLine(@"Start new server");
			ThreadPool.QueueUserWorkItem(async _ =>
			{
				try
				{
					var s = new Server(IPAddress.Parse(hostname), port);
					s.Register(new UpdateServerHandler(1));
					s.Register(new LogsServerHandler(2));
					await s.StartAsync();
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
				}
			});


			var platform = ClientPlatform.WinRT;
			Thread.Sleep(1000);
			Console.WriteLine(@"Upload logs for " + platform);
			var uh = new LogsClientHandler(2);
			using (var c = new TcpClient(hostname, port))
			{
				uh.UploadLogsAsync(c, new ClientVersion(platform, new Version(2, 2, 2, 2), @"PPetrov", @"secret"), new ClientLog(new DirectoryInfo(@"C:\temp\Logs"))).Wait();
			}

			Thread.Sleep(1000);
			return;

			Console.WriteLine(@"Get version for " + platform);
			var h = new UpdateClientHandler(1);
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
				h.UploadVersionAsync(c, new UpdateVersion(platform, new Version(2, 2, 2, 2), GetPackage())).Wait();
			}

			Thread.Sleep(1000);
			Console.WriteLine(@"Upload version for " + platform);
			using (var c = new TcpClient(hostname, port))
			{
				h.UploadVersionAsync(c, new UpdateVersion(ClientPlatform.WindowsMobile, new Version(3, 3, 3, 3), GetPackage())).Wait();
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
														 var th = new UpdateClientHandler(1);
														 var v = new ClientVersion(ClientPlatform.WinRT, new Version(1, 0, 12, 2317), @"ppetrov", @"sc1f1r3hack03");
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
