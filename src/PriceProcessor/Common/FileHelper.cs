﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Inforoom.Common
{
	public sealed class FileHlp
	{
		public static string NormalizeDir(string InputDir)
		{
			string result = Path.GetFullPath(InputDir);
			if ((result.Length > 0) && (result[result.Length - 1] == Path.DirectorySeparatorChar))
				result = result.Substring(0, result.Length - 1);
			return result;
		}

	}
}
