alter table farm.SynonymFirmCr
drop index `Synonym_Price`,
add unique `Producer_Synonym_Price` (`CodeFirmCr`, `Synonym`, `PriceCode`);
