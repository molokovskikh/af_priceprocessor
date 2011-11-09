
CREATE TABLE  `logs`.`CertificateTaskLogs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` datetime NOT NULL,
  `OperatorName` varchar(50) NOT NULL,
  `OperatorHost` varchar(50) NOT NULL,
  `Operation` tinyint(3) unsigned NOT NULL,
  `TaskId` int(10) unsigned not null,
  `CertificateSourceId` int(10) unsigned,
  `CatalogId` int(10) unsigned,
  `SerialNumber` varchar(50),
  `DocumentBodyId` int(10) unsigned,

  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;

DROP TRIGGER IF EXISTS Documents.CertificateTaskLogDelete; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Documents.CertificateTaskLogDelete AFTER DELETE ON Documents.CertificateTasks
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.CertificateTaskLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		TaskId = OLD.Id,
		CertificateSourceId = OLD.CertificateSourceId,
		CatalogId = OLD.CatalogId,
		SerialNumber = OLD.SerialNumber,
		DocumentBodyId = OLD.DocumentBodyId;
END;

DROP TRIGGER IF EXISTS Documents.CertificateTaskLogUpdate; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Documents.CertificateTaskLogUpdate AFTER UPDATE ON Documents.CertificateTasks
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.CertificateTaskLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		TaskId = OLD.Id,
		CertificateSourceId = NULLIF(NEW.CertificateSourceId, OLD.CertificateSourceId),
		CatalogId = NULLIF(NEW.CatalogId, OLD.CatalogId),
		SerialNumber = NULLIF(NEW.SerialNumber, OLD.SerialNumber),
		DocumentBodyId = NULLIF(NEW.DocumentBodyId, OLD.DocumentBodyId);
END;

DROP TRIGGER IF EXISTS Documents.CertificateTaskLogInsert; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Documents.CertificateTaskLogInsert AFTER INSERT ON Documents.CertificateTasks
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.CertificateTaskLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		TaskId = NEW.Id,
		CertificateSourceId = NEW.CertificateSourceId,
		CatalogId = NEW.CatalogId,
		SerialNumber = NEW.SerialNumber,
		DocumentBodyId = NEW.DocumentBodyId;
END;

