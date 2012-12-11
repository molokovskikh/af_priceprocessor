CREATE TABLE `usersettings`.`WaybillDirtyFile` (
  `Id` INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
  `Supplier` INT(10) UNSIGNED,
  `Date` DATETIME NOT NULL,
  `File` VARCHAR(255) NOT NULL,
  PRIMARY KEY (`Id`),
  CONSTRAINT `suppleirKey` FOREIGN KEY `suppleirKey` (`Supplier`)
    REFERENCES `customers`.`Suppliers` (`Id`)
    ON DELETE CASCADE
    ON UPDATE CASCADE
)
ENGINE = InnoDB;


ALTER TABLE `usersettings`.`waybilldirtyfile` ADD COLUMN `Mask` VARCHAR(45) NOT NULL AFTER `File`;
