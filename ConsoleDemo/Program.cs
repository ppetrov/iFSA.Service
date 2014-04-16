using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using iFSA.Service;
using iFSA.Service.Logs;
using iFSA.Service.Update;

namespace ConsoleDemo
{
	class Program
	{
		private static void Main(string[] args)
		{
			var hostname = @"127.0.0.1";
			var port = 11111;

			Console.WriteLine(@"Start new server");
			ThreadPool.QueueUserWorkItem(async _ =>
			{
				try
				{
					var s = new Server(IPAddress.Parse(hostname), port);
					await s.Register(new UpdateServerHandler(1));
					await s.Register(new LogsServerHandler(2));
					await s.StartAsync();
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
				}
			});

			Thread.Sleep(100);
			UploadPackage(hostname, port);

			

			//Thread.Sleep(100);
			//DisplayLogConfigs(hostname, port);

			//Thread.Sleep(100);
			//ConfigureLogFolders(hostname, port);

			//Thread.Sleep(100);
			//DisplayLogConfigs(hostname, port);

			//Thread.Sleep(100);
			//UploadData(hostname, port);

			//Console.WriteLine(@"Get version for " + platform);
			//var h = new UpdateClientHandler(1);
			//using (var c = new TcpClient(hostname, port))
			//{
			//	var package = h.GetPackageAsync(c, platform).Result;
			//	if (package != null)
			//	{
			//		Console.WriteLine(package);
			//	}
			//	else
			//	{
			//		Console.WriteLine(@"No version is available");
			//	}
			//}

			//Thread.Sleep(1000);
			//using (var c = new TcpClient(hostname, port))
			//{
			//	var package = h.GetPackagesAsync(c).Result;
			//	if (package != null)
			//	{
			//		Console.WriteLine(package);
			//	}
			//	else
			//	{
			//		Console.WriteLine(@"No versions available");
			//	}
			//}


			//Thread.Sleep(1000);
			//Console.WriteLine(@"Upload version for " + platform);
			//using (var c = new TcpClient(hostname, port))
			//{
			//	h.UploadPackageAsync(c, new RequestPackage(new Header(platform, new Version(2, 2, 2, 2), "", ""), GetPackage())).Wait();
			//}

			//Thread.Sleep(1000);
			//Console.WriteLine(@"Upload version for " + platform);
			//using (var c = new TcpClient(hostname, port))
			//{
			//	h.UploadPackageAsync(c, new RequestPackage(new Header(ClientPlatform.WinMobile, new Version(3, 3, 3, 3), "", ""), GetPackage())).Wait();
			//}


			//Thread.Sleep(1000);
			//Console.WriteLine(@"Get version for " + platform);
			//using (var c = new TcpClient(hostname, port))
			//{
			//	Console.WriteLine(h.GetPackageAsync(c, platform).Result);
			//}

			//Thread.Sleep(1000);
			//using (var c = new TcpClient(hostname, port))
			//{
			//	var versions = h.GetPackagesAsync(c).Result;
			//	if (versions != null)
			//	{
			//		foreach (var v in versions)
			//		{
			//			Console.WriteLine(v);
			//		}
			//		Console.WriteLine();
			//	}
			//	else
			//	{
			//		Console.WriteLine(@"No versions is available");
			//	}
			//}

			//Thread.Sleep(1000);
			//Console.WriteLine(@"Download version for " + platform + " latest");
			//using (var c = new TcpClient(hostname, port))
			//{
			//	var package = h.DownloadPackageAsync(c, new Header(platform, new Version(3, 0, 12, 2317), @"ppetrov", @"sc1f1r3hack03")).Result;
			//	if (package != null)
			//	{
			//		Console.WriteLine("Client" + package.Length);
			//	}
			//	else
			//	{
			//		Console.WriteLine("Client at latest");
			//	}
			//}

			//Thread.Sleep(1000);
			//Console.WriteLine(@"Download version for " + platform + " old");
			//using (var c = new TcpClient(hostname, port))
			//{
			//	var package = h.DownloadPackageAsync(c, new Header(platform, new Version(2, 0, 12, 2317), @"ppetrov", @"sc1f1r3hack03")).Result;
			//	Console.WriteLine("Client" + package.Length);
			//}

			//var w = Stopwatch.StartNew();
			//var thread = 7;
			//;
			//using (var ce = new CountdownEvent(thread))
			//{
			//	for (var i = 0; i < thread; i++)
			//	{
			//		ThreadPool.QueueUserWorkItem(_ =>
			//									 {
			//										 var e = _ as CountdownEvent;
			//										 try
			//										 {
			//											 var th = new UpdateClientHandler(1);
			//											 var v = new Header(ClientPlatform.WinRT, new Version(1, 0, 12, 2317), @"ppetrov", @"sc1f1r3hack03");
			//											 for (int j = 0; j < 23; j++)
			//											 {
			//												 using (var c = new TcpClient(hostname, port))
			//												 {
			//													 var package = th.DownloadPackageAsync(c, v).Result;
			//													 //Console.WriteLine(local + " Client bye bye " + package.Length);
			//													 //Thread.Sleep(TimeSpan.FromSeconds(1));
			//												 }
			//											 }
			//										 }
			//										 finally
			//										 {
			//											 e.Signal();
			//										 }
			//									 }, ce);
			//	}
			//	ce.Wait();
			//}
			//Console.WriteLine(w.ElapsedMilliseconds);

			//WaitHandle.WaitAll(new WaitHandle[] { new ManualResetEvent(false) });
		}

