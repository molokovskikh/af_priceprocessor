create table Documents.MailRecipients
(
  Id int unsigned not null auto_increment,
  MailId int unsigned not null,
  Type int not null default 0,
  RegionId bigint(20) unsigned DEFAULT NULL,
  ClientId int unsigned DEFAULT NULL,
  AddressId int unsigned DEFAULT NULL,
  primary key (Id),
  CONSTRAINT `FK_MailRecipients_MailId` FOREIGN KEY (MailId) REFERENCES Documents.Mails (Id) ON DELETE CASCADE,
  CONSTRAINT `FK_MailRecipients_RegionId` FOREIGN KEY (RegionId) REFERENCES farm.Regions (RegionCode) ON DELETE CASCADE,
  CONSTRAINT `FK_MailRecipients_ClientId` FOREIGN KEY (ClientId) REFERENCES future.clients (Id) ON DELETE CASCADE,
  CONSTRAINT `FK_MailRecipients_AddressId` FOREIGN KEY (AddressId) REFERENCES future.Addresses (Id) ON DELETE CASCADE
);

alter table Documents.Mails
  drop FOREIGN KEY FK_Mails_RegionId;

alter table Documents.Mails
  drop KEY FK_Mails_RegionId;

alter table Documents.Mails
  drop column RegionId;

alter table Documents.Mails
  add column SupplierEmail varchar(255) not null after SupplierId,
  add key IDX_Mails_SupplierEmail (SupplierEmail);