using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Inforoom.PriceProcessor.Waybills;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Documents;
using Document = Lucene.Net.Documents.Document;

namespace Inforoom.PriceProcessor
{
    public class IndexerHandler : AbstractHandler
    {
        protected TimeSpan _workTime;

        protected TimeSpan _now;

        protected string IdxDir;

        public IndexerHandler()
        {
            SleepTime = 60;
            _workTime = TimeSpan.Parse("02:00:00");
            IdxDir = "Idx";
        }

        protected bool CanExec()
        {            
            double diff = _now.TotalSeconds - _workTime.TotalSeconds;
            if (diff > 0 && diff <= SleepTime)
                return true;
            return false;
        }

        protected override void ProcessData()
        {
            _now = DateTime.Now.TimeOfDay;
            if (!CanExec()) return;            
            // производим индексацию данных
            DoIndex();
        }

        protected void DoIndex()
        {
            if(Directory.Exists(IdxDir)) Directory.Delete(IdxDir, true);
            _logger.Info("Загрузка синонимов из БД...");
            IList<SynonymProduct> synonyms = SynonymProduct.Queryable.Select(s => s).ToList();
            _logger.InfoFormat("Загрузили {0} синонимов", synonyms.Count());
            KeywordAnalyzer analyzer = new KeywordAnalyzer();
            IndexWriter writer = new IndexWriter(IdxDir, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
            _logger.Info("Старт индексации синонимов...");
            foreach (var synonym in synonyms)
            {
                Document doc = new Document();
                doc.Add(
                  new Field(
                    "FirmCode",                    
                    synonym.Price.Supplier.Id.ToString(),
                    Field.Store.YES,
                    Field.Index.NO));
                doc.Add(
                  new Field(
                    "Synonym",                    
                    synonym.Synonym.Trim().ToUpper(),
                    Field.Store.YES,
                    Field.Index.TOKENIZED));
                writer.AddDocument(doc);
            }
            _logger.Info("Оптимизация индекса...");
            writer.Optimize();
            writer.Close();
            _logger.Info("Индексация завершена");
        }

        public static void DoMatching(IList<string> positions)
        {
            
        }
    }
}
