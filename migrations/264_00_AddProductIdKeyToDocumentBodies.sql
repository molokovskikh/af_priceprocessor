alter table documents.DocumentBodies 
  add index IDX_DocumentBodies_SerialNumber (SerialNumber),
  add CONSTRAINT FK_DocumentBodies_ProductId FOREIGN KEY (ProductId) REFERENCES catalogs.Products (Id) ON DELETE RESTRICT ON UPDATE RESTRICT;
