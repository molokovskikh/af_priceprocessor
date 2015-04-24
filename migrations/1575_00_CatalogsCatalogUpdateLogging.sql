alter table Logs.CatalogLogs
add column Monobrend tinyint(1) unsigned
;
DROP TRIGGER IF EXISTS Catalogs.CatalogLogDelete;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Catalogs.CatalogLogDelete AFTER DELETE ON Catalogs.Catalog
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.CatalogLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		CatalogId = OLD.Id,
		FormId = OLD.FormId,
		NameId = OLD.NameId,
		VitallyImportant = OLD.VitallyImportant,
		MandatoryList = OLD.MandatoryList,
		NeedCold = OLD.NeedCold,
		Fragile = OLD.Fragile,
		Pharmacie = OLD.Pharmacie,
		Hidden = OLD.Hidden,
		Name = OLD.Name,
		UpdateTime = OLD.UpdateTime,
		Monobrend = OLD.Monobrend;
END;
DROP TRIGGER IF EXISTS Catalogs.CatalogLogUpdate;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Catalogs.CatalogLogUpdate AFTER UPDATE ON Catalogs.Catalog
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.CatalogLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		CatalogId = OLD.Id,
		FormId = NULLIF(NEW.FormId, OLD.FormId),
		NameId = NULLIF(NEW.NameId, OLD.NameId),
		VitallyImportant = NULLIF(NEW.VitallyImportant, OLD.VitallyImportant),
		MandatoryList = NULLIF(NEW.MandatoryList, OLD.MandatoryList),
		NeedCold = NULLIF(NEW.NeedCold, OLD.NeedCold),
		Fragile = NULLIF(NEW.Fragile, OLD.Fragile),
		Pharmacie = NULLIF(NEW.Pharmacie, OLD.Pharmacie),
		Hidden = NULLIF(NEW.Hidden, OLD.Hidden),
		Name = NULLIF(NEW.Name, OLD.Name),
		UpdateTime = NULLIF(NEW.UpdateTime, OLD.UpdateTime),
		Monobrend = NULLIF(NEW.Monobrend, OLD.Monobrend);
END;
DROP TRIGGER IF EXISTS Catalogs.CatalogLogInsert;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Catalogs.CatalogLogInsert AFTER INSERT ON Catalogs.Catalog
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.CatalogLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		CatalogId = NEW.Id,
		FormId = NEW.FormId,
		NameId = NEW.NameId,
		VitallyImportant = NEW.VitallyImportant,
		MandatoryList = NEW.MandatoryList,
		NeedCold = NEW.NeedCold,
		Fragile = NEW.Fragile,
		Pharmacie = NEW.Pharmacie,
		Hidden = NEW.Hidden,
		Name = NEW.Name,
		UpdateTime = NEW.UpdateTime,
		Monobrend = NEW.Monobrend;
END;
