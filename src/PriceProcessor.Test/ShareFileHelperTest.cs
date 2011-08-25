using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Inforoom.PriceProcessor;
using Inforoom.PriceProcessor.Helpers;
using NUnit.Framework;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class ShareFileHelperTest
	{
		[SetUp]
		public void Setup()
		{
			TestHelper.RecreateDirectories();
		}

		[Test]
		public void WaitFileTest()
		{
			var docRoot = Path.Combine(Settings.Default.DocumentPath, "12345");
			var waybillsPath = Path.Combine(docRoot, "Waybills");
			Directory.CreateDirectory(waybillsPath);
			var filename = Path.Combine(waybillsPath, "14356_4.dbf");
			File.Copy(@"..\..\Data\Waybills\14356_4.dbf", filename);
			bool exist = false;
			try
			{
				ShareFileHelper.WaitFile(filename);
				exist = true;
			}
			catch{}
			Assert.That(exist, Is.True);
			File.Delete(filename);
			try
			{
				ShareFileHelper.WaitFile(filename);
			}
			catch(Exception e)
			{
				Assert.That(e is WaitFileException, Is.True);
				Assert.That(e.Message, Is.EqualTo(String.Format("Файл {0} не появился в папке после 1000 мс ожидания.", filename)));
			}
			Thread thread = new Thread(() =>
			{
			    Thread.Sleep(2500);
				File.Copy(@"..\..\Data\Waybills\14356_4.dbf", filename);
			});			
			try
			{
				exist = false;				
				thread.Start();
				ShareFileHelper.WaitFile(filename, 5000);
				exist = true;
			}
			catch{}
			Assert.That(exist, Is.True);
		}
	}
}
