
CREATE TABLE  `logs`.`CertificateSourceLogs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` datetime NOT NULL,
  `OperatorName` varchar(50) NOT NULL,
  `OperatorHost` varchar(50) NOT NULL,
  `Operation` tinyint(3) unsigned NOT NULL,
  `SourceId` int(10) unsigned not null,
  `FtpSupplierId` int(10) unsigned,
  `SourceClassName` varchar(255),

  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;

DROP TRIGGER IF EXISTS Documents.CertificateSourceLogDelete; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Documents.CertificateSourceLogDelete AFTER DELETE ON Documents.CertificateSources
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.CertificateSourceLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		SourceId = OLD.Id,
		FtpSupplierId = OLD.FtpSupplierId,
		SourceClassName = OLD.SourceClassName;
END;

DROP TRIGGER IF EXISTS Documents.CertificateSourceLogUpdate; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Documents.CertificateSourceLogUpdate AFTER UPDATE ON Documents.CertificateSources
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.CertificateSourceLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		SourceId = OLD.Id,
		FtpSupplierId = NULLIF(NEW.FtpSupplierId, OLD.FtpSupplierId),
		SourceClassName = NULLIF(NEW.SourceClassName, OLD.SourceClassName);
END;

DROP TRIGGER IF EXISTS Documents.CertificateSourceLogInsert; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Documents.CertificateSourceLogInsert AFTER INSERT ON Documents.CertificateSources
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.CertificateSourceLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		SourceId = NEW.Id,
		FtpSupplierId = NEW.FtpSupplierId,
		SourceClassName = NEW.SourceClassName;
END;

