using System;
using System.Linq;
using Castle.ActiveRecord;
using Common.Tools;

namespace Inforoom.PriceProcessor.Models
{
	[ActiveRecord(Table = "Defaults", Schema = "UserSettings")]
	public class DefaultValues : ActiveRecordBase<DefaultValues>
	{
		[PrimaryKey]
		public uint Id { get; set; }

		[Property ]
		public string AllowedMiniMailExtensions { get; set; }

		[Property]
		public string ResponseSubjectMiniMailOnUnknownProvider { get; set; }

		[Property]
		public string ResponseBodyMiniMailOnUnknownProvider { get; set; }

		[Property]
		public string ResponseSubjectMiniMailOnEmptyRecipients { get; set; }

		[Property]
		public string ResponseBodyMiniMailOnEmptyRecipients { get; set; }

		[Property]
		public string ResponseSubjectMiniMailOnMaxAttachment { get; set; }

		[Property]
		public string ResponseBodyMiniMailOnMaxAttachment { get; set; }

		[Property]
		public string ResponseSubjectMiniMailOnAllowedExtensions { get; set; }

		[Property]
		public string ResponseBodyMiniMailOnAllowedExtensions { get; set; }

		public static DefaultValues Get()
		{
			return FindAll().First();
		}

		public bool ExtensionAllow(string extension)
		{
			if (string.IsNullOrEmpty(extension))
				return false;

			if (extension.StartsWith("."))
				extension = extension.RightSlice(extension.Length - 1).Trim();

			var extensions = AllowedMiniMailExtensions.Split(',');
			foreach (var s in extensions) {
				if (s.Trim().Equals(extension, StringComparison.OrdinalIgnoreCase))
					return true;
			}

			return false;
		}

	}
}