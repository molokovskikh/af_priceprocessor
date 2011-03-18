alter TABLE documents.DocumentBodies add COLUMN ProductId INT(10) UNSIGNED NULL;

alter table documents.DocumentBodies add INDEX IDX_DocumentBodies_ProductID (ProductId);

alter table documents.DocumentBodies add CONSTRAINT FK_ProductId FOREIGN KEY (ProductId)
REFERENCES catalogs.Products (Id) ON DELETE RESTRICT ON UPDATE RESTRICT;