using System;
using System.Text.RegularExpressions;
using Inforoom.PriceProcessor.Properties;

namespace Inforoom.Formalizer
{
	public enum NameGroup
	{
		Code,
		Name,
		Period,
		FirmCr,
		Unit,
		Note,
		Doc
	}

	public class ToughMask
	{
		private readonly Regex re;
		private Match m;
		private readonly string [] reGroupName;

		public ToughMask(string mask, PriceFormalizationInfo priceInfo)
		{
			reGroupName = new string[Enum.GetNames(typeof(NameGroup)).Length];
			try
			{
				re = new Regex(mask);
			}
			catch(Exception e)
			{
				throw new WarningFormalizeException(String.Format(Settings.Default.ParseMaskError, e), priceInfo);
			}
			foreach(var gname in re.GetGroupNames())
			{
				var gnameU = gname.ToUpper();
				foreach(NameGroup ng in Enum.GetValues(typeof(NameGroup)))
				{
					var sNG = ng.ToString().ToUpper();
					if (gnameU.Equals(sNG))
					{
						if (null == reGroupName[(int)ng])
							reGroupName[(int)ng] = gname;
						else
							throw new WarningFormalizeException(String.Format(Settings.Default.DoubleGroupMaskError, ng), priceInfo);
						break;
					}
				}
			}
		}

		public void Analyze(string Input)
		{
			m = re.Match(Input);
		}

		public void Clear(string Input)
		{
			m = null;
		}

		public string GetFieldValue(PriceFields PF)
		{
			if (null == m)
				return String.Empty;
			try
			{
				switch((int)PF)
				{
					case (int)PriceFields.Code:
						return m.Groups[reGroupName[(int)NameGroup.Code]].Value;

					case (int)PriceFields.Doc:
						return m.Groups[reGroupName[(int)NameGroup.Doc]].Value;

					case (int)PriceFields.FirmCr:
						return m.Groups[reGroupName[(int)NameGroup.FirmCr]].Value;

					case (int)PriceFields.Name1:
					case (int)PriceFields.Name2:
					case (int)PriceFields.Name3:
						return m.Groups[reGroupName[(int)NameGroup.Name]].Value;

					case (int)PriceFields.Note:
						return m.Groups[reGroupName[(int)NameGroup.Note]].Value;

					case (int)PriceFields.Period:
						return m.Groups[reGroupName[(int)NameGroup.Period]].Value;

					case (int)PriceFields.Unit:
						return m.Groups[reGroupName[(int)NameGroup.Unit]].Value;

					default:
						return String.Empty;
				}
			}
			catch
			{
				return String.Empty;
			}
		}
	}
}
