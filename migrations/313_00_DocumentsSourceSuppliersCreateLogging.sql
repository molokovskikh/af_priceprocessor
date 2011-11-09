
CREATE TABLE  `logs`.`SourceSupplierLogs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` datetime NOT NULL,
  `OperatorName` varchar(50) NOT NULL,
  `OperatorHost` varchar(50) NOT NULL,
  `Operation` tinyint(3) unsigned NOT NULL,
  `CertificateSourceId` int(10) unsigned,
  `SupplierId` int(10) unsigned,

  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;

DROP TRIGGER IF EXISTS Documents.SourceSupplierLogDelete; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Documents.SourceSupplierLogDelete AFTER DELETE ON Documents.SourceSuppliers
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.SourceSupplierLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		CertificateSourceId = OLD.CertificateSourceId,
		SupplierId = OLD.SupplierId;
END;

DROP TRIGGER IF EXISTS Documents.SourceSupplierLogUpdate; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Documents.SourceSupplierLogUpdate AFTER UPDATE ON Documents.SourceSuppliers
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.SourceSupplierLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		CertificateSourceId = NULLIF(NEW.CertificateSourceId, OLD.CertificateSourceId),
		SupplierId = NULLIF(NEW.SupplierId, OLD.SupplierId);
END;

DROP TRIGGER IF EXISTS Documents.SourceSupplierLogInsert; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Documents.SourceSupplierLogInsert AFTER INSERT ON Documents.SourceSuppliers
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.SourceSupplierLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		CertificateSourceId = NEW.CertificateSourceId,
		SupplierId = NEW.SupplierId;
END;

