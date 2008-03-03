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

	/// <summary>
	/// Summary description for ToughMask.
	/// </summary>
	public class ToughMask
	{
		private Regex re;
		private Match m = null;
		private string [] reGroupName;



		public ToughMask(string Mask, long clientCode, long priceCode, string clientShortName, string priceName)
		{
			reGroupName = new string[Enum.GetNames(typeof(NameGroup)).Length];
			try
			{
				re = new Regex(Mask);
			}
			catch(Exception e)
			{
				throw new WarningFormalizeException(String.Format(Settings.Default.ParseMaskError, e), clientCode, priceCode, clientShortName, priceName);
			}
			foreach(string gname in re.GetGroupNames())
			{
				string gnameU = gname.ToUpper();
				foreach(NameGroup ng in Enum.GetValues(typeof(NameGroup)))
				{
					string sNG = ng.ToString().ToUpper();
					if (gnameU.Equals(sNG))
					{
						if (null == reGroupName[(int)ng])
							reGroupName[(int)ng] = gname;
						else
							throw new WarningFormalizeException(String.Format(Settings.Default.DoubleGroupMaskError, ng), clientCode, priceCode, clientShortName, priceName);
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
			else
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
