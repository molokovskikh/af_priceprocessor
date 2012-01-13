using System;
using Castle.ActiveRecord;
using Common.Tools;
using Inforoom.PriceProcessor.Models;

namespace Inforoom.PriceProcessor
{
	public class MailTemplate
	{
		public ResponseTemplate ResponseType { get; set; }
		public string Subject  { get; set; }
		public string Body  { get; set; }

		public MailTemplate(ResponseTemplate template, string subject, string body)
		{
			ResponseType = template;
			Subject = subject;
			Body = body;
		}

		public bool IsValid()
		{
			return !String.IsNullOrWhiteSpace(Subject) && String.IsNullOrWhiteSpace(Body);
		}
	}


	public static class TemplateHolder
	{
		private static DefaultValues _values;
		private static DateTime _lastUpdate;

		static TemplateHolder()
		{
			_lastUpdate = DateTime.MinValue;
		}

		private static void UpdateValues()
		{
			using (new SessionScope()) {
				_values = DefaultValues.Get();
			}
			_lastUpdate = DateTime.Now;
		}

		private static bool NeedUpdate()
		{
			return DateTime.Now.Subtract(_lastUpdate).TotalMinutes > 15;
		}

		public static DefaultValues Values
		{
			get { 
				if (NeedUpdate())
					UpdateValues();
				return _values;
			}
		}

		public static MailTemplate GetTemplate(ResponseTemplate template)
		{
			var values = Values;

			switch (template) {
				case ResponseTemplate.MiniMailOnUnknownProvider:
					return new MailTemplate(template, values.ResponseSubjectMiniMailOnUnknownProvider, values.ResponseBodyMiniMailOnUnknownProvider);
					break;
				case ResponseTemplate.MiniMailOnEmptyRecipients:
					return new MailTemplate(template, values.ResponseSubjectMiniMailOnEmptyRecipients, values.ResponseBodyMiniMailOnEmptyRecipients);
					break;
				case ResponseTemplate.MiniMailOnMaxAttachment:
					return new MailTemplate(template, values.ResponseSubjectMiniMailOnMaxAttachment, values.ResponseBodyMiniMailOnMaxAttachment);
					break;
				case ResponseTemplate.MiniMailOnAllowedExtensions:
					return new MailTemplate(template, values.ResponseSubjectMiniMailOnAllowedExtensions, values.ResponseBodyMiniMailOnAllowedExtensions);
					break;
				default:
					throw new ArgumentOutOfRangeException("template");
			}
		}

	}
}