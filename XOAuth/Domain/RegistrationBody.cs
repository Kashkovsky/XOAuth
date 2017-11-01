using System.Linq;
using Newtonsoft.Json;
using XOAuth.Base;

namespace XOAuth.Domain
{
	public class RegistrationBody
	{
		[JsonProperty(RequestKey.ClientName)]
		public string ClientName { get; set; }

		[JsonProperty(RequestKey.RedirectUris)]
		public string[] RedirectUris { get; set; }

		[JsonProperty(RequestKey.LogoUri)]
		public string LogoUri { get; set; }

		[JsonProperty(RequestKey.Scope)]
		public string Scope { get; set; }

		[JsonProperty(RequestKey.GrantTypes)]
		public string[] GrantTypes { get; set; }

		[JsonProperty(RequestKey.ResponseTypes)]
		public string[] ResponseTypes { get; set; }

		[JsonProperty(RequestKey.TokenEndpointAuthMethod)]
		public string TokenEndpointAuthMethod { get; set; } //TODO: raw value int ???

		public RegistrationBody(ClientConfig config, string grantType, string responseType, bool allowRefreshTokens)
		{
			ClientName = config.ClientName;
			RedirectUris = config.RedirectUrls.ToArray();
			LogoUri = config.LogoUrl.AbsoluteString;
			Scope = config.Scope;

			GrantTypes = new string[] { grantType };
			if (allowRefreshTokens)
				GrantTypes.Append(RequestKey.RefreshToken);

			if (!string.IsNullOrEmpty(responseType))
				ResponseTypes = new string[] { responseType };

			TokenEndpointAuthMethod = config.EndpointAuthMethod; //TODO raw value??
		}

		public string AsJson() => JsonConvert.SerializeObject(this, Formatting.None,
											   new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
	}
}
