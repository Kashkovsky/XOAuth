using System.Collections.Generic;
using Foundation;
using Newtonsoft.Json;

namespace XOAuth.Domain
{
	[JsonDictionary]
	public class XOAuthDictionary : Dictionary<string, string>
	{
		public XOAuthDictionary()
		{
		}

		public XOAuthDictionary(int capacity) : base(capacity)
		{
		}

		public bool HasNonEmptyValue(string key, out string value)
		{
			value = null;
			if (ContainsKey(key) && !string.IsNullOrEmpty(this[key]))
			{
				value = this[key];
				return true;
			}

			return false;
		}

		public void SafeAdd(string key, string value)
		{
			if (!string.IsNullOrEmpty(value))
				this[key] = value;
		}

		public XOAuthDictionary Clone()
		{
			var clone = new XOAuthDictionary(this.Count);
			foreach (var kvp in this)
			{
				clone.Add(kvp.Key, (string)kvp.Value.Clone());
			}

			return clone;
		}

		public Dictionary<string, TValue> Clone<TValue>() where TValue : class
		{
			var clone = new Dictionary<string, TValue>(this.Count);
			foreach (var kvp in this)
			{
				clone.Add(kvp.Key, kvp.Value.Clone() as TValue);
			}

			return clone;
		}

		public static XOAuthDictionary FromNSMutableDictionary(NSMutableDictionary<NSString, NSObject> nsDict)
		{
			var result = new XOAuthDictionary((int)nsDict.Count);
			foreach (var item in nsDict)
			{
				result.Add(item.Key.ToString(), item.Value.ToString());
			}

			return result;
		}

		public override string ToString() => JsonConvert.SerializeObject(this);
	}
}