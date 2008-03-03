using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Inforoom.Common
{
    public sealed class WildcardsHelper
    {
        public static bool IsWildcards(string InputStr)
        {
            return (InputStr.IndexOfAny(new char[] { '*', '?' }) > -1);
        }

        public static bool Matched(string Mask, string Input)
        {
            Mask = "^" + Mask.Replace('?', '.').Replace("*", ".*?") + "$";
            try
            {
                Regex re = new Regex(Mask, RegexOptions.IgnoreCase);
                Match m = re.Match(Input);
                return m.Success;
            }
            catch
            {
                return false;
            }
        }
    }
}
