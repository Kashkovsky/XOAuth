using System.Collections.Generic;
using Foundation;

namespace XOAuth.KeychainUtils
{
	public abstract class KeychainServiceType
	{
		public abstract void InsertItemWithAttributes(NSMutableDictionary<NSString, NSObject> attributes);
		public abstract void RemoveItemWithAttributes(NSMutableDictionary<NSString, NSObject> attributes);
		public abstract NSMutableDictionary<NSString, NSObject> FetchItemWithAttributes(NSMutableDictionary<NSString, NSObject> attributes);
	}
}
