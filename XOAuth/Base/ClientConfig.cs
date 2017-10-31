using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Foundation;
using XOAuth.Domain;
using XOAuth.Extensions;

namespace XOAuth.Base
{
	public class ClientConfig
	{
		public string ClientId { get; set; }
		public string ClientSecret { get; set; }
		public string ClientName { get; set; }
		public NSUrl AuthorizeUrl { get; set; }
		public NSUrl TokenUrl { get; set; }
		public NSUrl LogoUrl { get; set; }
		public string Scope { get; set; }
		public string Redirect { get; set; }
		public IEnumerable<string> RedirectUrls { get; set; }
		public string AccessToken { get; set; }
		public string IdToken { get; set; }
		public DateTime? AccessTokenExpiry { get; set; }
		public bool AccessTokenAssumeUnexpired { get; set; } = true;
		public string RefreshToken { get; set; }
		public NSUrl RegistrationUrl { get; set; }
		public bool SecretInBody { get; set; } = false;
		public string EndpointAuthMethod { get; set; } = AuthMethod.None;
		public XOAuthDictionary AuthHeaders { get; set; }
		public XOAuthDictionary CustomParameters { get; set; }
		public Encoding AuthStringEncoding { get; set; } = Encoding.UTF8;
		public bool SafariCancelWorkaround { get; set; }

		public NSMutableDictionary<NSString, NSObject> StorableCredentialItems
		{
			get
			{
				if (string.IsNullOrEmpty(ClientId)) return null;

				var items = new NSMutableDictionary<NSString, NSObject>(nameof(ClientId).ToNS(), ClientId.ToNS());
				if (!string.IsNullOrEmpty(ClientSecret))
					items.Add(nameof(ClientSecret).ToNS(), ClientSecret.ToNS());
				if (!string.IsNullOrEmpty(EndpointAuthMethod))
					items.Add(nameof(EndpointAuthMethod).ToNS(), EndpointAuthMethod.ToNS());

				return items;
			}
		}

		public NSMutableDictionary<NSString, NSObject> StorableTokenItems
		{
			get
			{
				if (string.IsNullOrEmpty(AccessToken))
					return null;
				var items = new NSMutableDictionary<NSString, NSObject>(nameof(AccessToken).ToNS(), AccessToken.ToNS());

				if (AccessTokenExpiry.HasValue && AccessTokenExpiry.Value.IsFutureDate())
					items.Add(nameof(AccessTokenExpiry).ToNS(), AccessTokenExpiry.Value.ToString().ToNS());
				if (!string.IsNullOrEmpty(RefreshToken))
					items.Add(nameof(RefreshToken).ToNS(), RefreshToken.ToNS());
				if (!string.IsNullOrEmpty(IdToken))
					items.Add(nameof(IdToken).ToNS(), IdToken.ToNS());

				return items;
			}
		}

		public ClientConfig(XOAuthSettings settings)
		{
			ClientId = settings.ClientId;
			ClientSecret = settings.ClientSecret;
			ClientName = settings.ClientName;

			AuthorizeUrl = !string.IsNullOrEmpty(settings.AuthorizeUri)
								  ? new NSUrl(settings.AuthorizeUri)
								  : new NSUrl("https://localhost/XOAuth.defaultAuthorizeUri"); //TODO: ?

			if (!string.IsNullOrEmpty(settings.TokenUri))
				TokenUrl = new NSUrl(settings.TokenUri);
			if (!string.IsNullOrEmpty(settings.RegistrationUri))
				TokenUrl = new NSUrl(settings.RegistrationUri);
			if (!string.IsNullOrEmpty(settings.LogoUri))
				TokenUrl = new NSUrl(settings.LogoUri);

			Scope = settings.Scope;
			RedirectUrls = settings.RedirectUris;
			Redirect = RedirectUrls?.FirstOrDefault();
			SecretInBody = settings.SecretInBody;
			if (SecretInBody)
				EndpointAuthMethod = AuthMethod.ClientSecretPost;
			else if (!string.IsNullOrEmpty(ClientSecret))
				EndpointAuthMethod = AuthMethod.ClientSecretBasic;
			AuthHeaders = settings.Headers;
			CustomParameters = settings.Parameters;
			AccessTokenAssumeUnexpired = settings.AccessTokenAssumeUnexpired;
		}

		public void UpdateFromResponse(XOAuthDictionary json)
		{
			if (json.HasNonEmptyValue(ResponseKey.AccessToken, out var at))
				AccessToken = at;
			if (json.HasNonEmptyValue(ResponseKey.IdToken, out var idt))
				IdToken = idt;
			AccessTokenExpiry = null;
			if (json.HasNonEmptyValue(ResponseKey.ExpiresIn, out var ei))
				AccessTokenExpiry = DateTime.Parse(ei); //TODO: if int?
			if (json.HasNonEmptyValue(ResponseKey.RefreshToken, out var rt))
				RefreshToken = rt;
		}

		public IEnumerable<string> UpdateFromStorableItems(XOAuthDictionary items)
		{
			var messages = new List<string>();

			if (items.HasNonEmptyValue(nameof(ClientId), out var ci))
			{
				ClientId = ci;
				messages.Add("Found client id");
			}

			if (items.HasNonEmptyValue(nameof(ClientSecret), out var cs))
			{
				ClientSecret = cs;
				messages.Add("Found client secret");
			}

			if (items.HasNonEmptyValue(nameof(EndpointAuthMethod), out var eam))
			{
				EndpointAuthMethod = eam;
				messages.Add("Found auth method");
			}

			if (items.HasNonEmptyValue(nameof(AccessToken), out var at))
			{
				if (items.HasNonEmptyValue(nameof(AccessTokenExpiry), out var ate))
				{
					var date = DateTime.Parse(ate);
					if (DateTime.Compare(date, DateTime.Now) > 0)
					{
						messages.Add($"Found access token valid until {date}");
						AccessTokenExpiry = date;
						AccessToken = at;
					}
					else
						messages.Add("Found access token but it seems to be xpired");
				}
				else if (AccessTokenAssumeUnexpired)
				{
					messages.Add("Found access token but no expiration date, " +
								 "assuming unexpired (set 'AccessTokenAssumeUnexpired' " +
								 "to false to discard)");
					AccessToken = at;
				}
				else
				{
					messages.Add("Found access token, but no expiration date, " +
								 "discarding (set 'AccessTokenAssumeUnexpired' " +
								 "to true to still use it)");
				}
			}

			if (items.HasNonEmptyValue(nameof(RefreshToken), out var rt))
			{
				messages.Add("Found refresh token");
				RefreshToken = rt;
			}

			if (items.HasNonEmptyValue(nameof(IdToken), out var it))
			{
				messages.Add("Found id token");
				IdToken = it;
			}

			return messages;
		}

		public void ForgetCredentials()
		{
			ClientId = null;
			ClientSecret = null;
		}

		public void ForgetTokens()
		{
			AccessToken = null;
			AccessTokenExpiry = null;
			RefreshToken = null;
			IdToken = null;
		}
	}
}