alter table Farm.Synonym
add SupplierCode varchar(20),
add index (SupplierCode);

alter table Farm.SynonymFirmCr
add SupplierCode varchar(20),
add index (SupplierCode);
