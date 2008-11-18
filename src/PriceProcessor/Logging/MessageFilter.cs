using System;
using log4net.Core;
using log4net.Filter;

namespace PriceProcessor.Logging
{
	public class MessageFilter : FilterSkeleton
	{
		private string _key;
		private Level _levelToMatch;
		private string _stringToMatch;

		public Level LevelToMatch
		{
			get { return _levelToMatch; }
			set { _levelToMatch = value; }
		}

		public string Key
		{
			get { return _key; }
			set { _key = value; }
		}

		public string StringToMatch
		{
			get { return _stringToMatch; }
			set { _stringToMatch = value; }
		}

		public override FilterDecision Decide(LoggingEvent loggingEvent)
		{
			FilterDecision propertyFilterResult = CheckProperty(loggingEvent);
			FilterDecision levelFilterResult = CheckLevel(loggingEvent);

			if (propertyFilterResult == FilterDecision.Deny || levelFilterResult == FilterDecision.Deny)
				return FilterDecision.Deny;

			if (propertyFilterResult == FilterDecision.Neutral && levelFilterResult == FilterDecision.Neutral)
				return FilterDecision.Neutral;

			return FilterDecision.Accept;
		}

		private FilterDecision CheckProperty(LoggingEvent loggingEvent)
		{
			if (String.IsNullOrEmpty(_key) || String.IsNullOrEmpty(_stringToMatch))
				return FilterDecision.Neutral;

			string value = loggingEvent.Repository.RendererMap.FindAndRender(loggingEvent.LookupProperty(_key));

			if (String.IsNullOrEmpty(value))
				return FilterDecision.Neutral;

			if (value.IndexOf(_stringToMatch, StringComparison.CurrentCultureIgnoreCase) >= 0)
				return FilterDecision.Accept;

			return FilterDecision.Deny;
		}

		private FilterDecision CheckLevel(LoggingEvent loggingEvent)
		{
			if (_levelToMatch == null)
				return FilterDecision.Neutral;

			if (_levelToMatch == loggingEvent.Level)
				return FilterDecision.Accept;

			return FilterDecision.Deny;
		}
	}
}