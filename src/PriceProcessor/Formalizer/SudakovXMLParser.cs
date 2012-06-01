using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Xsl;
using System.Resources;
using System.IO;
using System.Data;
using Inforoom.PriceProcessor.Formalizer.New;
using MySql.Data.MySqlClient;
using Inforoom.PriceProcessor;


namespace Inforoom.Formalizer
{
	class SudakovXMLParser : InterPriceParser
	{
		public SudakovXMLParser(string file, MySqlConnection conn, PriceFormalizationInfo data)
			: base(file, conn, data)
		{
			conn.Close();
		}

		public override void Open()
		{
			string newXMLFile;
			try
			{
				// Create the XmlReader object.
				XmlReader reader = XmlReader.Create(priceFileName);

				// Parse the file. 
				while (reader.Read());

				//Создаем класс для выполнения XSLT-преобразований
				XslCompiledTransform xslt = new XslCompiledTransform();
				xslt.Load(XmlReader.Create(new StringReader(PriceProcessorResource.PriceSudakov)));

				//Производим преобразование
				newXMLFile = Path.GetDirectoryName(priceFileName) + Path.DirectorySeparatorChar + "PriceSudakov.xml";
				if (File.Exists(newXMLFile))
				{
					File.Delete(newXMLFile);
					System.Threading.Thread.Sleep(500);
				}
				xslt.Transform(priceFileName, newXMLFile);


				DataSet ds = new DataSet();
				ds.ReadXml(newXMLFile);

				dtPrice = ds.Tables["Position"];

			}
			catch (System.Xml.Schema.XmlSchemaException xex)
			{
				throw new Exception(String.Format("Не получилось прочитать XML-файл, строка {0}, позиция {1}, ошибка : {2}", xex.LineNumber, xex.LinePosition, xex.Message), xex);
			}

			CurrPos = 0;
		}

		public override object ProcessPeriod(string PeriodValue)
		{
			DateTime res;
			try
			{
				res = DateTime.Parse(PeriodValue, System.Globalization.CultureInfo.InvariantCulture);
				return res;
			}
			catch
			{
				return DBNull.Value;
			}
		}


	}
}
