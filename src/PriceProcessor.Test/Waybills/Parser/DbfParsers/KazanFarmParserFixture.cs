﻿using System;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Models;
using Inforoom.PriceProcessor.Waybills;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class KazanFarmParserFixture
	{
		[Test, Description("Тест для накладной в dbf формате от Казань-Фарм. Открываем специальным образом.")]
		public void Parse()
		{
			var document = WaybillParser.Parse(@"..\..\Data\Waybills\P-965021.dbf",
				new DocumentReceiveLog { Supplier = new Supplier { Id = 2747u } });

			Assert.AreEqual("KazanFarmDbfParser", document.Parser);
			Assert.That(document.Lines.Count, Is.EqualTo(15));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("Рн-Kz0000965021"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("26.01.2012")));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("694162947"));
			Assert.That(line.Product, Is.EqualTo("L-Тироксин 125 Берлин-Хеми 125мкг таб №100"));
			Assert.That(line.Producer, Is.EqualTo("Берлин/Менарини Груп"));
			Assert.That(line.Country, Is.EqualTo("Германия"));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(117.36));
			Assert.That(line.RegistryCost, Is.EqualTo(118.42));
			Assert.That(line.SupplierCost, Is.EqualTo(130.25));
			Assert.That(line.Period, Is.EqualTo("01.02.2013"));
			Assert.That(line.VitallyImportant, Is.True);
			Assert.That(line.Certificates, Is.EqualTo("РОСС DE.ФМ01.Д32296"));
			Assert.That(line.SerialNumber, Is.EqualTo("11002"));

			Assert.That(document.Lines[1].VitallyImportant, Is.False);
		}
	}
}