using System;
using System.Linq;
using System.Text.RegularExpressions;
using Foundation;
using XOAuth.Domain;

namespace XOAuth.Base
{
	public abstract class Requestable
	{
		private NSUrlSessionConfiguration _sessionConfig;
		private NSUrlSession _session;
		private INSUrlSessionDelegate _sessionDelegate;
		private RequestPerformer _requestPerformer;
		private NSUrlSessionTask _abortableTask;
		private bool _verbose = false;
		public Logger Logger { get; private set; }

		public Requestable(bool verbose)
		{
			SetVerbosity(verbose);
			Logger?.Log("Initialization finished");
		}

		public void SetVerbosity(bool enabled)
		{
			_verbose = enabled;
			Logger = _verbose ? new Logger() : null;
		}

		protected NSUrlSession Session
		{
			get
			{
				if (_session == null)
				{
					var config = SessionConfiguration ?? NSUrlSessionConfiguration.EphemeralSessionConfiguration;
					_session = NSUrlSession.FromConfiguration(config, SessionDelegate, null);
				}

				return _session;
			}
			set
			{
				_session = value;
				_requestPerformer = null;
			}
		}

		protected NSUrlSessionConfiguration SessionConfiguration
		{
			get { return _sessionConfig; }
			set
			{
				_sessionConfig = value;
				_session = null;
			}
		}

		protected INSUrlSessionDelegate SessionDelegate
		{
			get { return _sessionDelegate; }
			set
			{
				_sessionDelegate = value;
				_session = null;
			}
		}

		public void Perform(NSUrlRequest request, Action<XOAuthResponse> callback)
		{
			Logger?.Log($"REQUEST\r\n{request.DebugDescription}\r\n-----");
			if (_requestPerformer == null)
				_requestPerformer = new RequestPerformer(Session);
			var task = _requestPerformer.Perform(request, (data, response, error) =>
			{
				_abortableTask = null;
				Logger?.Log($"RESPONSE\r\n{response?.DebugDescription ?? "no response"}\r\n{data ?? "no data"}\r\n-----");
				var http = response as NSHttpUrlResponse ?? new NSHttpUrlResponse(request.Url, 499, null, null);
				var finalResponse = new XOAuthResponse(request, http, data, error);
				callback(finalResponse);
			});
		}

		protected bool AbortTask()
		{
			if (_abortableTask == null)
				return false;
			Logger.Log("Aborting request");
			_abortableTask.Cancel();
			return true;
		}

		protected XOAuthDictionary GetParamsFromQuery(string query)
		{
			return Regex.Matches(query, "([^?=&]+)(=([^&]*))?")
							 .Cast<Match>()
							 .ToDictionary(x => x.Groups[1].Value, x => System.Net.WebUtility.UrlDecode(x.Groups[3].Value))
						as XOAuthDictionary;
		}
	}
}
