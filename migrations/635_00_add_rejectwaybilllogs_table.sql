CREATE TABLE `logs`.`RejectWaybillLogs` (
  `RowId` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `DocumentSize` int(10) unsigned DEFAULT NULL,
  `FirmCode` int(10) unsigned DEFAULT NULL,
  `ClientCode` int(10) unsigned DEFAULT NULL,
  `FileName` varchar(50) DEFAULT NULL,
  `Addition` mediumtext,
  `AddressId` int(10) unsigned DEFAULT NULL,
  PRIMARY KEY (`RowId`),
  CONSTRAINT `FK_rejectwaybillslogs_FirmCode` FOREIGN KEY (`FirmCode`) REFERENCES `customers`.`suppliers` (`Id`) ON DELETE CASCADE
)
ENGINE = InnoDB;