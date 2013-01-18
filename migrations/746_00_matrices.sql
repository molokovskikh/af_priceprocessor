create table Farm.Matrices (
   Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
   primary key (Id)
);

alter table Farm.BuyingMatrix
add column MatrixId int unsigned,
add constraint FK_BuingMatrix_MatrixId foreign key (MatrixId)
references Farm.Matrices(Id)
on delete cascade;

alter table Usersettings.PricesData add column Matrix INTEGER UNSIGNED,
add constraint FK_PricesData_Matrix foreign key (Matrix)
references Farm.Matrices(Id) on delete set null
;

alter table Usersettings.PricesData add column CodeOkpFilterPrice INTEGER UNSIGNED,
add constraint FK_PricesData_CodeOkpFilterPrice foreign key(CodeOkpFilterPrice)
references usersettings.PricesData(PriceCode) on delete set null
;
