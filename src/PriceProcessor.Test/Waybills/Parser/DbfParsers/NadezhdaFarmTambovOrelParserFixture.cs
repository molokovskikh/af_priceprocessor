﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;
using Common.Tools;
using PriceProcessor.Test.TestHelpers;


namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	internal class NadezhdaFarmTambovOrelParserFixture
	{
		[Test]
		public void Parse()
		{
			var doc = WaybillParser.Parse("418312_0.dbf");
			Assert.That(doc.Lines.Count, Is.EqualTo(4));
			Assert.That(doc.ProviderDocumentId, Is.EqualTo("418312"));
			Assert.That(doc.DocumentDate.Value.ToShortDateString(), Is.EqualTo("14.04.2011"));
			var line = doc.Lines[0];
			Assert.That(line.Product, Is.EqualTo("1000 Трав от Мазнева крем-бальзам 85мл сбор N4 д/суставов и позвоночника"));
			Assert.That(line.Code, Is.EqualTo("73715"));
			Assert.That(line.SupplierCost, Is.EqualTo(84.7800));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(71.8450));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(68.4300));
			Assert.That(line.SupplierPriceMarkup, Is.EqualTo(4.9905));
			Assert.That(line.Nds, Is.EqualTo(18));
			Assert.That(line.Quantity, Is.EqualTo(2));
			Assert.That(line.Amount, Is.EqualTo(169.5600));
			Assert.That(line.NdsAmount, Is.EqualTo(25.8700));
			Assert.That(line.Producer, Is.EqualTo("Твинс Тэк ЗАО"));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.АИ86.Д00320"));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.SerialNumber, Is.EqualTo("0211"));
			Assert.That(line.Period, Is.EqualTo("01.08.2012"));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.RegistryCost, Is.EqualTo(0.0000));
		}

		[Test]
		public void Check_file_format()
		{
			Assert.IsTrue(NadezhdaFarmTambovOrelParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\418312_0.dbf")));
		}

		/// <summary>
		/// Новые данные от задачи
		/// http://redmine.analit.net/issues/28233
		/// </summary>
		[Test]
		public void Parse2()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\446406_0.dbf");
			Assert.That(document.Lines.Count, Is.EqualTo(3));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("446406"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("10.10.2014")));

			var line = document.Lines[2];
			Assert.That(line.Code, Is.EqualTo("10156"));
			Assert.That(line.BillOfEntryNumber, Is.EqualTo("10130032/240314/0001852/2"));
			Assert.That(line.EAN13, Is.EqualTo("4823004503669"));
			Assert.That(line.CountryCode, Is.EqualTo("80"));
		}
	
		/// <summary>
		/// Unitcode от задачи
		/// http://redmine.analit.net/issues/29029
		/// </summary>
		[Test]
		public void Parse3()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\467884_0.dbf");
			Assert.That(document.Lines.Count, Is.EqualTo(6));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("467884"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("23.10.2014")));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("32845"));
			Assert.That(line.UnitCode, Is.EqualTo("778"));
		}
	}
}