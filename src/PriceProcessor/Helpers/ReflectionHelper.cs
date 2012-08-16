using System;
using System.Linq;
using System.Reflection;
using Inforoom.Downloader.DocumentReaders;

namespace Inforoom.PriceProcessor.Helpers
{
	public class ReflectionHelper
	{
		public static T GetDocumentReader<T>(string clazz, Assembly assembly = null)
		{
			if (assembly == null)
				assembly = Assembly.GetExecutingAssembly();

			Type result = null;
			var types = assembly
				.GetModules()[0]
				.FindTypes(Module.FilterTypeNameIgnoreCase, clazz)
				.Where(t => t.IsClass && !t.IsAbstract && typeof(T).IsAssignableFrom(t))
				.ToArray();

			if (types.Length > 1)
				throw new Exception(String.Format("Найдено более одного типа с именем {0}", clazz));
			if (types.Length == 1)
				result = types[0];
			if (result == null)
				throw new Exception(String.Format("Класс {0} не найден", clazz));
			return (T)Activator.CreateInstance(result);
		}
	}
}