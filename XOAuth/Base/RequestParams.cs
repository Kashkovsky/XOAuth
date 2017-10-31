using System.Linq;
using Foundation;
using XOAuth.Domain;
using XOAuth.Exceptions;

namespace XOAuth.Base
{
	public class RequestParams
	{
		public XOAuthDictionary Params { get; private set; }
		public int Count => Params?.Count ?? 0;
		public bool RemoveValue(string key) => Params?.Remove(key) ?? false;

		public string this[string key]
		{
			get { return Params?[key]; }
			set
			{
				Params = Params ?? new XOAuthDictionary();
				Params[key] = value;
			}
		}

		public RequestParams()
		{
		}

		public RequestParams(XOAuthDictionary parameters)
		{
			Params = parameters;
		}

		public NSData UtfEncodedData()
		{
			var body = PercentEncodedQueryString();
			var data = new NSString(body).Encode(NSStringEncoding.UTF8, true);
			if (data == null)
				throw new XOAuthException(XOAuthError.Utf8EncodeError);

			return data;
		}

		public string PercentEncodedQueryString()
		{
			if (Params == null) return string.Empty;
			return FormEncodedQueryStringFor(Params);
		}

		public static string FormEncodedQueryStringFor(XOAuthDictionary queryParams)
		{
			return string.Join("&",
			queryParams
				.Select(x => $"{x.Key}={System.Net.WebUtility.UrlEncode(x.Value)}"));
		}
	}
}
