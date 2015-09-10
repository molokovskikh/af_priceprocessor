using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	public class KatrenMskKalugaParserFixture
	{
		[Test]
		public void Parse()
		{
			var document = WaybillParser.Parse(@"454666.dbf");
			Assert.That(document.Lines.Count, Is.EqualTo(2));
			Assert.That(document.ProviderDocumentId, Is.EqualTo("454666"));
			Assert.That(document.DocumentDate, Is.EqualTo(Convert.ToDateTime("08.08.2006")));
			Assert.That(document.Lines[0].Code, Is.EqualTo("1031022"));
			Assert.That(document.Lines[0].Product, Is.EqualTo("ЛЕДИ-С ФОРМУЛА БОЛЬШЕ ЧЕМ П/ВИТАМИНЫ N60 КАПС"));
			Assert.That(document.Lines[0].Producer, Is.EqualTo("ВИТАФАРМ КАНАДА Инк."));
			Assert.That(document.Lines[0].Country, Is.EqualTo("Канада"));
			Assert.That(document.Lines[0].Quantity, Is.EqualTo(1));
			Assert.That(document.Lines[0].SupplierCost, Is.EqualTo(372.85));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.EqualTo(315.97));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.EqualTo(289.90));
			Assert.That(document.Lines[0].ProducerCostWithoutNDS, Is.LessThanOrEqualTo(document.Lines[0].SupplierCost));
			Assert.That(document.Lines[0].SupplierCostWithoutNDS, Is.LessThanOrEqualTo(document.Lines[0].SupplierCost));
			Assert.That(document.Lines[0].Period, Is.EqualTo("01.11.2008"));
			Assert.That(document.Lines[0].VitallyImportant, Is.False);
			Assert.That(document.Lines[0].Nds.Value, Is.EqualTo(18));
			Assert.That(document.Lines[0].Certificates, Is.EqualTo("77.99.23.3.У.7647.12.04"));
			Assert.That(document.Lines[0].RegistryCost, Is.EqualTo(0));
			Assert.That(document.Lines[0].SerialNumber, Is.EqualTo("410010811"));

			Assert.That(document.Lines[1].VitallyImportant, Is.False);
		}

        /// <summary>
        /// Для задачи http://redmine.analit.net/issues/38525
        /// </summary>
        [Test]
        public void Parse2()
        {
            var document = WaybillParser.Parse(@"1360036-2.dbf");
            Assert.That(document.Lines[0].EAN13, Is.EqualTo("4607008131321"));
        }
    }
}