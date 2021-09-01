using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.KeyboardAndMouseInput;

namespace Soundboard__ {
	partial class RawInputHandler : IDisposable {
		private readonly RAWINPUTDEVICE[] DevicesToRegisterFor = new RAWINPUTDEVICE[] {
			new(){
				usUsagePage = (ushort) UsagePage.GENERIC,
				usUsage = (ushort)UsageID.GENERIC_KEYBOARD,
			},
			//new(){
			//	usUsagePage = (ushort) UsagePage.GENERIC,
			//	usUsage = (ushort)UsageID.GENERIC_MOUSE,
			//},
			//new() {
			//	usUsagePage = (ushort)UsagePage.GENERIC,
			//	usUsage = (ushort)UsageID.GENERIC_GAMEPAD,
			//},
		};

		private const int WM_INPUT = 0x00FF; //https://docs.microsoft.com/en-us/windows/win32/inputdev/wm-input

		public delegate void RawKeyboardEvent(ConsoleKey changed);

		public event RawKeyboardEvent? KeyboardDown;
		public event RawKeyboardEvent? KeyboardUp;

		

		public RawInputHandler(Window messageReceiver) { //pass in the window you want the events to go to
			var targetHwnd = new WindowInteropHelper(messageReceiver).Handle;
			unsafe {
				PInvoke.RegisterRawInputDevices(new Span<RAWINPUTDEVICE>(
					DevicesToRegisterFor.Select(dev => new RAWINPUTDEVICE {
						usUsagePage = dev.usUsagePage,
						usUsage = dev.usUsage,
						dwFlags = RAWINPUTDEVICE_FLAGS.RIDEV_INPUTSINK,
						hwndTarget = (HWND)targetHwnd
					}).ToArray()), (uint)sizeof(RAWINPUTDEVICE));
			}
			HwndSource.FromHwnd(targetHwnd).AddHook(HandleWM_Input);
		}
		~RawInputHandler() => Dispose();
		public void Dispose() {
			unsafe {
				PInvoke.RegisterRawInputDevices(new Span<RAWINPUTDEVICE>(//I don't know if I actually need to do this,
					DevicesToRegisterFor.Select(dev => new RAWINPUTDEVICE {//but I'm slightly scared of Win32
						usUsagePage = dev.usUsagePage,
						usUsage = dev.usUsage,
						dwFlags = RAWINPUTDEVICE_FLAGS.RIDEV_REMOVE
					}).ToArray()), (uint)sizeof(RAWINPUTDEVICE));
			}
		}
		private IntPtr HandleWM_Input(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
			if(msg == WM_INPUT) {
				unsafe {
					uint rawInputCount = 0;
					var rawInputHandle = new HRAWINPUT((nint)lParam.ToInt64());
					PInvoke.GetRawInputData(rawInputHandle, RAW_INPUT_DATA_COMMAND_FLAGS.RID_INPUT, null, ref rawInputCount, (uint)sizeof(RAWINPUTHEADER));//get count of RAWINPUTs
					var data = new RAWINPUT[rawInputCount];
					fixed(RAWINPUT* dataStart = &data[0]) {
						PInvoke.GetRawInputData(rawInputHandle, RAW_INPUT_DATA_COMMAND_FLAGS.RID_INPUT, dataStart, ref rawInputCount, (uint)sizeof(RAWINPUTHEADER));//actual data copy
					}

					foreach(var i in data) {
						switch((DeviceType)i.header.dwType) {
							case DeviceType.KEYBOARD:
								((i.data.keyboard.Flags & 1) != 0 ? KeyboardUp : KeyboardDown)?.Invoke((ConsoleKey)i.data.keyboard.VKey);
								break;
							default: break;
						}
					}
				}
				handled = true;
			}
			return IntPtr.Zero;
		}
	}
}
