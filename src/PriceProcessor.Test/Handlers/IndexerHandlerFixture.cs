using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Formalizer;
using Inforoom.PriceProcessor.Waybills;
using NUnit.Framework;

namespace PriceProcessor.Test.Handlers
{
    public class TestIndexerHandler : IndexerHandler
    {
        public TestIndexerHandler()
        {}

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

        public void DoMatching(IList<string> positions)
        {
            base.DoMatching(positions);
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
        public void DoIndexTest()
        {
            if (Directory.Exists(_handler.IdxDir))
                Directory.Delete(_handler.IdxDir, true);
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
        } 
       
        [Test]
        public void DoMatchTest()
        {     
            if(!Directory.Exists(_handler.IdxDir))
                DoIndexTest();
            IList<string> names = new List<string>();
            names.Add("90-60-90 (табл. N150");
            names.Add("А Т Ф (р-р д/ин. 1% 1 мл N10 амп.");
            names.Add("А-пар (аэроз. 125 г");
            names = names.Select(n => n.Trim().ToUpper()).ToList();
            _handler.DoMatching(names);
        }
    }
}
