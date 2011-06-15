using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Waybills;
using NUnit.Framework;
using Test.Support;

namespace PriceProcessor.Test.Handlers
{
    public class TestIndexerHandler : IndexerHandler
    {
        public TimeSpan Now 
        {   
            get { return now; }
            set { now = value; }
        }
        
        public TimeSpan WorkTime
        {
            get { return workTime; }
        }

        public string IdxDir
        {
            get { return base.IdxDir; }
        }

        public void Process()
        {
            ProcessData();
        }

        public void DoIndex()
        {
            base.DoIndex();
        }

        public bool DoIndex(IList<SynonymProduct> synonyms, bool append, bool optimize)
        {
            return base.DoIndex(synonyms, append, optimize);
        }

        public bool CanDoIndex()
        {
            return base.CanDoIndex();
        }
    }

    [TestFixture]
    public class IndexerHandlerFixture
    {
        private TestIndexerHandler _handler;

        [SetUp]
        public void Setup()
        {
            _handler = new TestIndexerHandler();           
        }

        [Test]
        public  void ExecHandlerTest()
        {
            DateTime dt = new DateTime(_handler.WorkTime.Ticks).AddMinutes(-1);
            _handler.Now = dt.TimeOfDay;
            bool res = _handler.CanDoIndex();
            Assert.That(res, Is.False);
            dt = dt.AddMinutes(1);
            _handler.Now = dt.TimeOfDay;
            res = _handler.CanDoIndex();
            Assert.That(res, Is.False);
            dt = dt.AddSeconds(15);
            _handler.Now = dt.TimeOfDay;
            res = _handler.CanDoIndex();
            Assert.That(res, Is.True);
            dt = dt.AddSeconds(45);
            _handler.Now = dt.TimeOfDay;
            res = _handler.CanDoIndex();
            Assert.That(res, Is.True);
            dt = dt.AddSeconds(1);
            _handler.Now = dt.TimeOfDay;
            res = _handler.CanDoIndex();
            Assert.That(res, Is.False);            
        }
       
