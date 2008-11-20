using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using System.IO;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class FileHelperTest
	{
		private void CreateFile(string fileName, bool isReadOnly)
		{
			//Создаем файл с атрибутом ReadOnly
			FileStream _tempStream = File.Create(fileName);
			_tempStream.Close();
			FileInfo _tempFileInfo = new FileInfo(fileName);
			Assert.That(!_tempFileInfo.IsReadOnly, String.Format("Файл '{0}' уже имеет атрибут ReadOnly", fileName));
			if (isReadOnly)
				_tempFileInfo.IsReadOnly = true;
		}

		private void ClearReadOnly(DirectoryInfo root)
		{
			FileSystemInfo[] _fileInfos = root.GetFileSystemInfos();
			foreach (FileSystemInfo _deleted in _fileInfos)
			{
				if (_deleted is DirectoryInfo)
					ClearReadOnly(_deleted as DirectoryInfo);
				if ((_deleted.Attributes & FileAttributes.ReadOnly) > 0)
					_deleted.Attributes &= ~FileAttributes.ReadOnly;
			}
		}

		[Test(Description = "проверка удаления директории, в которой были файлы ReadOnly")]
		public void DeleteDirectoryReadOnlyTest()
		{
			string _rootTempPath = Path.GetTempFileName();
			if (File.Exists(_rootTempPath))
				File.Delete(_rootTempPath);
			_rootTempPath = Path.GetDirectoryName(_rootTempPath) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(_rootTempPath);
				
			if (!Directory.Exists(_rootTempPath))
				Directory.CreateDirectory(_rootTempPath);

			string _testedDirectory = _rootTempPath + "\\tested";
			if (!Directory.Exists(_testedDirectory))
				Directory.CreateDirectory(_testedDirectory);
			DirectoryInfo _testedDirectoryInfo = new DirectoryInfo(_testedDirectory);

			//Создаем файл с атрибутом ReadOnly
			CreateFile(_testedDirectory + "\\file1.txt", true);
			//Создаем файл с обычными атрибутами
			CreateFile(_testedDirectory + "\\file2.txt", false);

			string _subTestedDirectory = _testedDirectory + "\\subTested";
			if (!Directory.Exists(_subTestedDirectory))
				Directory.CreateDirectory(_subTestedDirectory);

			//Создаем файл с атрибутом ReadOnly
			CreateFile(_subTestedDirectory + "\\file1.txt", true);
			//Создаем файл с обычными атрибутами
			CreateFile(_subTestedDirectory + "\\file2.txt", false);

			//Создаем директорию с атрибутом ReadOnly
			string _subReadOnlyTestedDirectory = _testedDirectory + "\\subReadOnlyTested";
			if (!Directory.Exists(_subReadOnlyTestedDirectory))
				Directory.CreateDirectory(_subReadOnlyTestedDirectory);
			DirectoryInfo _directoryInfo = new DirectoryInfo(_subReadOnlyTestedDirectory);
			Assert.That((_directoryInfo.Attributes & FileAttributes.ReadOnly) == 0, String.Format("Директория '{0}' уже имеет атрибут ReadOnly", _subReadOnlyTestedDirectory));
			_directoryInfo.Attributes |= FileAttributes.ReadOnly;			

			try
			{
				//Попытка удалить директорию не может быть успешной, т.к. есть файлы и директории с атрибутом ReadOnly
				Directory.Delete(_testedDirectory, true);
				Assert.Fail("Получилось удалить директорию с файлами и директориями с атрибутом ReadOnly");
			}
			catch (UnauthorizedAccessException)
			{ 
			}

			//Сбрасываем везде атрибуты ReadOnly
			ClearReadOnly(_testedDirectoryInfo);

			//Вторая попытка удалить директорию должна быть успешной
			Directory.Delete(_testedDirectory, true);

			//Удаляем временную директорию
			Directory.Delete(_rootTempPath, true);
		}
	}
}
