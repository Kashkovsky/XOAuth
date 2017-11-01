using System;
using AppKit;
using Foundation;
using XOAuth.Domain;
using XOAuth.Exceptions;
using XOAuth.Extensions;

namespace XOAuth.Base
{
	public abstract class XOAuthBase : Securable
	{
		protected ContextStore Context { get; }

		public virtual string GrantType => "__undefined";
		public virtual string ResponseType => null;
		public virtual AuthConfig AuthConfig { get; set; } = new AuthConfig();

		public string ClientId
		{
			get { return ClientConfig.ClientId; }
			set { ClientConfig.ClientId = value; }
		}

		public string ClientSecret
		{
			get { return ClientConfig.ClientSecret; }
			set { ClientConfig.ClientSecret = value; }
		}

		public virtual string ClientName => ClientConfig.ClientName;
		public NSUrl AuthUrl => ClientConfig.AuthorizeUrl;
		public NSUrl TokenUrl => ClientConfig.TokenUrl;

		public string Scope
		{
			get { return ClientConfig.Scope; }
			set { ClientConfig.Scope = value; }
		}

		public string Redirect
		{
			get { return ClientConfig.Redirect; }
			set { ClientConfig.Redirect = value; }
		}

		//public virtual ContextStore Context TODO

		public virtual string AccessToken
		{
			get { return ClientConfig.AccessToken; }
			set { ClientConfig.AccessToken = value; }
		}

		public virtual string IdToken
		{
			get { return ClientConfig.IdToken; }
			set { ClientConfig.IdToken = value; }
		}

		public virtual DateTime? AccessTokenExpiry
		{
			get { return ClientConfig.AccessTokenExpiry; }
			set { ClientConfig.AccessTokenExpiry = value; }
		}

		public virtual string RefreshToken
		{
			get { return ClientConfig.RefreshToken; }
			set { ClientConfig.RefreshToken = value; }
		}

		public XOAuthDictionary AuthHeaders
		{
			get { return ClientConfig.AuthHeaders; }
			set { ClientConfig.AuthHeaders = value; }
		}

		public XOAuthDictionary AuthParameters
		{
			get { return ClientConfig.CustomParameters; }
			set { ClientConfig.CustomParameters = value; }
		}
		public bool IsAuthorizing => DidAuthorizeOrFail != null;

		public Action<XOAuthDictionary, XOAuthError?> DidAuthorizeOrFail { get; set; }
		public Action<XOAuthDictionary, XOAuthError?> AfterAuthorizeOrFail { get; set; }
		public Action<bool, XOAuthError?> InternalAfterAuthorizeOrFail { get; set; }

		public XOAuthBase(XOAuthSettings settings) : base(settings)
		{
			if (!string.IsNullOrEmpty(settings.Title))
				AuthConfig.UI.Title = settings.Title;

			Context = new ContextStore();
		}

		public void DidAuthorize(XOAuthDictionary parameters)
		{
			if (UseKeychain)
				StoreTokensToKeychain();

			InvokePostAuthorizationActions(parameters, null);
		}

		//TODO: pass entire exception to log message
		public void DidFail(XOAuthError? error = null)
		{
			if (error.HasValue)
				Logger?.Log(error.Value);
			else
				error = XOAuthError.RequestCancelled;

			InvokePostAuthorizationActions(null, error);
		}

		public void AbortAuthorization()
		{
			if (!AbortTask())
			{
				Logger?.Log("Aborting authorization");
				DidFail();
			}
		}

		protected XOAuthDictionary ParseAccessTokenResponse(XOAuthDictionary parameters)
		{
			AssureNoErrorInResponse(parameters);
			AssureCorrectBearerType(parameters);
			AssureAccessTokenParamsAreValid(parameters);
			ClientConfig.UpdateFromResponse(NormalizeAccessTokenRequestKeys(parameters));
			return parameters;
		}

		protected XOAuthDictionary ParseRefreshTokenResponse(XOAuthDictionary parameters)
		{
			AssureNoErrorInResponse(parameters);
			AssureCorrectBearerType(parameters);
			AssureRefreshTokenParamsAreValid(parameters);
			ClientConfig.UpdateFromResponse(NormalizeRefreshTokenRequestKeys(parameters));
			return parameters;
		}

