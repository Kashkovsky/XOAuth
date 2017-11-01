using Foundation;
using XOAuth.Extensions;

namespace XOAuth.KeychainUtils
{
	public abstract class KeychainItemType
	{
		public string AccessMode { get; } = KSec.AttrAccessibleWhenUnlocked;
		public string AccessGroup { get; }

		public virtual NSMutableDictionary<NSString, NSObject> Attributes { get; } = new NSMutableDictionary<NSString, NSObject>();
		public NSMutableDictionary<NSString, NSObject> Data { get; set; }
		public virtual NSMutableDictionary<NSString, NSObject> DataToStore { get; }

		protected KeychainItemType(string accessMode, string accessGroup)
		{
			AccessMode = accessMode;
			AccessGroup = accessGroup;
		}

		public NSMutableDictionary<NSString, NSObject> AttributesToSave
		{
			get
			{
				var itemAttributes = Attributes;
				var archivedData = NSKeyedArchiver.ArchivedDataWithRootObject(DataToStore);
				itemAttributes[KSec.ValueData] = archivedData;
				if (!string.IsNullOrEmpty(AccessGroup))
					itemAttributes[KSec.AttrAccessGroup] = AccessGroup.ToNS();
				return itemAttributes;
			}
		}

		public NSMutableDictionary<NSString, NSObject> AttributesForFetch
		{
			get
			{
				var attributes = Attributes;
				attributes[KSec.ReturnData] = true.ToNS();
				attributes[KSec.ReturnAttributes] = true.ToNS();
				if (!string.IsNullOrEmpty(AccessGroup))
					attributes[KSec.AttrAccessGroup] = AccessGroup.ToNS();
				return attributes;
			}
		}

		protected NSMutableDictionary<NSString, NSObject> DataFromAttributes(NSMutableDictionary<NSString, NSObject> attributes)
		{
			var data = attributes[KSec.ValueData] as NSData;
			if (data == null) return null;
			var unarchived = NSKeyedUnarchiver.UnarchiveObject(data);
			var result = new NSMutableDictionary<NSString, NSObject>();
			foreach (var item in unarchived as NSDictionary)
			{
				result.Add(item.Key, item.Value);
			}
			return result;
		}

		public void SaveInKeychain(KeychainServiceType keychain = null)
		{
			if (keychain == null)
				keychain = new Keychain();

			keychain.InsertItemWithAttributes(AttributesToSave);
		}

		public void RemoveFromKeychain(KeychainServiceType keychain = null)
		{
			if (keychain == null)
				keychain = new Keychain();

			keychain.RemoveItemWithAttributes(Attributes);
		}

		public virtual KeychainItemType FetchFromKeychain(KeychainServiceType keychain = null)
		{
			if (keychain == null)
				keychain = new Keychain();

			var result = keychain.FetchItemWithAttributes(AttributesForFetch);
			if (result != null)
			{
				var itemData = DataFromAttributes(result);
				if (itemData != null)
					Data = itemData;
			}

			return this;
		}
	}
}