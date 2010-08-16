delete from farm.Excludes;
alter table farm.Excludes
drop column ProducerSynonymId,
add column ProducerSynonym varchar(255) not null after PriceCode;
