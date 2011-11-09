
CREATE TABLE  `logs`.`CertificateLogs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` datetime NOT NULL,
  `OperatorName` varchar(50) NOT NULL,
  `OperatorHost` varchar(50) NOT NULL,
  `Operation` tinyint(3) unsigned NOT NULL,
  `CertificateId` int(10) unsigned not null,
  `CatalogId` int(10) unsigned,
  `SerialNumber` varchar(50),

  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;

DROP TRIGGER IF EXISTS Documents.CertificateLogDelete; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Documents.CertificateLogDelete AFTER DELETE ON Documents.Certificates
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.CertificateLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		CertificateId = OLD.Id,
		CatalogId = OLD.CatalogId,
		SerialNumber = OLD.SerialNumber;
END;

DROP TRIGGER IF EXISTS Documents.CertificateLogUpdate; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Documents.CertificateLogUpdate AFTER UPDATE ON Documents.Certificates
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.CertificateLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		CertificateId = OLD.Id,
		CatalogId = NULLIF(NEW.CatalogId, OLD.CatalogId),
		SerialNumber = NULLIF(NEW.SerialNumber, OLD.SerialNumber);
END;

DROP TRIGGER IF EXISTS Documents.CertificateLogInsert; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Documents.CertificateLogInsert AFTER INSERT ON Documents.Certificates
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.CertificateLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		CertificateId = NEW.Id,
		CatalogId = NEW.CatalogId,
		SerialNumber = NEW.SerialNumber;
END;

