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

        public struct SynonymInfo
        {
            public uint FirmCode;
            public uint ProductId;
            public bool Junk;
        }

        public struct SynonymSummary
        {
            public SynonymSummary(string name)
            {
                _originalName = name;
                _summary = new List<SynonymInfo>();
            }
            public void AddInfo(uint code, uint productId, bool junk)
            {
                if (_summary.Where(i => i.FirmCode == code).Count() > 1) return;
                var info = new SynonymInfo() { FirmCode = code, ProductId = productId, Junk = junk };
                _summary.Add(info);
            }
            public IList<SynonymInfo> Summary()
            {
                return _summary;
            }
            public string OriginalName()
            {
                return _originalName;
            }

            private readonly string _originalName;
            private readonly IList<SynonymInfo> _summary;
        }
    
        protected Dictionary<string, SynonymSummary> matches;

        public IndexerHandler()
        {
            SleepTime = 60;
            _workTime = TimeSpan.Parse("02:00:00");            
            IdxDir = "Idx";            
            matches = new Dictionary<string, SynonymSummary>();
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
            if (!Directory.Exists(IdxDir)) // если при запуске индекса нет - делаем его
                DoIndex();
            _now = DateTime.Now.TimeOfDay;
            if (!CanExec()) return;            
            // производим индексацию данных
            DoIndex();
        }

        protected void DoIndex()
        {                        
            if (Directory.Exists(IdxDir)) Directory.Delete(IdxDir, true);
            _logger.Info("Загрузка синонимов из БД...");
//#if (DEBUG)            
//            IList<SynonymProduct> synonyms = SynonymProduct.Queryable.Where(s => s.Synonym.StartsWith("Т")).ToList();
//#else
            IList<SynonymProduct> synonyms = SynonymProduct.Queryable.Select(s => s).ToList();
//#endif

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
                            "ProductId",
                            synonym.ProductId.ToString(),
                            Field.Store.YES,
                            Field.Index.NO));
                    doc.Add(
                       new Field(
                           "Junk",
                           synonym.Junk.ToString(),
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

        public Dictionary<string, SynonymSummary> DoMatching(IList<string> positions)
        {
            _logger.InfoFormat("Старт сопоставления для {0} позиций", positions.Count());
            FSDirectory IdxDirectory = FSDirectory.Open(new System.IO.DirectoryInfo(IdxDir));
            IndexReader reader = IndexReader.Open(IdxDirectory, true);
            IndexSearcher searcher = new IndexSearcher(reader);
            KeywordAnalyzer analyzer = new KeywordAnalyzer();
            QueryParser parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_29, "Synonym", analyzer);
            try
            {
                foreach (var position in positions)
                {
                    string name = position.Trim().ToUpper();
                    if (matches.ContainsKey(name)) continue;
                    Query query = parser.Parse(String.Format("Synonym:\"{0}\"", name));
                    TopScoreDocCollector collector = TopScoreDocCollector.create(1000, true);
                    searcher.Search(query, collector);
                    ScoreDoc[] hits = collector.TopDocs().scoreDocs;
                    foreach (var scoreDoc in hits)
                    {
                        Document document = searcher.Doc(scoreDoc.doc);
                        if(!matches.ContainsKey(name))
                            matches[name] = new SynonymSummary(position);                        
                        matches[name].AddInfo(Convert.ToUInt32(document.Get("FirmCode")),
                                              Convert.ToUInt32(document.Get("ProductId")),
                                              Convert.ToBoolean(document.Get("Junk")));
                    }                    
                }
            }
            finally
            {
                reader.Close();
                searcher.Close();
                analyzer.Close();
                IdxDirectory.Close();
            }
            _logger.Info("Сопоставление завершено");
            return matches;
        }

        public static string[] TransformToStringArray(Dictionary<string, SynonymSummary> matches)
        {
            string[] result = new string[matches.Count];
            int i = 0;
            foreach (var key in matches)
            {
                string res = String.Empty;
                var summary = key.Value.Summary();
                res += summary.Count.ToString(); res += ";";
                foreach (var synonymInfo in summary)
                {
                    res += synonymInfo.FirmCode.ToString(); res += ";";
                    res += synonymInfo.ProductId.ToString(); res += ";";
                    res += synonymInfo.Junk.ToString(); res += ";";
                }
                res += key.Value.OriginalName(); res += ";";
                result[i] = res;
                i++;
            }
            return result;
        }
    }
}
