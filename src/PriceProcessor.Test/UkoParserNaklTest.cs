using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor.Waybills.Models;
using Inforoom.PriceProcessor.Waybills.Parser.SstParsers;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Transform;
using NUnit.Framework;
using PriceProcessor.Test.Waybills.Parser;
using Test.Support.log4net;

namespace PriceProcessor.Test
{
	public class ShortDocument
	{
		public DateTime WriteTime { get; set; }
		public uint FirmCode { get; set; }
		public uint AddressId { get; set; }
		public long DownloadId { get; set; }
	}

	[TestFixture]
	public class UkoParserNaklTest
	{
		public static void Evict(ISession session, IEnumerable items)
		{
			foreach (var item in items)
				session.Evict(item);
		}

		public static IList<T> WithSession<T>(Func<ISession, IList<T>> sessionDelegate)
		{
			var sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
			var session = sessionHolder.CreateSession(typeof(ActiveRecordBase));
			try
			{
				var result = sessionDelegate(session);
				Evict(session, result);
				return result;
			}
			finally
			{
				sessionHolder.ReleaseSession(session);
			}
		}

		[Test, Ignore("Этот тест позволяет пойти и посомтреть всех поставщиков, обрабатываемых UkonParser и понять, сколькие из них не обрабатываются")]
		public void Ukon_parser_check_format_in_all_waybills()
		{
			var documemnts = WithSession(session => session.CreateSQLQuery(@"
SELECT  d.WriteTime, d.FirmCode, d.AddressId, d.DownloadId FROM documents.documentheaders d
where parser like '%UkonParser%' and writetime > curdate() - interval 1 month #and downloadid is not null
order by writeTime desc;").SetResultTransformer(Transformers.AliasToBean(typeof(ShortDocument))).List<ShortDocument>());
			var groups = documemnts.GroupBy(d => d.FirmCode);
			foreach (var @group in groups) {
				var docs = group.OrderByDescending(d => d.WriteTime).ToList();
				foreach (var document in docs.Take(2)) {
					var file = Directory.GetFiles(string.Format(@"\\adc.analit.net\Inforoom\AptBox\{0}\Waybills\", document.AddressId), document.DownloadId + "*").FirstOrDefault();
					try {
						var doc = WaybillParser.Parse(file);
					}
					catch (Exception e) {
						Assert.That(docs.Count, Is.LessThan(50));
						Assert.That(e.Message, Is.EqualTo("Не удалось определить тип парсера"));
					}
				}
			}
		}
	}
}
