using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using iFSA.Service;
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

		private void DashboardScreenOnLoaded(object sender, RoutedEventArgs e)
		{
			this.DataContext = _viewModel;
		}

		private async void RefreshLogsTapped(object sender, TappedRoutedEventArgs e)
		{
			var btn = sender as Button;
			if (btn != null)
			{
				btn.IsEnabled = false;

				try
				{
					using (var socket = new StreamSocket())
					{
						await socket.ConnectAsync(new HostName(@"127.0.0.1"), @"11111");

						var handler = new WinRtTransferHandler(socket);

						_viewModel.Load(await new LogsClientHandler(2, handler).GetConfigsAsync());

						await handler.CloseAsync();
					}
				}
				catch
				{
				}
				finally
				{
					btn.IsEnabled = true;
				}
			}
		}

		private async void ConfigureFolderTapped(object sender, TappedRoutedEventArgs e)
		{
			var btn = (sender as Button);
			if (btn != null)
			{
				var ctx = btn.DataContext as LogCategoryViewModel;
				if (ctx != null)
				{
					btn.IsEnabled = false;

					try
					{
						var path = Path.Combine(new[] { @"C:\Temp\", ctx.Platform.ToString(), ctx.Category.ToString() });

						using (var socket = new StreamSocket())
						{
							await socket.ConnectAsync(new HostName(@"127.0.0.1"), @"11111");

							var handler = new WinRtTransferHandler(socket);
							var requestHeader = new RequestHeader(ctx.Platform, new Version(0, 0, 0, 0), string.Empty, string.Empty);
							var config = new LogConfig(requestHeader, ctx.Category, path);
							await new LogsClientHandler(2, handler).ConfigureAsync(config);

							await handler.CloseAsync();
						}

						ctx.Path = path;
					}
					catch
					{
					}
					finally
					{
						btn.IsEnabled = true;
					}
				}
			}
		}

		private async void UploadTapped(object sender, TappedRoutedEventArgs e)
		{
			var btn = (sender as Button);
			if (btn != null)
			{
				var ctx = btn.DataContext as LogCategoryViewModel;
				if (ctx != null)
				{
					btn.IsEnabled = false;

					try
					{
						var picker = new FileOpenPicker();

						picker.FileTypeFilter.Add(@".wmv");

						var file = await picker.PickSingleFileAsync();
						if (file != null)
						{
							var fileSize = (await file.GetBasicPropertiesAsync()).Size;

							var data = new byte[fileSize];
							var buffer = new byte[16 * 1024];

							using (var ms = new MemoryStream(data))
							{
								using (var s = (await file.OpenReadAsync()).AsStreamForRead())
								{
									int readBytes;
									while ((readBytes = await s.ReadAsync(buffer, 0, buffer.Length)) != 0)
									{
										await ms.WriteAsync(buffer, 0, readBytes);
									}
								}
							}

							var clientFile = new ClientFile(file.Name, data);

							using (var socket = new StreamSocket())
							{
								await socket.ConnectAsync(new HostName(@"127.0.0.1"), @"11111");

								var handler = new WinRtTransferHandler(socket);
								var requestHeader = new RequestHeader(ctx.Platform, new Version(0, 0, 0, 0), string.Empty, string.Empty);
								await new LogsClientHandler(2, handler).UploadFilesAsync(requestHeader, new List<ClientFile> { clientFile });

								await handler.CloseAsync();
							}
						}
					}
					catch (Exception ex)
					{
					}
					finally
					{
						btn.IsEnabled = true;
					}
				}
			}
		}
	}

	public class DashboardViewModel
	{
		private readonly ObservableCollection<ServerModule> _modules = new ObservableCollection<ServerModule>();
		private readonly LogCategoryViewModel[] _mobile;
		private readonly LogCategoryViewModel[] _ipad;
		private readonly LogCategoryViewModel[] _winrt;

		public ObservableCollection<ServerModule> Modules
		{
			get { return _modules; }
		}

		public LogCategoryViewModel[] Mobile
		{
			get { return _mobile; }
		}

		public LogCategoryViewModel[] Ipad
		{
			get { return _ipad; }
		}

		public LogCategoryViewModel[] Winrt
		{
			get { return _winrt; }
		}

		public DashboardViewModel()
		{
			this.Modules.Add(new ServerModule(@"Logs"));

			_mobile = new LogCategoryViewModel[Constants.SupportedPlatforms];
			_mobile[0] = new LogCategoryViewModel(ClientPlatform.WinMobile, LogCategory.Logs, "\uE19C");
			_mobile[1] = new LogCategoryViewModel(ClientPlatform.WinMobile, LogCategory.Database, "\uE19C");
			_mobile[2] = new LogCategoryViewModel(ClientPlatform.WinMobile, LogCategory.Files, "\uE19C");

			_ipad = new LogCategoryViewModel[Constants.SupportedPlatforms];
			_ipad[0] = new LogCategoryViewModel(ClientPlatform.IPad, LogCategory.Logs, "\uE19C");
			_ipad[1] = new LogCategoryViewModel(ClientPlatform.IPad, LogCategory.Database, "\uE19C");
			_ipad[2] = new LogCategoryViewModel(ClientPlatform.IPad, LogCategory.Files, "\uE19C");

			_winrt = new LogCategoryViewModel[Constants.SupportedPlatforms];
			_winrt[0] = new LogCategoryViewModel(ClientPlatform.WinRT, LogCategory.Logs, "\uE19C");
			_winrt[1] = new LogCategoryViewModel(ClientPlatform.WinRT, LogCategory.Database, "\uE19C");
			_winrt[2] = new LogCategoryViewModel(ClientPlatform.WinRT, LogCategory.Files, "\uE19C");
		}

		public void Load(LogConfig[] configs)
		{
			if (configs == null) throw new ArgumentNullException("configs");

			var localConfigs = _mobile.Concat(_ipad).Concat(_winrt).ToList();

			foreach (var config in configs)
			{
				var category = config.Category;
				var platform = config.RequestHeader.ClientPlatform;
				var match = localConfigs.SingleOrDefault(c => c.Category == category && c.Platform == platform);
				if (match != null)
				{
					var path = (config.Folder ?? string.Empty).Trim();
					if (path == string.Empty)
					{
						path = @"N/A";
					}
					match.Path = path;
				}
			}
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

	public class LogCategoryViewModel : BindableBase
	{
		public ClientPlatform Platform { get; private set; }
		public LogCategory Category { get; private set; }
		public string Icon { get; private set; }

		private string _path = @"N/A";
		public string Path
		{
			get { return _path; }
			set { this.SetProperty(ref _path, value); }
		}

		public LogCategoryViewModel(ClientPlatform platform, LogCategory category, string icon)
		{
			if (icon == null) throw new ArgumentNullException("icon");

			Platform = platform;
			this.Category = category;
			this.Icon = icon;
		}
	}
}

