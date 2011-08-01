using System;
using System.Collections.Generic;
using System.Text;
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
			excludeExtentions = new string[] { ".fls" };
		}

		public override string[] DivideFiles(string extractDir, string[] inputFiles)
		{
			var outFiles = new List<string>();
			string shortFileName;
			int fileID;

			foreach (var fileName in inputFiles)
			{
				shortFileName = extractDir + Path.GetFileNameWithoutExtension(fileName) + ".";
				fileID = 0;

				//��������� �������� XML c �����������
				var xml = new XmlDocument();
				xml.Load(fileName);

				//������������� �������� ���� "��������"
				foreach (XmlElement docs in xml.DocumentElement.GetElementsByTagName("��������"))
				{
					//��������� ����� ��� �����
					string newFileName = shortFileName + fileID.ToString() + ".xml";
					while (File.Exists(newFileName))
					{
						fileID++;
						newFileName = shortFileName + fileID.ToString() + ".xml";
					}
					fileID++;

					XmlDocument newXml = new XmlDocument();
					
					//������� �������� � �����������
					XmlDeclaration xmldecl = newXml.CreateXmlDeclaration("1.0", "WINDOWS-1251", "yes");
					newXml.AppendChild(xmldecl);

					//������� ������� "����������������������" � ����������
					XmlElement root = newXml.CreateElement(xml.DocumentElement.Name);
					newXml.AppendChild(root);
					foreach (XmlAttribute a in xml.DocumentElement.Attributes)
					{
						XmlAttribute newAtt = newXml.CreateAttribute(a.Name);
						newAtt.Value = a.Value;
						root.Attributes.Append(newAtt);
					}

					//��������� ��� ���������� �������� "��������" �� ��������� ��������� � �����
					root.InnerXml = docs.OuterXml;

					newXml.Save(newFileName);
					outFiles.Add(newFileName);
				}
				//��������� ����� �� ��������� ������ � ������� �������� ����
				File.Delete(fileName);
			}
			return outFiles.ToArray();
		}

		public override List<ulong> GetClientCodes(MySqlConnection connection, ulong supplierId, string archFileName, string currentFileName)
		{
			var list = new List<ulong>();
			/*string SQL = GetFilterSQLHeader() + Environment.NewLine + 
				" and (i.FirmClientCode = ?SupplierClientId) and (i.FirmClientCode2 = ?SupplierDeliveryId) " +
				SqlGetClientAddressId(true, true, true) +
				Environment.NewLine + GetFilterSQLFooter();*/
            string SQL = SqlGetClientAddressId(true, true) +
                Environment.NewLine + GetFilterSQLFooter();

			string FirmClientCode, DeliveryCode;
			try
			{
				var dsWaybill = new DataSet();
				dsWaybill.ReadXml(currentFileName);
				DataTable dtCounteragent = dsWaybill.Tables["����������"];
				FirmClientCode = dtCounteragent.Select("���� = '����������'")[0]["��"].ToString();
				DeliveryCode = dtCounteragent.Select("���� = '����������'")[0]["��"].ToString();
			}
			catch (Exception ex)
			{
				throw new Exception("�� ���������� ������������ SupplierClientId(FirmClientCode) � SupplierDeliveryId(FirmClientCode2) �� ���������.", ex);
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
				throw new Exception("�� ������� ����� �������� � SupplierClientId(FirmClientCode) = " + FirmClientCode + 
					" � SupplierDeliveryId(FirmClientCode2) = " + DeliveryCode + ".");

			return list;
		}
	}
}
