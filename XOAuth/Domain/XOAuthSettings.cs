using System.Collections.Generic;

namespace XOAuth.Domain
{
	public struct XOAuthSettings
	{
		public string Title { get; set; }
		public string ClientId { get; set; }
		public string ClientSecret { get; set; }
		public string ClientName { get; set; }
		public string AuthorizeUri { get; set; }
		public string TokenUri { get; set; }
		public string RegistrationUri { get; set; }
		public string LogoUri { get; set; }
		public IEnumerable<string> RedirectUris { get; set; }
		public string Scope { get; set; }
		public bool Verbose { get; set; }
		public bool SecretInBody { get; set; }
		public XOAuthDictionary Headers { get; set; }
		public XOAuthDictionary Parameters { get; set; }
		public bool AccessTokenAssumeUnexpired { get; set; }

		public string KeychainAccountForClientCredentials { get; set; }
		public string KeychainAccountForTokens { get; set; }
		public bool? UseKeychain { get; set; }
		public string KeychainAccessMode { get; set; }
		public string KeychainAccessGroup { get; set; }
	}
}
