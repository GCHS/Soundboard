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
			[JsonInclude]
			public Metakey Metakeys;
			[JsonInclude]
			public Key Key;
			public Keybind(Metakey Metakeys, Key Key) {
				this.Metakeys = Metakeys;
				this.Key = Key;
			}
			[JsonIgnore]
			public int Code => (int)Metakeys | (int)Key;
		}
		private Metakey CurrentlyPressed;
		private readonly Dictionary<int, Sting> Stings = new();

		Keybind StopAll = new(Metakey.Ctrl | Metakey.Alt, Key.S);

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
						var retriggerMode = stingInput.Children[1] as ComboBox;
						var source = stingInput.Children[2] as TextBox;
						keycombo.Text = $"{s.Metakeys} + {s.Key}";
						keycombo.Tag = new Keybind(s.Metakeys, s.Key);
						retriggerMode.SelectedItem = s.RetriggerMode;
						source.Text = s.Source.ToString();
					}
				}
				var potentialStopAllBinding = JsonSerializer.Deserialize(Settings.Default.StopAll, typeof(Keybind), options) as Keybind?;
				if(potentialStopAllBinding is not null) {
					StopAll = potentialStopAllBinding.Value;
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
			Settings.Default.StopAll = JsonSerializer.Serialize(StopAll, options);
			Settings.Default.Save();
		}


		internal void KbListenerKeyDown(object sender, RawKeyEventArgs e) {
			if(MetaMapping.TryGetValue(e.Key, out var metakey)) {
				CurrentlyPressed |= metakey;
			} else if(StopAll.Metakeys == CurrentlyPressed && StopAll.Key == e.Key) {
				foreach(var s in Stings.Values) s.Stop();
			} else if(Stings.TryGetValue((int)CurrentlyPressed | (int)e.Key, out var sting)) {
				sting.Play();
			} else if(ListeningForNextKeycombo) {
				var keycombo = FocusManager.GetFocusedElement(this) as TextBox;
				keycombo.Text = $"{CurrentlyPressed} + {e.Key}";
				var oldBind = keycombo.Tag as Keybind?;
				var newBind = new Keybind(CurrentlyPressed, e.Key);
				keycombo.Tag = newBind;
				if(oldBind is not null && Stings.TryGetValue(oldBind.Value.Code, out var updateSting)) {
					Stings.Remove(oldBind.Value.Code);
					(updateSting.Metakeys, updateSting.Key) = (newBind.Metakeys, newBind.Key);
					Stings[newBind.Code] = updateSting;
					Title = "Keybind Updated";
				} else {
					Title = "Gay Pink Soundboard";
				}

				MatchingRetriggerMode(keycombo).Focus();

				ListeningForNextKeycombo = false;
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
				if(UpdateOrAddStingFromInput(source) == SourceChangeStingEffect.Add) {
					AddNewStingInput();
				}
			}
		}
		private void Keycombo_GotFocus(object sender, RoutedEventArgs e) {
			ListeningForNextKeycombo = true;
			Title = "Listening for key combo...";
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
					if(UpdateOrAddStingFromInput(sender as TextBox) == SourceChangeStingEffect.Add) {
						AddNewStingInput();
					}
				}
			}
		}
		private void RetriggerMode_KeyDown(object sender, KeyEventArgs e) {
			if(e.Key == Key.Enter) {
				MatchingSource(sender as FrameworkElement).Focus();
			}
		}
		private void RetriggerMode_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if(Stings.TryGetValue((MatchingKeycombo(sender as FrameworkElement).Tag as Keybind?)?.Code ?? -1, out var sting)) {
				sting.RetriggerMode = (RetriggerMode)(sender as ComboBox).SelectedItem;
				Title = "Retrigger Mode Updated";
			}
		}

		private void AddNewStingInput() {
			Grid grid = new();
			grid.RowDefinitions.Add(new() { Height = new(18) });
			grid.ColumnDefinitions.Add(new());
			grid.ColumnDefinitions.Add(new());
			grid.ColumnDefinitions.Add(new());
			grid.ColumnDefinitions.Add(new() { Width = new(18) });

			TextBox keycombo = new() { Background = new SolidColorBrush(), Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255)), IsReadOnly = true, Text = "Click to set keybinding..." };
			keycombo.SetValue(Grid.ColumnProperty, 0);
			keycombo.GotFocus += Keycombo_GotFocus;

			ComboBox retriggerMode = new() { Foreground = new SolidColorBrush(Color.FromRgb(185, 11, 182)), Padding = new(0), ItemsSource = new[] { RetriggerMode.Overlap, RetriggerMode.Restart, RetriggerMode.Stop }, SelectedItem = RetriggerMode.Stop };
			retriggerMode.SetValue(Grid.ColumnProperty, 1);
			retriggerMode.KeyDown += RetriggerMode_KeyDown;
			retriggerMode.SelectionChanged += RetriggerMode_SelectionChanged;

			TextBox source = new() { Background = new SolidColorBrush(), Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255)) }; ;
			source.SetValue(Grid.ColumnProperty, 2);
			source.KeyDown += Source_KeyDown;

			Button openOfd = new() { Background = new SolidColorBrush(), Padding = new(0) };
			openOfd.SetValue(Grid.ColumnProperty, 3);
			openOfd.Content = new Image() { Source = new BitmapImage(new Uri("/Images/explorer.ico", UriKind.Relative)), Width = 16, Height = 16 };
			openOfd.Click += OpenOfd_Click;

			keycombo.Tag = new Keybind(Metakey.None, Key.None);

			grid.Children.Add(keycombo);
			grid.Children.Add(retriggerMode);
			grid.Children.Add(source);
			grid.Children.Add(openOfd);

			StingInputs.Children.Add(grid);
		}
		enum SourceChangeStingEffect {None, Update, Add}
		private SourceChangeStingEffect UpdateOrAddStingFromInput(FrameworkElement e) { //returns 
			var bind = MatchingKeycombo(e).Tag as Keybind?;
			var retriggerMode = (RetriggerMode)MatchingRetriggerMode(e).SelectedItem;
			var source = MatchingSource(e);
			if(bind is not null) {
				SourceChangeStingEffect ret;
				if(Stings.TryGetValue(bind.Value.Code, out var sting)) {
					ret = SourceChangeStingEffect.Update;
					sting.Stop();
					Title = "Source Updated";
				} else {
					ret = SourceChangeStingEffect.Add;
					Title = "Sting Added";
				}
				Sting s = new(bind.Value.Metakeys, bind.Value.Key, retriggerMode, new Uri(source.Text));
				Stings[s.Code] = s;
				return ret;
			}
			return SourceChangeStingEffect.None;
		}

		private TextBox MatchingKeycombo(FrameworkElement e) => (e.Parent as Grid).Children[0] as TextBox;
		private ComboBox MatchingRetriggerMode(FrameworkElement e) => (e.Parent as Grid).Children[1] as ComboBox;
		private TextBox MatchingSource(FrameworkElement e) => (e.Parent as Grid).Children[2] as TextBox;

	}
}
