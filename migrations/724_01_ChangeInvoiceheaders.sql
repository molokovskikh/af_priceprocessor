ALTER TABLE `documents`.`invoiceheaders` ADD COLUMN `Cipher` VARCHAR(255) COMMENT 'Шифр' AFTER `CommissionFee`,
 ADD COLUMN `StoreName` VARCHAR(255) COMMENT 'Склад' AFTER `Cipher`;
