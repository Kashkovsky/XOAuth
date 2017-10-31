using System;
using AppKit;
using Foundation;
using XOAuth.Base;
using XOAuth.Exceptions;
using XOAuth.Extensions;

namespace XOAuth.Platform
{
	public class Authorizer : IAuthorizerUI
	{
		public XOAuthBase OAuth { get; }
		private NSWindowController _windowController;

		public Authorizer(XOAuthBase oauth)
		{
			OAuth = oauth;
		}

		public NSWindow AuthorizeEmbedded(NSWindow window, NSUrl url)
		{
			var controller = PresentableAuthorizeViewController(url);
		}

		public void AuthorizeEmbedded(AuthConfig config, NSUrl url)
		{
			var window = config.AuthorizeContext as NSWindow;
			if (window != null)
			{
				var sheet = AuthorizeEmbedded(window, url);
			}
		}

		public void OpenAuthorizeUrlInBrowser(NSUrl url)
		{
			if (!NSWorkspace.SharedWorkspace.OpenUrl(url))
				throw new XOAuthException(XOAuthError.UnableToOpenAuthorizeURL, url.AbsoluteString);
		}

		private WebViewController PresentableAuthorizeViewController(NSUrl url)
		{
			var controller = new WebViewController();
			controller.StartUrl = url;
			controller.InterceptUrlString = OAuth.Redirect?.ToNS();
			controller.OnIntercept = (iUrl) =>
			{
				try
				{
					OAuth.HandleRedirectUrl(iUrl);
					return true;
				}
				catch (Exception ex)
				{
					OAuth.Logger?.Log($"Cannot intecept redirect url {ex.Message}");
				}

				return false;
			};

			controller.OnWillCancel = () => OAuth.DidFail(null);

			return controller;
		}
	}
}