		private static void UploadPackage(string hostname, int port)
		{
			Console.WriteLine(@"Upload package");

			using (var handler = new UpdateClientHandler(1, hostname, port))
			{
				//handler.TransferHandler.WriteProgress += (sender, _) => Console.WriteLine("Uploading ... " + _.ToString(@"F2") + "%");
				//handler.TransferHandler.ReadProgress += (sender, _) => Console.WriteLine("Downloading ... " + _.ToString(@"F2") + "%");

				//handler.PackageHelper.FileProgress += (sender, _) => Console.WriteLine(@"Packing file ..." + _);
				//handler.PackageHelper.PercentProgress += (sender, _) => Console.WriteLine(@"Pack progress " + _.ToString(@"F2") + "%");

				//foreach (var platform in GetConfigs().Select(c => c.Item1).Distinct())
				//{
				//	handler.UploadDatabaseAsync(new RequestHeader(platform, RequestHeader.EmptyVersion, @"PPetrov", @"secret"),
				//		new ClientFile(new FileInfo(@"C:\Users\bg900343\Desktop\ifsa.sqlite"))).Wait();
				//	handler.UploadFilesAsync(new RequestHeader(platform, RequestHeader.EmptyVersion, @"PPetrov", @"secret"),
				//		new[] { new ClientFile(new FileInfo(@"C:\temp\Schedule.png")) }).Wait();
				//	handler.UploadLogsAsync(new RequestHeader(platform, RequestHeader.EmptyVersion, @"PPetrov", @"secret"),
				//		new DirectoryInfo(@"C:\temp\Logs").GetFiles(@"*.txt").Select(f => new ClientFile(f)).ToArray()).Wait();
				//}

				handler.UploadPackageAsync(
					new RequestPackage(new RequestHeader(ClientPlatform.WinRT, new Version(2, 2, 2, 2), string.Empty, string.Empty),
						new byte[] { 1, 2, 3, 4, 5 })).Wait();
			}
		}


