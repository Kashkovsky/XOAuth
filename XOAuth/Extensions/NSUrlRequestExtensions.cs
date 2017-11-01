using System;
using System.Text;
using Foundation;
using XOAuth.Base;
using XOAuth.Exceptions;

namespace XOAuth.Extensions
{
	public static class NSUrlRequestExtensions
	{
		public static string DebugDescription(this NSUrlRequest request)
		{
			var msg = new StringBuilder($"HTTP/1.1 {request.HttpMethod ?? "METHOD"} {request.Url?.Description ?? "/"}");
			foreach (var header in request.Headers)
			{
				msg.Append($"{header.Key}: {header.Value}");
			}

			if (request.Body != null)
			{
				var body = request.Body.ToString(NSStringEncoding.UTF8);
				msg.Append($"\r\n\r\n{body}");
			}

			return msg.ToString();
		}

		public static void Sign(this NSUrlRequest request, XOAuthBase oauth)
		{
			if (string.IsNullOrEmpty(oauth.ClientConfig.AccessToken))
				throw new XOAuthException(XOAuthError.NoAccessToken);

			request.Headers["Authorization"] = $"Bearer {oauth.ClientConfig.AccessToken}".ToNS();
		}

		public static NSUrlRequest Signed(this NSUrlRequest request, XOAuthBase oauth)
		{
			request.Sign(oauth);
			return request;
		}
	}
}
