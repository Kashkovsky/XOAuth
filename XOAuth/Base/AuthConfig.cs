namespace XOAuth.Base
{
	public class AuthConfig
	{
		public bool AuthorizeEmbedded { get; set; }
		public bool AuthorizeEmbeddedAutoDismiss { get; set; } = true;
		public object AuthorizeContext { get; set; }
		public UI UI { get; set; } = new UI();
	}

	public class UI
	{
		public string Title { get; set; }
		public object BackButton { get; set; }
		public bool ShowCancelButton { get; set; } = true;
		public bool UseSafariView { get; set; } = true;
	}
}
