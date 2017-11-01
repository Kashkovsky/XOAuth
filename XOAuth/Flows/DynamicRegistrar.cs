using System;
using Foundation;
using XOAuth.Domain;
using XOAuth.Exceptions;
using XOAuth.Extensions;

namespace XOAuth.Flows
{
	public class DynamicRegistrar
	{
		public XOAuthDictionary ExtraHeaders { get; set; }
		public bool AllowRefreshTokens { get; set; } = true;

		public void Register(XOAuth client, Action<XOAuthDictionary, XOAuthError> callback)
		{
			try
			{
				var request = CreateRegistrationRequest(client);
				client.Logger?.Log($"Registering client at {request.Url.AbsoluteString} with scopes '{client.Scope ?? "(none)"}'");
				client.Perform(request, (response) =>
				{
					try
					{
						var data = response.ResponseJson();
						client.AssureNoErrorInResponse(data);
						if (response.Response.StatusCode >= 400)
						{
							client.Logger?.Log($"Registration failed with {response.Response.StatusCode}");
						}
						else
							DidRegisterWith(data, client);
					}
					catch (XOAuthException ex)
					{
						callback(null, ex.Error ?? XOAuthError.DynamicRegistrarError);
					}
				});
			}
			catch (XOAuthException ex)
			{
				callback(null, ex.Error ?? XOAuthError.DynamicRegistrarError);
			}
		}

		private NSUrlRequest CreateRegistrationRequest(XOAuth client)
		{
			var registrationUrl = client.ClientConfig.RegistrationUrl;
			if (registrationUrl == null)
				throw new XOAuthException(XOAuthError.NoRegistrationUrl);

			var request = new NSMutableUrlRequest(registrationUrl);
			request.HttpMethod = HttpMethod.POST;
			request.Headers["Content - Type"] = HttpContentType.Json.ToNS();
			request.Headers["Accept"] = "application/json".ToNS();

			if (ExtraHeaders != null)
			{
				foreach (var header in ExtraHeaders)
				{
					request.Headers[header.Key] = header.Value.ToNS();
				}
			}

			var body = new RegistrationBody(client.ClientConfig, client.GrantType, client.ResponseType, AllowRefreshTokens);
			request.Body = NSData.FromString(body.AsJson());
			return request;
		}

		private void DidRegisterWith(XOAuthDictionary json, XOAuth client)
		{
			if (json.HasNonEmptyValue(RequestKey.ClientId, out var clientId))
			{
				client.ClientId = clientId;
				client.Logger?.Log($"Did register with client id {client.ClientId}, params: {json}");
			}
			else
				client.Logger?.Log($"Did register but not get client id, params: {json}");

			if (json.HasNonEmptyValue(RequestKey.ClientSecret, out var secret))
			{
				client.ClientSecret = secret;

				if (json.HasNonEmptyValue(RequestKey.ClientSecretExpiresAt, out var value) &&
					Double.TryParse(value, out var clientSecretExpiresAt) &&
					clientSecretExpiresAt > 0)
					client.Logger?.Log($"Client secret will expire on {DateTime.FromOADate(clientSecretExpiresAt)}");
			}

			if (json.HasNonEmptyValue(RequestKey.TokenEndpointAuthMethod, out var methodName) && AuthMethod.IsValid(methodName))
				client.ClientConfig.EndpointAuthMethod = methodName;

			if (client.UseKeychain)
				client.StoreClientToKeychain();
		}
	}
}