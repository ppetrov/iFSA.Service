using System;
using System.Net;
using System.Threading;
using iFSA.Service.Core.Server;
using iFSA.Service.Logs.Server;
using iFSA.Service.Update.Server;

namespace iFSA.ConsoleServer
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine(@"Create server");
			var server = new Server(IPAddress.Parse(@"127.0.0.1"), 11111);

			Console.WriteLine(@"Start server");
			ThreadPool.QueueUserWorkItem(async _ =>
											   {
												   var s = _ as Server;
												   try
												   {
													   await server.RegisterAsync(new UpdateServerHandler(1));
													   await server.RegisterAsync(new LogsServerHandler(2));
													   await server.StartAsync();
												   }
												   catch (Exception ex)
												   {
													   Console.WriteLine(ex);
												   }
												   finally
												   {
													   s.Stop();
												   }
											   });

			Console.ReadLine();
			Console.WriteLine(@"Stop server");

			try
			{
				server.Stop();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}

			Console.WriteLine(@"Complete");
		}
	}
}
