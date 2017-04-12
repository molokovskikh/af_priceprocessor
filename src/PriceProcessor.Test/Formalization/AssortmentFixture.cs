using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using Castle.ActiveRecord;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Formalizer.Core;
using Inforoom.PriceProcessor.Models;
using NHibernate;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;
using Test.Support;
using Test.Support.Suppliers;

namespace PriceProcessor.Test
{
	public class FakeReader : IReader
	{
		public IEnumerable<FormalizationPosition> Read()
		{
			throw new NotImplementedException();
		}

		public List<CostDescription> CostDescriptions { get; set; }

		public IEnumerable<Customer> Settings()
		{
			throw new NotImplementedException();
		}

		public void SendWarning(FormLog stat)
		{
			throw new NotImplementedException();
		}
	}

	[TestFixture(Description = "тесты для проверки функциональности ассортимента")]
	public class AssortmentFixture
	{
		private DataTable _producerSynonyms;
		private DataTable _assortiment;
		private DataTable _forbiddenProducers;
		private DataTable _monobrendAssortiment;
		[SetUp]
		public void SetUp()
		{
			_producerSynonyms = new DataTable();
			_producerSynonyms.Columns.Add("OriginalSynonym", typeof(string));
			_producerSynonyms.Columns.Add("Synonym", typeof(string));
			_producerSynonyms.Columns.Add("SynonymFirmCrCode", typeof(Int64));
			_producerSynonyms.Columns.Add("IsAutomatic", typeof(bool));
			_producerSynonyms.Columns.Add("CodeFirmCr", typeof(UInt32));
			_producerSynonyms.Columns.Add("InternalProducerSynonymId");
			_producerSynonyms.Columns["InternalProducerSynonymId"].AutoIncrement = true;

			_assortiment = new DataTable();
			_assortiment.Columns.Add("Id", typeof(uint));
			_assortiment.Columns.Add("CatalogId", typeof(uint));
			_assortiment.Columns.Add("ProducerId", typeof(uint));
			_assortiment.Columns.Add("Checked", typeof(bool));

			_monobrendAssortiment = new DataTable();
			_monobrendAssortiment.Columns.Add("Id", typeof(uint));
			_monobrendAssortiment.Columns.Add("CatalogId", typeof(uint));

			_forbiddenProducers = new DataTable();
			_forbiddenProducers.Columns.Add("Name", typeof(string));
		}
		[Test]
		public void ProducerInForbiddenListTest()
		{
			var newAssort = _assortiment.NewRow();
			newAssort["Id"] = 77;
			newAssort["CatalogId"] = 777;
			newAssort["ProducerId"] = 111;
			newAssort["Checked"] = true;
			_assortiment.Rows.Add(newAssort);

			var position = new FormalizationPosition {
				Pharmacie = true,
				FirmCr = "TestFirm",
				CatalogId = 777,
				Status = UnrecExpStatus.NameForm,
				Offer = new NewOffer()
			};

			var row = _forbiddenProducers.NewRow();
			row["Name"] = "TestFirm";
			_forbiddenProducers.Rows.Add(row);
			var resolver = new ProducerResolver(new FormalizeStats(), null, _producerSynonyms, new BasePriceParser.Barcode[0]);
			resolver.ForbiddenProdusers = _forbiddenProducers;
			resolver.Assortment = _assortiment;
			resolver.MonobrendAssortment = _monobrendAssortiment;
			resolver.ResolveProducer(position);
			Assert.IsNotNull(position.Offer.CreatedProducerSynonym);
			Assert.That(position.Offer.CreatedProducerSynonym["CodeFirmCr"], Is.EqualTo(DBNull.Value));
			Assert.IsFalse((bool)position.Offer.CreatedProducerSynonym["IsAutomatic"]);
			Assert.IsTrue(position.NotCreateUnrecExp);
		}

