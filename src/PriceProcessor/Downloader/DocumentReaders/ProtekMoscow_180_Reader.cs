using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using System.IO;
using System.Data;
using System.Xml;

namespace Inforoom.Downloader.DocumentReaders
{
	class ProtekMoscow_180_Reader : BaseDocumentReader
	{
		public override string[] DivideFiles(string ExtractDir, string[] InputFiles)
		{
			List<string> outFiles = new List<string>();
			string shortFileName;
			int FileID ;
			foreach (string fileName in InputFiles)
			{
				shortFileName = ExtractDir + Path.GetFileNameWithoutExtension(fileName) + ".";
				FileID = 0;

				//Прочитали исходный XML c документами
				XmlDocument xml = new XmlDocument();
				xml.Load(fileName);

				//Рассматриваем элементы типа "Документ"
				foreach (XmlElement docs in xml.DocumentElement.GetElementsByTagName("Документ"))
				{
					//Формируем новое имя файла
					string newFileName = shortFileName + FileID.ToString() + ".xml";
					while (File.Exists(newFileName))
					{
						FileID++;
						newFileName = shortFileName + FileID.ToString() + ".xml";
					}
					FileID++;

					XmlDocument newXml = new XmlDocument();
					
					//Создали документ с декларацией
					XmlDeclaration xmldecl = newXml.CreateXmlDeclaration("1.0", "WINDOWS-1251", "yes");
					newXml.AppendChild(xmldecl);

					//Создали элемент "КоммерческаяИнформация" с атрибутами
					XmlElement root = newXml.CreateElement(xml.DocumentElement.Name);
					newXml.AppendChild(root);
					foreach (XmlAttribute a in xml.DocumentElement.Attributes)
					{
						XmlAttribute newAtt = newXml.CreateAttribute(a.Name);
						newAtt.Value = a.Value;
						root.Attributes.Append(newAtt);
					}

					//Перенесли все содержимое элемента "Документ" из исходного документа в новый
					root.InnerXml = docs.OuterXml;

					newXml.Save(newFileName);
					outFiles.Add(newFileName);
				}

				//Разделили файла не несколько файлов и удалили исходный файл
				File.Delete(fileName);
			}

			return outFiles.ToArray();
		}

		public override List<ulong> GetClientCodes(MySqlConnection Connection, ulong FirmCode, string ArchFileName, string CurrentFileName)
		{
			List<ulong> list = new List<ulong>();
			string SQL = GetFilterSQLHeader() + Environment.NewLine + "and i.FirmClientCode = ?FirmClientCode and i.FirmClientCode2 = ?DeliveryCode " + Environment.NewLine + GetFilterSQLFooter();

			string FirmClientCode, DeliveryCode;
			try
			{
				DataSet dsWaybill = new DataSet();
				dsWaybill.ReadXml(CurrentFileName);
				DataTable dtCounteragent = dsWaybill.Tables["Контрагент"];
				FirmClientCode = dtCounteragent.Select("Роль = 'Плательщик'")[0]["Ид"].ToString();
				DeliveryCode = dtCounteragent.Select("Роль = 'Получатель'")[0]["Ид"].ToString();
			}
			catch (Exception ex)
			{
				throw new Exception("Не получилось сформировать FirmClientCode и FirmClientCode2 из документа.", ex);
			}

			DataSet ds = MySqlHelper.ExecuteDataset(
				Connection,
				SQL,
				new MySqlParameter("?FirmCode", FirmCode),
				new MySqlParameter("?FirmClientCode", FirmClientCode),
				new MySqlParameter("?DeliveryCode", DeliveryCode));

			foreach (DataRow drApteka in ds.Tables[0].Rows)
				list.Add(Convert.ToUInt64(drApteka["ClientCode"]));

			if (list.Count == 0)
				throw new Exception("Не удалось найти клиентов с FirmClientCode = " + FirmClientCode + " и FirmClientCode2 = " + DeliveryCode + ".");

			return list;
		}
	}
}
