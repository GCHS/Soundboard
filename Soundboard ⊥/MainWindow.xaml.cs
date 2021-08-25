using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Soundboard__ {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		private readonly Dictionary<Key, Metakey> MetaMapping = new(){
			{Key.LeftShift, Metakey.Shift},
			{Key.RightShift, Metakey.Shift},
			{Key.LeftAlt, Metakey.Alt},
			{Key.RightAlt, Metakey.Alt},
			{Key.LeftCtrl, Metakey.Ctrl},
			{Key.RightCtrl, Metakey.Ctrl},
			{Key.LWin, Metakey.Meta},
			{Key.RWin, Metakey.Meta}
		};
		private Metakey CurrentlyPressed;
		private readonly Dictionary<int, Sting> Stings = new();
		public MainWindow() {
			InitializeComponent();

			if(Settings.Default.SettingsMigrationRequired) {
				Settings.Default.Upgrade();
				Settings.Default.SettingsMigrationRequired = false;
				Settings.Default.Save();
			}
			try {
				JsonSerializerOptions options = new() { WriteIndented = true };
				options.Converters.Add(new JsonStringEnumConverter());
				var potentialStings = JsonSerializer.Deserialize(Settings.Default.Stings, typeof(Sting), options) as IEnumerable<Sting>;
				if(potentialStings is not null) {
					Stings = potentialStings.ToDictionary(s => s.Code);
				}
			} catch(Exception e) {
				Title = e.Message;
			}
		}
		private void Window_Closing(object sender, CancelEventArgs e) {
			JsonSerializerOptions options = new() { WriteIndented = true };
			options.Converters.Add(new JsonStringEnumConverter());
			Settings.Default.Stings = JsonSerializer.Serialize(Stings.Values, options);
			Settings.Default.Save();
		}


		internal void KbListenerKeyDown(object s, RawKeyEventArgs e) {
			if(MetaMapping.TryGetValue(e.Key, out var metakey)) {
				CurrentlyPressed |= metakey;
			} else if(Stings.TryGetValue((int)CurrentlyPressed | (int)e.Key, out var sting)) {
				sting.Play();
			}
		}

		internal void KbListenerKeyUp(object s, RawKeyEventArgs e) {
			if(MetaMapping.TryGetValue(e.Key, out var metakey)) {
				CurrentlyPressed &= ~metakey;
			}
		}
	}
}
