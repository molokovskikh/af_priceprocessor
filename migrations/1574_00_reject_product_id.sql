alter table Documents.RejectLines add column ProductId INTEGER UNSIGNED;
alter table Documents.RejectLines add index (ProductId), add constraint FK_Documents_RejectLines_ProductId foreign key (ProductId) references Catalogs.Products (Id) on delete set null;
