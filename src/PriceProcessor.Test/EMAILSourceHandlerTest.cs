using System;
using System.Collections.Generic;
using System.Text;
using Inforoom.Downloader;
using NUnit.Framework;

namespace PriceProcessor.Test
{
	[TestFixture]
	public class EMAILSourceHandlerTest : EMAILSourceHandler
	{
		[Test]
		public void IsMailAddresTest()
		{
			Assert.AreEqual(true, IsMailAddress("test@analit.net"), "Адрес некорректен");
			Assert.AreEqual(false, IsMailAddress("zakaz"), "Адрес некорректен");
			Assert.AreEqual(false, IsMailAddress("zakaz@"), "Адрес некорректен");
			Assert.AreEqual(true, IsMailAddress("zakaz@dsds"), "Адрес некорректен");
			Assert.AreEqual(true, IsMailAddress("<'prices@spb.analit.net'>"), "Адрес некорректен");
		}

	}
}
