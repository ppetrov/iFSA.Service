using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace iFSA.Service
{
	public sealed class Server
	{
		private bool _isRunning;
		private readonly TcpListener _listener;
		private readonly Dictionary<byte, ServerHandlerBase> _handlers = new Dictionary<byte, ServerHandlerBase>();

		public Server(IPAddress address, int port)
		{
			if (address == null) throw new ArgumentNullException("address");

			_listener = new TcpListener(address, port);
		}

		public void Register(ServerHandlerBase handler)
		{
			if (handler == null) throw new ArgumentNullException("handler");

			_handlers.Add(handler.Id, handler);
		}

		public async Task StartAsync()
		{
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
			_isRunning = false;
			_listener.Stop();
		}

		private async void ProcessAsync(TcpClient client)
		{
			try
			{
				using (client)
				{
					using (var s = client.GetStream())
					{
						s.ReadTimeout = (int)TimeSpan.FromSeconds(45).TotalMilliseconds;

						var value = s.ReadByte();
						if (value != -1)
						{
							var handleId = (byte)value;
							value = s.ReadByte();
							if (value != -1)
							{
								var functionId = (byte)value;
								await _handlers[handleId].ProcessAsync(s, functionId);
								s.ReadByte();
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				ex.Log();
			}
		}
	}
}