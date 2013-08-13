update `farm`.`FormRules` set `PriceEncode` = 866;

ALTER TABLE `farm`.`FormRules` MODIFY COLUMN `PriceEncode` INT(3) UNSIGNED NOT NULL DEFAULT 0;

update `farm`.`FormRules` fr
join farm.pricefmts p on p.Id = fr.PriceFormatId
 set `PriceEncode` = 1251
 where p.ParserClassName = 'DelimiterTextParser1251' or p.ParserClassName = 'FixedTextParser1251';

update `farm`.`FormRules` fr
 set PriceFormatId = 11
 where PriceFormatId = 12;

 update `farm`.`FormRules` fr
 set PriceFormatId = 13
 where PriceFormatId = 14;

 delete from farm.pricefmts
where id = 12 or id = 14;

update farm.pricefmts
set
`Format` = 'NativeDelim',
ParserClassName = 'DelimiterTextParser'
where id = 11;

update farm.pricefmts
set
`Format` = 'NativeFixed',
ParserClassName = 'FixedTextParser'
where id = 13;

update farm.pricefmts
set Comment = 'Текстовый файл с разделителями без Jet'
where id = 11;

update farm.pricefmts
set Comment = 'Текстовый файл с фиксированной шириной колонок без Jet'
where id = 13;