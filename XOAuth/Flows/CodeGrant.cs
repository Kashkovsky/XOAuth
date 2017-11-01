using Foundation;
using XOAuth.Base;
using XOAuth.Domain;
using XOAuth.Exceptions;

namespace XOAuth.Flows
{
	public class CodeGrant : XOAuth
	{
		public override string GrantType => "authorization_code";
		public override string ResponseType => "code";

		public CodeGrant(XOAuthSettings settings) : base(settings)
		{
		}

		public override void HandleRedirectUrl(NSUrl redirect)
		{
			Logger?.Log($"Handling redirect URL {redirect.Description}");
			try
			{
				var code = ValidateRedirectUrl(redirect);
				GetExchangeCodeForToken(code);

			}
			catch (XOAuthException ex)
			{
				DidFail(ex.Error);
			}
		}

		public void GetExchangeCodeForToken(string code)
		{
			try
			{
				if (string.IsNullOrEmpty(code))
					throw new XOAuthException(XOAuthError.PrerequisiteFailed, "I don't have a code to exchange, let the user authorize first");

				var post = CreateAccessTokenRequest(code).AsUrlRequest(this);
				Logger?.Log($"Exchanging code {code} for access token at {post.Url.Description}");
				Perform(post, (response) =>
				{
					try
					{
						var data = response.ResponseJson();
						if (response.Response.StatusCode >= 400)
							throw new XOAuthException(XOAuthError.Generic, $"Failed with status {response.Response.StatusCode}");

						Logger?.Log($"Did exchange code for access [{!string.IsNullOrEmpty(ClientConfig.AccessToken)}] and refresh [{!string.IsNullOrEmpty(ClientConfig.RefreshToken)}] tokens");
						DidAuthorize(data);
					}
					catch (XOAuthException ex)
					{
						DidFail(ex.Error);
					}
				});
			}
			catch (XOAuthException ex)
			{
				DidFail(ex.Error);
			}
		}

		private string ValidateRedirectUrl(NSUrl redirect)
		{
			if (string.IsNullOrEmpty(Context.RedirectUrl))
				throw new XOAuthException(XOAuthError.NoRedirectUrl);

			var comp = new NSUrlComponents(redirect, true);
			if (!redirect.AbsoluteString.StartsWith(Context.RedirectUrl, System.StringComparison.OrdinalIgnoreCase)
				&& (!redirect.AbsoluteString.StartsWith("urn:ietf:wg:oauth:2.0:oob", System.StringComparison.OrdinalIgnoreCase)
					&& comp?.Host != "localhost"))
				throw new XOAuthException(XOAuthError.InvalidRedirectUrl, $"Expecting '{Context.RedirectUrl}' but received '{redirect}'");

			var compQuery = comp?.Query;
			if (!string.IsNullOrEmpty(compQuery))
			{
				var query = GetParamsFromQuery(comp.PercentEncodedQuery);
				AssureNoErrorInResponse(query);
				if (query.HasNonEmptyValue(RequestKey.Code, out var cd))
				{
					AssureMatchesState(query);
					return cd;
				}

				throw new XOAuthException(XOAuthError.ResponseError, "No 'code' received");
			}

			throw new XOAuthException(XOAuthError.PrerequisiteFailed, "The redirect URL contains no query fragment");
		}

		private XOAuthRequest CreateAccessTokenRequest(string code, XOAuthDictionary parameters = null)
		{
			if (string.IsNullOrEmpty(ClientConfig.ClientId))
				throw new XOAuthException(XOAuthError.NoClientId);

			if (string.IsNullOrEmpty(Context.RedirectUrl))
				throw new XOAuthException(XOAuthError.NoRedirectUrl);

			var request = new XOAuthRequest(ClientConfig.TokenUrl ?? ClientConfig.AuthorizeUrl);
			request.RequestParams[RequestKey.Code] = code;
			request.RequestParams[RequestKey.GrantType] = GrantType;
			request.RequestParams[RequestKey.RedirectUri] = Redirect;
			request.RequestParams[RequestKey.ClientId] = ClientId;

			return request;
		}
	}
}
