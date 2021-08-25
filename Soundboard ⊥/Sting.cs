using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Windows.Input;
using System.Windows.Media;

namespace Soundboard__ {
	internal class Sting {
		[JsonInclude]
		public readonly Key Keycode;
		[JsonInclude]
		public readonly Metakey Metakeys;
		[JsonInclude]
		public readonly Uri Source;
		[JsonInclude]
		public readonly RetriggerMode RetriggerMode;

		//Keycode and Metakeys are stored separately to make serialization easier to write

		[JsonIgnore]
		internal readonly HashSet<MediaPlayer> NowPlaying = new();
		[JsonIgnore]
		internal readonly List<MediaPlayer> Pool = new();
		[JsonIgnore]
		public bool IsPlaying => NowPlaying.Any();
		[JsonIgnore]
		public int Code => (int)Metakeys | (int)Keycode; //keycode and metakeys combined

		[JsonConstructor]
		public Sting(Metakey Metakeys, Key Keycode, RetriggerMode RetriggerMode, Uri Source) {
			this.Metakeys = Metakeys;
			this.Keycode = Keycode;
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
						foreach(var s in NowPlaying) {
							s.Stop();
							Pool.Add(s);
						}
						NowPlaying.Clear();
						break;
					default:
						break;
				}
			} else {
				StartNewSound();
			}
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