INSERT INTO Parsers (Name, Supplier) 
VALUES ('PulsRyazanParser', 163); 

set @parser_id = (SELECT id 
FROM Parsers
Where Supplier = 163 and Name = 'PulsRyazanParser'
LIMIT 1);

INSERT INTO Parserlines (Src, Dst, Parser) 
VALUES ('CODE', 'Code', @parser_id),
		 ('GOODE', 'Product', @parser_id),
		 ('PRODUCER', 'Producer', @parser_id),
		 ('COUNTRY', 'Country', @parser_id),
		 ('PRICE', 'SupplierCost', @parser_id),
		 ('QUANT', 'Quantity', @parser_id),
		 ('PPRICEWT', 'ProducerCostWithoutNDS', @parser_id),
		 ('DATEB', 'Period', @parser_id),
		 ('SERT', 'Certificates', @parser_id),
		 ('NDS', 'Nds', @parser_id),
		 ('REESTR', 'RegistryCost',  @parser_id),
		 ('JVLS', 'VitallyImportant', @parser_id),
		 ('SERIAL', 'SerialNumber', @parser_id);
		 