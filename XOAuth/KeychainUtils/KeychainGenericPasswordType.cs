using Foundation;
using XOAuth.Exceptions;
using XOAuth.Extensions;

namespace XOAuth.KeychainUtils
{
	public abstract class KeychainGenericPasswordType : KeychainItemType
	{
		public string ServiceName { get; } = "XOAuth.Keychain.Service";
		public string AccountName { get; }

		public KeychainGenericPasswordType(string serviceName, string accountName, string accessMode, string accessGroup)
			: base(accessMode, accessGroup)
		{
			ServiceName = serviceName;
			AccountName = accountName;
		}

		public override NSMutableDictionary<NSString, NSObject> Attributes
		{
			get
			{
				return new NSMutableDictionary<NSString, NSObject>(
					new[] { KSec.Class, KSec.AttrAccessible, KSec.AttrService, KSec.AttrAccount },
					new[] { KSec.ClassGenericPassword, AccessMode.ToNS(), ServiceName.ToNS(), AccountName.ToNS() });
			}
		}

		public NSMutableDictionary<NSString, NSObject> FetchedFromKeychain()
		{
			try
			{
				FetchFromKeychain();
				return Data;
			}
			catch (XOAuthException ex)
			{
				if (ex.StatusCode.HasValue && ex.StatusCode.Value == KSec.ErrSecItemNotFound)
				{
					return new NSMutableDictionary<NSString, NSObject>();
				}

				throw ex;
			}
		}
	}
}
