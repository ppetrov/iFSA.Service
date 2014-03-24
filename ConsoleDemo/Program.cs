using System;
using System.Globalization;
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
		private static double GetDistance(Tuple<double, double> p1, Tuple<double, double> p2)
		{
			var latitude1 = p1.Item1;
			var longitude1 = p1.Item2;

			var latitude2 = p2.Item1;
			var longitude2 = p2.Item2;

			var dLat = (latitude2 - latitude1) / 180 * Math.PI;
			var dLong = (longitude2 - longitude1) / 180 * Math.PI;

			var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(latitude2) * Math.Sin(dLong / 2) * Math.Sin(dLong / 2);
			var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

			//Calculate radius of earth
			// For this you can assume any of the two points.
			double radiusE = 6378135; // Equatorial radius, in metres
			double radiusP = 6356750; // Polar Radius

			//Numerator part of function
			var nr = Math.Pow(radiusE * radiusP * Math.Cos(latitude1 / 180 * Math.PI), 2);
			//Denominator part of the function
			var dr = Math.Pow(radiusE * Math.Cos(latitude1 / 180 * Math.PI), 2) + Math.Pow(radiusP * Math.Sin(latitude1 / 180 * Math.PI), 2);

			//Calaculate distance in metres.
			return Math.Sqrt(nr / dr) * c;
		}

		static void Main(string[] args)
		{
			var hostname = @"127.0.0.1";
			var port = 11111;

			Console.WriteLine(@"Start new server");
			ThreadPool.QueueUserWorkItem(_ =>
			{
				try
				{
					var s = new Server(IPAddress.Parse(hostname), port);
					s.Register(new ServerHandler(1));
					s.Start();
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
				}
			});

			Thread.Sleep(1000);
			var platform = Platform.Metro;
			Console.WriteLine(@"Get version for " + platform);
			var h = new ClientHandler(1, new TransferHandler());
			using (var c = new TcpClient(hostname, port))
			{
				var package = h.GetVersion(c, platform);
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
			Console.WriteLine(@"Upload version for " + platform);
			using (var c = new TcpClient(hostname, port))
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
				h.PublishVersion(c, new ServerVersion(platform, new Version(2, 2, 2, 2), package));
			}

			Thread.Sleep(1000);
			Console.WriteLine(@"Get version for " + platform);
			using (var c = new TcpClient(hostname, port))
			{
				var package = h.GetVersion(c, platform) ?? new Version();
				Console.WriteLine(package);
			}

			Thread.Sleep(1000);
			Console.WriteLine(@"Download version for " + platform + " latest");
			using (var c = new TcpClient(hostname, port))
			{
				var package = h.DownloadVersion(c, new ClientVersion(platform, new Version(3, 0, 12, 2317), @"ppetrov", @"sc1f1r3hack03"));
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
				var package = h.DownloadVersion(c, new ClientVersion(platform, new Version(2, 0, 12, 2317), @"ppetrov", @"sc1f1r3hack03"));
				Console.WriteLine("Client" + package.Length);
			}

			//for (var i = 0; i < 1; i++)
			//{
			//	var local = i;
			//	ThreadPool.QueueUserWorkItem(_ =>
			//	{
			//		var h = new ClientHandler(1, new TransferHandler());
			//		var v = new ClientVersion(Platform.Metro, new Version(1, 0, 12, 2317), @"ppetrov", @"sc1f1r3hack03");
			//		while (true)
			//		{
			//			using (var c = new TcpClient(hostname, port))
			//			{
			//				var package = h.DownloadVersion(c, v);
			//				Console.WriteLine(local + " Client bye bye " + package.Length);
			//				Thread.Sleep(TimeSpan.FromSeconds(1));
			//			}
			//			break;
			//		}
			//	});
			//}

			WaitHandle.WaitAll(new WaitHandle[] { new ManualResetEvent(false) });
		}
	}
}
