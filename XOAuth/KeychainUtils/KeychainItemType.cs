using Foundation;

namespace XOAuth.KeychainUtils
{
	public abstract class KeychainItemType
	{
		public string AccessMode { get; } = KSec.AttrAccessibleWhenUnlocked;
		public string AccessGroup { get; }

		public virtual NSMutableDictionary<NSString, NSObject> Attributes { get; }
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
				var itemAttributes = Attributes.Copy() as NSMutableDictionary<NSString, NSObject>;
				var archivedData = NSKeyedArchiver.ArchivedDataWithRootObject(DataToStore);
				itemAttributes[KSec.ValueData] = archivedData;
				if (!string.IsNullOrEmpty(AccessGroup))
					itemAttributes[KSec.AttrAccessGroup] = NSObject.FromObject(AccessGroup);
				return itemAttributes;
			}
		}

		public NSMutableDictionary<NSString, NSObject> AttributesForFetch
		{
			get
			{
				var attributes = Attributes.Copy() as NSMutableDictionary<NSString, NSObject>;
				attributes[KSec.ReturnData] = NSObject.FromObject(true);
				attributes[KSec.ReturnAttributes] = NSObject.FromObject(true);
				if (!string.IsNullOrEmpty(AccessGroup))
					attributes[KSec.AttrAccessGroup] = NSObject.FromObject(AccessGroup);
				return attributes;
			}
		}

		protected NSMutableDictionary<NSString, NSObject> DataFromAttributes(NSMutableDictionary<NSString, NSObject> attributes)
		{
			var data = attributes[KSec.ValueData] as NSData;
			if (data == null) return null;
			return NSKeyedUnarchiver.UnarchiveObject(data) as NSMutableDictionary<NSString, NSObject>;
		}

		public void SaveInKeychain(KeychainServiceType keychain = null)
		{
			if (keychain == null)
				keychain = new Keychain();

			keychain.InsertItemWithAttributes(Attributes);
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