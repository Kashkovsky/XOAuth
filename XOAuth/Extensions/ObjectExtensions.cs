using System;
using Foundation;

namespace XOAuth.Extensions
{
	public static class ObjectExtensions
	{
		public static NSObject ToNS(this object value) => NSObject.FromObject(value);
	}
}