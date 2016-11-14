alter table documents.invoiceheaders add column IsSupplierAmount TINYINT(1) NOT NULL DEFAULT '0' COMMENT 'Общая стоимость товаров с налогом указана поставщиком';
