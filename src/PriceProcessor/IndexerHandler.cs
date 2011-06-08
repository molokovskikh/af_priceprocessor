using System;
using System.Collections.Generic;
using System.Linq;
using Inforoom.PriceProcessor.Waybills;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Documents;
using Lucene.Net.QueryParsers;
using Lucene.Net.Store;
using Directory = System.IO.Directory;
using Document = Lucene.Net.Documents.Document;

namespace Inforoom.PriceProcessor
{
    public class IndexerHandler : AbstractHandler
    {
        protected TimeSpan _workTime;
        protected TimeSpan _now;
        protected string IdxDir;
        protected Dictionary<string, IList<uint>> matches;

        public IndexerHandler()
        {
            SleepTime = 60;
            _workTime = TimeSpan.Parse("02:00:00");
            IdxDir = "Idx";
            matches = new Dictionary<string, IList<uint>>();
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
            if (Directory.Exists(IdxDir)) Directory.Delete(IdxDir, true);
            _logger.Info("Загрузка синонимов из БД...");
            IList<SynonymProduct> synonyms = SynonymProduct.Queryable.Select(s => s).ToList();
            _logger.InfoFormat("Загрузили {0} синонимов", synonyms.Count());

            FSDirectory IdxDirectory = FSDirectory.Open(new System.IO.DirectoryInfo(IdxDir));         
            KeywordAnalyzer analyzer = new KeywordAnalyzer();
            IndexWriter writer = new IndexWriter(IdxDirectory, analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED);
            try
            {
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
            }
            finally
            {
                writer.Close();
                analyzer.Close();
                IdxDirectory.Close();
            }
            _logger.Info("Индексация завершена");
        }

        public void DoMatching(IList<string> positions)
        {
            FSDirectory IdxDirectory = FSDirectory.Open(new System.IO.DirectoryInfo(IdxDir));
            IndexReader reader = IndexReader.Open(IdxDirectory, true);
            IndexSearcher searcher = new IndexSearcher(reader);
            KeywordAnalyzer analyzer = new KeywordAnalyzer();
            QueryParser parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_29, "Synonym", analyzer);
            try
            {
                foreach (var position in positions)
                {                    
                    Query query = parser.Parse(String.Format("Synonym:\"{0}\"", position));
                    TopScoreDocCollector collector = TopScoreDocCollector.create(1000, true);
                    searcher.Search(query, collector);
                    ScoreDoc[] hits = collector.TopDocs().scoreDocs;
                    foreach (var scoreDoc in hits)
                    {
                        Document document = searcher.Doc(scoreDoc.doc);
                        if (!matches.ContainsKey(position))
                            matches[position] = new List<uint>();
                        matches[position].Add(Convert.ToUInt32(document.Get("FirmCode")));                        
                    }
                    matches[position] = matches[position].Distinct().ToList();
                }
            }
            finally
            {
                reader.Close();
                searcher.Close();
                analyzer.Close();
                IdxDirectory.Close();
            }
        }
    }
}
