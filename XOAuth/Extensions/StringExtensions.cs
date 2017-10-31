using Foundation;

namespace XOAuth.Extensions
{
	public static class StringExtensions
	{
		public static NSString ToNS(this string value) => new NSString(value);
	}
}
