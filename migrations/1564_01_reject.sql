create table Documents.RejectLines (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Product VARCHAR(255),
       Producer VARCHAR(255),
       Cost NUMERIC(19,2),
       Ordered INTEGER UNSIGNED,
       Rejected INTEGER UNSIGNED,
       HeaderId INTEGER UNSIGNED,
       primary key (Id)
    );
create table Documents.RejectHeaders (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       WriteTime DATETIME,
       SupplierId INTEGER UNSIGNED,
       AddressId INTEGER UNSIGNED,
       DownloadId INTEGER UNSIGNED,
       primary key (Id)
    );
alter table Documents.RejectLines add index (HeaderId), add constraint FK_Documents_RejectLines_HeaderId foreign key (HeaderId) references Documents.RejectHeaders (Id) on delete cascade;
alter table Documents.RejectHeaders add index (SupplierId), add constraint FK_Documents_RejectHeaders_SupplierId foreign key (SupplierId) references Customers.Suppliers (Id) on delete cascade;
alter table Documents.RejectHeaders add index (AddressId), add constraint FK_Documents_RejectHeaders_AddressId foreign key (AddressId) references Customers.Addresses (Id) on delete cascade;
alter table Documents.RejectHeaders add index (DownloadId), add constraint FK_Documents_RejectHeaders_DownloadId foreign key (DownloadId) references logs.Document_logs (RowId) on delete set null;
