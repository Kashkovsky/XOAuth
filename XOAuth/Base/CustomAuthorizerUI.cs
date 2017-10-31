using Foundation;

namespace XOAuth.Base
{
	public interface ICustomAuthorizerUI
	{
		void Present(NSObject loginController, NSObject context, bool animated);
		void DismissLoginController(bool animated);
	}

	public interface IAuthorizerUI
	{
		XOAuthBase OAuth { get; }
		void OpenAuthorizeUrlInBrowser(NSUrl url);
		void AuthorizeEmbedded(AuthConfig config, NSUrl url);
	}
}
