ALTER TABLE `usersettings`.`PricesData` MODIFY COLUMN `IsUpdate` TINYINT(1) UNSIGNED NOT NULL DEFAULT 0,
 ADD COLUMN `IsStrict` TINYINT(1) UNSIGNED NOT NULL DEFAULT 1 AFTER `IsUpdate`;