		private static void UploadData(string hostname, int port)
		{
			Console.WriteLine(@"Upload data");
			using (var handler = new LogsClientHandler(2, hostname, port))
			{
				//handler.TransferHandler.WriteProgress += (sender, _) => Console.WriteLine("Uploading ... " + _.ToString(@"F2") + "%");
				//handler.TransferHandler.ReadProgress += (sender, _) => Console.WriteLine("Downloading ... " + _.ToString(@"F2") + "%");

				//handler.PackageHelper.FileProgress += (sender, _) => Console.WriteLine(@"Packing file ..." + _);
				//handler.PackageHelper.PercentProgress += (sender, _) => Console.WriteLine(@"Pack progress " + _.ToString(@"F2") + "%");

				foreach (var platform in GetConfigs().Select(c => c.Item1).Distinct())
				{
					handler.UploadDatabaseAsync(new RequestHeader(platform, RequestHeader.EmptyVersion, @"PPetrov", @"secret"),
						new ClientFile(new FileInfo(@"C:\Users\bg900343\Desktop\ifsa.sqlite"))).Wait();
					handler.UploadFilesAsync(new RequestHeader(platform, RequestHeader.EmptyVersion, @"PPetrov", @"secret"),
						new[] { new ClientFile(new FileInfo(@"C:\temp\Schedule.png")) }).Wait();
					handler.UploadLogsAsync(new RequestHeader(platform, RequestHeader.EmptyVersion, @"PPetrov", @"secret"),
						new DirectoryInfo(@"C:\temp\Logs").GetFiles(@"*.txt").Select(f => new ClientFile(f)).ToArray()).Wait();
				}
			}
		}

		private static void DisplayLogConfigs(string hostname, int port)
		{
			Console.WriteLine(@"Get log configs");
			using (var handler = new LogsClientHandler(2, hostname, port))
			{
				//handler.TransferHandler.WriteProgress += (sender, _) => Console.WriteLine("Uploading ... " + _.ToString(@"F2") + "%");
				//handler.TransferHandler.ReadProgress += (sender, _) => Console.WriteLine("Downloading ... " + _.ToString(@"F2") + "%");

				var configs = handler.GetConfigsAsync().Result;
				foreach (var cfg in configs)
				{
					Console.WriteLine(
						cfg.RequestHeader.ClientPlatform.ToString().PadRight(15) + " " +
						cfg.LogMethod.ToString().PadRight(15) + " " +
						cfg.Folder);
				}
				configs = handler.GetConfigsAsync().Result;
				Console.WriteLine(configs.Length);
			}
		}

		private static void ConfigureLogFolders(string hostname, int port)
		{
			Console.WriteLine(@"Configure log folders");

			using (var handler = new LogsClientHandler(2, hostname, port))
			{
				//handler.TransferHandler.WriteProgress += (sender, _) => Console.WriteLine("Uploading ... " + _.ToString(@"F2") + "%");
				//handler.TransferHandler.ReadProgress += (sender, _) => Console.WriteLine("Downloading ... " + _.ToString(@"F2") + "%");

				foreach (var cfg in GetConfigs())
				{
					var platform = cfg.Item1;
					var method = cfg.Item2;
					var folder = Path.Combine(Path.Combine(@"C:\Temp\Server", platform.ToString()), method.ToString().Replace(@"Configure", string.Empty));
					handler.ConfigureAsync(new LogConfig(new RequestHeader(platform, RequestHeader.EmptyVersion, "", ""), method, folder)).Wait();
				}
			}
		}

		public static IEnumerable<Tuple<ClientPlatform, LogMethod>> GetConfigs()
		{
			foreach (var platform in new[]
				                         {
					                         ClientPlatform.WinMobile,
					                         ClientPlatform.WinRT,
					                         ClientPlatform.IPad,
				                         })
			{
				foreach (var method in new[]
					                       {
						                       LogMethod.ConfigureLogs,
						                       LogMethod.ConfigureFiles,
						                       LogMethod.ConfigureDatabase,
					                       })
				{
					yield return Tuple.Create(platform, method);
				}
			}
		}
	}
}
