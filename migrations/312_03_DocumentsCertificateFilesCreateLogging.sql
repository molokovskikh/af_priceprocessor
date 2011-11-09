
CREATE TABLE  `logs`.`CertificateFileLogs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` datetime NOT NULL,
  `OperatorName` varchar(50) NOT NULL,
  `OperatorHost` varchar(50) NOT NULL,
  `Operation` tinyint(3) unsigned NOT NULL,
  `FileId` int(10) unsigned not null,
  `OriginFilename` varchar(255),
  `ExternalFileId` varchar(255),
  `CertificateSourceId` int(10) unsigned,
  `Extension` varchar(255),

  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;

DROP TRIGGER IF EXISTS Documents.CertificateFileLogDelete; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Documents.CertificateFileLogDelete AFTER DELETE ON Documents.CertificateFiles
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.CertificateFileLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		FileId = OLD.Id,
		OriginFilename = OLD.OriginFilename,
		ExternalFileId = OLD.ExternalFileId,
		CertificateSourceId = OLD.CertificateSourceId,
		Extension = OLD.Extension;
END;

DROP TRIGGER IF EXISTS Documents.CertificateFileLogUpdate; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Documents.CertificateFileLogUpdate AFTER UPDATE ON Documents.CertificateFiles
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.CertificateFileLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		FileId = OLD.Id,
		OriginFilename = NULLIF(NEW.OriginFilename, OLD.OriginFilename),
		ExternalFileId = NULLIF(NEW.ExternalFileId, OLD.ExternalFileId),
		CertificateSourceId = NULLIF(NEW.CertificateSourceId, OLD.CertificateSourceId),
		Extension = NULLIF(NEW.Extension, OLD.Extension);
END;

DROP TRIGGER IF EXISTS Documents.CertificateFileLogInsert; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Documents.CertificateFileLogInsert AFTER INSERT ON Documents.CertificateFiles
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.CertificateFileLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		FileId = NEW.Id,
		OriginFilename = NEW.OriginFilename,
		ExternalFileId = NEW.ExternalFileId,
		CertificateSourceId = NEW.CertificateSourceId,
		Extension = NEW.Extension;
END;

