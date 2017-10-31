using Foundation;

namespace XOAuth.Base
{
	public class RequestPerformer
	{
		private NSUrlSession _session;
		public RequestPerformer(NSUrlSession session)
		{
			_session = session;
		}

		public NSUrlSessionTask Perform(NSUrlRequest request, NSUrlSessionResponse completionHandler)
		{
			var task = _session.CreateDataTask(request, completionHandler);
			task.Resume();
			return task;
		}
	}
}
