alter table Customers.Suppliers
add column UseSupplierCodes tinyint(1) unsigned not null default 0;

update Customers.Suppliers
set UseSupplierCodes = 1
where Id in (11288, 3492, 149, 3746, 216, 12675, 12718, 12714, 2754);
