using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Inforoom.Formalizer;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Loader
{
	[TestFixture]
	public class FarmaimpeksOKPFormalizerFixture : IntegrationFixture
	{
		private TestPriceItem _priceItem;
		private TestPrice _price;

		[SetUp]
		public void SetUp()
		{
			var supplier = TestSupplier.CreateNaked();
			_price = supplier.Prices[0];
			_price.PriceType = PriceType.Assortment;
			Save(_price);
			_priceItem = _price.Costs[0].PriceItem;
			_priceItem.Format.PriceFormat = PriceFormatType.FarmaimpeksOKPFormalizer;
			Save(_priceItem);
		}
		[Test]
		public void FormalizeTest()
		{
			Reopen();
			var formalizer = PricesValidator.Validate(@"..\..\Data\FarmimpeksOKP.xml", Path.GetTempFileName(), _priceItem.Id);
			formalizer.Formalize();
			formalizer.Formalize();

			var cores = session.Query<TestCore>().Where(c => c.Price.Id == _price.Id).ToList();
			Assert.That(cores.Count, Is.EqualTo(4));
			Assert.That(cores.Any(c => c.CodeOKP == 931201));
		}
	}
}
