// This file was generated by the Gtk# code generator.
// Any changes made will be lost if regenerated.

namespace GnomeKeyringSharp {

	using System;

#region Autogenerated code
	[GLib.CDeclCallback]
	internal delegate void OperationGetIntCallbackNative(int result, uint val, IntPtr data);

	internal class OperationGetIntCallbackWrapper {

		public void NativeCallback (int result, uint val, IntPtr data)
		{
			GnomeKeyring.Result _arg0 = (GnomeKeyring.Result) result;
			uint _arg1 = val;
			managed ( _arg0,  _arg1);
		}

		internal OperationGetIntCallbackNative NativeDelegate;
		GnomeKeyring.OperationGetIntCallback managed;

		public OperationGetIntCallbackWrapper (GnomeKeyring.OperationGetIntCallback managed)
		{
			this.managed = managed;
			if (managed != null)
				NativeDelegate = new OperationGetIntCallbackNative (NativeCallback);
		}

		public static GnomeKeyring.OperationGetIntCallback GetManagedDelegate (OperationGetIntCallbackNative native)
		{
			if (native == null)
				return null;
			OperationGetIntCallbackWrapper wrapper = (OperationGetIntCallbackWrapper) native.Target;
			if (wrapper == null)
				return null;
			return wrapper.managed;
		}
	}
#endregion
}
