ALTER TABLE documents.DocumentBodies ADD COLUMN ProducerId INT(10) UNSIGNED NULL;

ALTER TABLE documents.DocumentBodies ADD INDEX IDX_DocumentBodies_ProducerId (ProducerId);

ALTER TABLE documents.DocumentBodies ADD CONSTRAINT FK_DocumentBodies_ProducerId
FOREIGN KEY (ProducerId) REFERENCES catalogs.Producers (Id) ON DELETE RESTRICT ON UPDATE RESTRICT;