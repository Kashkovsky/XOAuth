using System;
namespace XOAuth.Base
{
	public class ContextStore
	{
		private string _state;

		public string State => _state ?? (_state = Guid.NewGuid().ToString().Split('-')[0]);
		public string RedirectUrl { get; set; }

		public bool MatchesState(string state)
		{
			if (string.IsNullOrEmpty(state)) return false;
			return state.Equals(State, StringComparison.OrdinalIgnoreCase);
		}

		public void ResetState()
		{
			_state = null;
		}
	}
}
