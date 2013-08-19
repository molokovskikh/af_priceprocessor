update farm.FormRules
set PriceFormatId = 19
where PriceFormatId = 11;

update farm.FormRules
set PriceEncode = 1251
where PriceFormatId = 19;

update farm.FormRules
set PriceFormatId = 19
where PriceFormatId = 12;


update farm.FormRules
set PriceFormatId = 21
where PriceFormatId = 13;

update farm.FormRules
set PriceEncode = 1251
where PriceFormatId = 21;

update farm.FormRules
set PriceFormatId = 21
where PriceFormatId = 14;

update farm.formrules f
set PriceEncode = 0
where PriceFormatId not in (8, 19, 21);