using System;
using System.Runtime.InteropServices;
using Foundation;
using ObjCRuntime;
using XOAuth.Exceptions;

namespace XOAuth.KeychainUtils
{
	public class Keychain : KeychainServiceType
	{
		/// <summary>
		/// Add a security item.
		/// </summary>
		/// <param name="query">The handle to the dictionary.</param>
		/// <param name="result">Receives the handle to the added item.</param>
		/// <returns>The status of the operation.</returns>
		[DllImport(Constants.SecurityLibrary)]
		internal static extern int SecItemAdd(IntPtr query, out IntPtr result);

		[DllImport(Constants.SecurityLibrary)]
		internal static extern int SecItemDelete(IntPtr query, out IntPtr result);

		[DllImport(Constants.SecurityLibrary)]
		private static extern int SecItemCopyMatching(IntPtr query, out IntPtr result);

		public override NSMutableDictionary<NSString, NSObject> FetchItemWithAttributes(NSMutableDictionary<NSString, NSObject> attributes)
		{
			IntPtr resultHandle;
			var statusCode = SecItemCopyMatching(attributes.Handle, out resultHandle);

			if (statusCode != KSec.ErrSecSuccess)
				throw ErrorForStatusCode(statusCode);

			return Runtime.GetNSObject<NSMutableDictionary<NSString, NSObject>>(resultHandle);
		}

		public override void InsertItemWithAttributes(NSMutableDictionary<NSString, NSObject> attributes)
		{
			IntPtr result;
			var statusCode = SecItemAdd(attributes.Handle, out result);
			if (statusCode == KSec.ErrSecDuplicateItem)
			{
				SecItemDelete(attributes.Handle, out result);
				statusCode = SecItemAdd(attributes.Handle, out result);
			}

			if (statusCode != KSec.ErrSecSuccess)
				throw ErrorForStatusCode(statusCode);
		}

		public override void RemoveItemWithAttributes(NSMutableDictionary<NSString, NSObject> attributes)
		{
			IntPtr result;
			var statusCode = SecItemDelete(attributes.Handle, out result);

			if (statusCode != KSec.ErrSecSuccess)
				throw ErrorForStatusCode(statusCode);
		}

		internal XOAuthException ErrorForStatusCode(int statusCode) => new XOAuthException(statusCode); //TODO descriptable status code
	}
}
