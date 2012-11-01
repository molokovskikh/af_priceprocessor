
alter table Logs.PricesdatumLogs
add column IsRejects tinyint(1),
add column IsRejectCancellations tinyint(1),
add column IsUpdate tinyint(1) unsigned,
add column IsStrict tinyint(1) unsigned
;

DROP TRIGGER IF EXISTS usersettings.PricesdatumLogDelete;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER usersettings.PricesdatumLogDelete AFTER DELETE ON usersettings.pricesdata
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.PricesdatumLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 2,
		PriceCode = OLD.PriceCode,
		FirmCode = OLD.FirmCode,
		RegionMask = OLD.RegionMask,
		AgencyEnabled = OLD.AgencyEnabled,
		Enabled = OLD.Enabled,
		PriceType = OLD.PriceType,
		PriceName = OLD.PriceName,
		MinReq = OLD.MinReq,
		UpCost = OLD.UpCost,
		PriceInfo = OLD.PriceInfo,
		CostType = OLD.CostType,
		ParentSynonym = OLD.ParentSynonym,
		BuyingMatrix = OLD.BuyingMatrix,
		IsRejects = OLD.IsRejects,
		IsRejectCancellations = OLD.IsRejectCancellations,
		IsUpdate = OLD.IsUpdate,
		IsStrict = OLD.IsStrict;
END;

DROP TRIGGER IF EXISTS usersettings.PricesdatumLogUpdate;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER usersettings.PricesdatumLogUpdate AFTER UPDATE ON usersettings.pricesdata
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.PricesdatumLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 1,
		PriceCode = OLD.PriceCode,
		FirmCode = NULLIF(NEW.FirmCode, OLD.FirmCode),
		RegionMask = NULLIF(NEW.RegionMask, OLD.RegionMask),
		AgencyEnabled = NULLIF(NEW.AgencyEnabled, OLD.AgencyEnabled),
		Enabled = NULLIF(NEW.Enabled, OLD.Enabled),
		PriceType = NULLIF(NEW.PriceType, OLD.PriceType),
		PriceName = NULLIF(NEW.PriceName, OLD.PriceName),
		MinReq = NULLIF(NEW.MinReq, OLD.MinReq),
		UpCost = NULLIF(NEW.UpCost, OLD.UpCost),
		PriceInfo = NULLIF(NEW.PriceInfo, OLD.PriceInfo),
		CostType = NULLIF(NEW.CostType, OLD.CostType),
		ParentSynonym = NULLIF(NEW.ParentSynonym, OLD.ParentSynonym),
		BuyingMatrix = NULLIF(NEW.BuyingMatrix, OLD.BuyingMatrix),
		IsRejects = NULLIF(NEW.IsRejects, OLD.IsRejects),
		IsRejectCancellations = NULLIF(NEW.IsRejectCancellations, OLD.IsRejectCancellations),
		IsUpdate = NULLIF(NEW.IsUpdate, OLD.IsUpdate),
		IsStrict = NULLIF(NEW.IsStrict, OLD.IsStrict);
END;

DROP TRIGGER IF EXISTS usersettings.PricesdatumLogInsert;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER usersettings.PricesdatumLogInsert AFTER INSERT ON usersettings.pricesdata
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.PricesdatumLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 0,
		PriceCode = NEW.PriceCode,
		FirmCode = NEW.FirmCode,
		RegionMask = NEW.RegionMask,
		AgencyEnabled = NEW.AgencyEnabled,
		Enabled = NEW.Enabled,
		PriceType = NEW.PriceType,
		PriceName = NEW.PriceName,
		MinReq = NEW.MinReq,
		UpCost = NEW.UpCost,
		PriceInfo = NEW.PriceInfo,
		CostType = NEW.CostType,
		ParentSynonym = NEW.ParentSynonym,
		BuyingMatrix = NEW.BuyingMatrix,
		IsRejects = NEW.IsRejects,
		IsRejectCancellations = NEW.IsRejectCancellations,
		IsUpdate = NEW.IsUpdate,
		IsStrict = NEW.IsStrict;
END;