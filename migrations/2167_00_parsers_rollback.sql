alter table Customers.ParserLines drop foreign key FK_Customers_ParserLines_Parser;
alter table Customers.Parsers drop foreign key FK_Customers_Parsers_Supplier;
drop table if exists Customers.Parsers;
drop table if exists Customers.ParserLines;
