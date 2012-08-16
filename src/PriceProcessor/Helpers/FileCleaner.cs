using System;
using System.Collections.Generic;
using System.IO;
using log4net;

namespace Inforoom.PriceProcessor.Helpers
{
	public class FileCleaner : IDisposable
	{
		private readonly ILog _log = LogManager.GetLogger(typeof(FileCleaner));
		private readonly List<string> _files = new List<string>();

		public void Watch(string file)
		{
			_files.Add(file);
		}

		public void Watch(params string[] files)
		{
			_files.AddRange(files);
		}

		public void Watch(IEnumerable<string> files)
		{
			_files.AddRange(files);
		}

		public void Dispose()
		{
			foreach (var file in _files) {
				try {
					if (File.Exists(file))
						File.Delete(file);
				}
				catch (Exception e) {
					_log.Error(String.Format("Ошибка при удалении файла '{0}'", file), e);
				}
			}
		}
	}
}