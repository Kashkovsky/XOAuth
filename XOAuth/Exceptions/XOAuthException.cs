﻿using System;
using System.Diagnostics;

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
			Debug.WriteLine($"[ERROR] Exception thrown: {error}, {message}");
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
		NoRefreshToken,
		ResponseError,
		FromResponseError,
		UnsupportedTokenType,
		NoTokenType,
		MissingState,
		InvalidState,
		Utf8EncodeError,
		DynamicRegistrarError,
		NoRegistrationUrl,
		UnableToOpenAuthorizeURL,
		NoClientId,
		NoRedirectUrl,
		AlreadyAuthorizing,
		Generic,
		InvalidRedirectUrl,
		PrerequisiteFailed
	}
}
