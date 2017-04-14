using System;
using System.Data;
using System.Linq;
using Common.Tools;
using Inforoom.Formalizer;
using Inforoom.PriceProcessor.Formalizer.Core;
using Inforoom.PriceProcessor.Models;
using NUnit.Framework;
using PriceProcessor.Test.TestHelpers;
using Test.Support;

namespace PriceProcessor.Test
{
	//[TestFixture, Ignore("Тест что бы разбирать проблемные ситуации")]
	[TestFixture]
	public class Troubleshoot
	{
		[Test]
		public void t()
		{
			var session = IntegrationFixture2.Factory.OpenSession();
			var table = PricesValidator.LoadFormRules(223);
			var row = table.Rows[0];
			var localPrice = session.Load<Price>(1864u);
			var info = new PriceFormalizationInfo(row, localPrice);
			var formalizer = new BufferFormalizer(@"C:\Users\kvasov\tmp\analit.txt", info);
			formalizer.Formalize();
		}
	}
}
