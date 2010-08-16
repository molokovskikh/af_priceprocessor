
CREATE TABLE  `logs`.`ExcludeLogs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` datetime NOT NULL,
  `OperatorName` varchar(50) NOT NULL,
  `OperatorHost` varchar(50) NOT NULL,
  `Operation` tinyint(3) unsigned NOT NULL,
  `ExcludeId` int(10) unsigned not null,
  `OriginalSynonymId` int(10) unsigned,
  `CatalogId` int(10) unsigned,
  `PriceCode` int(10) unsigned,
  `ProducerSynonym` varchar(255),
  `DoNotShow` tinyint(1),
  `CreatedOn` timestamp,
  `LastUsedOn` datetime,

  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;

DROP TRIGGER IF EXISTS Farm.ExcludeLogDelete; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Farm.ExcludeLogDelete AFTER DELETE ON Farm.Excludes
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.ExcludeLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		ExcludeId = OLD.Id,
		OriginalSynonymId = OLD.OriginalSynonymId,
		CatalogId = OLD.CatalogId,
		PriceCode = OLD.PriceCode,
		ProducerSynonym = OLD.ProducerSynonym,
		DoNotShow = OLD.DoNotShow,
		CreatedOn = OLD.CreatedOn,
		LastUsedOn = OLD.LastUsedOn;
END;

DROP TRIGGER IF EXISTS Farm.ExcludeLogUpdate; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Farm.ExcludeLogUpdate AFTER UPDATE ON Farm.Excludes
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.ExcludeLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		ExcludeId = OLD.Id,
		OriginalSynonymId = NULLIF(NEW.OriginalSynonymId, OLD.OriginalSynonymId),
		CatalogId = NULLIF(NEW.CatalogId, OLD.CatalogId),
		PriceCode = NULLIF(NEW.PriceCode, OLD.PriceCode),
		ProducerSynonym = NULLIF(NEW.ProducerSynonym, OLD.ProducerSynonym),
		DoNotShow = NULLIF(NEW.DoNotShow, OLD.DoNotShow),
		CreatedOn = NULLIF(NEW.CreatedOn, OLD.CreatedOn),
		LastUsedOn = NULLIF(NEW.LastUsedOn, OLD.LastUsedOn);
END;

DROP TRIGGER IF EXISTS Farm.ExcludeLogInsert; 
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Farm.ExcludeLogInsert AFTER INSERT ON Farm.Excludes
FOR EACH ROW BEGIN
	INSERT 
	INTO `logs`.ExcludeLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		ExcludeId = NEW.Id,
		OriginalSynonymId = NEW.OriginalSynonymId,
		CatalogId = NEW.CatalogId,
		PriceCode = NEW.PriceCode,
		ProducerSynonym = NEW.ProducerSynonym,
		DoNotShow = NEW.DoNotShow,
		CreatedOn = NEW.CreatedOn,
		LastUsedOn = NEW.LastUsedOn;
END;

