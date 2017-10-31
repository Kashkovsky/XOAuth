using System;
namespace XOAuth.Extensions
{
	public static class DateTimeExtensions
	{
		public static bool IsFutureDate(this DateTime date) => DateTime.Compare(date, DateTime.Now) > 0;
	}
}