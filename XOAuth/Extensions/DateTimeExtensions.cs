using System;
namespace XOAuth.Extensions
{
	public static class DateTimeExtensions
	{
		public static bool IsFutureDate(this DateTime date) => DateTime.Compare(date, DateTime.Now) > 0;
		public static DateTime ParseAsExpiryDate(this string value)
		{
			if (int.TryParse(value, out var s))
				return DateTime.Now.AddSeconds(s);
			if (DateTime.TryParse(value, out var dt))
				return dt;

			return DateTime.Now;
		}
	}
}