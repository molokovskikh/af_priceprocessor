
CREATE TABLE  `logs`.`FileCertificateLogs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` datetime NOT NULL,
  `OperatorName` varchar(50) NOT NULL,
  `OperatorHost` varchar(50) NOT NULL,
  `Operation` tinyint(3) unsigned NOT NULL,
  `CertificateId` int(10) unsigned,
  `CertificateFileId` int(10) unsigned,

  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;

DROP TRIGGER IF EXISTS Documents.FileCertificateLogDelete; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Documents.FileCertificateLogDelete AFTER DELETE ON Documents.FileCertificates
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.FileCertificateLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		CertificateId = OLD.CertificateId,
		CertificateFileId = OLD.CertificateFileId;
END;

DROP TRIGGER IF EXISTS Documents.FileCertificateLogUpdate; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Documents.FileCertificateLogUpdate AFTER UPDATE ON Documents.FileCertificates
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.FileCertificateLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		CertificateId = NULLIF(NEW.CertificateId, OLD.CertificateId),
		CertificateFileId = NULLIF(NEW.CertificateFileId, OLD.CertificateFileId);
END;

DROP TRIGGER IF EXISTS Documents.FileCertificateLogInsert; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Documents.FileCertificateLogInsert AFTER INSERT ON Documents.FileCertificates
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.FileCertificateLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		CertificateId = NEW.CertificateId,
		CertificateFileId = NEW.CertificateFileId;
END;

