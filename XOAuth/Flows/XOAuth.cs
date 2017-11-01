using System;
using AppKit;
using Foundation;
using XOAuth.Base;
using XOAuth.Domain;
using XOAuth.Exceptions;
using XOAuth.Extensions;
using XOAuth.Platform;
using AuthCallback = System.Action<XOAuth.Domain.XOAuthDictionary, XOAuth.Exceptions.XOAuthError?>;

namespace XOAuth.Flows
{
	public class XOAuth : XOAuthBase
	{
		public virtual bool ClientIdMandatory => true;
		public Func<NSUrl, DynamicRegistrar> OnBeforDynamicClientRegistration;

		protected IAuthorizerUI Authorizer { get; }

		protected bool HasUnexpiredAccessToken
		{
			get
			{
				if (string.IsNullOrEmpty(AccessToken))
					return false;
				if (AccessTokenExpiry.HasValue)
					return AccessTokenExpiry.Value.IsFutureDate();

				return ClientConfig.AccessTokenAssumeUnexpired;
			}
		}

		public XOAuth(XOAuthSettings settings) : base(settings)
		{
			Authorizer = new Authorizer(this);
		}


		public void Authorize(AuthCallback callback)
		{
			Authorize(null, callback);
		}

		public void Authorize(XOAuthDictionary parameters, AuthCallback callback)
		{
			if (IsAuthorizing)
			{
				callback(null, XOAuthError.AlreadyAuthorizing);
				return;
			}

			DidAuthorizeOrFail = callback;
			Logger?.Log("Starting authorization");

			TryToObtainAccessTokenIfNeeded(parameters, (successParams, error) =>
			{
				if (successParams != null)
					DidAuthorize(successParams);
				else if (error != null)
					DidFail(error);
				else
				{
					RegisterClientIfNeeded((json, regError) =>
					{
						if (error != null)
							DidFail(error);
						else
						{
							try
							{
								DoAuthorize(parameters);
							}
							catch (XOAuthException ex)
							{
								DidFail(ex.Error);
							}
						}
					});
				}
			});
		}

		public void AuthorizeEmbedded(object context, XOAuthDictionary parameters, AuthCallback callback)
		{
			if (IsAuthorizing)
			{
				callback(null, XOAuthError.AlreadyAuthorizing);
				return;
			}

			AuthConfig.AuthorizeEmbedded = true;
			AuthConfig.AuthorizeContext = context;
			Authorize(parameters, callback);
		}

		public NSUrl GetAuthorizeUrl(string redirect, string scope, XOAuthDictionary parameters)
		{
			var redirectUrl = redirect ?? ClientConfig.Redirect;
			if (string.IsNullOrEmpty(redirectUrl))
				throw new XOAuthException(XOAuthError.NoRedirectUrl);
			var request = CreateAuthorizeRequest(redirectUrl, scope, parameters);
			Context.RedirectUrl = redirectUrl;
			return request.AsUrl();
		}

		public NSUrl GetAuthorizeUrl(XOAuthDictionary parameters = null) => GetAuthorizeUrl(null, null, parameters);

		private void TryToObtainAccessTokenIfNeeded(XOAuthDictionary parameters, AuthCallback callback)
		{
			if (HasUnexpiredAccessToken)
			{
				Logger?.Log("Have an apparently unexpired access token");
				callback(new XOAuthDictionary(), null);
			}
			else
			{
				Logger?.Log("No access token, checking if a refresh token is available");
				DoRefreshToken(parameters, (successparams, error) =>
				{
					if (successparams != null)
						callback(successparams, null);
					else
					{
						XOAuthError? returnedError = null;
						if (error != null)
						{
							Logger?.Log($"Error refreshing token: {error}");
							switch (error)
							{
								case XOAuthError.NoRefreshToken:
								case XOAuthError.NoClientId:
								case XOAuthError.UnauthorizedClient:
									returnedError = null;
									break;
								default:
									returnedError = error;
									break;
							}
						}

						callback(null, returnedError);
					}
				});
			}
		}

