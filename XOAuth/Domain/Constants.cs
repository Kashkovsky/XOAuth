namespace XOAuth.Domain
{
	public static class HttpMethod
	{
		public const string GET = "GET";
		public const string POST = "POST";
	}

	public static class HttpContentType
	{
		public const string Json = "application/json; charset=utf-8";
		public const string Form = "application/x-www-form-urlencoded; charset=utf-8";
	}

	public static class AuthMethod
	{
		public const string None = "none";
		public const string ClientSecretPost = "client_secret_post";
		public const string ClientSecretBasic = "client_secret_basic";
		public static bool IsValid(string methodName) =>
		methodName.Equals(None) || methodName.Equals(ClientSecretPost) || methodName.Equals(ClientSecretBasic);
	}

	public static class RequestKey
	{
		public const string ClientId = "client_id";
		public const string ClientSecret = "client_secret";
		public const string ClientName = "client_name";
		public const string RedirectUris = "redirect_uris";
		public const string LogoUri = "logo_uri";
		public const string Scope = "scope";
		public const string GrantTypes = "grant_types";
		public const string ResponseTypes = "response_types";
		public const string TokenEndpointAuthMethod = "token_endpoint_auth_method";
	}

	public static class ResponseKey
	{
		public const string AccessToken = "access_token";
		public const string IdToken = "id_token";
		public const string ExpiresIn = "expires_in";
		public const string RefreshToken = "refresh_token";
		public const string ErrorDescription = "error_description";
		public const string Error = "error";
		public const string TokenType = "token_type";
		public const string State = "state";
		public const string ClientSecretExpiresAt = "client_secret_expires_at";
	}
}
