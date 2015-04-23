alter table Documents.RejectLines add column ProducerId INTEGER UNSIGNED;
alter table Documents.RejectLines add index (ProducerId), add constraint FK_Documents_RejectLines_ProducerId foreign key (ProducerId) references Catalogs.Producers (Id) on delete set null;
