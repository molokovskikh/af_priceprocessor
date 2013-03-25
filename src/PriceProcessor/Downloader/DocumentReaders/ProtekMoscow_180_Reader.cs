using System;
using System.Collections.Generic;
using System.Text;
using Inforoom.PriceProcessor.Queries;
using MySql.Data.MySqlClient;
using System.IO;
using System.Data;
using System.Xml;

namespace Inforoom.Downloader.DocumentReaders
{
	public class ProtekMoscow_180_Reader : BaseDocumentReader
	{
		public ProtekMoscow_180_Reader()
		{
			excludeExtentions = new[] { ".fls" };
		}

		public override string[] DivideFiles(string extractDir, string[] inputFiles)
		{
			var outFiles = new List<string>();
			string shortFileName;
			int fileID;

			foreach (var fileName in inputFiles) {
				shortFileName = Path.Combine(extractDir, Path.GetFileNameWithoutExtension(fileName) + ".");
				fileID = 0;

				//Прочитали исходный XML c документами
				var xml = new XmlDocument();
				xml.Load(fileName);

				//Рассматриваем элементы типа "Документ"
				foreach (XmlElement docs in xml.DocumentElement.GetElementsByTagName("Документ")) {
					//Формируем новое имя файла
					var newFileName = shortFileName + fileID.ToString() + ".xml";
					while (File.Exists(newFileName)) {
						fileID++;
						newFileName = shortFileName + fileID.ToString() + ".xml";
					}
					fileID++;

					var newXml = new XmlDocument();

					//Создали документ с декларацией
					var xmldecl = newXml.CreateXmlDeclaration("1.0", "WINDOWS-1251", "yes");
					newXml.AppendChild(xmldecl);

					//Создали элемент "КоммерческаяИнформация" с атрибутами
					var root = newXml.CreateElement(xml.DocumentElement.Name);
					newXml.AppendChild(root);
					foreach (XmlAttribute a in xml.DocumentElement.Attributes) {
						var newAtt = newXml.CreateAttribute(a.Name);
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

		public override List<ulong> ParseAddressIds(MySqlConnection connection, ulong supplierId, string archFileName, string currentFileName)
		{
			var list = new List<ulong>();

			string SQL = AddressIdQuery.SqlGetClientAddressId(true, true);

			string FirmClientCode, DeliveryCode;
			try {
				var dsWaybill = new DataSet();
				dsWaybill.ReadXml(currentFileName);
				DataTable dtCounteragent = dsWaybill.Tables["Контрагент"];
				FirmClientCode = dtCounteragent.Select("Роль = 'Плательщик'")[0]["Ид"].ToString();
				DeliveryCode = dtCounteragent.Select("Роль = 'Получатель'")[0]["Ид"].ToString();
			}
			catch (Exception ex) {
				throw new Exception("Не получилось сформировать SupplierClientId(FirmClientCode) и SupplierDeliveryId(FirmClientCode2) из документа.", ex);
			}

			DataSet ds = MySqlHelper.ExecuteDataset(
				connection,
				SQL,
				new MySqlParameter("?SupplierId", supplierId),
				new MySqlParameter("?SupplierClientId", FirmClientCode),
				new MySqlParameter("?SupplierDeliveryId", DeliveryCode));

			foreach (DataRow drApteka in ds.Tables[0].Rows)
				list.Add(Convert.ToUInt64(drApteka["AddressId"]));

			if (list.Count == 0)
				throw new Exception("Не удалось найти клиентов с SupplierClientId(FirmClientCode) = " + FirmClientCode +
					" и SupplierDeliveryId(FirmClientCode2) = " + DeliveryCode + ".");

			return list;
		}
	}
}