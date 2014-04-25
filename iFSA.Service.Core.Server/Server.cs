using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace iFSA.Service.Core.Server
{
	public sealed class Server
	{
		private volatile bool _isRunning;
		private readonly TcpListener _listener;
		private readonly Dictionary<byte, ServerHandlerBase> _handlers = new Dictionary<byte, ServerHandlerBase>();

		public Server(IPAddress address, int port)
		{
			if (address == null) throw new ArgumentNullException("address");

			_listener = new TcpListener(address, port);
		}

		public async Task RegisterAsync(ServerHandlerBase handler)
		{
			if (handler == null) throw new ArgumentNullException("handler");

			await handler.InitializeAsync();

			_handlers.Add(handler.Id, handler);
		}

		public async Task StartAsync()
		{
			Trace.WriteLine(string.Format(@"Start server:{0}", _listener.LocalEndpoint));
			try
			{
				_isRunning = true;
				_listener.Start();

				while (_isRunning)
				{
					this.ProcessAsync(await _listener.AcceptTcpClientAsync());
				}
			}
			catch (SocketException) { }
			catch (Exception ex)
			{
				ex.Log();
			}
			finally
			{
				this.Stop();
			}
		}

		public void Stop()
		{
			Trace.WriteLine(string.Format(@"Stop server:{0}", _listener.LocalEndpoint));

			_isRunning = false;
			_listener.Stop();
		}

		private async void ProcessAsync(TcpClient client)
		{
			try
			{
				Trace.WriteLine(string.Format(@"Client connected"));

				using (client)
				{
					using (var s = client.GetStream())
					{
						s.ReadTimeout = (int)TimeSpan.FromSeconds(60).TotalMilliseconds;

						int value;
						do
						{
							value = s.ReadByte();
							if (value != -1 && value != byte.MaxValue)
							{
								var handleId = (byte)value;
								value = s.ReadByte();
								if (value != -1 && value != byte.MaxValue)
								{
									await _handlers[handleId].ProcessAsync(s, (byte)value);
								}
							}
						} while (value != -1 && value != byte.MaxValue);
					}
				}
			}
			catch (Exception ex)
			{
				ex.Log();
			}

			Trace.WriteLine(string.Format(@"Client disconnected"));
		}
	}
}