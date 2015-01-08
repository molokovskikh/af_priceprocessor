alter table Farm.AutomaticProducerSynonyms
add constraint FK_AutomaticProducerSynonym_PriceCode foreign key (PriceCode)
references Usersettings.PricesData(PriceCode) on delete cascade;
