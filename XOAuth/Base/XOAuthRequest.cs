using System;
using System.Net;
using Foundation;
using XOAuth.Domain;
using XOAuth.Exceptions;
using XOAuth.Extensions;

namespace XOAuth.Base
{
	public class XOAuthRequest
	{
		public NSUrl Url { get; }
		public string Method { get; }
		public string ContentType { get; set; } = HttpContentType.Form;
		public XOAuthDictionary Headers { get; private set; }
		public RequestParams RequestParams { get; set; } = new RequestParams();

		public XOAuthRequest(NSUrl url, string method = HttpMethod.POST)
		{
			Url = url;
			Method = method;
		}

		public void AddParams(XOAuthDictionary requestParams)
		{
			if (requestParams != null)
			{
				foreach (var param in requestParams)
				{
					RequestParams[param.Key] = param.Value;
				}
			}
		}

		public NSUrlComponents AsUrlComponents()
		{
			var comp = new NSUrlComponents(Url, false);
			if (!comp.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
				throw new XOAuthException(XOAuthError.NotUsingTls);

			if (Method.Equals(HttpMethod.GET, StringComparison.OrdinalIgnoreCase)
				&& RequestParams.Count > 0)
				comp.PercentEncodedQuery = RequestParams.PercentEncodedQueryString();

			return comp;
		}

		public NSUrl AsUrl()
		{
			var components = AsUrlComponents();
			if (components.Url != null)
				return components.Url;

			throw new XOAuthException(XOAuthError.InvalidUrlComponents);
		}

		public NSUrlRequest AsUrlRequest(XOAuthBase oauth)
		{
			var finalParams = new RequestParams(RequestParams.Params.Clone());

			var finalUrl = AsUrl();
			var request = new NSMutableUrlRequest(finalUrl);
			request.HttpMethod = Method;
			request.SetValueForKey(NSObject.FromObject(ContentType), new NSString("Content-Type"));
			request.SetValueForKey(NSObject.FromObject(HttpContentType.Json), new NSString("Accept"));

			if (!string.IsNullOrEmpty(oauth.ClientConfig.ClientId) && !string.IsNullOrEmpty(oauth.ClientConfig.ClientSecret))
			{
				if (oauth.ClientConfig.SecretInBody)
				{
					oauth.Logger?.Log("Adding “client_id” and “client_secret” to request body");
					finalParams[RequestKey.ClientId] = oauth.ClientConfig.ClientId;
					finalParams[RequestKey.ClientSecret] = oauth.ClientConfig.ClientSecret;
				}
				else
				{
					oauth.Logger?.Log("Adding “Authorization” header as “Basic client-key:client-secret”");
					var pw = $"{WebUtility.UrlEncode(oauth.ClientConfig.ClientId)}:{WebUtility.UrlEncode(oauth.ClientConfig.ClientSecret)}";
					var base64 = Convert.ToBase64String(oauth.ClientConfig.AuthStringEncoding.GetBytes(pw));
					request.SetValueForKey($"Basic {base64}".ToNS(), "Authorization".ToNS());

					finalParams.RemoveValue(RequestKey.ClientId);
					finalParams.RemoveValue(RequestKey.ClientSecret);
				}
			}

			if (oauth.AuthHeaders != null)
			{
				foreach (var header in oauth.AuthHeaders)
				{
					oauth.Logger?.Log($"Overriding {header.Key} header");
					request.SetValueForKey(header.Value.ToNS(), header.Key.ToNS());
				}
			}

			if (Headers != null)
			{
				foreach (var header in Headers)
				{
					oauth.Logger?.Log($"Adding custom {header.Key} header");
					request.SetValueForKey(header.Value.ToNS(), header.Key.ToNS());
				}
			}

			if (oauth.ClientConfig.CustomParameters != null)
			{
				foreach (var kvp in oauth.ClientConfig.CustomParameters)
				{
					finalParams[kvp.Key] = kvp.Value;
				}
			}

			if (Method.Equals(HttpMethod.POST) && finalParams.Count > 0)
			{
				request.Body = finalParams.UtfEncodedData();
			}

			return request;
		}
	}
}