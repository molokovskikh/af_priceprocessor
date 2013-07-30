using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;

namespace PriceProcessor.Test.Waybills.Parser
{
	[TestFixture]
	class CalculateValuesFixture
	{
		[Test]
		public void DelayOfPayment()
		{
			var document = new Document();
			var invoice = new Invoice();
			invoice.Document = document;

			invoice.InvoiceDate = null;
			invoice.DateOfPaymentDelay = "02.08";
			invoice.CalculateValues();
			Assert.That(invoice.DelayOfPaymentInDays, Is.Null);

			invoice.InvoiceDate = new DateTime(2012, 7, 12);
			invoice.CalculateValues();
			Assert.That(invoice.DelayOfPaymentInDays, Is.EqualTo(21));

			invoice.DelayOfPaymentInDays = null;
			invoice.DateOfPaymentDelay = "02.08.2013";
			invoice.CalculateValues();
			Assert.That(invoice.DelayOfPaymentInDays, Is.EqualTo(386));

			invoice.DelayOfPaymentInDays = null;
			invoice.DateOfPaymentDelay = "-";
			invoice.CalculateValues();
			Assert.That(invoice.DelayOfPaymentInDays, Is.Null);
		}
	}
}
