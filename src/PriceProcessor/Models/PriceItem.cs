using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;

namespace Inforoom.PriceProcessor.Models
{
	[ActiveRecord("pricefmts", Schema = "Farm")]
	public class Format
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string FileExtention { get; set; }
	}

	[ActiveRecord("FormRules", Schema = "Farm")]
	public class FormRule
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[BelongsTo("PriceFormatId")]
		public virtual Format Format { get; set; }
	}

	[ActiveRecord("PriceItems", Schema = "Usersettings")]
	public class PriceItem
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[BelongsTo("FormRuleId")]
		public virtual FormRule FormRule { get; set; }

		[Property]
		public virtual DateTime PriceDate { get; set; }

		[Property]
		public virtual DateTime LastDownload { get; set; }

		[Property]
		public virtual DateTime LocalLastDownload { get; set; }

		public virtual string BaseFile
		{
			get {
				return GetFilePath(Settings.Default.BasePath);
			}
		}

		public virtual string InboundFile
		{
			get {
				return GetFilePath(Settings.Default.InboundPath);
			}
		}

		public virtual string GetFilePath(string path)
		{
			if (FormRule == null)
				return "";
			if (FormRule.Format == null)
				return "";
			return Path.Combine(Path.GetFullPath(path), Id.ToString() + FormRule.Format.FileExtention);
		}
	}
}