		[Test]
		public void Create_synonym_whith_producer_if_this_position_not_in_monobrend()
		{
			var producerSynonyms = new DataTable();
			producerSynonyms.Columns.Add("OriginalSynonym", typeof(string));
			producerSynonyms.Columns.Add("Synonym", typeof(string));
			producerSynonyms.Columns.Add("SynonymFirmCrCode", typeof(Int64));
			producerSynonyms.Columns.Add("IsAutomatic", typeof(bool));
			producerSynonyms.Columns.Add("CodeFirmCr", typeof(UInt32));
			producerSynonyms.Columns.Add("InternalProducerSynonymId");
			producerSynonyms.Columns["InternalProducerSynonymId"].AutoIncrement = true;

			var resolver = new ProducerResolver(new FormalizeStats(), null, producerSynonyms, new BasePriceParser.Barcode[0]);
			var position = new FormalizationPosition {
				Pharmacie = true,
				FirmCr = "TestFirm",
				CatalogId = 777,
				Status = UnrecExpStatus.NameForm,
				Offer = new NewOffer()
			};
			var assortiment = new DataTable();
			assortiment.Columns.Add("Id", typeof(uint));
			assortiment.Columns.Add("CatalogId", typeof(uint));
			assortiment.Columns.Add("ProducerId", typeof(uint));
			assortiment.Columns.Add("Checked", typeof(bool));
			var newAssort = assortiment.NewRow();
			newAssort["Id"] = 77;
			newAssort["CatalogId"] = 777;
			newAssort["ProducerId"] = 111;
			newAssort["Checked"] = true;
			assortiment.Rows.Add(newAssort);
			resolver.Assortment = assortiment;
			var monobrendAssortiment = new DataTable();
			monobrendAssortiment.Columns.Add("Id", typeof(uint));
			monobrendAssortiment.Columns.Add("CatalogId", typeof(uint));
			resolver.MonobrendAssortment = monobrendAssortiment;
			resolver.ResolveProducer(position);

			Assert.IsNotNull(position.Offer.CreatedProducerSynonym);
			Assert.That(position.Offer.CreatedProducerSynonym["CodeFirmCr"], Is.EqualTo(DBNull.Value));
			Assert.IsTrue((bool)position.Offer.CreatedProducerSynonym["IsAutomatic"]);
		}
		[Test]
		public void Create_synonym_whith_producer_if_this_position_in_monobrend()
		{
			var producerSynonyms = new DataTable();
			producerSynonyms.Columns.Add("OriginalSynonym", typeof(string));
			producerSynonyms.Columns.Add("Synonym", typeof(string));
			producerSynonyms.Columns.Add("SynonymFirmCrCode", typeof(Int64));
			producerSynonyms.Columns.Add("IsAutomatic", typeof(bool));
			producerSynonyms.Columns.Add("CodeFirmCr", typeof(UInt32));
			producerSynonyms.Columns.Add("InternalProducerSynonymId");
			producerSynonyms.Columns["InternalProducerSynonymId"].AutoIncrement = true;

			var resolver = new ProducerResolver(new FormalizeStats(), null, producerSynonyms, new BasePriceParser.Barcode[0]);
			var position = new FormalizationPosition {
				Pharmacie = true,
				FirmCr = "TestFirm",
				CatalogId = 777,
				Status = UnrecExpStatus.NameForm,
				Offer = new NewOffer()
			};
			var assortiment = new DataTable();
			assortiment.Columns.Add("Id", typeof(uint));
			assortiment.Columns.Add("CatalogId", typeof(uint));
			assortiment.Columns.Add("ProducerId", typeof(uint));
			assortiment.Columns.Add("Checked", typeof(bool));
			var newAssort = assortiment.NewRow();
			newAssort["Id"] = 77;
			newAssort["CatalogId"] = 777;
			newAssort["ProducerId"] = 111;
			newAssort["Checked"] = true;
			assortiment.Rows.Add(newAssort);
			resolver.Assortment = assortiment;

			var monobrendAssortiment = new DataTable();
			monobrendAssortiment.Columns.Add("Id", typeof(uint));
			monobrendAssortiment.Columns.Add("CatalogId", typeof(uint));
			var newMbAssort = monobrendAssortiment.NewRow();
			newMbAssort["Id"] = 77;
			newMbAssort["CatalogId"] = 777;
			monobrendAssortiment.Rows.Add(newMbAssort);
			resolver.MonobrendAssortment = monobrendAssortiment;

			resolver.ResolveProducer(position);

			Assert.IsNotNull(position.Offer.CreatedProducerSynonym);
			Assert.That(position.Offer.CreatedProducerSynonym["CodeFirmCr"], Is.EqualTo(111u));
			Assert.IsFalse(Convert.ToBoolean(position.Offer.CreatedProducerSynonym["IsAutomatic"]));
		}
	}
}