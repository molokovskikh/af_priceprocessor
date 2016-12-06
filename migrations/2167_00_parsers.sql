create table Customers.Parsers (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Name VARCHAR(255),
       Supplier INTEGER UNSIGNED,
       primary key (Id)
    );
create table Customers.ParserLines (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Src VARCHAR(255),
       Dst VARCHAR(255),
       DstType INTEGER,
       Parser INTEGER UNSIGNED,
       primary key (Id)
    );
alter table Customers.Parsers add index (Supplier), add constraint FK_Customers_Parsers_Supplier foreign key (Supplier) references Customers.Suppliers (Id) on delete cascade;
alter table Customers.ParserLines add index (Parser), add constraint FK_Customers_ParserLines_Parser foreign key (Parser) references Customers.Parsers (Id) on delete cascade;
