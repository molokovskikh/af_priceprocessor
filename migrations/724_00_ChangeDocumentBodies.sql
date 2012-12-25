ALTER TABLE `documents`.`documentbodies` ADD COLUMN `TradeCost` DECIMAL(12,6) COMMENT 'оптовая цена' AFTER `CountryCode`,
 ADD COLUMN `SaleCost` DECIMAL(12,6) COMMENT 'отпускная цена' AFTER `TradeCost`,
 ADD COLUMN `RetailCost` DECIMAL(12,6) COMMENT 'розничная цена' AFTER `SaleCost`,
 ADD COLUMN `Cipher` VARCHAR(255) COMMENT 'Шифр' AFTER `RetailCost`;