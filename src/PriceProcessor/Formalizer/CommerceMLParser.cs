using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Xsl;
using System.Resources;
using System.IO;
using System.Data;
using MySql.Data.MySqlClient;
using Inforoom.PriceProcessor;


namespace Inforoom.Formalizer
{
	class CommerceMLParser : InterPriceParser
	{
		public CommerceMLParser(string PriceFileName, MySqlConnection conn, DataTable mydr)
			: base(PriceFileName, conn, mydr)
		{
			conn.Close();
		}

		public override void Open()
		{
			string newXMLFile;
			try
			{

				// Set the validation settings.
				XmlReaderSettings settings = new XmlReaderSettings();
				settings.ValidationType = System.Xml.ValidationType.None;
				//Если установим CallBack, то будем получать все ошибки в нем. Если не установим, то получем первую и завершим разбор
				//settings.ValidationEventHandler += new ValidationEventHandler(test79ValidationCallBack);
				//settings.Schemas.Add(XmlSchema.Read(new StreamReader("CommerceML.xsd", Encoding.Default), test79SchemaValidationCallBack));
				settings.Schemas.Add(XmlSchema.Read(new StringReader(PriceProcessorResource.CommerceML), null));

				// Create the XmlReader object.
				XmlReader reader = XmlReader.Create(priceFileName, settings);

				// Parse the file. 
				while (reader.Read());

				//Создаем класс для выполнения XSLT-преобразований
				XslCompiledTransform xslt = new XslCompiledTransform();
				xslt.Load(XmlReader.Create(new StringReader(PriceProcessorResource.PriceProtek)));

				//Производим преобразование
				newXMLFile = Path.GetDirectoryName(priceFileName) + Path.DirectorySeparatorChar + "PriceProtek.xml";
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
