using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Windows.Input;
using System.Windows.Media;

namespace Soundboard__ {
	internal class Sting {
		[JsonInclude]
		public ConsoleKey Key;
		[JsonInclude]
		public Metakey Metakeys;
		[JsonInclude]
		public readonly Uri Source;
		[JsonInclude]
		public RetriggerMode RetriggerMode;

		//Keycode and Metakeys are stored separately to make serialization easier to write

		[JsonIgnore]
		internal readonly HashSet<MediaPlayer> NowPlaying = new();
		[JsonIgnore]
		internal readonly List<MediaPlayer> Pool = new();
		[JsonIgnore]
		public bool IsPlaying => NowPlaying.Any();
		[JsonIgnore]
		public int Code => (int)Metakeys | (int)Key; //keycode and metakeys combined

		[JsonConstructor]
		public Sting(Metakey Metakeys, ConsoleKey Key, RetriggerMode RetriggerMode, Uri Source) {
			this.Metakeys = Metakeys;
			this.Key = Key;
			this.RetriggerMode = RetriggerMode;
			this.Source = Source;
		}
		private void Sound_MediaFailed(object sender, ExceptionEventArgs e) {
			NowPlaying.Remove(sender as MediaPlayer);
			Pool.Add(sender as MediaPlayer);
		}
		private void Sound_MediaEnded(object sender, EventArgs e) {
			NowPlaying.Remove(sender as MediaPlayer);
			Pool.Add(sender as MediaPlayer);
		}

		public void Play() {
			if(IsPlaying) {
				switch(RetriggerMode) {
					case RetriggerMode.Overlap:
						StartNewSound();
						break;
					case RetriggerMode.Restart:
						foreach(var s in NowPlaying) s.Position = new();
						break;
					case RetriggerMode.Stop:
						Stop();
						break;
					default:
						break;
				}
			} else {
				StartNewSound();
			}
		}
		public void Stop() {
			foreach(var s in NowPlaying) {
				s.Stop();
				Pool.Add(s);
			}
			NowPlaying.Clear();
		}

		private void StartNewSound() {
			if(Pool.Count != 0) {
				var s = Pool.Last();
				Pool.RemoveAt(Pool.Count - 1);
				s.Position = new();
				s.Play();
				NowPlaying.Add(s);
			} else {
				MediaPlayer s = new();
				s.Open(Source);
				s.MediaEnded += Sound_MediaEnded;
				s.MediaFailed += Sound_MediaFailed;
				s.Play();
				NowPlaying.Add(s);
			}
		}
	}
}