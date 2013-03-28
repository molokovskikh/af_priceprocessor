insert into Farm.PriceFmts(Format, Comment, FileExtention, ParserClassName)
values ('NativeDelimWIN', 'Текстовый файл с разделителями кодировки Windows без Jet', '.txt', 'DelimiterTextParser1251'),
('NativeDelimDOS', 'Текстовый файл с разделителями кодировки DOS без Jet', '.txt', 'DelimiterTextParser866'),
('NativeFixedWIN', 'Текстовый файл с фиксированной шириной колонок кодировки Windows без Jet', '.txt', 'FixedTextParser1251'),
('NativeFixedDOS', 'Текстовый файл с фиксированной шириной колонок кодировки DOS без Jet', '.txt', 'FixedTextParser866'),
('NativeXls', 'Excel-файл', '.xls', 'ExcelParser'),
('NativeDbf', 'DBF-файл', '.dbf', 'PriceDbfParser');
