namespace Soundboard__ {
	partial class RawInputHandler {
		enum UsagePage : ushort {//from Windows header hidusage.h
			UNDEFINED = 0x00,
			GENERIC = 0x01,
			SIMULATION = 0x02,
			VR = 0x03,
			SPORT = 0x04,
			GAME = 0x05,
			GENERIC_DEVICE = 0x06,
			KEYBOARD = 0x07,
			LED = 0x08,
			BUTTON = 0x09,
			ORDINAL = 0x0A,
			TELEPHONY = 0x0B,
			CONSUMER = 0x0C,
			DIGITIZER = 0x0D,
			HAPTICS = 0x0E,
			PID = 0x0F,
			UNICODE = 0x10,
			ALPHANUMERIC = 0x14,
			SENSOR = 0x20,
			LIGHTING_ILLUMINATION = 0x59,
			BARCODE_SCANNER = 0x8C,
			WEIGHING_DEVICE = 0x8D,
			MAGNETIC_STRIPE_READER = 0x8E,
			CAMERA_CONTROL = 0x90,
			ARCADE = 0x91,
			MICROSOFT_BLUETOOTH_HANDSFREE = 0xFFF3,
			VENDOR_DEFINED_BEGIN = 0xFF00,
			VENDOR_DEFINED_END = 0xFFFF,
		}
	}
}
