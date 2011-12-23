﻿using System.Linq;
using Inforoom.PriceProcessor.Waybills.Models;
using NUnit.Framework;
using Test.Support;

namespace PriceProcessor.Test.Waybills
{
	[TestFixture]
	public class MailRecipientFixture
	{
		
		[Test]
		public void CheckOtherEmailDomen()
		{
			var recipient = MailRecipient.Parse("test@test.data");
			Assert.That(recipient, Is.Null);
		}

		[Test]
		public void CheckNonExistsAddress()
		{
			var address = TestAddress.Queryable.OrderByDescending(a => a.Id).First();
			var recipient = MailRecipient.Parse((address.Id+10) + "@docs.analit.net");
			Assert.That(recipient, Is.Null);
		}

		[Test]
		public void CheckNonExistsRegion()
		{
			var recipient = MailRecipient.Parse( "mlfds@docs.analit.net");
			Assert.That(recipient, Is.Null);
		}

		[Test]
		public void CheckNonExistsClient()
		{
			var client = TestClient.Queryable.OrderByDescending(c => c.Id).First();
			var recipient = MailRecipient.Parse((client.Id+10) + "@client.docs.analit.net");
			Assert.That(recipient, Is.Null);
		}

		[Test]
		public void CheckExistsAddress()
		{
			var address = TestAddress.Queryable.First();
			var recipient = MailRecipient.Parse(address.Id + "@docs.analit.net");
			Assert.That(recipient, Is.Not.Null);
			Assert.That(recipient.Email, Is.EqualTo(address.Id + "@docs.analit.net"));
			Assert.That(recipient.Type, Is.EqualTo(RecipientType.Address));
			Assert.That(recipient.Address, Is.Not.Null);
			Assert.That(recipient.Region, Is.Null);
			Assert.That(recipient.Client, Is.Null);
			Assert.That(recipient.Address.Id, Is.EqualTo(address.Id));
		}

		[Test]
		public void CheckExistsRegion()
		{
			var region = TestRegion.Find(1ul);
			var recipient = MailRecipient.Parse(region.ShortAliase + "@docs.analit.net");
			Assert.That(recipient, Is.Not.Null);
			Assert.That(recipient.Email, Is.EqualTo(region.ShortAliase + "@docs.analit.net"));
			Assert.That(recipient.Type, Is.EqualTo(RecipientType.Region));
			Assert.That(recipient.Address, Is.Null);
			Assert.That(recipient.Region, Is.Not.Null);
			Assert.That(recipient.Client, Is.Null);
			Assert.That(recipient.Region.Id, Is.EqualTo(region.Id));
		}

		[Test]
		public void CheckExistsClient()
		{
			var client = TestClient.Create();
			var recipient = MailRecipient.Parse(client.Id + "@client.docs.analit.net");
			Assert.That(recipient, Is.Not.Null);
			Assert.That(recipient.Email, Is.EqualTo(client.Id + "@client.docs.analit.net"));
			Assert.That(recipient.Type, Is.EqualTo(RecipientType.Client));
			Assert.That(recipient.Address, Is.Null);
			Assert.That(recipient.Region, Is.Null);
			Assert.That(recipient.Client, Is.Not.Null);
			Assert.That(recipient.Client.Id, Is.EqualTo(client.Id));
		}
	}
}