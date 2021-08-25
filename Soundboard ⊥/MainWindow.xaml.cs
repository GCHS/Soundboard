using Microsoft.Win32;
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
		private struct Keybind {
			public Metakey Metakey; public Key Key;
			public Keybind(Metakey metakey, Key key) {
				Metakey = metakey;
				Key = key;
			}
		}
		private Metakey CurrentlyPressed;
		private readonly Dictionary<int, Sting> Stings = new();

		private bool ListeningForNextKeycombo;
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
				var potentialStings = JsonSerializer.Deserialize(Settings.Default.Stings, typeof(Sting[]), options) as Sting[];
				if(potentialStings is not null) {
					Stings = potentialStings.ToDictionary(s => s.Code);
					foreach(var s in potentialStings) {
						AddNewStingInput();
						var stingInput = StingInputs.Children[^1] as Grid;
						var keycombo = stingInput.Children[0] as TextBox;
						var source = stingInput.Children[1] as TextBox;
						keycombo.Text = $"{s.Metakeys} + {s.Keycode}";
						keycombo.Tag = new Keybind(s.Metakeys, s.Keycode);
						source.Text = s.Source.ToString();
					}
				}
			} catch(Exception e) {
				Title = e.Message;
			}

			AddNewStingInput();
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
			} else if(ListeningForNextKeycombo) {
				var keycombo = (StingInputs.Children[^1] as Grid).Children[0] as TextBox;
				keycombo.Text = $"{CurrentlyPressed} + {e.Key}";
				keycombo.Tag = new Keybind(CurrentlyPressed, e.Key);

				var source = (StingInputs.Children[^1] as Grid).Children[1] as TextBox;
				source.IsEnabled = true;
				source.Focus();
				source.Text = source.Tag as string;

				ListeningForNextKeycombo = false;
				Title = "Gay Pink Soundboard";
			}
		}

		internal void KbListenerKeyUp(object s, RawKeyEventArgs e) {
			if(MetaMapping.TryGetValue(e.Key, out var metakey)) {
				CurrentlyPressed &= ~metakey;
			}
		}

		private void OpenOfd_Click(object sender, RoutedEventArgs e) {
			OpenFileDialog ofd = new();
			ofd.Title = "Select Media";
			if(ofd.ShowDialog() ?? false) {
				var source = MatchingSource(sender as Button);
				source.Text = ofd.FileName;
				source.Focus();
			}
		}
		private void Keycombo_GotFocus(object sender, RoutedEventArgs e) {
			ListeningForNextKeycombo = true;
			var source = MatchingSource(sender as TextBox);
			source.Tag = source.Text;
			source.IsEnabled = false;

			Title = "Listening for key combo...";
		}

		private void AddNewStingInput() {
			Grid grid = new();
			grid.RowDefinitions.Add(new() { Height = new(18) });
			grid.ColumnDefinitions.Add(new());
			grid.ColumnDefinitions.Add(new());
			grid.ColumnDefinitions.Add(new() { Width = new(18) });

			TextBox keycombo = new(){Background = new SolidColorBrush(), Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255)), IsReadOnly = true, Text = "Click to set keybinding..."};
			keycombo.SetValue(Grid.ColumnProperty, 0);
			keycombo.GotFocus += Keycombo_GotFocus;
			
			TextBox source = new(){Background = new SolidColorBrush(), Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255))};;
			source.SetValue(Grid.ColumnProperty, 1);
			source.KeyDown += Source_KeyDown;
			
			Button openOfd = new() {Background = new SolidColorBrush(), Padding = new(0)};
			openOfd.SetValue(Grid.ColumnProperty, 2);
			openOfd.Content = new Image() {Source = new BitmapImage(new Uri("/Images/explorer.ico",	UriKind.Relative)), Width = 16, Height = 16};
			openOfd.Click += OpenOfd_Click;

			keycombo.Tag = new Keybind(Metakey.None, Key.None);

			grid.Children.Add(keycombo);
			grid.Children.Add(source);
			grid.Children.Add(openOfd);

			StingInputs.Children.Add(grid);
		}

		private void Source_KeyDown(object sender, KeyEventArgs e) {
			if(e.Key == Key.Enter) {
				if((sender as TextBox).Text.Length == 0) {
					OpenFileDialog ofd = new();
					ofd.Title = "Select Media";
					if(ofd.ShowDialog() ?? false) {
						(sender as TextBox).Text = ofd.FileName;
					}
				} else {
					UpdateFromSourceBox(sender as TextBox);
					AddNewStingInput();
				}
			}
		}

		private void UpdateFromSourceBox(TextBox source) {
			var bind = MatchingKeycombo(source).Tag as Keybind?;
			if(bind is not null) {
				Sting s = new(bind.Value.Metakey, bind.Value.Key, RetriggerMode.Stop, new Uri(source.Text));
				Stings[s.Code] = s;
			}
		}

		private TextBox MatchingKeycombo(FrameworkElement e) => (e.Parent as Grid).Children[0] as TextBox;
		private TextBox MatchingSource(FrameworkElement e) => (e.Parent as Grid).Children[1] as TextBox;

	}
}