		protected void DoRefreshToken(XOAuthDictionary parameters, AuthCallback callback)
		{
			try
			{
				var post = TokenRequestForTokenRefresh(parameters).AsUrlRequest(this);
				Logger?.Log($"Using refresh token to receive access token from {post.Url?.Description ?? "--null--"}");
				Perform(post, (response) =>
				{
					try
					{
						var data = response.ResponseJson();
						var json = ParseAccessTokenResponse(data);
						if (response.Response.StatusCode >= 400)
						{
							ClientConfig.RefreshToken = null;
							throw new XOAuthException(XOAuthError.Generic, $"Failed with status {response.Response.StatusCode}");
						}
						Logger?.Log($"Did use refresh token for access token [{ClientConfig.AccessToken}]");
						callback(json, null);
					}
					catch (XOAuthException ex)
					{
						Logger?.Log("$Error refreshing access token: {ex}");
						callback(null, ex.Error);
					}
				});
			}
			catch (XOAuthException ex)
			{
				callback(null, ex.Error);
			}
		}

		protected void DoAuthorize(XOAuthDictionary parameters)
		{
			if (AuthConfig.AuthorizeEmbedded)
				DoAuthorizeEmbedded(AuthConfig, parameters);
			else
				DoOpenAuthorizeUrlInBrowser(parameters);
		}

		protected void DoAuthorizeEmbedded(AuthConfig config, XOAuthDictionary parameters = null)
		{
			var url = GetAuthorizeUrl(parameters);
			Logger?.Log($"Opening authorize URL embedded: {url.Description}");
			Authorizer.AuthorizeEmbedded(config, url);
		}

		private void DoOpenAuthorizeUrlInBrowser(XOAuthDictionary parameters = null)
		{
			var url = GetAuthorizeUrl(parameters);
			Logger?.Log($"Opening authorize url in system browser: {url.Description}");
			Authorizer.OpenAuthorizeUrlInBrowser(url);
		}

		private XOAuthRequest CreateAuthorizeRequest(string redirect, string scope, XOAuthDictionary parameters)
		{
			if (ClientIdMandatory && (string.IsNullOrEmpty(ClientConfig.ClientId)))
				throw new XOAuthException(XOAuthError.NoClientId);

			var request = new XOAuthRequest(ClientConfig.AuthorizeUrl, HttpMethod.GET);
			request.RequestParams[RequestKey.RedirectUri] = redirect;
			request.RequestParams[RequestKey.State] = Context.State;
			request.RequestParams[RequestKey.ClientId] = ClientConfig.ClientId;
			request.RequestParams[RequestKey.ResponseType] = ResponseType;
			request.RequestParams[RequestKey.Scope] = scope ?? ClientConfig.Scope;
			request.RequestParams[RequestKey.Swa] = DateTime.Now.ToOADate().ToString();
			request.AddParams(parameters);

			return request;
		}

		private XOAuthRequest TokenRequestForTokenRefresh(XOAuthDictionary parameters = null)
		{
			var clientId = ClientConfig.ClientId;
			var refreshToken = ClientConfig.RefreshToken;
			if (ClientIdMandatory && string.IsNullOrEmpty(clientId))
				throw new XOAuthException(XOAuthError.NoClientId);
			if (string.IsNullOrEmpty(refreshToken))
				throw new XOAuthException(XOAuthError.NoRefreshToken);

			var request = new XOAuthRequest(ClientConfig.TokenUrl ?? ClientConfig.AuthorizeUrl);
			request.RequestParams[RequestKey.GrantType] = RequestKey.RefreshToken;
			request.RequestParams[RequestKey.RefreshToken] = refreshToken;
			request.RequestParams[RequestKey.ClientId] = clientId;
			request.AddParams(parameters);
			return request;
		}

		private void RegisterClientIfNeeded(AuthCallback callback)
		{
			if (!string.IsNullOrEmpty(ClientId) || ClientIdMandatory)
				NSApplication.SharedApplication.InvokeOnMainThread(() => callback(null, null));
			else if (ClientConfig.RegistrationUrl != null)
			{
				var dynReg = OnBeforDynamicClientRegistration?.Invoke(ClientConfig.RegistrationUrl) ?? new DynamicRegistrar();
				dynReg.Register(this, (json, error) => NSApplication.SharedApplication.InvokeOnMainThread(() => callback(json, error)));
			}
			else
				callback(null, XOAuthError.NoRegistrationUrl);
		}
	}
}
