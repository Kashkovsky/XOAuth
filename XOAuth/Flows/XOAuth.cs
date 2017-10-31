using System;
using Foundation;
using XOAuth.Base;
using XOAuth.Domain;

namespace XOAuth.Flows
{
	public class XOAuth : XOAuthBase
	{
		private IAuthorizerUI _authorizer;

		public virtual bool ClientIdMandatory => true;
		public Func<NSUrl, DynamicRegistrar> OnBeforDynamicClientRegistration;

		public XOAuth(XOAuthSettings settings) : base(settings)
		{
		}

		//public Func<NSUrl>

		protected override void HandleRedirectUrl(NSUrl redirect)
		{
			throw new NotImplementedException(); //TODO where's impl?

		}
	}
}
