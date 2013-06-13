using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	public class AllianceHealthCareParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("15791655_Альянс Хелскеа Рус(n0118884).dbf");
			Assert.That(document.DocumentDate, Is.EqualTo(DateTime.Parse("01.10.2012")));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("СЧ-0118884/00"));
			Assert.That(document.Lines.Count, Is.EqualTo(2));

			var line = document.Lines[0];
			Assert.That(line.Code, Is.EqualTo("35857"));
			Assert.That(line.Product, Is.EqualTo("Сан Пауэр молочко-спрей защитное д/детей СПФ 20 фл. 200мл Россия"));
			Assert.That(line.SerialNumber, Is.EqualTo("032012"));
			Assert.That(line.Period, Is.EqualTo("01.03.2014"));
			Assert.That(line.Quantity, Is.EqualTo(1));
			Assert.That(line.VitallyImportant, Is.False);
			Assert.That(line.Producer, Is.EqualTo("Мишель СМ ООО"));
			Assert.That(line.Country, Is.EqualTo("Россия"));
			Assert.That(line.BillOfEntryNumber, Is.Null);
			Assert.That(line.ProducerCostWithoutNDS, Is.EqualTo(0));
			Assert.That(line.SupplierCostWithoutNDS, Is.EqualTo(157.2));
			Assert.That(line.Amount, Is.EqualTo(185.5));
			Assert.That(line.NdsAmount, Is.EqualTo(28.3));
			Assert.That(line.Nds, Is.EqualTo(18));
			Assert.That(line.RegistryCost, Is.EqualTo(0));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.ПК08.Д07127"));
			Assert.That(line.CertificatesDate, Is.EqualTo("12.01.2012"));
		}
	}
}
