using System;
using System.Text.RegularExpressions;

namespace Inforoom.Formalizer
{
	/// <summary>
	/// Summary description for ToughDate.
	/// </summary>
	public class ToughDate
	{
		private Regex re;
		string[] EngMonth;
		string[] RusMonth;

		public ToughDate()
		{
			re = new Regex(FormalizeSettings.DateMask);
			EngMonth = new String[] {"JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"};
			RusMonth = new String[] {"ﬂÕ¬", "‘≈¬", "Ã¿–", "¿œ–", "Ã¿…", "»ﬁÕ", "»ﬁÀ", "¿¬√", "—≈Õ", "Œ “", "ÕŒﬂ", "ƒ≈ "};
		}

		public DateTime Analyze(string Input)
		{
			int year, month, day;
			try
			{
				Input = Input.Trim();
				Match m = re.Match(Input);
				if (String.Empty != m.Groups["Year"].Value)
				{
					if (1 == m.Groups["Year"].Value.Length)
						year = Convert.ToInt32("200" + m.Groups["Year"].Value);
					else
						if (2 == m.Groups["Year"].Value.Length)
							year = Convert.ToInt32("20" + m.Groups["Year"].Value);
						else
							if (3 == m.Groups["Year"].Value.Length)
								year = Convert.ToInt32("2" + m.Groups["Year"].Value);
							else
								year = Convert.ToInt32(m.Groups["Year"].Value);

					if (String.Empty != m.Groups["Month"].Value)
					{
						month = GetMonth(m.Groups["Month"].Value);
						if (-1 == month)
							month = Convert.ToInt32(m.Groups["Month"].Value);

						if (String.Empty != m.Groups["Day"].Value)
							day = Convert.ToInt32(m.Groups["Day"].Value);
						else
							day = 1;

						return new DateTime(year, month, day);
					}
					else
						return DateTime.MaxValue;
				  
				}
				else
					return DateTime.MaxValue;
			}
			catch
			{
				return DateTime.MaxValue;
			}

		}

		private int GetMonth(string month)
		{
			month = month.ToUpper();
			for(int i = EngMonth.GetLowerBound(0); i<=EngMonth.GetUpperBound(0); i++)
			{
				if (month.StartsWith(EngMonth[i]) || month.StartsWith(RusMonth[i]))
					return i+1;
			}
			return -1;
		}

	}
}
