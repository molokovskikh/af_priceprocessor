update farm.FormRules fr
join farm.pricefmts p on p.Id = fr.PriceFormatId
set
fr.PriceFormatId = 11
where p.Format = 'NativeDelim' and fr.PriceEncode = 1251;

update farm.FormRules fr
join farm.pricefmts p on p.Id = fr.PriceFormatId
set
fr.PriceFormatId = 12
where p.Format = 'NativeDelim' and fr.PriceEncode = 866;

update farm.FormRules fr
join farm.pricefmts p on p.Id = fr.PriceFormatId
set
fr.PriceFormatId = 13
where p.Format = 'NativeFixed' and fr.PriceEncode = 1251;

update farm.FormRules fr
join farm.pricefmts p on p.Id = fr.PriceFormatId
set
fr.PriceFormatId = 14
where p.Format = 'NativeFixed' and fr.PriceEncode = 866;