using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom.PriceProcessor.Waybills.Parser.DbfParsers;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser.DbfParsers
{
	[TestFixture]
	internal class RiaPandaVolgogradParserFixture
	{
		/// <summary>
		/// К задаче
		/// http://redmine.analit.net/issues/29237
		/// </summary>
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse("605 3311.dbf");
			Assert.IsTrue(RiaPandaVolgogradParser.CheckFileFormat(Dbf.Load(@"..\..\Data\Waybills\605 3311.dbf")));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("33117"));
			Assert.That(document.DocumentDate, Is.EqualTo(new DateTime(2014, 11, 06)));
			Assert.That(document.Lines.Count, Is.EqualTo(13));

			var line = document.Lines[0];
			Assert.That(line.Product, Is.EqualTo("Dt. Vistong: Сироп Подорожника и Мать-и-мачехи, фл 150мл"));
			Assert.That(line.Code, Is.EqualTo("3303"));
			Assert.That(line.Period, Is.EqualTo("10.09.2016"));
			Assert.That(line.Quantity, Is.EqualTo(3));
			Assert.That(line.SerialNumber, Is.EqualTo("0914"));
			Assert.That(line.SupplierCost.ToString(), Is.EqualTo("77,79"));
			Assert.That(line.SupplierCostWithoutNDS.ToString(), Is.EqualTo("65,92"));
			Assert.That(line.Nds.ToString(), Is.EqualTo("18"));
			Assert.That(line.Producer, Is.EqualTo("ООО ВИС"));
			Assert.That(line.EAN13, Is.EqualTo(605));
			Assert.That(line.Certificates, Is.EqualTo("РОСС RU.АЯ61.Н12103"));
			Assert.That(line.CertificatesDate, Is.EqualTo("16.12.2016"));
			Assert.That(line.CertificateAuthority, Is.EqualTo("Таможенный Союз"));
			Assert.That(line.Amount, Is.EqualTo(233.37));
		}
	}
}
