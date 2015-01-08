alter table Farm.AutomaticProducerSynonyms
add column PriceCode int unsigned,
add column SynonymText varchar(255),
add unique key SynonymText_PriceCode (PriceCode, SynonymText);
