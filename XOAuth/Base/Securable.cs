using Foundation;
using XOAuth.Domain;
using XOAuth.Exceptions;
using XOAuth.KeychainUtils;

namespace XOAuth.Base
{
	public abstract class Securable : Requestable
	{
		private XOAuthSettings _settings;
		private bool _useKeychain = true;
		private string _keychainAccountForTokens = "currentTokens";
		private string _keychainAccountForClientCredentials = "clientCredentials";

		public string KeychainAccessMode { get; } = KSec.AttrAccessibleWhenUnlocked;
		public string KeychainAccessGroup { get; }
		public string KeychainServiceName { get; } = "http://localhost";

		public bool UseKeychain
		{
			get { return _useKeychain; }
			set
			{
				_useKeychain = value;
				UpdateFromKeychain();
			}
		}

		protected string KeychainAccountForTokens
		{
			get { return _keychainAccountForTokens; }
			set
			{
				_keychainAccountForTokens = value;
				UpdateFromKeychain();
			}
		}

		protected string KeychainAccountForClientCredentials
		{
			get { return _keychainAccountForClientCredentials; }
			set
			{
				_keychainAccountForClientCredentials = value;
				UpdateFromKeychain();
			}
		}

		public Securable(XOAuthSettings settings, bool verbose) : base(verbose)
		{
			_settings = settings;
			if (!string.IsNullOrEmpty(settings.KeychainAccountForClientCredentials))
				_keychainAccountForClientCredentials = settings.KeychainAccountForClientCredentials;
			if (!string.IsNullOrEmpty(settings.KeychainAccountForTokens))
				_keychainAccountForTokens = settings.KeychainAccountForTokens;
			if (settings.UseKeychain.HasValue)
				_useKeychain = settings.UseKeychain.Value;
			if (!string.IsNullOrEmpty(settings.KeychainAccessMode))
				KeychainAccessMode = settings.KeychainAccessMode;
			if (!string.IsNullOrEmpty(settings.KeychainAccessGroup))
				KeychainAccessGroup = settings.KeychainAccessGroup;

			if (_useKeychain)
				UpdateFromKeychain();
		}

		public Securable(XOAuthSettings settins) : this(settins, false)
		{
		}

		protected abstract void UpdateFromKeychainItems(NSMutableDictionary<NSString, NSObject> items);

		protected virtual NSMutableDictionary<NSString, NSObject> StorableCredentialItems() => null;
		protected virtual NSMutableDictionary<NSString, NSObject> StorableTokenItems() => null;

		public void StoreClientToKeychain()
		{
			var items = StorableCredentialItems();
			if (items != null)
			{
				Logger?.Log("Storing client credentials to keychain");
				StoreToKeychain(KeychainAccountForClientCredentials, items);
			}
		}

		public void StoreTokensToKeychain()
		{
			var items = StorableTokenItems();
			if (items != null)
			{
				Logger?.Log("Storing client tokens to keychain");
				StoreToKeychain(KeychainAccountForTokens, items);
			}
		}

		public virtual void ForgetClient()
		{
			Logger?.Log("Forgetting client credentials from keychain");
			var keychain = new KeychainAccount(this, KeychainAccountForClientCredentials);
			try
			{
				keychain.RemoveFromKeychain();
			}
			catch (XOAuthException ex)
			{
				Logger?.Log($"Failed to delete credentials from keychain: {ex}");
			}
		}

		public virtual void ForgetTokens()
		{
			Logger?.Log("Forgetting tokens from keychain");
			var keychain = new KeychainAccount(this, KeychainAccountForTokens);
			try
			{
				keychain.RemoveFromKeychain();
			}
			catch (XOAuthException ex)
			{
				Logger?.Log($"Failed to delete tokens from keychain: {ex}");
			}
		}

		private void StoreToKeychain(string account, NSMutableDictionary<NSString, NSObject> items)
		{
			var keychain = new KeychainAccount(this, account, items);
			try
			{
				keychain.SaveInKeychain();
			}
			catch (XOAuthException ex)
			{
				Logger.Log($"Failed to store to keychain: {ex}", LogLevel.Error);
			}
		}

		private void UpdateFromKeychain()
		{
			Logger?.Log("Looking for items in keychain");
			try
			{
				var creds = new KeychainAccount(this, KeychainAccountForClientCredentials);
				var credsData = creds.FetchedFromKeychain();
				UpdateFromKeychainItems(credsData);
			}
			catch (XOAuthException ex)
			{
				Logger.Log($"Failed to load client credentials from keychain: {ex}", LogLevel.Error);
			}

			try
			{
				var tokens = new KeychainAccount(this, KeychainAccountForTokens);
				var tokensData = tokens.FetchedFromKeychain();
				UpdateFromKeychainItems(tokensData);
			}
			catch (XOAuthException ex)
			{
				Logger.Log($"Failed to load tokens from keychain: {ex}", LogLevel.Error);
			}
		}
	}
}