		protected XOAuthDictionary ParseAccessTokenResponse(NSData data)
		{
			var dict = data.ReadAs<XOAuthDictionary>();
			return ParseAccessTokenResponse(dict);
		}

		protected virtual XOAuthDictionary NormalizeAccessTokenRequestKeys(XOAuthDictionary parameters) => parameters;
		protected virtual XOAuthDictionary NormalizeRefreshTokenRequestKeys(XOAuthDictionary parameters) => parameters;

		public virtual void HandleRedirectUrl(NSUrl redirect)
		{
			Logger?.Log("HandleRedirectUrl is used from abstract XOAUthBase", LogLevel.Error);
		}

		public void AssureNoErrorInResponse(XOAuthDictionary parameters, string fallback = null)
		{
			if (parameters.HasNonEmptyValue(RequestKey.ErrorDescription, out var ed))
				throw new XOAuthException(XOAuthError.ResponseError, ed);

			if (parameters.HasNonEmptyValue(RequestKey.Error, out var e))
				throw new XOAuthException(XOAuthError.FromResponseError, e, fallback);
		}

		public void AssureCorrectBearerType(XOAuthDictionary parameters)
		{
			if (parameters.HasNonEmptyValue(RequestKey.TokenType, out var tt))
			{
				var tokenType = tt;
				if (tokenType.Equals("bearer", StringComparison.OrdinalIgnoreCase)) return;

				throw new XOAuthException(XOAuthError.UnsupportedTokenType, $"Only “bearer” token is supported, but received {tokenType}");
			}

			throw new XOAuthException(XOAuthError.NoTokenType);
		}

		public void AssureMatchesState(XOAuthDictionary parameters)
		{
			if (parameters.HasNonEmptyValue(RequestKey.State, out var s))
			{
				Logger?.Log($"Checking state, got {s}, expecting {Context.State}");
				if (!Context.MatchesState(s))
					throw new XOAuthException(XOAuthError.InvalidState);

				Context.ResetState();
			}
			else
			{
				throw new XOAuthException(XOAuthError.MissingState);
			}
		}

		public virtual void AssureAccessTokenParamsAreValid(XOAuthDictionary parameters) { }

		public virtual void AssureRefreshTokenParamsAreValid(XOAuthDictionary parameters) { }

		protected override void UpdateFromKeychainItems(NSMutableDictionary<NSString, NSObject> items)
		{
			var dict = XOAuthDictionary.FromNSMutableDictionary(items);
			foreach (var message in ClientConfig.UpdateFromStorableItems(dict))
			{
				Logger?.Log(message);
			}

			ClientConfig.SecretInBody = ClientConfig.EndpointAuthMethod == AuthMethod.ClientSecretPost;
		}

		protected override NSMutableDictionary<NSString, NSObject> StorableCredentialItems() => ClientConfig.StorableCredentialItems;

		protected override NSMutableDictionary<NSString, NSObject> StorableTokenItems() => ClientConfig.StorableTokenItems;

		public override void ForgetClient()
		{
			base.ForgetClient();
			ClientConfig.ForgetCredentials();
		}

		public override void ForgetTokens()
		{
			base.ForgetTokens();
			ClientConfig.ForgetTokens();
		}

		public NSUrlRequest Request(NSUrl fromUrl, NSUrlRequestCachePolicy cachePolicy = NSUrlRequestCachePolicy.ReloadIgnoringLocalCacheData) =>
		new NSUrlRequest(fromUrl, cachePolicy, 20).Signed(this);

		private void InvokePostAuthorizationActions(XOAuthDictionary parameters, XOAuthError? error)
		{
			NSApplication.SharedApplication.InvokeOnMainThread(() =>
			{
				DidAuthorizeOrFail?.Invoke(parameters, error);
				DidAuthorizeOrFail = null;
				InternalAfterAuthorizeOrFail?.Invoke(error.HasValue, error);
				AfterAuthorizeOrFail?.Invoke(parameters, error);
			});
		}
	}
}