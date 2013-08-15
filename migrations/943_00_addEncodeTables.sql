update `farm`.`FormRules` set `PriceEncode` = 866;

ALTER TABLE `farm`.`FormRules` MODIFY COLUMN `PriceEncode` INT(3) UNSIGNED NOT NULL DEFAULT 0;

insert into farm.pricefmts (Format, Comment, FileExtention, ParserClassName)
value ('NativeDelim', 'Текстовый файл с разделителями без Jet', '.txt', 'DelimiterTextParser');

set @NativeDelim = LAST_INSERT_ID();

update farm.FormRules
set PriceFormatId = @NativeDelim
where PriceFormatId = 11;

update farm.FormRules
set PriceEncode = 1251
where PriceFormatId = @NativeDelim;

update farm.FormRules
set PriceFormatId = @NativeDelim
where PriceFormatId = 12;

insert into farm.pricefmts (Format, Comment, FileExtention, ParserClassName)
value ('NativeFixed', 'Текстовый файл с фиксированной шириной колонок без Jet', '.txt', 'FixedTextParser');

set @NativeFixedId = LAST_INSERT_ID();

update farm.FormRules
set PriceFormatId = @NativeFixedId
where PriceFormatId = 13;

update farm.FormRules
set PriceEncode = 1251
where PriceFormatId = @NativeFixedId;

update farm.FormRules
set PriceFormatId = @NativeFixedId
where PriceFormatId = 14;