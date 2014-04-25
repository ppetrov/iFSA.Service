using System;
using System.Collections.ObjectModel;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.UI.Xaml;
using iFSA.Service.Core;
using iFSA.Service.Logs.Client;

namespace iFSA.Win8Client
{
	public sealed partial class DashboardScreen
	{
		private readonly DashboardViewModel _viewModel = new DashboardViewModel();

		public DashboardScreen()
		{
			this.InitializeComponent();
		}

		private async void DashboardScreenOnLoaded(object sender, RoutedEventArgs e)
		{
			this.DataContext = _viewModel;

			using (var socket = new StreamSocket())
			{
				socket.Control.NoDelay = true;

				await socket.ConnectAsync(new HostName(@"127.0.0.1"), @"11111");

				//var package = new RequestPackage(new RequestHeader(ClientPlatform.WinRT, new Version(2, 2, 2, 2), string.Empty, string.Empty), new byte[] { 1, 2, 3, 4, 5 });

				var handler = new WinRtTransferHandler(socket);

				//var updateHandler = new UpdateClientHandler(1, handler);
				var logsHandler = new LogsClientHandler(2, handler);

				for (var i = 0; i < 10; i++)
				{
					//updateHandler.UploadPackageAsync(package).Wait();
					var tmp = await logsHandler.GetConfigsAsync();

					this.Tag = tmp;

					await logsHandler.ConfigureAsync(new LogConfig(new RequestHeader(ClientPlatform.WinRT, new Version(1, 1, i, 1),
						string.Empty, string.Empty), LogMethod.ConfigureDatabase, @"C:\temp\db"));
				}

				await handler.CloseAsync();
			}
		}
	}

	public class DashboardViewModel
	{
		private readonly ObservableCollection<ServerModule> _modules = new ObservableCollection<ServerModule>();

		public ObservableCollection<ServerModule> Modules
		{
			get { return _modules; }
		}

		public DashboardViewModel()
		{
			this.Modules.Add(new ServerModule(@"Logs"));
		}
	}

	public class ServerModule
	{
		public string Name { get; private set; }

		public ServerModule(string name)
		{
			if (name == null) throw new ArgumentNullException("name");

			this.Name = name;
		}
	}
}
