using Foundation;
using XOAuth.Domain;
using XOAuth.Exceptions;
using XOAuth.Extensions;

namespace XOAuth.Base
{
	public class XOAuthResponse
	{
		public NSData Data { get; }
		public NSUrlRequest Request { get; }
		public NSHttpUrlResponse Response { get; }
		public NSError Error { get; }

		public XOAuthResponse(NSUrlRequest request, NSHttpUrlResponse response, NSData data = null, NSError error = null)
		{
			Data = data;
			Request = request;
			Response = response;
			Error = error;
		}

		public NSData ResponseData()
		{
			if (Error != null && Error.Code == -999)
				throw new XOAuthException(XOAuthError.RequestCancelled);
			else if (Response.StatusCode == 401)
				throw new XOAuthException(XOAuthError.UnauthorizedClient);
			else if (Response.StatusCode == 403)
				throw new XOAuthException(XOAuthError.Forbidden);
			else if (Data.RetainCount > 0)
				return Data;
			else throw new XOAuthException(XOAuthError.NoDataInResponse);
		}

		public XOAuthDictionary ResponseJson()
		{
			var data = ResponseData();
			return data.ReadAs<XOAuthDictionary>();
		}
	}
}
