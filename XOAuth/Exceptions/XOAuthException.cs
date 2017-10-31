using System;
namespace XOAuth.Exceptions
{
	public class XOAuthException : Exception
	{
		public int? StatusCode { get; }
		public XOAuthError? Error { get; }
		public string Fallback { get; set; }

		public XOAuthException(XOAuthError error, string message = null, string fallback = null) : base(message ?? error.ToString())
		{
			Error = error;
			Fallback = fallback;
		}

		public XOAuthException(int statusCode) : base(statusCode.ToString())
		{
			StatusCode = statusCode;
		}

		public override string ToString()
		{
			return string.Format("[XOAuthException: StatusCode={0}] {1}", StatusCode, Message);
		}
	}

	public enum XOAuthError
	{
		NotUsingTls,
		InvalidUrlComponents,
		RequestCancelled,
		UnauthorizedClient,
		Forbidden,
		NoDataInResponse,
		NoAccessToken,
		ResponseError,
		FromResponseError,
		UnsupportedTokenType,
		NoTokenType,
		MissingState,
		InvalidState,
		Utf8EncodeError,
		DynamicRegistrarError,
		NoRegistrationUrl,
		UnableToOpenAuthorizeURL
	}
}
