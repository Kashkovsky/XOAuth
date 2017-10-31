using System;
using System.IO;
using Foundation;
using Newtonsoft.Json;

namespace XOAuth.Extensions
{
	public static class JsonHelper
	{
		public static T ReadAs<T>(this NSData data)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));

			using (var ms = new MemoryStream())
			using (var stream = data.AsStream())
			{
				stream.CopyTo(ms);
				ms.Seek(0, SeekOrigin.Begin);
				var array = ms.ToArray();
				string result = System.Text.Encoding.UTF8.GetString(array, 0, array.Length);
				return JsonConvert.DeserializeObject<T>(result);
			}
		}
	}
}
