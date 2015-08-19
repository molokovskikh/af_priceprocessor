create table Documents.SupplierMaps (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Name VARCHAR(255),
       SupplierId INTEGER UNSIGNED,
       primary key (Id)
    );
alter table Documents.SupplierMaps add index (SupplierId), add constraint FK_Documents_SupplierMaps_SupplierId foreign key (SupplierId) references Customers.Suppliers (Id) on delete cascade;
