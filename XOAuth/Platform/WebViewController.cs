using System;
using AppKit;
using CoreGraphics;
using Foundation;
using WebKit;
using XOAuth.Base;
using XOAuth.Extensions;

namespace XOAuth.Platform
{
	public class WebViewController : NSViewController, INSWindowDelegate, IWKNavigationDelegate
	{
		private const float WebViewWindowWidth = 600f;
		private const float WebViewWindowHeight = 500f;

		private XOAuthBase _oauth;
		private bool _willBecomeSheet;

		private NSUrl _startUrl;
		private NSUrlComponents _interceptcomponents;
		private NSString _interceptUrlString;

		private WKWebView _webView;
		private NSProgressIndicator _progressIndicator;

		private NSView LoadingView
		{
			get
			{
				var view = new NSView(View.Bounds);
				view.TranslatesAutoresizingMaskIntoConstraints = false;
				_progressIndicator = new NSProgressIndicator(new CGRect(0f, 0f, 0f, 0f))
				{
					Style = NSProgressIndicatorStyle.Spinning,
					IsDisplayedWhenStopped = false
				};

				_progressIndicator.SizeToFit();
				_progressIndicator.TranslatesAutoresizingMaskIntoConstraints = false;
				view.AddSubviewToCenter(_progressIndicator);

				return view;
			}
		}

		public NSUrl StartUrl
		{
			get { return _startUrl; }
			set
			{
				if (value != null && _startUrl == null && ViewLoaded)
					LoadUrl(value);

				_startUrl = value;
			}
		}

		public NSString InterceptUrlString
		{
			get { return _interceptUrlString; }
			set
			{
				_interceptUrlString = value;
				if (_interceptUrlString != null)
				{
					var url = new NSUrl(_interceptUrlString);
					if (url != null)
						_interceptcomponents = new NSUrlComponents(url, true);
					else
					{
						_oauth?.Logger?.Log($"Failed to parse URL {_interceptUrlString}, discarding");
						_interceptUrlString = null;
					}
				}
				else
				{
					_interceptcomponents = null;
				}
			}
		}

		public Func<NSUrl, bool> OnIntercept { get; set; }
		public Action OnWillCancel { get; set; }

		public WebViewController() : base(null, null)
		{
		}

		public WebViewController(NSCoder coder) : base(coder)
		{
		}

		public override void LoadView()
		{
			View = new NSView(new CGRect(0f, 0f, WebViewWindowWidth, WebViewWindowHeight));
			View.TranslatesAutoresizingMaskIntoConstraints = false;
			_webView = new WKWebView(View.Bounds, new WKWebViewConfiguration());
			_webView.TranslatesAutoresizingMaskIntoConstraints = false;
			_webView.NavigationDelegate = this;
			_webView.AlphaValue = 0f;

			View.AddSubviewFullSize(_webView);

			if (_willBecomeSheet)
			{
				var btn = new NSButton(new CGRect(0f, 0f, 120f, 20f))
				{
					TranslatesAutoresizingMaskIntoConstraints = false,
					Title = "Cancel",
					Target = this,
					Action = new ObjCRuntime.Selector("cancel:")
				};

				View.AddSubview(btn);
				View.AddConstraint(NSLayoutConstraint.Create(btn, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, View, NSLayoutAttribute.Trailing, 1f, -10f));
				View.AddConstraint(NSLayoutConstraint.Create(btn, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, View, NSLayoutAttribute.Bottom, 1f, -10f));
			}

			ShowLoadingIndicator();
		}

		public override void ViewWillAppear()
		{
			base.ViewWillAppear();
			if (!_webView.CanGoBack)
			{
				if (StartUrl != null)
					LoadUrl(StartUrl);
				else
					_webView.LoadHtmlString("There is no 'start URL'", null);
			}
		}

		public override void ViewDidAppear()
		{
			base.ViewDidAppear();
			if (View.Window != null)
			{
				View.Window.Delegate = this;
			}
		}

