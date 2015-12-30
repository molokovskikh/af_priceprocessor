using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Common.MySql;
using Common.Tools;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Models;
using MySql.Data.MySqlClient;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;
using Test.Support.log4net;
using Test.Support.Suppliers;

namespace PriceProcessor.Test.Formalization
{
	[TestFixture]
	public class RejectsFixture : IntegrationFixture2
	{
		private DataTable data;
		private TestPrice price;
		private TestPriceItem priceItem;
		private Price realPrice;

		[SetUp]
		public void Setup()
		{
			data = new DataTable();
			data.Columns.Add("MNFGRNX");
			data.Columns.Add("MNFNX");
			data.Columns.Add("MNFNMR");
			data.Columns.Add("COUNTRYR");
			data.Columns.Add("DRUGTXT");
			data.Columns.Add("SERNM");
			data.Columns.Add("LETTERSNR");
			data.Columns.Add("LETTERSDT", typeof(DateTime));
			data.Columns.Add("LABNMR");
			data.Columns.Add("QUALNMR");

			data.Rows.Add(null, null,
				"Биохимик ОАО", null,
				"Диклофенак р-р для в/м введ 25 мг/мл  (ампулы) 3 мл №10",
				"171209",
				"04И-851/10",
				"02/09/2010",
				null,
				"Отзыв предприятием-производителем. Описание.Цветность.");

			data.Rows.Add(null, null,
				"Уралбиофарм ОАО", null,
				"Мукалтин табл. 50мг (упак. ячейковые контурные) № 10",
				"151109",
				"04И-849/10",
				"02/09/2010",
				null,
				"Описание. Средняя масса. Отклонение от средней массы.");

			var supplier = TestSupplier.CreateNaked(session);
			price = supplier.Prices[0];
			price.PriceType = PriceType.Assortment;
			priceItem = price.Costs[0].PriceItem;
			var format = priceItem.Format;
			format.PriceFormat = PriceFormatType.NativeDbf;
			format.FName1 = "DRUGTXT";
			format.FFirmCr = "MNFNMR";
			format.FCode = "SERNM";
			format.FCodeCr = "LETTERSNR";
			format.FNote = "LETTERSDT";
			format.FDoc = "QUALNMR";
			session.Save(price);
			session.Save(format);

			realPrice = session.Load<Price>(price.Id);
			realPrice.IsRejects = true;
			session.Save(realPrice);

			price.CreateAssortmentBoundSynonyms(session,
				"Диклофенак р-р для в/м введ 25 мг/мл (ампулы) 3 мл №10", "Биохимик ОАО");
		}

		[Test]
		public void Save_rejects()
		{
			Formalize();

			Assert.That(Rejects().Count, Is.EqualTo(2));
		}

		[Test]
		public void Update_reject()
		{
			Formalize();

			var reject = session.Query<Reject>().First(r => r.Product == "Диклофенак р-р для в/м введ 25 мг/мл (ампулы) 3 мл №10");

			data.Rows[0]["QUALNMR"] = "Подлинность.Спирт этиловый.";
			Formalize();

			session.Clear();
			reject = session.Get<Reject>(reject.Id);
			Assert.That(reject, Is.Null);
			Assert.That(Rejects().Count, Is.EqualTo(2));
		}

		[Test]
		public void Set_reject_cancel()
		{
			Formalize();

			realPrice = session.Load<Price>(realPrice.Id);
			realPrice.IsRejects = false;
			realPrice.IsRejectCancellations = true;
			session.Save(realPrice);

			data.Rows.Clear();
			data.Rows.Add(null, null,
				"Биохимик ОАО", null,
				"Диклофенак р-р для в/м введ 25 мг/мл (ампулы) 3 мл №10",
				"171209",
				"04И-849/10",
				"03/09/2010",
				null,
				"Разбраковка");
			Formalize();

			var reject = session.Query<Reject>().First(r => r.Product == "Диклофенак р-р для в/м введ 25 мг/мл (ампулы) 3 мл №10");
			Assert.That(reject.CancelDate, Is.EqualTo(new DateTime(2010, 09, 03)));
		}

		[Test]
		public void Process_forbidden()
		{
			session.Save(new TestForbidden(price, "Мукалтин табл. 50мг (упак. ячейковые контурные) № 10"));
			Formalize();
			Assert.That(Rejects().Count, Is.EqualTo(2));
		}

		private List<Reject> Rejects()
		{
			return session.Query<Reject>().ToList();
		}

		private void Formalize()
		{
			FlushAndCommit();
			var file = Path.GetTempFileName();
			Dbf.Save(data, file);
			var formalizer = PricesValidator.Validate(file, Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()),
				priceItem.Id);
			formalizer.Formalize();
		}
	}
}