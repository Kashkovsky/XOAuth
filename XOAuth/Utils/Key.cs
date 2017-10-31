using System;
using ObjCRuntime;

namespace XOAuth.Utils
{
	public class Key
	{
		private static IntPtr Handle = Dlfcn.dlopen(Constants.SecurityLibrary, 0);
		public string this[string key] => Dlfcn.GetStringConstant(Handle, key);
	}
}