        [Test]
        public void DoMatchTest()
        {
            if (Directory.Exists(_handler.IdxDir)) Directory.Delete(_handler.IdxDir, true);

            Price price1 = Price.Queryable.FirstOrDefault();
            Price price2 = Price.Queryable.Where(p => p.Id != price1.Id && p.Supplier.Id != price1.Supplier.Id).FirstOrDefault();
            TestProduct product = TestProduct.Queryable.FirstOrDefault();
            
            DateTime now = DateTime.Now;

            IList<string > names = new List<string>();
            names.Add(String.Format("Тестовое наименование 1 ({0})", now));
            names.Add(String.Format("Тестовое наименование 2 ({0})", now));
            names.Add(String.Format("Тестовое наименование 3 ({0})", now));
            names.Add(String.Format("Тестовое наименование 4 ({0})", now));
            names.Add(String.Format("Тестовое наименование 5 ({0})", now));


            IList<SynonymProduct> synonyms = new List<SynonymProduct>();
            synonyms.Add(new SynonymProduct() { ProductId = (int?)product.Id, Junk = false, Price = price1, Synonym = names[0] });
            synonyms.Add(new SynonymProduct() { ProductId = (int?)product.Id, Junk = false, Price = price1, Synonym = names[1] });
            synonyms.Add(new SynonymProduct() { ProductId = (int?)product.Id, Junk = false, Price = price1, Synonym = names[2] });
            synonyms.Add(new SynonymProduct() { ProductId = (int?)product.Id, Junk = false, Price = price2, Synonym = names[3] });
            synonyms.Add(new SynonymProduct() { ProductId = (int?)product.Id, Junk = false, Price = price2, Synonym = names[4] });
            synonyms.Add(new SynonymProduct() { ProductId = (int?)product.Id, Junk = false, Price = price1, Synonym = names[4] });

            using (new TransactionScope())
            {
                foreach (var synonymProduct in synonyms)
                {
                    synonymProduct.Save();
                }                
            }

            if (Directory.Exists(_handler.IdxDir)) Directory.Delete(_handler.IdxDir, true);
            
            _handler.DoIndex(synonyms, false, true);

            Assert.That(Directory.Exists(_handler.IdxDir), Is.True);
            var files = Directory.GetFiles(_handler.IdxDir, "*.*");
            Assert.That(files.Count(), Is.GreaterThan(0));
            long size = 0;
            foreach (var file in files)
            {
                FileInfo f = new FileInfo(file);
                size += f.Length;
            }
            Assert.That(size, Is.GreaterThan(0));
            long taskId = _handler.AddTask(names, 0);
            Assert.That(_handler.GetTask(taskId), Is.Not.Null);
            while (_handler.GetTask(taskId).State == TaskState.Running) { Thread.Sleep(1000); }
            var matches = _handler.GetTask(taskId).Matches;
            var rate = _handler.GetTask(taskId).Rate;
            var str_res = IndexerHandler.TransformToStringArray(matches);

            Assert.That(rate, Is.EqualTo(100));
            Assert.That(matches.Count, Is.EqualTo(5));
            for (int i = 1; i <= 5; i++)
                Assert.That(matches.ContainsKey(String.Format("Тестовое наименование {0} ({1})", i, now.ToString()).ToUpper()));
            Assert.That(matches[names[0].Trim().ToUpper()].Summary().Count, Is.EqualTo(1));
            Assert.That(matches[names[1].Trim().ToUpper()].Summary().Count, Is.EqualTo(1));
            Assert.That(matches[names[2].Trim().ToUpper()].Summary().Count, Is.EqualTo(1));
            Assert.That(matches[names[3].Trim().ToUpper()].Summary().Count, Is.EqualTo(1));
            Assert.That(matches[names[4].Trim().ToUpper()].Summary().Count, Is.EqualTo(2));
            Assert.That(matches[names[0].Trim().ToUpper()].Summary()[0].FirmCode, Is.EqualTo(price1.Supplier.Id));
            Assert.That(matches[names[1].Trim().ToUpper()].Summary()[0].FirmCode, Is.EqualTo(price1.Supplier.Id));
            Assert.That(matches[names[2].Trim().ToUpper()].Summary()[0].FirmCode, Is.EqualTo(price1.Supplier.Id));
            Assert.That(matches[names[3].Trim().ToUpper()].Summary()[0].FirmCode, Is.EqualTo(price2.Supplier.Id));
            Assert.That(matches[names[4].Trim().ToUpper()].Summary()[0].FirmCode, Is.EqualTo(price2.Supplier.Id));
            Assert.That(matches[names[4].Trim().ToUpper()].Summary()[1].FirmCode, Is.EqualTo(price1.Supplier.Id));
            Assert.That(str_res.Length, Is.EqualTo(6));
            Assert.That(str_res[5], Is.EqualTo(String.Format("2;{0};{1};{2};{3};{4};{5};{6};{7};{8}",
                price2.Supplier.Id, price2.Supplier.Name + " (" + price2.Supplier.FullName + ")", product.Id, "False", price1.Supplier.Id, price1.Supplier.Name + " (" + price1.Supplier.FullName + ")", product.Id, "False", names[4])));

            names.Add(String.Format("Тестовое наименование 6 ({0})", now));
            synonyms.Clear();
            synonyms.Add(new SynonymProduct() { ProductId = (int?)product.Id, Junk = false, Price = price1, Synonym = names[5] });
            using (new TransactionScope()){ synonyms[0].Save(); }
            _handler.DoIndex(synonyms, true, false);
            
            taskId = _handler.AddTask(names, 0);
            Assert.That(_handler.GetTask(taskId), Is.Not.Null);
            while (_handler.GetTask(taskId).State == TaskState.Running) { Thread.Sleep(1000); }
            matches = _handler.GetTask(taskId).Matches;
            Assert.That(rate, Is.EqualTo(100));
            Assert.That(matches.Count, Is.EqualTo(6));
            for (int i = 1; i <= 6; i++)
                Assert.That(matches.ContainsKey(String.Format("Тестовое наименование {0} ({1})", i, now.ToString()).ToUpper()));
            Assert.That(matches[names[0].Trim().ToUpper()].Summary().Count, Is.EqualTo(1));
            Assert.That(matches[names[1].Trim().ToUpper()].Summary().Count, Is.EqualTo(1));
            Assert.That(matches[names[2].Trim().ToUpper()].Summary().Count, Is.EqualTo(1));
            Assert.That(matches[names[3].Trim().ToUpper()].Summary().Count, Is.EqualTo(1));
            Assert.That(matches[names[4].Trim().ToUpper()].Summary().Count, Is.EqualTo(2));
            Assert.That(matches[names[5].Trim().ToUpper()].Summary().Count, Is.EqualTo(1));
            Assert.That(matches[names[0].Trim().ToUpper()].Summary()[0].FirmCode, Is.EqualTo(price1.Supplier.Id));
            Assert.That(matches[names[1].Trim().ToUpper()].Summary()[0].FirmCode, Is.EqualTo(price1.Supplier.Id));
            Assert.That(matches[names[2].Trim().ToUpper()].Summary()[0].FirmCode, Is.EqualTo(price1.Supplier.Id));
            Assert.That(matches[names[3].Trim().ToUpper()].Summary()[0].FirmCode, Is.EqualTo(price2.Supplier.Id));
            Assert.That(matches[names[4].Trim().ToUpper()].Summary()[0].FirmCode, Is.EqualTo(price2.Supplier.Id));
            Assert.That(matches[names[4].Trim().ToUpper()].Summary()[1].FirmCode, Is.EqualTo(price1.Supplier.Id));
            Assert.That(matches[names[5].Trim().ToUpper()].Summary()[0].FirmCode, Is.EqualTo(price1.Supplier.Id));
        }
        
        [Test]
        [Ignore("Для проверки времени сопоставления")]
        public void DoMatchForBigPrice()
        {            
            _handler.DoIndex();

            IList<string> names = new List<string>();

            for (int i = 0; i < 2000; i++)
            {
                names.Add(String.Format("Наименование {0}", i));
            }

            DateTime begin = DateTime.Now;
            
            var matches = new Dictionary<string, SynonymSummary>();

            _handler.StartWork();

            long taskId = _handler.AddTask(names, 1);            

            while (_handler.GetTask(taskId).State == TaskState.Running)
            {
                Thread.Sleep(1000);               
            }

            DateTime end = DateTime.Now;

            SynonymTask task = _handler.GetTask(taskId);

            double diff = end.TimeOfDay.TotalSeconds - begin.TimeOfDay.TotalSeconds;            
        }
    }
}
