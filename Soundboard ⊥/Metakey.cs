using System;

namespace Soundboard__ {
	[Flags]
	internal enum Metakey {
		None, Shift = 1 << 8, Ctrl = 2 << 8, Alt = 4 << 8, Meta = 8 << 8
	}
}
