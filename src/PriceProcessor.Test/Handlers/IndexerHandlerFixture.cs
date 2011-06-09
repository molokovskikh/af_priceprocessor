﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Inforoom.PriceProcessor;
using NUnit.Framework;
using Test.Support;

namespace PriceProcessor.Test.Handlers
{
    public class TestIndexerHandler : IndexerHandler
    {
        public TimeSpan Now 
        {   
            get { return _now; }
            set { _now = value; }
        }
        
        public TimeSpan WorkTime
        {
            get { return _workTime; }
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

        public Dictionary<string, SynonymSummary> DoMatching(IList<string> positions)
        {
            return base.DoMatching(positions);
        }

        public bool CanExec()
        {
            return base.CanExec();
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
            bool res = _handler.CanExec();
            Assert.That(res, Is.False);
            dt = dt.AddMinutes(1);
            _handler.Now = dt.TimeOfDay;
            res = _handler.CanExec();
            Assert.That(res, Is.False);
            dt = dt.AddSeconds(15);
            _handler.Now = dt.TimeOfDay;
            res = _handler.CanExec();
            Assert.That(res, Is.True);
            dt = dt.AddSeconds(45);
            _handler.Now = dt.TimeOfDay;
            res = _handler.CanExec();
            Assert.That(res, Is.True);
            dt = dt.AddSeconds(1);
            _handler.Now = dt.TimeOfDay;
            res = _handler.CanExec();
            Assert.That(res, Is.False);            
        }
       
        [Test]
        public void DoMatchTest()
        {
            TestPrice price1 = TestPrice.Queryable.FirstOrDefault();
            TestPrice price2 = TestPrice.Queryable.Where(p => p.Id != price1.Id && p.Supplier.Id != price1.Supplier.Id).FirstOrDefault();

            TestProduct product = TestProduct.Queryable.FirstOrDefault();

            DateTime now = DateTime.Now;

            IList<string> names = new List<string>();
            names.Add(String.Format("Тестовое наименование 1 ({0})", now.ToString()));
            names.Add(String.Format("Тестовое наименование 2 ({0})", now.ToString()));
            names.Add(String.Format("Тестовое наименование 3 ({0})", now.ToString()));
            names.Add(String.Format("Тестовое наименование 4 ({0})", now.ToString()));
            names.Add(String.Format("Тестовое наименование 5 ({0})", now.ToString()));

            using (new TransactionScope())
            {
                new TestSynonym() { ProductId = (int?)product.Id, PriceCode = (int?)price1.Id, Synonym = names[0] }.Save();
                new TestSynonym() { ProductId = (int?)product.Id, PriceCode = (int?)price1.Id, Synonym = names[1] }.Save();
                new TestSynonym() { ProductId = (int?)product.Id, PriceCode = (int?)price1.Id, Synonym = names[2] }.Save();
                new TestSynonym() { ProductId = (int?)product.Id, PriceCode = (int?)price2.Id, Synonym = names[3] }.Save();
                new TestSynonym() { ProductId = (int?)product.Id, PriceCode = (int?)price2.Id, Synonym = names[4] }.Save();
                new TestSynonym() { ProductId = (int?)product.Id, PriceCode = (int?)price1.Id, Synonym = names[4] }.Save();
            }
            
            _handler.DoIndex();

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

            var matches = _handler.DoMatching(names);

            var str_res = IndexerHandler.TransformToStringArray(matches);
                       
            Assert.That(matches.Count, Is.EqualTo(5));
            for (int i = 1; i <= 5; i++ )
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

            Assert.That(str_res.Length, Is.EqualTo(5));
            Assert.That(str_res[4], Is.EqualTo(String.Format("2;{0};{1};{2};{3};{4};{5};{6};", 
                price2.Supplier.Id, product.Id, "False", price1.Supplier.Id, product.Id, "False", names[4])));
        }

        [Test]
        [Ignore("Для проверки времени сопоставления")]
        public void DoMatchForBigPrice()
        {
            if (!Directory.Exists(_handler.IdxDir))
                _handler.DoIndex();

            IList<string> names = new List<string>();
            for (int i = 0; i < 2000; i++)
            {
                names.Add(String.Format("Наименование {0}", i));
            }

            DateTime begin = DateTime.Now;

            _handler.DoMatching(names);

            DateTime end = DateTime.Now;

            double diff = end.TimeOfDay.TotalSeconds - begin.TimeOfDay.TotalSeconds;

        }

        [Test]
        public void Temp()
        {

        }
    }
}
