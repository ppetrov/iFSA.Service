﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace iFSA.Service.Core
{
	public sealed class Server
	{
		private int _clients;
		private bool _isRunning;
		private readonly TcpListener _listener;
		private readonly Dictionary<byte, ServerRequestHandler> _handlers = new Dictionary<byte, ServerRequestHandler>();

		public int Clients
		{
			get { return _clients; }
		}

		public Server(IPAddress address, int port)
		{
			if (address == null) throw new ArgumentNullException("address");

			_listener = new TcpListener(address, port);
		}

		public void Add(ServerRequestHandler requestHandler)
		{
			if (requestHandler == null) throw new ArgumentNullException("requestHandler");

			_handlers.Add(requestHandler.Id, requestHandler);
		}

		public void Start()
		{
			try
			{
				_isRunning = true;
				_listener.Start();

				while (_isRunning)
				{
					var client = _listener.AcceptTcpClient();
					ThreadPool.QueueUserWorkItem(this.Handle, client);
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

		private void Handle(object _)
		{
			try
			{
				Interlocked.Increment(ref _clients);

				using (var c = _ as TcpClient)
				{
					using (var s = c.GetStream())
					{
						s.ReadTimeout = (int)TimeSpan.FromSeconds(45).TotalMilliseconds;

						var value = s.ReadByte();
						if (value != -1)
						{
							_handlers[(byte)value].Handle(s);
							s.ReadByte();
						}
					}
				}
			}
			catch (Exception ex)
			{
				ex.Log();
			}
			finally
			{
				Interlocked.Decrement(ref _clients);
			}
		}
	}
}