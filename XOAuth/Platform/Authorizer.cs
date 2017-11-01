using System;
using AppKit;
using CoreGraphics;
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
			var controller = CreatePresentableAuthorizeViewController(url);
			controller.WillBecomeSheet = true;
			var sheet = CreateWindowController(controller, OAuth.AuthConfig).Window;

			window.MakeKeyAndOrderFront(null);
			window.BeginSheet(sheet, null);

			return sheet;
		}

		public void AuthorizeEmbedded(AuthConfig config, NSUrl url)
		{
			NSApplication.SharedApplication.InvokeOnMainThread(() =>
			{
				var window = config.AuthorizeContext as NSWindow;
				if (window != null)
				{
					var sheet = AuthorizeEmbedded(window, url);
					if (config.AuthorizeEmbeddedAutoDismiss)
						OAuth.InternalAfterAuthorizeOrFail = (wasFailure, error) => window.EndSheet(sheet);
				}
				else
				{
					_windowController = AuthorizeInNewWindow(url);
					if (config.AuthorizeEmbeddedAutoDismiss)
						OAuth.InternalAfterAuthorizeOrFail = (wasFailure, error) =>
						{
							_windowController?.Window?.Close();
							_windowController.Dispose();
							_windowController = null;
						};
				}
			});
		}

		public void OpenAuthorizeUrlInBrowser(NSUrl url)
		{
			if (!NSWorkspace.SharedWorkspace.OpenUrl(url))
				throw new XOAuthException(XOAuthError.UnableToOpenAuthorizeURL, url.AbsoluteString);
		}

		private WebViewController CreatePresentableAuthorizeViewController(NSUrl url)
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

		private NSWindowController CreateWindowController(WebViewController vc, AuthConfig config)
		{
			var rect = new CGRect(0f, 0f, WebViewController.WebViewWindowWidth, WebViewController.WebViewWindowHeight);
			var styleMask = NSWindowStyle.Titled | NSWindowStyle.Closable | NSWindowStyle.Resizable | NSWindowStyle.FullSizeContentView;
			var window = new NSWindow(rect, styleMask, NSBackingStore.Buffered, false)
			{
				BackgroundColor = NSColor.White,
				IsMovable = true,
				TitlebarAppearsTransparent = true,
				TitleVisibility = NSWindowTitleVisibility.Hidden,
				AnimationBehavior = NSWindowAnimationBehavior.AlertPanel
			};

			if (!string.IsNullOrEmpty(config.UI.Title))
				window.Title = config.UI.Title;

			var windowController = new NSWindowController(window);
			windowController.ContentViewController = vc;

			return windowController;
		}

		private NSWindowController AuthorizeInNewWindow(NSUrl url)
		{
			var vc = CreatePresentableAuthorizeViewController(url);
			var wc = CreateWindowController(vc, OAuth.AuthConfig);

			wc.Window?.Center();
			wc.ShowWindow(null);

			return wc;
		}
	}
}