		public void LoadUrl(NSUrl url)
		{
			_webView.LoadRequest(new NSUrlRequest(url));
		}

		public void GoBack(NSObject sender = null)
		{
			_webView.GoBack();
		}

		[Export("cancel:")]
		public void Cancel(NSObject sender = null)
		{
			_webView.StopLoading();
			OnWillCancel?.Invoke();
		}

		#region IWKNavigationDelegate inteface implementation
		[Export("webView:decidePolicyForNavigationAction:decisionHandler:")]
		public void DecidePolicy(WKWebView webView, WKNavigationAction navigationAction, Action<WKNavigationActionPolicy> decisionHandler)
		{
			var request = navigationAction.Request;
			if (OnIntercept == null)
			{
				decisionHandler(WKNavigationActionPolicy.Allow);
				return;
			}

			var url = request.Url;
			if (url != null && url.Scheme == _interceptcomponents?.Scheme && url.Host == _interceptcomponents?.Host)
			{
				var haveComponents = new NSUrlComponents(url, true);
				var hp = haveComponents?.Path;
				var ip = _interceptcomponents?.Path;

				if (hp != null && ip != null && (hp.Equals(ip, StringComparison.OrdinalIgnoreCase) || ("/".Equals(hp + ip))))
				{
					if (OnIntercept(url))
						decisionHandler(WKNavigationActionPolicy.Cancel);
					else
						decisionHandler(WKNavigationActionPolicy.Allow);

					return;
				}
			}

			decisionHandler(WKNavigationActionPolicy.Allow);
		}

		[Export("webView:didFinishNavigation:")]
		public void DidFinishNavigation(WKWebView webView, WKNavigation navigation)
		{
			const string scheme = "urn";
			const string pathPrefix = "ietf:wg:oauth:2.0:oob";
			if (_interceptcomponents?.Scheme == scheme)
			{
				var path = _interceptcomponents?.Path;
				if (!string.IsNullOrEmpty(path) && path.StartsWith(pathPrefix, StringComparison.OrdinalIgnoreCase))
				{
					var title = _webView.Title;
					if (!string.IsNullOrEmpty(title) && title.StartsWith("Success ", StringComparison.OrdinalIgnoreCase))
					{
						_oauth?.Logger?.Log("Creating redirect url from document.title");
						var qry = title.Replace("Success ", "");
						var url = new NSUrl($"http://localhost/?{qry}");
						if (url != null)
						{
							OnIntercept?.Invoke(url);
							return;
						}

						_oauth?.Logger?.Log($"Failed to create a URL with query parts {qry}", Domain.LogLevel.Error);
					}
				}
			}

			_webView.AlphaValue = 1f;
			HideLoadingIndicator();
		}

		[Export("webView:didFailNavigation:withError:")]
		public void DidFailNavigation(WKWebView webView, WKNavigation navigation, NSError error)
		{
			if (error.Domain.Equals(NSError.NSUrlErrorDomain) && error.Code == -999)
				return;
			// TODO do we still need to intercept "WebKitErrorDomain" error 102?

			ShowErrorMessage(error.LocalizedDescription);
		}
		#endregion

		#region INSWindowDelegate interface implementation
		[Export("windowShouldClose:")]
		public bool WindowShouldClose(NSObject sender)
		{
			OnWillCancel?.Invoke();
			return false;
		}
		#endregion

		private void ShowLoadingIndicator()
		{
			View.AddSubviewFullSize(LoadingView);
			_progressIndicator.StartAnimation(null);
		}

		private void HideLoadingIndicator()
		{
			if (_progressIndicator == null)
				return;
			_progressIndicator.StopAnimation(null);
			_progressIndicator.Superview?.RemoveFromSuperview();
		}

		private void ShowErrorMessage(string message)
		{
			HideLoadingIndicator();
			_webView.AlphaValue = 1f;
			_webView.LoadHtmlString($"<p style=\"text-align:center;font:'helvetica neue', sans-serif;color:red\">{message}</p>", null);
		}
	}
}
