update Farm.FormRules
set PriceFormatId = (select Id from Farm.PriceFmts where ParserClassName = 'FixedNativeTextParser1251')
where PriceFormatId = (select Id from Farm.PriceFmts where ParserClassName = 'FixedTextParser1251');

update Farm.FormRules
set PriceFormatId = (select Id from Farm.PriceFmts where ParserClassName = 'FixedNativeTextParser866')
where PriceFormatId = (select Id from Farm.PriceFmts where ParserClassName = 'FixedTextParser866');

update Farm.FormRules
set PriceFormatId = (select Id from Farm.PriceFmts where ParserClassName = 'DelimiterNativeTextParser866')
where PriceFormatId = (select Id from Farm.PriceFmts where ParserClassName = 'DelimiterTextParser866');

update Farm.FormRules
set PriceFormatId = (select Id from Farm.PriceFmts where ParserClassName = 'DelimiterNativeTextParser1251')
where PriceFormatId = (select Id from Farm.PriceFmts where ParserClassName = 'DelimiterTextParser1251');

update Farm.FormRules
set PriceFormatId = (select Id from Farm.PriceFmts where ParserClassName = 'NativeDbfPriceParser')
where PriceFormatId = (select Id from Farm.PriceFmts where ParserClassName = 'PriceDbfParser');

update Farm.FormRules
set PriceFormatId = (select Id from Farm.PriceFmts where ParserClassName = 'NativeExcelParser')
where PriceFormatId = (select Id from Farm.PriceFmts where ParserClassName = 'ExcelParser');

delete from Farm.PriceFmts
where ParserClassName in ('ExcelParser', 'PriceDbfParser', 'DelimiterTextParser1251', 'DelimiterTextParser866', 'FixedTextParser1251', 'FixedTextParser1251');

update Farm.PriceFmts
set ParserClassName = 'ExcelParser'
where ParserClassName like 'NativeExcelParser';

update Farm.PriceFmts
set ParserClassName = 'PriceDbfParser'
where ParserClassName like 'NativeDbfPriceParser';

update Farm.PriceFmts
set ParserClassName = 'DelimiterTextParser1251'
where ParserClassName like 'DelimiterNativeTextParser1251';

update Farm.PriceFmts
set ParserClassName = 'DelimiterTextParser866'
where ParserClassName like 'DelimiterNativeTextParser866';

update Farm.PriceFmts
set ParserClassName = 'FixedTextParser866'
where ParserClassName like 'FixedNativeTextParser866';

update Farm.PriceFmts
set ParserClassName = 'FixedTextParser1251'
where ParserClassName like 'FixedNativeTextParser1251';

delete from Farm.PriceFmts
where ParserClassName like 'CommerceMLParser';
