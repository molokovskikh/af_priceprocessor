using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Inforoom.PriceProcessor.Models;

namespace Inforoom.PriceProcessor.Waybills.Models
{
	public enum RecipientType
	{
		Address = 0,
		Region = 1,
		Client = 2
	}

	public enum RecipientStatus
	{
		Verified = 0,
		[Description("Не найдена ни одна аптека для данного адреса")] NotFound = 1,
		[Description("Аптека отключена в рамках системы АналитФармация")] Disabled = 2,
		[Description("Aптека находится вне Вашего региона работы в рамках системы АналитФармация")] NotAvailable = 3,
		[Description("письмо уже доставлялось данному получателю")] Duplicate = 4
	}

	[ActiveRecord("MailRecipients", Schema = "documents")]
	public class MailRecipient : ActiveRecordLinqBase<MailRecipient>
	{
		[PrimaryKey]
		public uint Id { get; set; }

		[Property]
		public RecipientType Type { get; set; }

		[BelongsTo("MailId")]
		public Mail Mail { get; set; }

		[BelongsTo("RegionId")]
		public Region Region { get; set; }

		[BelongsTo("ClientId")]
		public Client Client { get; set; }

		[BelongsTo("AddressId")]
		public Address Address { get; set; }

		public string Email { get; set; }

		public RecipientStatus Status { get; set; }

		public override bool Equals(object obj)
		{
			if (obj is MailRecipient) {
				var recipient = (MailRecipient)obj;
				return recipient.Type == Type && recipient.Email.Equals(Email, StringComparison.OrdinalIgnoreCase);
			}

			return base.Equals(obj);
		}

		public static MailRecipient Parse(string email)
		{
			email = email.Trim();
			MailRecipient recipient = null;

			if (email.EndsWith("@docs.analit.net")) {
				var addressIdStr = email.Substring(0, email.Length - "@docs.analit.net".Length);
				uint addressId;
				if (uint.TryParse(addressIdStr, out addressId)) {
					var address = Address.Queryable.FirstOrDefault(a => a.Id == addressId);
					if (address != null)
						recipient = new MailRecipient {
							Type = RecipientType.Address,
							Address = address,
							Email = email,
							Status = address.Enabled ? RecipientStatus.Verified : RecipientStatus.Disabled
						};
					else
						recipient = new MailRecipient {
							Type = RecipientType.Address,
							Email = email,
							Status = RecipientStatus.NotFound
						};
				}
				else {
					var region =
						Region.Queryable.FirstOrDefault(r => r.ShortAliase == addressIdStr);
					if (region != null)
						recipient = new MailRecipient {
							Type = RecipientType.Region,
							Region = region,
							Email = email
						};
					else
						recipient = new MailRecipient {
							Type = RecipientType.Region,
							Email = email,
							Status = RecipientStatus.NotFound
						};
				}
			}
			else if (email.EndsWith("@client.docs.analit.net")) {
				var clientIdStr = email.Substring(0, email.Length - "@client.docs.analit.net".Length);
				uint clientId;
				if (uint.TryParse(clientIdStr, out clientId)) {
					var client = Client.Queryable.FirstOrDefault(c => c.Id == clientId);
					if (client != null)
						recipient = new MailRecipient {
							Type = RecipientType.Client,
							Client = client,
							Email = email,
							Status = client.Enabled ? RecipientStatus.Verified : RecipientStatus.Disabled
						};
					else
						recipient = new MailRecipient {
							Type = RecipientType.Client,
							Email = email,
							Status = RecipientStatus.NotFound
						};
				}
			}

			return recipient;
		}

		public List<User> GetUsers(ulong regionMask)
		{
			var result = new List<User>();

			if (Type == RecipientType.Region && ((Region.Id & regionMask) > 0))
				result = User.Queryable.Where(u => u.Enabled && u.Client.Enabled && (u.WorkRegionMask & u.Client.MaskRegion & Region.Id) > 0).ToList();

			if (Type == RecipientType.Client)
				result = User.Queryable.Where(u => u.Enabled && u.Client.Enabled && u.Client.Id == Client.Id && (u.WorkRegionMask & u.Client.MaskRegion & regionMask) > 0).ToList();

			if (Type == RecipientType.Address)
				result = User.Queryable.Where(u => u.Enabled && u.Client.Enabled && u.AvaliableAddresses.Any(a => a.Id == Address.Id) && (u.WorkRegionMask & u.Client.MaskRegion & regionMask) > 0).ToList();

			return result;
		}
	}
}