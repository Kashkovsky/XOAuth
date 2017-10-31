using System;
using Foundation;
using XOAuth.Base;

namespace XOAuth.KeychainUtils
{
	public class KeychainAccount : KeychainGenericPasswordType
	{
		public override NSMutableDictionary<NSString, NSObject> DataToStore => Data;

		public KeychainAccount(Securable oauth, string account, NSMutableDictionary<NSString, NSObject> data = null)
			: base(oauth.KeychainServiceName, account, oauth.KeychainAccessMode, oauth.KeychainAccessGroup)
		{
			if (data == null)
				data = new NSMutableDictionary<NSString, NSObject>();
			Data = data;
		}
	}
}
