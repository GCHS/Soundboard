using System.Windows;

namespace Soundboard__ {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {
		private readonly KeyboardListener KbListener = new();

		private void Application_Startup(object sender, StartupEventArgs e) {
			KbListener.KeyDown += new RawKeyEventHandler((s, e) => { if(MainWindow is not null) ((MainWindow)MainWindow).KbListenerKeyDown(s, e); });
			KbListener.KeyUp += new RawKeyEventHandler((s, e) => { if(MainWindow is not null) ((MainWindow)MainWindow).KbListenerKeyUp(s, e); });
		}

		private void Application_Exit(object sender, ExitEventArgs e) {
			KbListener.Dispose();
		}

	}
}
