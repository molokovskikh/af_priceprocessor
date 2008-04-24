set names cp1251;

-- обновляем все таблицы взяв эти значения из PriceItems

-- обновления Core0
update
  farm.Core0,
  usersettings.PricesCosts
set
  Core0.PriceCode = PricesCosts.ShowPriceCode
where
    Core0.PriceCode = PricesCosts.PriceCode
and PricesCosts.ShowPriceCode != PricesCosts.PriceCode;


-- обновления blockedPrice
update
  farm.blockedPrice,
  usersettings.PricesCosts
set
  blockedPrice.PriceItemId = PricesCosts.PriceItemId
where
  blockedPrice.PriceCode = PricesCosts.PriceCode;

update
  farm.blockedPrice,
  usersettings.PricesCosts
set
  blockedPrice.PriceItemId = PricesCosts.PriceItemId
where
    blockedPrice.PriceCode = PricesCosts.ShowPriceCode
and blockedPrice.PriceItemId is null
and PricesCosts.BaseCost = 1;

update
  farm.blockedPrice,
  usersettings.PricesCosts,
  usersettings.PricesCosts parent
set
  blockedPrice.PriceItemId = parent.PriceItemId
where
    blockedPrice.PriceCode = PricesCosts.PriceCode
and blockedPrice.PriceItemId is null
and parent.ShowPriceCode = PricesCosts.ShowPriceCode
and parent.BaseCost = 1;

delete from
  farm.blockedPrice
where
  blockedPrice.PriceItemId is null;

alter table farm.blockedPrice
  drop primary key,
  drop column `PriceCode`,
  add primary key (`PriceItemId`);


-- обновления forb
update
  farm.forb,
  usersettings.PricesCosts
set
  forb.PriceItemId = PricesCosts.PriceItemId
where
  forb.PriceCode = PricesCosts.PriceCode;

update
  farm.forb,
  usersettings.PricesCosts
set
  forb.PriceItemId = PricesCosts.PriceItemId
where
    forb.PriceCode = PricesCosts.ShowPriceCode
and forb.PriceItemId is null
and PricesCosts.BaseCost = 1;

update
  farm.forb,
  usersettings.PricesCosts,
  usersettings.PricesCosts parent
set
  forb.PriceItemId = parent.PriceItemId
where
    forb.PriceCode = PricesCosts.PriceCode
and forb.PriceItemId is null
and parent.ShowPriceCode = PricesCosts.ShowPriceCode
and parent.BaseCost = 1;

delete from
  farm.forb
where
  forb.PriceItemId is null;

alter table farm.forb
  drop key `PriceCode_IDX`,
  drop column `PriceCode`,
  add key `PriceItemId_IDX` using BTree (`PriceItemId`);


-- обновления zero
update
  farm.zero,
  usersettings.PricesCosts
set
  zero.PriceItemId = PricesCosts.PriceItemId
where
  zero.PriceCode = PricesCosts.PriceCode;

update
  farm.zero,
  usersettings.PricesCosts
set
  zero.PriceItemId = PricesCosts.PriceItemId
where
    zero.PriceCode = PricesCosts.ShowPriceCode
and zero.PriceItemId is null
and PricesCosts.BaseCost = 1;

update
  farm.zero,
  usersettings.PricesCosts,
  usersettings.PricesCosts parent
set
  zero.PriceItemId = parent.PriceItemId
where
    zero.PriceCode = PricesCosts.PriceCode
and zero.PriceItemId is null
and parent.ShowPriceCode = PricesCosts.ShowPriceCode
and parent.BaseCost = 1;

delete from
  farm.zero
where
  zero.PriceItemId is null;

alter table farm.zero
  drop key `PriceCode_IDX`,
  drop column `PriceCode`,
  add key `PriceItemId_IDX` using BTree (`PriceItemId`);

-- обновления unrecexp
update
  farm.unrecexp,
  usersettings.PricesCosts
set
  unrecexp.PriceItemId = PricesCosts.PriceItemId
where
  unrecexp.PriceCode = PricesCosts.PriceCode;

update
  farm.unrecexp,
  usersettings.PricesCosts
set
  unrecexp.PriceItemId = PricesCosts.PriceItemId
where
    unrecexp.PriceCode = PricesCosts.ShowPriceCode
and unrecexp.PriceItemId is null
and PricesCosts.BaseCost = 1;

update
  farm.unrecexp,
  usersettings.PricesCosts,
  usersettings.PricesCosts parent
set
  unrecexp.PriceItemId = parent.PriceItemId
where
    unrecexp.PriceCode = PricesCosts.PriceCode
and unrecexp.PriceItemId is null
and parent.ShowPriceCode = PricesCosts.ShowPriceCode
and parent.BaseCost = 1;

delete from
  farm.unrecexp
where
  unrecexp.PriceItemId is null;

alter table farm.unrecexp
  drop key `PriceCode_IDX`,
  drop column `PriceCode`,
  add key `PriceItemId_IDX` using BTree (`PriceItemId`);


-- обновления PricesRetrans
update
  logs.PricesRetrans,
  usersettings.PricesCosts
set
  PricesRetrans.PriceItemId = PricesCosts.PriceItemId
where
  PricesRetrans.PriceCode = PricesCosts.PriceCode;

update
  logs.PricesRetrans,
  usersettings.PricesCosts
set
  PricesRetrans.PriceItemId = PricesCosts.PriceItemId
where
    PricesRetrans.PriceCode = PricesCosts.ShowPriceCode
and PricesRetrans.PriceItemId is null
and PricesCosts.BaseCost = 1;

update
  logs.PricesRetrans,
  usersettings.PricesCosts,
  usersettings.PricesCosts parent
set
  PricesRetrans.PriceItemId = parent.PriceItemId
where
    PricesRetrans.PriceCode = PricesCosts.PriceCode
and PricesRetrans.PriceItemId is null
and parent.ShowPriceCode = PricesCosts.ShowPriceCode
and parent.BaseCost = 1;

delete from
  logs.PricesRetrans
where
  PricesRetrans.PriceItemId is null;

alter table logs.PricesRetrans
  drop key `PriceCode`,
  drop key `LogTime`,
  drop column `PriceCode`,
  add key `PriceItemId_IDX` (`PriceItemId`),
  add key `LogTime_IDX` (`LogTime`, `PriceItemId`);


-- обновления formlogs
update
  logs.formlogs,
  usersettings.PricesCosts
set
  formlogs.PriceItemId = PricesCosts.PriceItemId
where
  formlogs.PriceCode = PricesCosts.PriceCode;

update
  logs.formlogs,
  usersettings.PricesCosts
set
  formlogs.PriceItemId = PricesCosts.PriceItemId
where
    formlogs.PriceCode = PricesCosts.ShowPriceCode
and formlogs.PriceItemId is null
and PricesCosts.BaseCost = 1;

update
  logs.formlogs,
  usersettings.PricesCosts,
  usersettings.PricesCosts parent
set
  formlogs.PriceItemId = parent.PriceItemId
where
    formlogs.PriceCode = PricesCosts.PriceCode
and formlogs.PriceItemId is null
and parent.ShowPriceCode = PricesCosts.ShowPriceCode
and parent.BaseCost = 1;

delete from
  logs.formlogs
where
  formlogs.PriceItemId is null;

alter table logs.formlogs
  drop primary key,
  drop key `PriceCode`,
  drop key `AppCode`,
  drop column `PriceCode`,
  drop column `AppCode`,
  add primary key (`RowId`),
  add key `PriceItemId_IDX` (`RowId`, `PriceItemId`),
  add key `Host_IDX` (`Host`);



-- обновления downlogs
update
  logs.downlogs,
  usersettings.PricesCosts
set
  downlogs.PriceItemId = PricesCosts.PriceItemId
where
  downlogs.PriceCode = PricesCosts.PriceCode;

update
  logs.downlogs,
  usersettings.PricesCosts
set
  downlogs.PriceItemId = PricesCosts.PriceItemId
where
    downlogs.PriceCode = PricesCosts.ShowPriceCode
and downlogs.PriceItemId is null
and PricesCosts.BaseCost = 1;

update
  logs.downlogs,
  usersettings.PricesCosts,
  usersettings.PricesCosts parent
set
  downlogs.PriceItemId = parent.PriceItemId
where
    downlogs.PriceCode = PricesCosts.PriceCode
and downlogs.PriceItemId is null
and parent.ShowPriceCode = PricesCosts.ShowPriceCode
and parent.BaseCost = 1;

delete from
  logs.downlogs
where
    downlogs.PriceItemId is null
and downlogs.PriceCode != 0;

alter table logs.downlogs
  drop primary key,
  drop key `PriceCode`,
  drop column `PriceCode`,
  drop column `AppCode`,
  add primary key (`RowId`),
  add key `PriceItemId_IDX` (`RowId`, `PriceItemId`),
  add key `Host_IDX` (`Host`);


-- обновляем таблицу sources
alter table farm.sources
  drop foreign key `sources_ibfk_1`,
  drop primary key,
  drop column `FirmCode`,
  change column `Id` `Id` int(11) unsigned NOT NULL AUTO_INCREMENT,
  add primary key (`Id`),
  drop column `SourceType`,
  drop column `LastDateTime`,
  drop column `PriceDateTime`,
  add constraint `SourceTypeId_FK` foreign key `SourceTypeId_IDX` (`SourceTypeId`) REFERENCES `farm`.`sourcetypes` (`Id`) ON DELETE SET NULL ON UPDATE CASCADE; 

-- обновляем таблицу formrules
alter table farm.formrules
  drop primary key,
  drop key `FirmCode_IDX`,
  drop key `ParentSynonym_IDX`,
  drop key `ParentFormRules_IDX`,
  drop foreign key `formrules_ibfk_1`,
  drop foreign key `formrules_ibfk_2`,
  drop foreign key `formrules_ibfk_3`;

alter table farm.formrules
  drop column `FirmCode`,
  drop column `PriceCode`,
  drop column `PriceFmt`,
  drop column `ParentSynonym`;

alter table farm.formrules
  change column `Id` `Id` int(11) unsigned NOT NULL AUTO_INCREMENT PRIMARY KEY;

alter table farm.formrules
  add constraint `PriceFormatId_FK` foreign key `PriceFormatId_IDX` (`PriceFormatId`) REFERENCES `farm`.`pricefmts` (`Id`) ON DELETE SET NULL ON UPDATE CASCADE,
  add constraint `ParentFormRules_FK` foreign key `ParentFormRules_IDX` (`ParentFormRules`) REFERENCES `farm`.`formrules` (`Id`) ON DELETE SET NULL ON UPDATE CASCADE;



-- удаляем цены из PricesData и реструктуризации PricesCosts
alter table usersettings.PricesCosts
  drop foreign key `pricescosts_ibfk_1`;

delete from usersettings.PricesData
where
  not exists(select * from usersettings.pricescosts pc where pc.PriceCode = PricesData.PriceCode);

delete from usersettings.PricesData
using 
  usersettings.PricesData,
  usersettings.PricesCosts
where
    PricesData.PriceCode = PricesCosts.PriceCode
and PricesCosts.PriceCode != PricesCosts.ShowPriceCode;

delete from usersettings.PricesCosts
where
  not exists(select * from usersettings.PricesData where PricesData.PriceCode = PricesCosts.ShowPriceCode);

update
  usersettings.PricesCosts
set
  PriceCode = ShowPriceCode;

alter table usersettings.PricesCosts
  drop key `ShowPriceCode_IDX`,
  drop column `ShowPriceCode`,
  add constraint `PriceCode_FK` foreign key (`PriceCode`) REFERENCES `usersettings`.`pricesdata` (`PriceCode`) ON DELETE CASCADE ON UPDATE CASCADE;

alter table usersettings.PricesData
  drop column `WaitingDownloadInterval`,
  add constraint `PricesData_ParentSynonym_FK` foreign key `PricesData_ParentSynonym_IDX` (`ParentSynonym`) REFERENCES `usersettings`.`pricesdata` (`PriceCode`) ON DELETE SET NULL ON UPDATE CASCADE;

update 
  usersettings.PricesCosts pc,
  usersettings.PricesCosts BasePC
set
  pc.PriceItemId = BasePC.PriceItemId
where
    pc.PriceItemId is null
and pc.BaseCost = 0
and BasePC.PriceCode = pc.PriceCode
and BasePC.BaseCost = 1;

-- везде восстанавливаем логирование

-- восстанавливаем CostFormRules
alter table logs.cost_form_rules_logs
  drop column `FR_Id`,
  change column `PC_CostCode` `CostCode` int(11) unsigned DEFAULT NULL;

DELIMITER ;;

CREATE TRIGGER farm.CostFormRulesLogInsert AFTER INSERT
ON farm.costformrules FOR EACH ROW BEGIN
INSERT INTO `logs`.cost_form_rules_logs
SET LogTime = now() ,
OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)) ,
OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)) ,
Operation = 0,
CostCode = NEW.CostCode
,FieldName = NEW.FieldName
,TxtBegin = NEW.TxtBegin
,TxtEnd = NEW.TxtEnd
;END ;;

DELIMITER ;


DELIMITER ;;

CREATE TRIGGER farm.CostFormRulesLogDelete AFTER DELETE
ON farm.costformrules FOR EACH ROW BEGIN
INSERT INTO `logs`.cost_form_rules_logs
SET LogTime = now() ,
OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)) ,
OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)) ,
Operation = 2,
CostCode = OLD.CostCode
,FieldName = OLD.FieldName
,TxtBegin = OLD.TxtBegin
,TxtEnd = OLD.TxtEnd
;END ;;

DELIMITER ;


DELIMITER ;;


CREATE TRIGGER farm.CostFormRulesLogUpdate AFTER UPDATE
ON farm.costformrules FOR EACH ROW BEGIN
INSERT INTO `logs`.cost_form_rules_logs
SET LogTime = now() ,
OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)) ,
OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)) ,
Operation = 1,
CostCode = IFNULL(NEW.CostCode, OLD.CostCode)
,FieldName = NULLIF(NEW.FieldName, OLD.FieldName)
,TxtBegin = NULLIF(NEW.TxtBegin, OLD.TxtBegin)
,TxtEnd = NULLIF(NEW.TxtEnd, OLD.TxtEnd)
;END ;;

DELIMITER ;


-- восстанавливаем PricesRetrans

DELIMITER ;;

CREATE TRIGGER logs.PricesRetransAfterInsert AFTER INSERT
ON logs.PricesRetrans FOR EACH ROW BEGIN
  update usersettings.PriceItems
  set
    LastRetrans = NEW.LogTime
  where
    PriceItemId = NEW.PriceItemId;
END ;;

DELIMITER ;



-- восстанавливаем PricesCosts

DELIMITER ;;

CREATE TRIGGER usersettings.pricescostsLogInsert AFTER INSERT
ON usersettings.pricescosts FOR EACH ROW BEGIN
INSERT INTO `logs`.prices_costs_logs
SET LogTime = now() ,
OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)) ,
OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)) ,
Operation = 0,
pricescostsID = NEW.CostCode
,PriceCode = NEW.PriceCode
,PriceItemId = NEW.PriceItemId
,Enabled = NEW.Enabled
,AgencyEnabled = NEW.AgencyEnabled
,CostName = NEW.CostName
,BaseCost = NEW.BaseCost
;END ;;

DELIMITER ;


DELIMITER ;;

CREATE TRIGGER usersettings.pricescostsLogUpdate AFTER UPDATE
ON usersettings.pricescosts FOR EACH ROW BEGIN
INSERT INTO `logs`.prices_costs_logs
SET LogTime = now() ,
OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)) ,
OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)) ,
Operation = 1,
pricescostsID = OLD.CostCode
,PriceCode = IFNULL(NEW.PriceCode, OLD.PriceCode)
,PriceItemId = IFNULL(NEW.PriceItemId, OLD.PriceItemId)
,Enabled = NULLIF(NEW.Enabled, OLD.Enabled)
,AgencyEnabled = NULLIF(NEW.AgencyEnabled, OLD.AgencyEnabled)
,CostName = NULLIF(NEW.CostName, OLD.CostName)
,BaseCost = NULLIF(NEW.BaseCost, OLD.BaseCost)
;END ;;

DELIMITER ;


DELIMITER ;;

CREATE TRIGGER usersettings.pricescostsLogDelete AFTER DELETE
ON usersettings.pricescosts FOR EACH ROW BEGIN
INSERT INTO `logs`.prices_costs_logs
SET LogTime = now() ,
OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)) ,
OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)) ,
Operation = 2,
pricescostsID = OLD.CostCode
,PriceCode = OLD.PriceCode
,PriceItemId = OLD.PriceItemId
,Enabled = OLD.Enabled
,AgencyEnabled = OLD.AgencyEnabled
,CostName = OLD.CostName
,BaseCost = OLD.BaseCost
;END ;;

DELIMITER ;


-- восстанавливаем PricesData

DELIMITER ;;

CREATE TRIGGER usersettings.pricesdataLogInsert AFTER INSERT
ON usersettings.pricesdata FOR EACH ROW BEGIN
INSERT INTO `logs`.prices_data_logs
SET LogTime = now() ,
OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)) ,
OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)) ,
Operation = 0,
pricesdataID = NEW.PriceCode
,FirmCode = NEW.FirmCode
,RegionMask = NEW.RegionMask
,AgencyEnabled = NEW.AgencyEnabled
,Enabled = NEW.Enabled
,ShowInWeb = NEW.ShowInWeb
,AlowInt = NEW.AlowInt
,PriceType = NEW.PriceType
,PriceName = NEW.PriceName
,MinReq = NEW.MinReq
,upcost = NEW.upcost
,priceinfo = NEW.priceinfo
,percentcorr = NEW.percentcorr
,OrderEmailSubject = NEW.OrderEmailSubject
,Protek = NEW.Protek
,CostType = NEW.CostType
,ParentSynonym = NEW.ParentSynonym
;END ;;

DELIMITER ;


DELIMITER ;;

CREATE TRIGGER usersettings.pricesdataLogUpdate AFTER UPDATE
ON usersettings.pricesdata FOR EACH ROW BEGIN
INSERT INTO `logs`.prices_data_logs
SET LogTime = now() ,
OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)) ,
OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)) ,
Operation = 1,
pricesdataID = OLD.PriceCode
,FirmCode = IFNULL(NEW.FirmCode, OLD.FirmCode)
,RegionMask = NULLIF(NEW.RegionMask, OLD.RegionMask)
,AgencyEnabled = NULLIF(NEW.AgencyEnabled, OLD.AgencyEnabled)
,Enabled = NULLIF(NEW.Enabled, OLD.Enabled)
,ShowInWeb = NULLIF(NEW.ShowInWeb, OLD.ShowInWeb)
,AlowInt = NULLIF(NEW.AlowInt, OLD.AlowInt)
,PriceType = NULLIF(NEW.PriceType, OLD.PriceType)
,PriceName = NULLIF(NEW.PriceName, OLD.PriceName)
,MinReq = NULLIF(NEW.MinReq, OLD.MinReq)
,upcost = NULLIF(NEW.upcost, OLD.upcost)
,priceinfo = NULLIF(NEW.priceinfo, OLD.priceinfo)
,percentcorr = NULLIF(NEW.percentcorr, OLD.percentcorr)
,OrderEmailSubject = NULLIF(NEW.OrderEmailSubject, OLD.OrderEmailSubject)
,Protek = NULLIF(NEW.Protek, OLD.Protek)
,CostType = NULLIF(NEW.CostType, OLD.CostType)
,ParentSynonym = NULLIF(NEW.ParentSynonym, OLD.ParentSynonym)
;END ;;

DELIMITER ;


DELIMITER ;;

CREATE TRIGGER usersettings.pricesdataLogDelete AFTER DELETE
ON usersettings.pricesdata FOR EACH ROW BEGIN
INSERT INTO `logs`.prices_data_logs
SET LogTime = now() ,
OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)) ,
OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)) ,
Operation = 2,
pricesdataID = OLD.PriceCode
,FirmCode = OLD.FirmCode
,RegionMask = OLD.RegionMask
,AgencyEnabled = OLD.AgencyEnabled
,Enabled = OLD.Enabled
,ShowInWeb = OLD.ShowInWeb
,AlowInt = OLD.AlowInt
,PriceType = OLD.PriceType
,PriceName = OLD.PriceName
,MinReq = OLD.MinReq
,upcost = OLD.upcost
,priceinfo = OLD.priceinfo
,percentcorr = OLD.percentcorr
,OrderEmailSubject = OLD.OrderEmailSubject
,Protek = OLD.Protek
,CostType = OLD.CostType
,ParentSynonym = OLD.ParentSynonym
;END ;;

DELIMITER ;

-- восстанавливаем FormRules

DELIMITER ;;

CREATE TRIGGER farm.FormRulesLogInsert AFTER Insert ON farm.FormRules FOR EACH ROW
BEGIN
INSERT INTO `logs`.form_rules_logs
SET LogTime = now() ,
OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)) ,
OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)) ,
Operation = 0,
FormRulesID = NEW.Id
,PriceFormatId = NEW.PriceFormatId
,MaxOld = NEW.MaxOld
,Delimiter = NEW.Delimiter
,ParentFormRules = NEW.ParentFormRules
,FormByCode = NEW.FormByCode
,NameMask = NEW.NameMask
,ForbWords = NEW.ForbWords
,JunkPos = NEW.JunkPos
,AwaitPos = NEW.AwaitPos
,StartLine = NEW.StartLine
,ListName = NEW.ListName
,TxtCodeBegin = NEW.TxtCodeBegin
,TxtCodeEnd = NEW.TxtCodeEnd
,TxtCodeCrBegin = NEW.TxtCodeCrBegin
,TxtCodeCrEnd = NEW.TxtCodeCrEnd
,TxtNameBegin = NEW.TxtNameBegin
,TxtNameEnd = NEW.TxtNameEnd
,TxtFirmCrBegin = NEW.TxtFirmCrBegin
,TxtFirmCrEnd = NEW.TxtFirmCrEnd
,TxtCountryCrBegin = NEW.TxtCountryCrBegin
,TxtCountryCrEnd = NEW.TxtCountryCrEnd
,TxtBaseCostBegin = NEW.TxtBaseCostBegin
,TxtBaseCostEnd = NEW.TxtBaseCostEnd
,TxtMinBoundCostBegin = NEW.TxtMinBoundCostBegin
,TxtMinBoundCostEnd = NEW.TxtMinBoundCostEnd
,TxtCurrencyBegin = NEW.TxtCurrencyBegin
,TxtCurrencyEnd = NEW.TxtCurrencyEnd
,TxtUnitBegin = NEW.TxtUnitBegin
,TxtUnitEnd = NEW.TxtUnitEnd
,TxtVolumeBegin = NEW.TxtVolumeBegin
,TxtVolumeEnd = NEW.TxtVolumeEnd
,TxtQuantityBegin = NEW.TxtQuantityBegin
,TxtQuantityEnd = NEW.TxtQuantityEnd
,TxtNoteBegin = NEW.TxtNoteBegin
,TxtNoteEnd = NEW.TxtNoteEnd
,TxtPeriodBegin = NEW.TxtPeriodBegin
,TxtPeriodEnd = NEW.TxtPeriodEnd
,TxtDocBegin = NEW.TxtDocBegin
,TxtDocEnd = NEW.TxtDocEnd
,TxtJunkBegin = NEW.TxtJunkBegin
,TxtJunkEnd = NEW.TxtJunkEnd
,TxtAwaitBegin = NEW.TxtAwaitBegin
,TxtAwaitEnd = NEW.TxtAwaitEnd
,FCode = NEW.FCode
,FCodeCr = NEW.FCodeCr
,FName1 = NEW.FName1
,FName2 = NEW.FName2
,FName3 = NEW.FName3
,FFirmCr = NEW.FFirmCr
,FCountryCr = NEW.FCountryCr
,FBaseCost = NEW.FBaseCost
,FMinBoundCost = NEW.FMinBoundCost
,FCurrency = NEW.FCurrency
,FUnit = NEW.FUnit
,FVolume = NEW.FVolume
,FQuantity = NEW.FQuantity
,FNote = NEW.FNote
,FPeriod = NEW.FPeriod
,FDoc = NEW.FDoc
,FJunk = NEW.FJunk
,FAwait = NEW.FAwait
,Memo = NEW.Memo
,Currency = NEW.Currency
,TxtVitallyImportantBegin = NEW.TxtVitallyImportantBegin
,TxtVitallyImportantEnd = NEW.TxtVitallyImportantEnd
,FVitallyImportant = NEW.FVitallyImportant
,VitallyImportantMask = NEW.VitallyImportantMask
,TxtRequestRatioBegin = NEW.TxtRequestRatioBegin
,TxtRequestRatioEnd = NEW.TxtRequestRatioEnd
,FRequestRatio = NEW.FRequestRatio
,TxtRegistryCostBegin = NEW.TxtRegistryCostBegin
,TxtRegistryCostEnd = NEW.TxtRegistryCostEnd
,FRegistryCost = NEW.FRegistryCost
,FMaxBoundCost = NEW.FMaxBoundCost
,TxtMaxBoundCostBegin = NEW.TxtMaxBoundCostBegin
,TxtMaxBoundCostEnd = NEW.TxtMaxBoundCostEnd
,FOrderCost = NEW.FOrderCost
,TxtOrderCostBegin = NEW.TxtOrderCostBegin
,TxtOrderCostEnd = NEW.TxtOrderCostEnd
,FMinOrderCount = NEW.FMinOrderCount
,TxtMinOrderCountBegin = NEW.TxtMinOrderCountBegin
,TxtMinOrderCountEnd = NEW.TxtMinOrderCountEnd
;END ;;

DELIMITER ;


DELIMITER ;;

CREATE TRIGGER farm.FormRulesLogUpdate AFTER Update ON farm.FormRules FOR EACH ROW
BEGIN
INSERT INTO `logs`.form_rules_logs
SET LogTime = now() ,
OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)) ,
OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)) ,
Operation = 1,
FormRulesID = OLD.Id
,PriceFormatId = IFNULL(NEW.PriceFormatId, OLD.PriceFormatId)
,MaxOld = NULLIF(NEW.MaxOld, OLD.MaxOld)
,Delimiter = NULLIF(NEW.Delimiter, OLD.Delimiter)
,ParentFormRules = IFNULL(NEW.ParentFormRules, OLD.ParentFormRules)
,FormByCode = NULLIF(NEW.FormByCode, OLD.FormByCode)
,NameMask = NULLIF(NEW.NameMask, OLD.NameMask)
,ForbWords = NULLIF(NEW.ForbWords, OLD.ForbWords)
,JunkPos = NULLIF(NEW.JunkPos, OLD.JunkPos)
,AwaitPos = NULLIF(NEW.AwaitPos, OLD.AwaitPos)
,StartLine = NULLIF(NEW.StartLine, OLD.StartLine)
,ListName = NULLIF(NEW.ListName, OLD.ListName)
,TxtCodeBegin = NULLIF(NEW.TxtCodeBegin, OLD.TxtCodeBegin)
,TxtCodeEnd = NULLIF(NEW.TxtCodeEnd, OLD.TxtCodeEnd)
,TxtCodeCrBegin = NULLIF(NEW.TxtCodeCrBegin, OLD.TxtCodeCrBegin)
,TxtCodeCrEnd = NULLIF(NEW.TxtCodeCrEnd, OLD.TxtCodeCrEnd)
,TxtNameBegin = NULLIF(NEW.TxtNameBegin, OLD.TxtNameBegin)
,TxtNameEnd = NULLIF(NEW.TxtNameEnd, OLD.TxtNameEnd)
,TxtFirmCrBegin = NULLIF(NEW.TxtFirmCrBegin, OLD.TxtFirmCrBegin)
,TxtFirmCrEnd = NULLIF(NEW.TxtFirmCrEnd, OLD.TxtFirmCrEnd)
,TxtCountryCrBegin = NULLIF(NEW.TxtCountryCrBegin, OLD.TxtCountryCrBegin)
,TxtCountryCrEnd = NULLIF(NEW.TxtCountryCrEnd, OLD.TxtCountryCrEnd)
,TxtBaseCostBegin = NULLIF(NEW.TxtBaseCostBegin, OLD.TxtBaseCostBegin)
,TxtBaseCostEnd = NULLIF(NEW.TxtBaseCostEnd, OLD.TxtBaseCostEnd)
,TxtMinBoundCostBegin = NULLIF(NEW.TxtMinBoundCostBegin, OLD.TxtMinBoundCostBegin)
,TxtMinBoundCostEnd = NULLIF(NEW.TxtMinBoundCostEnd, OLD.TxtMinBoundCostEnd)
,TxtCurrencyBegin = NULLIF(NEW.TxtCurrencyBegin, OLD.TxtCurrencyBegin)
,TxtCurrencyEnd = NULLIF(NEW.TxtCurrencyEnd, OLD.TxtCurrencyEnd)
,TxtUnitBegin = NULLIF(NEW.TxtUnitBegin, OLD.TxtUnitBegin)
,TxtUnitEnd = NULLIF(NEW.TxtUnitEnd, OLD.TxtUnitEnd)
,TxtVolumeBegin = NULLIF(NEW.TxtVolumeBegin, OLD.TxtVolumeBegin)
,TxtVolumeEnd = NULLIF(NEW.TxtVolumeEnd, OLD.TxtVolumeEnd)
,TxtQuantityBegin = NULLIF(NEW.TxtQuantityBegin, OLD.TxtQuantityBegin)
,TxtQuantityEnd = NULLIF(NEW.TxtQuantityEnd, OLD.TxtQuantityEnd)
,TxtNoteBegin = NULLIF(NEW.TxtNoteBegin, OLD.TxtNoteBegin)
,TxtNoteEnd = NULLIF(NEW.TxtNoteEnd, OLD.TxtNoteEnd)
,TxtPeriodBegin = NULLIF(NEW.TxtPeriodBegin, OLD.TxtPeriodBegin)
,TxtPeriodEnd = NULLIF(NEW.TxtPeriodEnd, OLD.TxtPeriodEnd)
,TxtDocBegin = NULLIF(NEW.TxtDocBegin, OLD.TxtDocBegin)
,TxtDocEnd = NULLIF(NEW.TxtDocEnd, OLD.TxtDocEnd)
,TxtJunkBegin = NULLIF(NEW.TxtJunkBegin, OLD.TxtJunkBegin)
,TxtJunkEnd = NULLIF(NEW.TxtJunkEnd, OLD.TxtJunkEnd)
,TxtAwaitBegin = NULLIF(NEW.TxtAwaitBegin, OLD.TxtAwaitBegin)
,TxtAwaitEnd = NULLIF(NEW.TxtAwaitEnd, OLD.TxtAwaitEnd)
,FCode = NULLIF(NEW.FCode, OLD.FCode)
,FCodeCr = NULLIF(NEW.FCodeCr, OLD.FCodeCr)
,FName1 = NULLIF(NEW.FName1, OLD.FName1)
,FName2 = NULLIF(NEW.FName2, OLD.FName2)
,FName3 = NULLIF(NEW.FName3, OLD.FName3)
,FFirmCr = NULLIF(NEW.FFirmCr, OLD.FFirmCr)
,FCountryCr = NULLIF(NEW.FCountryCr, OLD.FCountryCr)
,FBaseCost = NULLIF(NEW.FBaseCost, OLD.FBaseCost)
,FMinBoundCost = NULLIF(NEW.FMinBoundCost, OLD.FMinBoundCost)
,FCurrency = NULLIF(NEW.FCurrency, OLD.FCurrency)
,FUnit = NULLIF(NEW.FUnit, OLD.FUnit)
,FVolume = NULLIF(NEW.FVolume, OLD.FVolume)
,FQuantity = NULLIF(NEW.FQuantity, OLD.FQuantity)
,FNote = NULLIF(NEW.FNote, OLD.FNote)
,FPeriod = NULLIF(NEW.FPeriod, OLD.FPeriod)
,FDoc = NULLIF(NEW.FDoc, OLD.FDoc)
,FJunk = NULLIF(NEW.FJunk, OLD.FJunk)
,FAwait = NULLIF(NEW.FAwait, OLD.FAwait)
,Memo = NULLIF(NEW.Memo, OLD.Memo)
,Currency = NULLIF(NEW.Currency, OLD.Currency)
,TxtVitallyImportantBegin = NULLIF(NEW.TxtVitallyImportantBegin, OLD.TxtVitallyImportantBegin)
,TxtVitallyImportantEnd = NULLIF(NEW.TxtVitallyImportantEnd, OLD.TxtVitallyImportantEnd)
,FVitallyImportant = NULLIF(NEW.FVitallyImportant, OLD.FVitallyImportant)
,VitallyImportantMask = NULLIF(NEW.VitallyImportantMask, OLD.VitallyImportantMask)
,TxtRequestRatioBegin = NULLIF(NEW.TxtRequestRatioBegin, OLD.TxtRequestRatioBegin)
,TxtRequestRatioEnd = NULLIF(NEW.TxtRequestRatioEnd, OLD.TxtRequestRatioEnd)
,FRequestRatio = NULLIF(NEW.FRequestRatio, OLD.FRequestRatio)
,TxtRegistryCostBegin = NULLIF(NEW.TxtRegistryCostBegin, OLD.TxtRegistryCostBegin)
,TxtRegistryCostEnd = NULLIF(NEW.TxtRegistryCostEnd, OLD.TxtRegistryCostEnd)
,FRegistryCost = NULLIF(NEW.FRegistryCost, OLD.FRegistryCost)
,FMaxBoundCost = NULLIF(NEW.FMaxBoundCost, OLD.FMaxBoundCost)
,TxtMaxBoundCostBegin = NULLIF(NEW.TxtMaxBoundCostBegin, OLD.TxtMaxBoundCostBegin)
,TxtMaxBoundCostEnd = NULLIF(NEW.TxtMaxBoundCostEnd, OLD.TxtMaxBoundCostEnd)
,FOrderCost = NULLIF(NEW.FOrderCost, OLD.FOrderCost)
,TxtOrderCostBegin = NULLIF(NEW.TxtOrderCostBegin, OLD.TxtOrderCostBegin)
,TxtOrderCostEnd = NULLIF(NEW.TxtOrderCostEnd, OLD.TxtOrderCostEnd)
,FMinOrderCount = NULLIF(NEW.FMinOrderCount, OLD.FMinOrderCount)
,TxtMinOrderCountBegin = NULLIF(NEW.TxtMinOrderCountBegin, OLD.TxtMinOrderCountBegin)
,TxtMinOrderCountEnd = NULLIF(NEW.TxtMinOrderCountEnd, OLD.TxtMinOrderCountEnd)
;END ;;

DELIMITER ;


DELIMITER ;;

CREATE TRIGGER farm.FormRulesLogDelete AFTER Delete ON farm.FormRules FOR EACH ROW
BEGIN
INSERT INTO `logs`.form_rules_logs
SET LogTime = now() ,
OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)) ,
OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)) ,
Operation = 2,
FormRulesID = OLD.Id
,PriceFormatId = OLD.PriceFormatId
,MaxOld = OLD.MaxOld
,Delimiter = OLD.Delimiter
,ParentFormRules = OLD.ParentFormRules
,FormByCode = OLD.FormByCode
,NameMask = OLD.NameMask
,ForbWords = OLD.ForbWords
,JunkPos = OLD.JunkPos
,AwaitPos = OLD.AwaitPos
,StartLine = OLD.StartLine
,ListName = OLD.ListName
,TxtCodeBegin = OLD.TxtCodeBegin
,TxtCodeEnd = OLD.TxtCodeEnd
,TxtCodeCrBegin = OLD.TxtCodeCrBegin
,TxtCodeCrEnd = OLD.TxtCodeCrEnd
,TxtNameBegin = OLD.TxtNameBegin
,TxtNameEnd = OLD.TxtNameEnd
,TxtFirmCrBegin = OLD.TxtFirmCrBegin
,TxtFirmCrEnd = OLD.TxtFirmCrEnd
,TxtCountryCrBegin = OLD.TxtCountryCrBegin
,TxtCountryCrEnd = OLD.TxtCountryCrEnd
,TxtBaseCostBegin = OLD.TxtBaseCostBegin
,TxtBaseCostEnd = OLD.TxtBaseCostEnd
,TxtMinBoundCostBegin = OLD.TxtMinBoundCostBegin
,TxtMinBoundCostEnd = OLD.TxtMinBoundCostEnd
,TxtCurrencyBegin = OLD.TxtCurrencyBegin
,TxtCurrencyEnd = OLD.TxtCurrencyEnd
,TxtUnitBegin = OLD.TxtUnitBegin
,TxtUnitEnd = OLD.TxtUnitEnd
,TxtVolumeBegin = OLD.TxtVolumeBegin
,TxtVolumeEnd = OLD.TxtVolumeEnd
,TxtQuantityBegin = OLD.TxtQuantityBegin
,TxtQuantityEnd = OLD.TxtQuantityEnd
,TxtNoteBegin = OLD.TxtNoteBegin
,TxtNoteEnd = OLD.TxtNoteEnd
,TxtPeriodBegin = OLD.TxtPeriodBegin
,TxtPeriodEnd = OLD.TxtPeriodEnd
,TxtDocBegin = OLD.TxtDocBegin
,TxtDocEnd = OLD.TxtDocEnd
,TxtJunkBegin = OLD.TxtJunkBegin
,TxtJunkEnd = OLD.TxtJunkEnd
,TxtAwaitBegin = OLD.TxtAwaitBegin
,TxtAwaitEnd = OLD.TxtAwaitEnd
,FCode = OLD.FCode
,FCodeCr = OLD.FCodeCr
,FName1 = OLD.FName1
,FName2 = OLD.FName2
,FName3 = OLD.FName3
,FFirmCr = OLD.FFirmCr
,FCountryCr = OLD.FCountryCr
,FBaseCost = OLD.FBaseCost
,FMinBoundCost = OLD.FMinBoundCost
,FCurrency = OLD.FCurrency
,FUnit = OLD.FUnit
,FVolume = OLD.FVolume
,FQuantity = OLD.FQuantity
,FNote = OLD.FNote
,FPeriod = OLD.FPeriod
,FDoc = OLD.FDoc
,FJunk = OLD.FJunk
,FAwait = OLD.FAwait
,Memo = OLD.Memo
,Currency = OLD.Currency
,TxtVitallyImportantBegin = OLD.TxtVitallyImportantBegin
,TxtVitallyImportantEnd = OLD.TxtVitallyImportantEnd
,FVitallyImportant = OLD.FVitallyImportant
,VitallyImportantMask = OLD.VitallyImportantMask
,TxtRequestRatioBegin = OLD.TxtRequestRatioBegin
,TxtRequestRatioEnd = OLD.TxtRequestRatioEnd
,FRequestRatio = OLD.FRequestRatio
,TxtRegistryCostBegin = OLD.TxtRegistryCostBegin
,TxtRegistryCostEnd = OLD.TxtRegistryCostEnd
,FRegistryCost = OLD.FRegistryCost
,FMaxBoundCost = OLD.FMaxBoundCost
,TxtMaxBoundCostBegin = OLD.TxtMaxBoundCostBegin
,TxtMaxBoundCostEnd = OLD.TxtMaxBoundCostEnd
,FOrderCost = OLD.FOrderCost
,TxtOrderCostBegin = OLD.TxtOrderCostBegin
,TxtOrderCostEnd = OLD.TxtOrderCostEnd
,FMinOrderCount = OLD.FMinOrderCount
,TxtMinOrderCountBegin = OLD.TxtMinOrderCountBegin
,TxtMinOrderCountEnd = OLD.TxtMinOrderCountEnd
,FormRulesID = OLD.Id
,PriceFormatId = OLD.PriceFormatId
;END ;;

DELIMITER ;


-- восстанавливаем логирование в Sources

alter table logs.sources_logs
  drop column `SourceType`,
  drop column `LastDateTime`,
  drop column `PriceDateTime`,
  drop column `EMailPassword`,
  add column `SourceTypeId` int(11) unsigned default NULL after `sourcesId`,
  add column `FTPPassiveMode` tinyint(1) default NULL after `FTPPassword`;


DELIMITER ;;

CREATE TRIGGER farm.sourcesLogInsert AFTER Insert ON farm.sources FOR EACH ROW
BEGIN
INSERT INTO `logs`.sources_logs
SET LogTime = now() ,
OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)) ,
OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)) ,
Operation = 0,
sourcesID = NEW.Id
,SourceTypeId = NEW.SourceTypeId
,PricePath = NEW.PricePath
,EMailTo = NEW.EMailTo
,EMailFrom = NEW.EMailFrom
,FTPDir = NEW.FTPDir
,FTPLogin = NEW.FTPLogin
,FTPPassword = NEW.FTPPassword
,FTPPassiveMode = NEW.FTPPassiveMode
,PriceMask = NEW.PriceMask
,ExtrMask = NEW.ExtrMask
,HTTPLogin = NEW.HTTPLogin
,HTTPPassword = NEW.HTTPPassword
;END ;;

DELIMITER ;


DELIMITER ;;

CREATE TRIGGER farm.sourcesLogUpdate AFTER Update ON farm.sources FOR EACH ROW
BEGIN
INSERT INTO `logs`.sources_logs
SET LogTime = now() ,
OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)) ,
OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)) ,
Operation = 1,
sourcesID = OLD.Id
,SourceTypeId = IFNULL(NEW.SourceTypeId, OLD.SourceTypeId)
,PricePath = NULLIF(NEW.PricePath, OLD.PricePath)
,EMailTo = NULLIF(NEW.EMailTo, OLD.EMailTo)
,EMailFrom = NULLIF(NEW.EMailFrom, OLD.EMailFrom)
,FTPDir = NULLIF(NEW.FTPDir, OLD.FTPDir)
,FTPLogin = NULLIF(NEW.FTPLogin, OLD.FTPLogin)
,FTPPassword = NULLIF(NEW.FTPPassword, OLD.FTPPassword)
,FTPPassiveMode = NULLIF(NEW.FTPPassiveMode, OLD.FTPPassiveMode)
,PriceMask = NULLIF(NEW.PriceMask, OLD.PriceMask)
,ExtrMask = NULLIF(NEW.ExtrMask, OLD.ExtrMask)
,HTTPLogin = NULLIF(NEW.HTTPLogin, OLD.HTTPLogin)
,HTTPPassword = NULLIF(NEW.HTTPPassword, OLD.HTTPPassword)
;END ;;

DELIMITER ;


DELIMITER ;;

CREATE TRIGGER farm.sourcesLogDelete AFTER Delete ON farm.sources FOR EACH ROW
BEGIN
INSERT INTO `logs`.sources_logs
SET LogTime = now() ,
OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)) ,
OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)) ,
Operation = 2,
sourcesID = OLD.Id
,SourceTypeId = OLD.SourceTypeId
,PricePath = OLD.PricePath
,EMailTo = OLD.EMailTo
,EMailFrom = OLD.EMailFrom
,FTPDir = OLD.FTPDir
,FTPLogin = OLD.FTPLogin
,FTPPassword = OLD.FTPPassword
,FTPPassiveMode = OLD.FTPPassiveMode
,PriceMask = OLD.PriceMask
,ExtrMask = OLD.ExtrMask
,HTTPLogin = OLD.HTTPLogin
,HTTPPassword = OLD.HTTPPassword
;END ;;

DELIMITER ;


-- восстанавливаем триггеры для обновления PriceItems

drop trigger if exists Catalogs.AssortmentAfterInsert;

drop trigger if exists farm.AssortmentAfterInsert;


DELIMITER ;;

CREATE TRIGGER Catalogs.AssortmentAfterInsert AFTER INSERT ON Catalogs.Assortment FOR EACH ROW
BEGIN
  update
    usersettings.PriceItems,
    usersettings.PricesCosts
  set
    PriceItems.PriceDate = now(),
    PriceItems.LastFormalization = now()
  where
        PricesCosts.PriceCode = 2647
    and PricesCosts.BaseCost = 1
    and PriceItems.Id = PricesCosts.PriceItemId;
end ;;

DELIMITER ;


-- изменяем процедуры в базе reports

DELIMITER $$

DROP PROCEDURE IF EXISTS reports.GetPriceCode $$

CREATE PROCEDURE reports.GetPriceCode(IN inFirmCode BIGINT, IN inFilter VARCHAR(255), IN inID BIGINT)
BEGIN
  DECLARE filterStr varchar(257);
  drop temporary table if exists tempGetPriceCode;
  create temporary table tempGetPriceCode
  ENGINE=memory
  SELECT
    pd.PriceCode as PriceCode,
    convert(concat(pd.PriceCode, ' - ', cd.ShortName, ' (', pd.PriceName, ') - ', rg.Region) using cp1251) AS PriceName
  FROM
    usersettings.pricesdata pd
    inner join usersettings.clientsdata cd on cd.FirmCode = pd.FirmCode
    inner join farm.regions rg on rg.RegionCode = cd.RegionCode
  where
      cd.FirmType = 0
  and cd.FirmStatus = 1
  and pd.AgencyEnabled = 1
  and pd.Enabled = 1;
  if (inID is not null) then
    select
      tmp.PriceCode as ID,
      tmp.PriceName as DisplayValue
    from
      tempGetPriceCode tmp
    where
      tmp.PriceCode = inID
    order by tmp.PriceName;
  else
    if ((inFilter is not null) and (length(inFilter) > 0)) then
      set filterStr = concat('%', inFilter, '%');
      select
        tmp.PriceCode as ID,
        tmp.PriceName as DisplayValue
      from
        tempGetPriceCode tmp
      where
        tmp.PriceName like filterStr
      order by tmp.PriceName;
    else
      select
        tmp.PriceCode as ID,
        tmp.PriceName as DisplayValue
      from
        tempGetPriceCode tmp
      order by tmp.PriceName;
    end if;
  end if;
  drop table if exists tempGetPriceCode;
END $$

DELIMITER ;


DELIMITER $$

DROP PROCEDURE IF EXISTS reports.GetAllPriceCode $$

CREATE PROCEDURE reports.GetAllPriceCode(IN inFirmCode BIGINT, IN inFilter VARCHAR(255), IN inID BIGINT)
BEGIN
  DECLARE filterStr varchar(257);
  drop temporary table if exists tempGetPriceCode;
  create temporary table tempGetPriceCode
  ENGINE=memory
  SELECT
    pd.PriceCode as PriceCode,
    convert(concat(pd.PriceCode, ' - ', cd.ShortName, ' (', pd.PriceName, ') - ', rg.Region) using cp1251) AS PriceName
  FROM
    usersettings.pricesdata pd
    inner join usersettings.clientsdata cd on cd.FirmCode = pd.FirmCode
    inner join farm.regions rg on rg.RegionCode = cd.RegionCode
  where
      cd.FirmType = 0;
  if (inID is not null) then
    select
      tmp.PriceCode as ID,
      tmp.PriceName as DisplayValue
    from
      tempGetPriceCode tmp
    where
      tmp.PriceCode = inID
    order by tmp.PriceName;
  else
    if ((inFilter is not null) and (length(inFilter) > 0)) then
      set filterStr = concat('%', inFilter, '%');
      select
        tmp.PriceCode as ID,
        tmp.PriceName as DisplayValue
      from
        tempGetPriceCode tmp
      where
        tmp.PriceName like filterStr
      order by tmp.PriceName;
    else
      select
        tmp.PriceCode as ID,
        tmp.PriceName as DisplayValue
      from
        tempGetPriceCode tmp
      order by tmp.PriceName;
    end if;
  end if;
  drop table if exists tempGetPriceCode;
END $$

DELIMITER ;



-- изменяем процедуру usersettings.GetStatLog
DELIMITER $$

DROP PROCEDURE IF EXISTS usersettings.GetStatLog $$


Create PROCEDURE usersettings.GetStatLog(IN inLogStart DATETIME, IN inLogEnd DATETIME)
BEGIN
  
  drop temporary table if exists  tempdownlogs;
  create temporary table tempdownlogs (
    rowid int unsigned,
    priceitemid int unsigned,
    resultcode mediumint unsigned,
    fixedtime datetime,
    key (rowid),
    key (priceitemid),
    key (resultcode)) engine=MEMORY;
  insert into tempdownlogs
    SELECT
      rowid, priceitemid, resultcode, null
    FROM
      logs.downlogs d
    where (inLogStart < logtime) and (logtime < inLogEnd);
  
  select
    min(Rowid)
  into @minrowid
  from
    tempdownlogs
  where
        resultcode in (3, 5);
  update tempdownlogs, logs.downlogs dl
  set
    fixedtime = dl.logtime
  where
        tempdownlogs.priceitemid=dl.priceitemid
    and tempdownlogs.rowid >= @minrowid
    and @minrowid < dl.rowid
    and tempdownlogs.rowid < dl.rowid 
    and dl.resultcode = 2
    and tempdownlogs.resultcode in (3, 5);
  
  drop temporary table if exists  tempformlogs;
  create temporary table tempformlogs (
    rowid int unsigned primary key,
    logtime datetime,
    priceitemid int unsigned,
    resultid mediumint unsigned,
    fixedtime datetime,
    key priceitemid(rowid, priceitemid),
    key resultid(rowid, resultid)) engine=MEMORY;
  insert into tempformlogs
    SELECT
      rowid, logtime, priceitemid, resultid, null
    FROM
      logs.formlogs d
    where (inLogStart < logtime) and (logtime < inLogEnd);
  update tempformlogs, logs.formlogs dl
  set
    fixedtime = dl.logtime
  where
        tempformlogs.priceitemid=dl.priceitemid
    and tempformlogs.rowid < dl.rowid
    and dl.resultid in (2, 3)
    and tempformlogs.resultid = 5;
SELECT
  1 As LAppCode,
  LogTime AS LLogTime,
  logs.priceitemid as LPriceItemId,
  null As LFirmName,
  null As LRegion,
  null As LFirmSegment,
  null as LPriceName,
  null As LPriceCode,
  null as LForm,
  null as LUnform,
  null as LZero,
  null as LForb,
  
  if(ResultCode = 2, null, Addition) As LAddition,
  ResultCode As LResultID,
  1 as LStatus
FROM
  logs.downlogs as logs
WHERE
    logs.priceitemid is null
and logs.LogTime > inLogStart
and logs.LogTime < inLogEnd
union
SELECT
  1 As LAppCode,
  logs.LogTime AS LLogTime,
  logs.priceitemid as LPriceItemId,
  cd.ShortName As LFirmName,
  r.Region As LRegion,
  cd.FirmSegment As LFirmSegment,
  if(pd.CostType = 1, convert(concat(convert("[Колонка] " using cp1251), pc.CostName) using cp1251), pd.PriceName) as LPriceName,
  pd.PriceCode As LPriceCode,
  null as LForm,
  null as LUnform,
  null as LZero,
  null as LForb,
  
  if(logs.ResultCode = 2, null, logs.Addition) As LAddition,
  logs.ResultCode As LResultID,
  if((logs.ResultCode = 2), 0, if(tempdownlogs.fixedtime is null, 1, 2)) as LStatus
FROM
  logs.downlogs as logs,
  usersettings.PriceItems pim,
  usersettings.clientsdata cd,
  usersettings.pricesdata pd,
  usersettings.pricescosts pc,
  farm.regions r,
  tempdownlogs
WHERE
    pim.Id = logs.PriceItemId
and pc.PriceItemId = pim.Id
and pc.PriceCode = pd.PriceCode
and ((pd.CostType = 1) or (pc.BaseCost = 1))
and cd.firmcode=pd.firmcode
and r.regioncode=cd.regioncode
and tempdownlogs.RowId = logs.RowID
and logs.LogTime > inLogStart
and logs.LogTime < inLogEnd
union
SELECT
  0 As LAppCode,
  LogTime AS LLogTime,
  logs.priceitemid as LPriceItemId,
  null As LFirmName,
  null As LRegion,
  null As LFirmSegment,
  null as LPriceName,
  null As LPriceCode,
  if(Form is null, 0, Form) As LForm,
  if(Unform is null, 0, Unform) As LUnform,
  if(Zero is null, 0, Zero) As LZero,
  if(Forb is null, 0, Forb) As LForb,
  Addition As LAddition,
  ResultID As LResultID,
  1 as LStatus
FROM
  logs.formlogs as logs
WHERE
    logs.PriceItemId is null
and logs.LogTime > inLogStart
and logs.LogTime < inLogEnd
union
SELECT
  0 As LAppCode,
  logs.LogTime AS LLogTime,
  logs.priceitemid as LPriceItemId,
  cd.ShortName As LFirmName,
  r.Region As LRegion,
  cd.FirmSegment As LFirmSegment,
  if(pd.CostType = 1, convert(concat(convert("[Колонка] " using cp1251), pc.CostName) using cp1251), pd.PriceName) as LPriceName,
  pd.PriceCode As LPriceCode,
  if(logs.Form is null, 0, logs.Form) As LForm,
  if(logs.Unform is null, 0, logs.Unform) As LUnform,
  if(logs.Zero is null, 0, logs.Zero) As LZero,
  if(logs.Forb is null, 0, logs.Forb) As LForb,
  logs.Addition As LAddition,
  logs.ResultID As LResultID,
  if((logs.ResultID = 2) or (logs.ResultID = 3), 0, if(tempformlogs.fixedtime is null, 1, 2)) as LStatus
FROM
  logs.formlogs as logs,
  usersettings.PriceItems pim,
  usersettings.clientsdata cd,
  usersettings.pricesdata pd,
  usersettings.pricescosts pc,
  farm.regions r,
  tempformlogs
WHERE
    pim.Id = logs.PriceItemId
and pc.PriceItemId = pim.Id
and pc.PriceCode = pd.PriceCode
and ((pd.CostType = 1) or (pc.BaseCost = 1))
and cd.firmcode=pd.firmcode
and r.regioncode=cd.regioncode
and tempformlogs.RowId = logs.RowId
and logs.LogTime > inLogStart
and logs.LogTime < inLogEnd
order by LLogTime;
END $$

DELIMITER ;


-- ordersendrules

DELIMITER $$

DROP PROCEDURE IF EXISTS ordersendrules.GetNonProcessedID $$


CREATE PROCEDURE ordersendrules.`GetNonProcessedID`()
BEGIN
SELECT  oh.RowId
FROM    usersettings.pricesdata pd,
        usersettings.retclientsset rcs,
        usersettings.clientsdata cd,
        OrderSendRules.order_send_rules osc,
        orders.ordershead oh
WHERE   oh.processed = 0
        AND if(SubmitOrders = 1, Submited, 1)
        AND Deleted = 0
        AND oh.pricecode  = pd.pricecode
        AND rcs.clientcode = oh.clientcode
        AND cd.firmcode   = pd.firmcode
        AND  osc.FirmCode = cd.FirmCode
        AND  (select count(ol.rowid) from orders.orderslist ol where ol.orderid=oh.rowid)=oh.rowcount
group by date(writetime), oh.clientcode, rowcount, clientorderid
;
END $$


DELIMITER ;


DELIMITER $$

DROP PROCEDURE IF EXISTS ordersendrules.GetOrderHeader $$

CREATE PROCEDURE ordersendrules.`GetOrderHeader`(IN idparam  integer unsigned)
BEGIN
SELECT  Writetime + interval reg.MoscowBias hour Writetime,
        oh.ClientCode                                     ,
        PriceDate + interval reg.MoscowBias hour PriceDate,
        clientAddition ClientComment                      ,
        RowCount                                          ,
        oh.PriceCode                                      ,
        cd.ShortName ClientShortName                      ,
        cd.FullName ClientFullName                        ,
        cd.Adress ClientAddress                           ,
        (select c.contactText
        from contacts.contact_groups cg
          join contacts.contacts c on cg.Id = c.ContactOwnerId
        where cd.ContactGroupOwnerId = cg.ContactGroupOwnerId
              and cg.Type = 0
              and c.Type = 1
        limit 1) as ClientPhone,
        i2.FirmClientCode                                 ,
        i2.FirmClientCode2                                ,
        i2.FirmClientCode3                                ,
        min(i.PublicCostCorr) PublicCostCorr              ,
        min(i.FirmCostCorr) FirmCostCorr                  ,
        (SELECT round(sum(ol.cost*ol.Quantity),2)
        FROM    orders.orderslist as ol
        WHERE   ol.orderid= oh.RowId
        )             as Summ         ,
        cdf.ShortName as FirmShortName,
        cd.regionCode ClientRegionCode,
        pd.FirmCode                   ,
        pd.pricename PriceName        ,
        region RegionName             ,
        cd.firmsegment OrderSegment   ,
        rcs.ServiceClient             ,
        cd.BillingCode                ,
        pc.CostName
FROM    usersettings.clientsdata         as cd ,
        usersettings.clientsdata         as cdf,
        usersettings.pricesdata          as pd ,
        usersettings.regionaldata        as rd ,
        farm.regions                     as reg,
        usersettings.retclientsset       as rcs,
        orders.ordershead                as oh
LEFT JOIN usersettings.includeregulation as ir
ON      ir.includeclientcode= oh.ClientCode
    AND IncludeType         =0
LEFT JOIN usersettings.intersection i2
ON      i2.clientcode = oh.clientcode
    AND i2.regioncode = oh.regioncode
    AND i2.pricecode  = oh.pricecode
LEFT JOIN usersettings.intersection i
ON      i.PriceCode  = oh.PriceCode
    AND i.regionCode = oh.regionCode
    AND i.ClientCode = if(ir.primaryclientcode is null, oh.ClientCode, ir.primaryclientcode)
LEFT JOIN usersettings.pricescosts pc
ON      pc.costcode    = i.costcode
WHERE   cd.firmcode    = oh.ClientCode
    AND oh.PriceCode   = pd.PriceCode
    AND cdf.firmcode   = pd.FirmCode
    AND rd.regionCode  = oh.regionCode
    AND rd.firmCode    = pd.FirmCode
    AND reg.regioncode = cd.regioncode
    AND cd.firmcode    = rcs.clientcode
    AND oh.rowid       = idparam
GROUP BY oh.RowId;
END $$

DELIMITER ;


DELIMITER $$

DROP PROCEDURE IF EXISTS ordersendrules.GetOrderRows $$

CREATE PROCEDURE ordersendrules.`GetOrderRows`(IN IdParam INTEGER UNSIGNED)
BEGIN
  SELECT  ol.Code,
        ol.CodeCr,
        if(st.Synonym is null, concat(cn.name,'  ', concat(cf.form, ' ', ifnull(group_concat(pv.`value` SEPARATOR ' '), ''))), st.synonym) as FullName,
        if(si.SynonymFirmcrCode is null, ifNull(firmCr, '-'), si.synonym) as CrName,
        ol.ProductId as FullCode,
        ol.CodeFirmCr,
        ol.Quantity,
        ol.Cost,
        ol.Junk,
        ol.Await,
        c.period,
        ol.Cost as basecost,
        ol.RowId
FROM    (orders.ordershead as oh, orders.orderslist as ol, usersettings.intersection i, usersettings.retclientsset rcs)
  JOIN Catalogs.Products as p on p.Id = ol.ProductId
  JOIN Catalogs.Catalog as ca on ca.Id = p.CatalogId
  JOIN Catalogs.catalognames cn on cn.id = ca.nameid
  JOIN Catalogs.catalogforms cf on cf.id = ca.formid
    LEFT JOIN Catalogs.productproperties pp on p.id = pp.productid
      LEFT JOIN Catalogs.propertyvalues pv on pv.id = pp.propertyvalueid
LEFT JOIN farm.synonymArchive as st
        ON st.SynonymCode = ol.SynonymCode
LEFT JOIN farm.synonymFirmCr as si
        ON si.SynonymFirmCrCode = ol.SynonymFirmCrCode
LEFT JOIN farm.core0 as c
        ON c.SynonymCode       = ol.SynonymCode
        AND c.SynonymFirmCrCode= ol.SynonymFirmCrCode
        AND c.PriceCode         = i.costcode
        AND ol.junk            = c.junk
LEFT JOIN farm.CatalogFirmCr as i
        ON i.CodefirmCr    = ol.CodefirmCr
WHERE   oh.RowId           = ol.Orderid
        AND p.Id           = ol.ProductId
        AND i.clientcode   = oh.clientcode
        AND i.regioncode   = oh.regioncode
        AND i.pricecode    = oh.pricecode
        AND rcs.clientcode = oh.clientcode
        AND ol.orderid     =IdParam
GROUP BY ol.RowId, p.id, cf.id
ORDER BY FullName;
END $$

DELIMITER ;


DELIMITER $$

DROP PROCEDURE IF EXISTS ordersendrules.GetOrderSendConfig $$

CREATE PROCEDURE ordersendrules.`GetOrderSendConfig`(IN idparam INTEGER UNSIGNED)
BEGIN IF (SELECT count(*) > 0
  FROM OrderSendRules.order_send_rules osr
    JOIN usersettings.pricesdata pd on pd.firmcode = osr.firmcode
      JOIN orders.ordershead oh on oh.pricecode = pd.pricecode and osr.RegionCode = oh.RegionCode
   WHERE oh.rowid = idparam) THEN
   SELECT osr.id,
          if(customer.billingcode=921,
            'tech@analit.net',
            if(length(ifnull(adminMail, ''))+length(ifnull(tmpMail, ''))>5,
              concat(ifnull(adminMail, ''),
              if(length(tmpMail)>3,
                concat(',', ifnull(tmpMail, '')), '')),
                'tech@analit.net')) Destination,
          ohs.ClassName as SenderClassName,
          ohf.ClassName as FormaterClassName,
          ErrorNotificationDelay,
          SendDebugMessage
   FROM OrderSendRules.order_send_rules osr
      JOIN OrderSendRules.order_handlers ohs on ohs.Id = osr.SenderId
      JOIN OrderSendRules.order_handlers ohf on ohf.Id = osr.FormaterId
      JOIN usersettings.pricesdata pd on pd.firmcode = osr.firmcode
        JOIN orders.ordershead oh on oh.pricecode = pd.pricecode and osr.RegionCode = oh.RegionCode
          JOIN usersettings.regionaldata rd on rd.regioncode = oh.regioncode and rd.firmcode = pd.firmcode
            JOIN usersettings.clientsdata cd on cd.firmcode = pd.firmcode
              JOIN usersettings.clientsdata customer on customer.firmcode = oh.clientcode
   WHERE oh.rowid = idparam;
ELSE
   SELECT osr.id,
          if(customer.billingcode=921,
            'tech@analit.net',
            if(length(ifnull(adminMail, ''))+length(ifnull(tmpMail, ''))  > 5,
              concat(ifnull(adminMail, ''), if(length(tmpMail) > 3,
              concat(',',ifnull(tmpMail, '')), '')), 'tech@analit.net')) Destination,
          ohs.ClassName as SenderClassName,
          ohf.ClassName as FormaterClassName,
          ErrorNotificationDelay,
          SendDebugMessage
    FROM OrderSendRules.order_send_rules osr
      JOIN OrderSendRules.order_handlers ohs on ohs.Id = osr.SenderId
      JOIN OrderSendRules.order_handlers ohf on ohf.Id = osr.FormaterId
      JOIN usersettings.pricesdata pd on pd.firmcode = osr.firmcode
        JOIN orders.ordershead oh on oh.pricecode = pd.pricecode
          JOIN usersettings.regionaldata rd on rd.regioncode = oh.regioncode and rd.firmcode = pd.firmcode
            JOIN usersettings.clientsdata cd on cd.firmcode = pd.firmcode
              JOIN usersettings.clientsdata customer on customer.firmcode = oh.clientcode
   WHERE osr.RegionCode is null
         AND oh.rowid =  idparam;
END IF;
END $$

DELIMITER ;


-- usersettings GetOffers GetPrices GetActivePrices


DROP PROCEDURE IF EXISTS usersettings.PrgData1;

DROP PROCEDURE IF EXISTS usersettings.GetOffers;

DROP PROCEDURE IF EXISTS usersettings.GetActivePrices;

DROP PROCEDURE IF EXISTS usersettings.GetPrices;

DROP PROCEDURE IF EXISTS usersettings.CreateOrders;

DROP PROCEDURE IF EXISTS usersettings.GetAllOffersForClient;



DELIMITER $$

CREATE PROCEDURE usersettings.`GetPrices`(IN ClientCodeIN INT UNSIGNED)
BEGIN
Declare ClientCodeP int unsigned;
SELECT  min(primaryclientcode) into ClientCodeP  FROM includeregulation where includeclientcode=ClientCodeIN;
if ClientCodeP is null then
set ClientCodeP =ClientCodeIN;
end if;
drop temporary table IF EXISTS Prices;
create temporary table
Prices
(
 FirmCode int Unsigned,
 PriceCode int Unsigned, 
 CostCode int Unsigned,  
 PriceSynonymCode int Unsigned,
 RegionCode BigInt Unsigned,
 AlowInt bool,
 Fresh bool,
 Upcost decimal(7,5),
 PublicUpCost decimal(7,5),
 MaxSynonymCode Int Unsigned,
 MaxSynonymFirmCrCode Int Unsigned,
 DisabledByClient bool,
 Actual bool,
 CostType bool,
 PriceDate DateTime,
 ShowPriceName bool,
 PriceName VarChar(50),
 PositionCount int Unsigned,
 MinReq smallint Unsigned,
 ControlMinReq bool,
 CostCorrByClient bool,
 AllowOrder bool,
 ShortName varchar(50),
 FirmCategory tinyint unsigned,
 MainFirm bool,
 Storage bool,
 index using hash (PriceCode),
 index using hash  (RegionCode)
 
 
)engine=MEMORY;
INSERT
INTO    Prices
SELECT  pricesdata.firmcode,
        i.pricecode,
        i.costcode,
        ifnull(pricesdata.ParentSynonym, pricesdata.pricecode) PriceSynonymCode,
        i.RegionCode,
        AlowInt,
        iui.lastsent< pi.PriceDate, 
        round((1+pricesdata.UpCost/100) * (1+pricesregionaldata.UpCost/100) * (1+(i.FirmCostCorr+i.PublicCostCorr)/100), 5),
        i.PublicCostCorr,
        iui.MaxSynonymCode,
        iui.MaxSynonymFirmCrCode,
       if(iu.id is not null, iu.DisabledByClient, i.DisabledByClient),
        to_days(now())-to_days(pi.PriceDate)< f.maxold,
        pricesdata.CostType,
        date_sub(pi.PriceDate, interval time_to_sec(date_sub(now(), interval unix_timestamp() second)) second),
        if(iu.id is not null, ru.ShowPriceName, r.ShowPriceName),
        PriceName,
        pi.RowCount,
        if(i.MinReq>0, i.MinReq, pricesregionaldata.MinReq),
        if(iu.id is not null AND ir.includetype IN (1), iu.ControlMinReq, i.ControlMinReq),
        if(ir.includetype is not null, 0, i.CostCorrByClient),
        (if(iu.id is not null, ru.OrderRegionMask, r.OrderRegionMask) & i.RegionCode) > 0,
        clientsdata.ShortName,
        if(iu.id is not null, iu.FirmCategory, i.FirmCategory),
        if(iu.id is not null, iu.FirmCategory>=ru.BaseFirmCategory, i.FirmCategory>=r.BaseFirmCategory),
        Storage
FROM usersettings.intersection i
  JOIN usersettings.pricesdata ON pricesdata.pricecode = i.pricecode
  JOIN usersettings.PricesCosts pc on pc.CostCode = i.CostCode
    JOIN usersettings.PriceItems pi on pi.Id = pc.PriceItemId
      JOIN farm.formrules f on f.Id = pi.FormRuleId
    JOIN usersettings.clientsdata ON clientsdata.firmcode = pricesdata.firmcode
    JOIN usersettings.pricesregionaldata ON pricesregionaldata.regioncode = i.regioncode AND pricesregionaldata.pricecode = pricesdata.pricecode
    JOIN usersettings.RegionalData rd ON rd.RegionCode = i.regioncode AND rd.FirmCode = pricesdata.firmcode
  JOIN usersettings.clientsdata as AClientsData ON AClientsData.firmcode = i.clientcode and clientsdata.firmsegment = AClientsData.firmsegment
    JOIN usersettings.retclientsset r ON r.clientcode = AClientsData.FirmCode
  JOIN usersettings.intersection_update_info iui ON iui.pricecode = i.pricecode AND iui.regioncode = i.regioncode AND iui.ClientCode = ClientCodeIN
  LEFT JOIN usersettings.intersection iu ON iu.pricecode = iui.PriceCode and iu.clientcode = iui.clientcode and iui.regioncode = iu.regioncode
  LEFT JOIN usersettings.retclientsset ru ON ru.clientcode = iu.clientcode
  LEFT JOIN usersettings.includeregulation ir ON ir.primaryclientcode = ClientCodeP AND ir.includeclientcode = iui.clientcode
WHERE   i.DisabledByAgency = 0
    AND clientsdata.firmstatus = 1
    AND clientsdata.firmtype = 0
    AND (clientsdata.maskregion & i.regioncode) > 0
    AND (AClientsData.maskregion & i.regioncode) > 0
    AND pricesdata.agencyenabled = 1
    AND pricesdata.enabled = 1
    AND pricesdata.pricetype <> 1
    AND pricesregionaldata.enabled = 1
    AND (if(iu.id is not null, ru.WorkRegionMask, r.WorkRegionMask) & i.regioncode) > 0
    AND if(iu.id is not null AND ir.includetype IN (1), 1,  i.invisibleonclient = 0)
    AND if(iu.id is not null, iu.disabledbyagency = 0, 1)
    AND if(iu.id is not null AND ir.includetype IN (1), iu.invisibleonclient=0, 1)
    AND i.clientcode = ClientCodeP;
END $$

DELIMITER ;


DELIMITER $$

CREATE PROCEDURE usersettings.`GetActivePrices`(IN ClientCodeParam INT UNSIGNED)
BEGIN
Declare TabelExsists Bool DEFAULT false;
DECLARE CONTINUE HANDLER FOR 1146
begin
Call GetPrices(ClientCodeParam);
end;
if not TabelExsists then
DROP TEMPORARY TABLE IF EXISTS  ActivePrices;
create temporary table
ActivePrices
(
 Id int Unsigned auto_increment primary key,
 FirmCode int Unsigned,
 PriceCode int Unsigned,
 CostCode int Unsigned,
 PriceSynonymCode int Unsigned,
 RegionCode BigInt Unsigned,
 Fresh bool,
 Upcost decimal(7,5),
 PublicUpCost decimal(7,5),
 MaxSynonymCode Int Unsigned,
 MaxSynonymFirmCrCode Int Unsigned,
 CostType bool,
 PriceDate DateTime,
 ShowPriceName bool,
 PriceName VarChar(50),
 PositionCount int Unsigned,
 MinReq smallint Unsigned,
 CostCorrByClient bool,
 FirmCategory tinyint unsigned,
 MainFirm bool,
 unique using hash (PriceCode, RegionCode, CostCode),
 index using  hash (CostCode, PriceCode),
 index using hash (PriceSynonymCode),
 index using hash  (MaxSynonymCode),
 index using hash  (PriceCode),
 index using hash  (MaxSynonymFirmCrCode)
 )engine=MEMORY
 ;
set TabelExsists=true;
end if;
select null from Prices limit 0;
INSERT
INTO    ActivePrices 
        (
 FirmCode,
 PriceCode,
 CostCode,
 PriceSynonymCode,
 RegionCode,
 Fresh,
 Upcost,
 PublicUpCost,
 MaxSynonymCode,
 MaxSynonymFirmCrCode,
 
 CostType,
 PriceDate,
 ShowPriceName,
 PriceName,
 PositionCount,
 MinReq,
 CostCorrByClient,
 FirmCategory,
 MainFirm
        ) 
SELECT  FirmCode,
        PriceCode, 
        CostCode,
        PriceSynonymCode,
        RegionCode,
 Fresh,
        Upcost,
        PublicUpCost,
        MaxSynonymCode,
        MaxSynonymFirmCrCode,
        CostType,
        PriceDate,
        ShowPriceName,
        PriceName,
        PositionCount,
        MinReq,
        CostCorrByClient,
        FirmCategory,
        MainFirm
FROM    Prices 
WHERE   AlowInt =0
    AND DisabledByClient=0
    AND Actual=1;
drop temporary table IF EXISTS Prices;
END $$

DELIMITER ;


DELIMITER $$

CREATE PROCEDURE usersettings.`GetOffers`(IN ClientCodeParam INT UNSIGNED, IN FreshOnly BOOLEAN)
BEGIN
Declare SClientCode int unsigned;
Declare ClientRegionCode bigint unsigned;
Declare TableExsists Bool DEFAULT false;
DECLARE CONTINUE HANDLER FOR 1146
if not TableExsists then
call GetActivePrices(ClientCodeParam);
set TableExsists=true;
end if;
SELECT NULL FROM ActivePrices limit 0;
DROP TEMPORARY TABLE IF EXISTS Core, MinCosts;
CREATE TEMPORARY TABLE Core (
PriceCode INT unsigned,
RegionCode INT unsigned,
ProductId INT unsigned,
Cost DECIMAL(8,2) unsigned,
CryptCost VARCHAR(32) NOT NULL,
id bigint unsigned,
INDEX USING hash(id),
INDEX USING btree(PriceCode),
INDEX USING hash(ProductId, RegionCode, Cost),
INDEX USING hash(RegionCode, id)
)engine=MEMORY ;
CREATE TEMPORARY TABLE MinCosts (
MinCost DECIMAL(8,2) unsigned,
ProductId INT unsigned,
RegionCode INT unsigned,
PriceCode INT unsigned,
id bigint unsigned,
UNIQUE  MultiK(ProductId, RegionCode, MinCost),
INDEX USING hash(id)
)engine=MEMORY;

INSERT
INTO    Core
SELECT
        Prices.PriceCode,
        Prices.regioncode,
        c.ProductId,
        if(if(round(cc.Cost*Prices.UpCost,2)<MinBoundCost, MinBoundCost, round(cc.Cost*Prices.UpCost,2))>MaxBoundCost,
        MaxBoundCost, if(round(cc.Cost*Prices.UpCost,2)<MinBoundCost, MinBoundCost, round(cc.Cost*Prices.UpCost,2))),
        '',
        c.id
FROM farm.core0 c
  JOIN ActivePrices Prices on c.PriceCode = Prices.PriceCode
    JOIN farm.CoreCosts cc on cc.Core_Id = c.Id and cc.PC_CostCode = Prices.CostCode;

Delete from Core where Cost<0.01;
if (select PriceCodeOnly from retclientsset where clientcode=ClientCodeParam) is not null then
update core, retclientsset
 set cost=(1+(rand()*if(rand()>0.5,2, -2)/100))*cost
 where RetClientsSet.PriceCodeOnly!=Core.PriceCode
 and RetClientsSet.clientcode=ClientCodeParam;
end if;
INSERT
INTO    MinCosts
SELECT  
        min(Cost),
        ProductId,
        RegionCode,
        null,
        null
FROM    Core
GROUP BY ProductId,
        RegionCode;
UPDATE MinCosts,
        Core
        SET MinCosts.ID         = Core.ID,
            MinCosts.PriceCode  = Core.PriceCode
WHERE   Core.ProductId       = MinCosts.ProductId
        And Core.RegionCode = MinCosts.RegionCode
        And Core.Cost       = MinCosts.MinCost;
END $$

DELIMITER ;


DROP PROCEDURE IF EXISTS usersettings.GetAllOffersForClient;

DELIMITER $$

CREATE PROCEDURE usersettings.CreateOrders(IN customerId INT, IN FromOrderId INT)
BEGIN
  DECLARE customerHomeRegion BIGINT(20);
  DECLARE message varchar(255);
  DECLARE done bool DEFAULT 0;
  DECLARE currentOrderPriceDate datetime;
  DECLARE currentOrderPriceCode, currentOrderRowCount, CurrentOrderId int;
  DECLARE OrdersCursor CURSOR FOR SELECT Pricecode, PriceDate, RowCount from TempOrders;
  DECLARE CONTINUE HANDLER FOR SQLSTATE '02000' SET done = 1;
  IF FromOrderId is not null THEN
    SELECT oh.ClientAddition
    INTO message
    FROM Orders.OrdersHead oh
    WHERE oh.rowid = FromOrderId;
  END IF;
  SELECT RegionCode
  INTO customerHomeRegion
  FROM usersettings.clientsdata
  WHERE FirmCode = CustomerId;
  DROP TEMPORARY TABLE IF EXISTS TempOrders;
  CREATE TEMPORARY TABLE TempOrders as
  SELECT ap.pricecode, ap.PriceDate, count(*) as RowCount
  FROM supplies s
    JOIN allcoret c on s.CoreId = c.id
      JOIN ActivePrices ap on c.PriceCode = ap.pricecode
        JOIN UserSettings.Pricesdata pd on pd.PriceCode = ap.PriceCode
          JOIN usersettings.clientsdata cd on cd.FirmCode = pd.FirmCode
  GROUP BY PriceCode;
  OPEN OrdersCursor;
  SET Done = 0;
  REPEAT
    FETCH OrdersCursor INTO CurrentOrderPriceCode, CurrentOrderPriceDate, CurrentOrderRowCount;
      IF NOT done THEN
        INSERT INTO Orders.OrdersHead(WriteTime, ClientCode, PriceCode, RegionCode, PriceDate, RowCount, ClientAddition)
        SELECT now(), customerId, currentOrderPriceCode, customerHomeRegion, currentOrderPriceDate, currentOrderRowCount, message;
        SELECT LAST_INSERT_ID()
        INTO CurrentOrderId;
        INSERT INTO Orders.OrdersList(OrderId, ProductId, CodeFirmCr, SynonymCode, SynonymFirmCrCode, Code, CodeCr, Quantity, Cost, Junk, Await, CoreId)
        SELECT CurrentOrderId, c.ProductId, c.CodeFirmCr, c.SynonymCode, c.SynonymFirmCrCode, c.Code, c.CodeCr, s.Quantity, c.Cost, junk, Await, s.CoreId
        FROM Supplies s
          JOIN allcoret c ON s.CoreId = c.Id
            JOIN Orders.OrdersHead oh ON oh.PriceCode = c.PriceCode
        WHERE oh.RowId = CurrentOrderId;
        INSERT INTO Reports
        SELECT CurrentOrderId, ac.ProductId, ac.CodeFirmCr, ac.Cost, cd.ShortName
        FROM Supplies s
          JOIN allcoret ac ON s.CoreId = ac.Id
        		JOIN Orders.OrdersHead oh ON oh.PriceCode = ac.PriceCode
        			JOIN UserSettings.Pricesdata pd on pd.PriceCode = ac.PriceCode
				        JOIN usersettings.clientsdata cd on cd.FirmCode = pd.FirmCode
        WHERE oh.RowId = CurrentOrderId;
       SET Done = 0;
     END IF;
  UNTIL done END REPEAT;
  DROP TEMPORARY TABLE IF EXISTS TempOrders;
  CLOSE OrdersCursor;
END $$

DELIMITER ;


DELIMITER $$

create PROCEDURE usersettings.GetAllOffersForClient(IN ClientCodeParam INT UNSIGNED)
BEGIN
  call GetActivePrices(ClientCodeParam);
  drop temporary table IF EXISTS AllCoreT;
  create temporary table AllCoreT
  (
     ID bigint unsigned primary key,
     PriceCode int unsigned,
     ProductId int unsigned,
     CodeFirmCr int unsigned,
     SynonymCode int unsigned,
     SynonymFirmCrCode int unsigned,
     Code varchar(32) not null default '',
     CodeCr varchar(32) not null default '',
     Junk bit,
     Await bit,
     Quantity mediumint not null default 1000,
     RequestRatio SMALLINT unsigned not null default 0,
     MinCost decimal(8,2),
     Cost decimal(8,2) ,
     index MultiK(Cost, ProductId)
   )engine=MEMORY PACK_KEYS = 0;
INSERT
INTO    AllCoreT
SELECT
        c.ID,
        ap.PriceCode,
        c.ProductId,
        codefirmcr,
        synonymcode,
        SynonymFirmCrCode,
        code,
        codecr,
        junk,
        Await,
        if(quantity>0, quantity, 10000),
        RequestRatio,
        MinBoundCost,
        if(if(round(cc.Cost*ap.UpCost,2)<MinBoundCost, MinBoundCost, round(cc.Cost*ap.UpCost,2))>MaxBoundCost,MaxBoundCost, if(round(cc.Cost*ap.UpCost,2)<MinBoundCost, MinBoundCost, round(cc.Cost*ap.UpCost,2)))
FROM farm.core0 c
  JOIN ActivePrices ap on c.PriceCode = ap.PriceCode
    JOIN farm.CoreCosts cc on cc.Core_Id = c.Id and cc.PC_CostCode = ap.CostCode
WHERE   ap.pricecode != 2647;

END $$

DELIMITER ;



DELIMITER $$

CREATE PROCEDURE usersettings.PrgData1(IN ClientCodeParam INT UNSIGNED, IN FieldsTerminatedParam CHAR(1), IN LinesTerminatedParam CHAR(1), IN Cumulative BOOL)
BEGIN
DECLARE FirmSegmentParam bool;
DECLARE CostsPasswordParam char(16);
DECLARE FieldsTerminated, LinesTerminated char(1);
Declare SClientCode int unsigned;
Declare ClientRegionCode bigint unsigned;
drop temporary table IF EXISTS MaxCodesSynFirmCr, MinCosts, ActivePrices, Core, tmpprd, MaxCodesSyn, ParentCodes;
if length(FieldsTerminatedParam)<1 then
set FieldsTerminated=Char(159);
else
set FieldsTerminated=FieldsTerminatedParam;
end if;
if length(LinesTerminatedParam)<1 then
set LinesTerminated=Char(161);
else
set LinesTerminated=LinesTerminatedParam;
end if;
select FirmSegment into FirmSegmentParam from clientsdata where firmcode=ClientCodeParam;
select BaseCostPassword
        into CostsPasswordParam
        from retclientsset where clientcode=ClientCodeParam;
 SET @a:=concat(
'SELECT  P.Id, P.CatalogId ',
'FROM    ret_update_info r, ',
'        Catalogs.Products P ',
'WHERE   IF(', not Cumulative, ', (P.UpdateTime >= r.UpdateTime), 1) ',
'    AND hidden       = 0 ',
'    AND clientcode= ',ClientCodeParam ,
' INTO OUTFILE ', '"results/Products', ClientCodeParam, '.txt"',
"FIELDS TERMINATED BY '", FieldsTerminated,
"' OPTIONALLY ENCLOSED BY '' ESCAPED BY '' LINES TERMINATED BY '",
LinesTerminated, "'");
PREPARE QueryTXT FROM @a;
EXECUTE QueryTXT;
insert into ready_client_files values(null, ClientCodeParam, 'Products', 0);
 SET @a:=concat(
'SELECT  C.Id            , ',
'        CN.Id           , ',
'        LEFT(name, 250) , ',
'        LEFT(form, 250) , ',
'        vitallyimportant, ',
'        needcold        , ',
'        fragile ',
'FROM    Catalogs.Catalog C      , ',
'        Catalogs.CatalogForms CF, ',
'        Catalogs.CatalogNames CN, ',
'        ret_update_info r ',
'WHERE   C.NameId            =CN.Id ',
'    AND C.FormId            =CF.Id ',
'    AND hidden              =0 ',
'    AND r.ClientCode        =',ClientCodeParam ,
'    AND IF(', not Cumulative, ', (C.UpdateTime >= r.UpdateTime), 1) ',
' INTO OUTFILE ', '"results/Catalog', ClientCodeParam, '.txt"',
"FIELDS TERMINATED BY '", FieldsTerminated,
"' OPTIONALLY ENCLOSED BY '' ESCAPED BY '' LINES TERMINATED BY '",
LinesTerminated, "'");
PREPARE QueryTXT FROM @a;
EXECUTE QueryTXT;
insert into ready_client_files values(null, ClientCodeParam, 'Catalog', 0);
SET @a:=concat(
'SELECT  regions.regioncode, ',
'        region ',
'FROM    farm.regions, ',
'        clientsdata ',
'WHERE   firmcode                           = ',ClientCodeParam ,
'        and regions.regioncode & maskregion> 0 ',
'UNION ',
'        SELECT  regions.regioncode, ',
'        region ',
'FROM    farm.regions, ',
'        clientsdata ',
'WHERE   firmcode              = ',ClientCodeParam ,
'        and regions.regioncode= clientsdata.regioncode ',
'UNION ',
'SELECT  distinct regions.regioncode, ',
'        region ',
'FROM    farm.regions, ',
'        includeregulation, ',
'        clientsdata ',
'WHERE   includeclientcode     = ', ClientCodeParam ,
'        and firmcode          = primaryclientcode ',
'        and includetype        in (1, 2) ',
'        and regions.regioncode & clientsdata.maskregion>0 ',
'UNION ',
'        SELECT  regions.regioncode, ',
'        region ',
'FROM    farm.regions, ',
'        clientsdata, ',
'        includeregulation ',
'WHERE   primaryclientcode     = ',ClientCodeParam ,
'        and firmcode          = includeclientcode ',
'        and firmstatus        = 1 ',
'        and includetype        = 0 ',
'        and regions.regioncode= clientsdata.regioncode ',
' INTO OUTFILE ', '"results/Regions', ClientCodeParam, '.txt"',
"FIELDS TERMINATED BY '", FieldsTerminated,
"' OPTIONALLY ENCLOSED BY '' ESCAPED BY '' LINES TERMINATED BY '",
LinesTerminated, "'");
PREPARE QueryTXT FROM @a;
EXECUTE QueryTXT;
insert into ready_client_files values(null, ClientCodeParam, 'Regions', 0);
SET @a:=concat(
'SELECT  C.Id ',
'FROM    Catalogs.Catalog C, ',
'        ret_update_info r ',
'WHERE   C.UpdateTime > r.Updatetime ',
'    AND hidden       = 1 ',
'    AND NOT ', Cumulative,
'    AND clientcode= ',ClientCodeParam ,
' INTO OUTFILE ', '"results/CatDel', ClientCodeParam, '.txt"',
"FIELDS TERMINATED BY '", FieldsTerminated,
"' OPTIONALLY ENCLOSED BY '' ESCAPED BY '' LINES TERMINATED BY '",
LinesTerminated, "'");
PREPARE QueryTXT FROM @a;
EXECUTE QueryTXT;
insert into ready_client_files values(null, ClientCodeParam, 'CatDel', 0);
call GetPrices(ClientCodeParam);
SET @a:=concat(
'SELECT  firm.FirmCode, ',
'        firm.FullName, ',
'        firm.Fax, ',
'        "-", ',
'        "-", ',
'        "-", ',
'        "-", ',
'        "-", ',
'        "-", ',
'        "-", ',
'        "-", ',
'        "-", ',
'        "-", ',
'        "-", ',
'        "-" ',
'FROM    clientsdata as firm ',
'WHERE   firmcode in ',
'        (SELECT DISTINCT FirmCode ',
'        FROM    Prices ',
'        ) ',
' INTO OUTFILE ', '"results/ClientsDataN', ClientCodeParam, '.txt"',
"FIELDS TERMINATED BY '", FieldsTerminated,
"' OPTIONALLY ENCLOSED BY '' ESCAPED BY '' LINES TERMINATED BY '",
LinesTerminated, "'");
PREPARE QueryTXT FROM @a;
EXECUTE QueryTXT;
insert into ready_client_files values(null, ClientCodeParam, 'ClientsDataN', 0);
SET @a:=concat(
'SELECT  DISTINCT regionaldata.FirmCode, ',
'        regionaldata.RegionCode, ',
'        supportphone, ',
'        left(adminmail, 50), ',
'        ContactInfo, ',
'        OperativeInfo ',
'FROM    regionaldata, ',
'        Prices ',
'WHERE   regionaldata.firmcode      = Prices.firmcode ',
'        and regionaldata.regioncode= Prices.regioncode ',
' INTO OUTFILE ', '"results/RegionalData', ClientCodeParam, '.txt"',
"FIELDS TERMINATED BY '", FieldsTerminated,
"' OPTIONALLY ENCLOSED BY '' ESCAPED BY '' LINES TERMINATED BY '",
LinesTerminated, "'");
PREPARE QueryTXT FROM @a;
EXECUTE QueryTXT;
insert into ready_client_files values(null, ClientCodeParam, 'RegionalData', 0);
SET @a:=concat(
'SELECT  PriceCode, ',
'        RegionCode, ',
'        Storage, ',
'        PublicUpCost, ',
'        MinReq, ',
'        MainFirm, ',
'        not disabledbyclient, ',
'        CostCorrByClient, ',
'        ControlMinReq ',
'FROM    Prices ',
' INTO OUTFILE ', '"results/PricesRegionalData', ClientCodeParam, '.txt"',
"FIELDS TERMINATED BY '", FieldsTerminated,
"' OPTIONALLY ENCLOSED BY '' ESCAPED BY '' LINES TERMINATED BY '",
LinesTerminated, "'");
PREPARE QueryTXT FROM @a;
EXECUTE QueryTXT;
insert into ready_client_files values(null, ClientCodeParam, 'PricesRegionalData', 0);
create temporary table tmpprd
(
FirmCode int unsigned,
PriceCount int unsigned
)engine=MEMORY;
INSERT
INTO    tmpprd
SELECT  firmcode,
        count(pricecode)
FROM    Prices
GROUP BY FirmCode,
        RegionCode;
SET @a:=concat(
'SELECT  Prices.FirmCode, ',
'        Prices.pricecode, ',
'        concat(firm.shortname, if(PriceCount> 1 ',
'        or ShowPriceName                    = 1, concat(" (", pricename, ")"), "")), ',
'        if(AlowInt, 1, 0), ',
'        " ", ',
'        PriceDate, ',
'        (Fresh                            = 1 and AlowInt= 0) ',
'        or actual=0, ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "", ',
'        "0" ',
'FROM    clientsdata as firm, ',
'        tmpprd, ',
'        Prices ',
'WHERE   tmpprd.firmcode             = firm.firmcode ',
'        and firm.firmcode           = Prices.FirmCode ',
' GROUP BY Prices.FirmCode, ',
'        Prices.pricecode ',
' INTO OUTFILE ', '"results/PricesData', ClientCodeParam, '.txt"',
"FIELDS TERMINATED BY '", FieldsTerminated,
"' OPTIONALLY ENCLOSED BY '' ESCAPED BY '' LINES TERMINATED BY '",
LinesTerminated, "'");
PREPARE QueryTXT FROM @a;
EXECUTE QueryTXT;
insert into ready_client_files values(null, ClientCodeParam, 'PricesData', 0);
 if not Cumulative then
SET @a:=concat(
'SELECT  FirmCr, ',
'        CountryCr, ',
'        FullName, ',
'        Series, ',
'        LetterNo, ',
'        LetterDate, ',
'        LaboratoryName, ',
'        CauseRejects ',
'FROM    addition.rejects, ',
'        ret_update_info rui,  ',
'        retclientsset rcs  ',
'WHERE   accessTime      >= rui.RejectTime ',
'        and alowrejection= 1 ',
'        and rui.clientcode   = ',ClientCodeParam ,
'        and rcs.clientcode   = ',ClientCodeParam ,
' INTO OUTFILE ', '"results/Rejects', ClientCodeParam, '.txt"',
"FIELDS TERMINATED BY '", FieldsTerminated,
"' OPTIONALLY ENCLOSED BY '' ESCAPED BY '' LINES TERMINATED BY '",
LinesTerminated, "'");
PREPARE QueryTXT FROM @a;
EXECUTE QueryTXT;
insert into ready_client_files values(null, ClientCodeParam, 'Rejects', 0);
end if;
SET @a:=concat(
'SELECT  clientsdata.firmcode, ',
'        ShortName, ',
'        RegionCode, ',
'        OverCostPercent, ',
'        DifferenceCalculation, ',
'        MultiUserLevel, ',
'        OrderRegionMask, ',
'        "", ',
'        CalculateLeader ',
'FROM    retclientsset, ',
'        clientsdata ',
'WHERE   clientsdata.firmcode        = ',ClientCodeParam ,
'        and retclientsset.clientcode= clientsdata.firmcode ',
'UNION ',
'SELECT  clientsdata.firmcode, ',
'        ShortName, ',
'        RegionCode, ',
'        OverCostPercent, ',
'        DifferenceCalculation, ',
'        MultiUserLevel, ',
'        OrderRegionMask, ',
'        "", ',
'        CalculateLeader ',
'FROM    retclientsset, ',
'        clientsdata, ',
'        IncludeRegulation ',
'WHERE   clientsdata.firmcode        = IncludeClientCode ',
'        and retclientsset.clientcode= clientsdata.firmcode ',
'        and firmstatus              = 1 ',
'        and IncludeType             = 0 ',
'        and Primaryclientcode       = ',ClientCodeParam ,
' INTO OUTFILE ', '"results/Clients', ClientCodeParam, '.txt"',
"FIELDS TERMINATED BY '", FieldsTerminated,
"' OPTIONALLY ENCLOSED BY '' ESCAPED BY '' LINES TERMINATED BY '",
LinesTerminated, "'");
PREPARE QueryTXT FROM @a;
EXECUTE QueryTXT;
insert into ready_client_files values(null, ClientCodeParam, 'Clients', 0);
call GetActivePrices(ClientCodeParam);
SET @a:=concat('SELECT  o.ClientCode, ',
'        ol.ProductId, ',
'        round(avg(ol.Cost), 2) ',
'FROM    orders.ordershead o, ',
'        orders.orderslist ol, ',
'        ret_update_info rcs ',
'WHERE   o.ClientCode    = ', ClientCodeParam,
'        and o.WriteTime > if(', Cumulative, ', curdate() - interval 1 month, rcs.UpdateTime) ',
'        and ol.OrderID  = o.RowID ',
'        and rcs.clientcode = o.ClientCode ',
'GROUP BY o.ClientCode, ',
'        ol.ProductId ',
'UNION ALL ',
'SELECT  cd.FirmCode, ',
'        ol.ProductId, ',
'        round(avg(ol.Cost), 2) ',
'FROM    usersettings.IncludeRegulation ir, ',
'        usersettings.clientsdata cd, ',
'        orders.ordershead o, ',
'        orders.orderslist ol, ',
'        ret_update_info rcs ',
'WHERE   ir.Primaryclientcode = ', ClientCodeParam,
'        and cd.firmstatus    = 1 ',
'        and includetype      = 0 ',
'        and o.WriteTime > if(', Cumulative, ', curdate() - interval 1 month, rcs.UpdateTime) ',
'        and cd.FirmCode      = ir.IncludeClientCode ',
'        and o.ClientCode     = cd.FirmCode ',
'        and rcs.clientcode   = o.ClientCode ',
'        and ol.OrderID       = o.RowID ',
'GROUP BY cd.FirmCode, ',
'        ol.ProductId ',
' INTO OUTFILE ', '"results/PriceAvg', ClientCodeParam, '.txt"',
                "FIELDS TERMINATED BY '", FieldsTerminated,
                "' OPTIONALLY ENCLOSED BY '' ESCAPED BY '' LINES TERMINATED BY '",
                LinesTerminated, "'");
PREPARE QueryTXT FROM @a;
EXECUTE QueryTXT;
insert into ready_client_files values(null, ClientCodeParam, 'PriceAvg', 0);
create temporary table  MaxCodesSyn
(
PriceCode int unsigned primary key,
Synonym int unsigned
)engine=MEMORY;
create temporary table  MaxCodesSynFirmCr
(
PriceCode int unsigned primary key,
Synonym int unsigned
)engine=MEMORY;
INSERT
INTO    MaxCodesSyn
SELECT  Prices.pricecode,
        max(synonym.synonymcode)
        FROM  ActivePrices Prices,
        farm.synonym
WHERE   synonym.pricecode= PriceSynonymCode
        and synonym.synonymcode> MaxSynonymCode
GROUP BY 1;
INSERT
INTO    MaxCodesSynFirmCr
SELECT  Prices.pricecode,
        max(synonymfirmcr.synonymfirmcrcode)
FROM    ActivePrices Prices,
        farm.synonymfirmcr
WHERE   synonymfirmcr.pricecode      = PriceSynonymCode
        and synonymfirmcr.synonymfirmcrcode> MaxSynonymFirmCrCode
GROUP BY 1;
create temporary table  ParentCodes
(
PriceCode int unsigned ,
MaxSynonymCode int unsigned,
MaxSynonymFirmCrCode int unsigned,
index using btree (PriceCode, MaxSynonymCode),
index using btree (PriceCode, MaxSynonymFirmCrCode)
)engine=MEMORY;
INSERT
INTO    ParentCodes
SELECT  PriceSynonymCode,
        max(if(cumulative, 0, MaxSynonymCode)),
        max(if(cumulative, 0, MaxSynonymFirmCrCode))
FROM    ActivePrices Prices
WHERE   if(cumulative, 1, fresh)
GROUP BY 1;
SET @a:=concat(
'SELECT synonymfirmcr.synonymfirmcrcode, ',
'        left(Synonym, 250) ');
SET @a:=concat(@a,
'FROM    farm.synonymfirmcr,',
'        ParentCodes ',
'WHERE   synonymfirmcr.pricecode             = ParentCodes.PriceCode ',
'        and synonymfirmcr.synonymfirmcrcode>MaxSynonymFirmCrCode ');
if cumulative then
SET @a:=concat(@a,
'        union ',
'SELECT  synonymfirmcrcode, ',
'        left(synonym, 250)  ');
SET @a:=concat(@a,
'FROM    farm.synonymfirmcr ',
'WHERE   synonymfirmcrcode=0 '
);
end if;
  SET @a:=concat(@a, ' INTO OUTFILE ', '"results/SynonymFirmCr', ClientCodeParam, '.txt"',
"FIELDS TERMINATED BY '", FieldsTerminated,
"' OPTIONALLY ENCLOSED BY '' ESCAPED BY '' LINES TERMINATED BY '",
LinesTerminated, "'");
PREPARE QueryTXT FROM @a;
EXECUTE QueryTXT;
insert into ready_client_files values(null, ClientCodeParam, 'SynonymFirmCr', 0);
SET @a:=concat(
'SELECT  synonym.synonymcode, ',
'        left(synonym.synonym, 250)  ');
SET @a:=concat(@a,
'FROM    farm.synonym, ',
'        ParentCodes ',
'WHERE   synonym.pricecode       = ParentCodes.PriceCode ',
'        and synonym.synonymcode> MaxSynonymCode ',
' INTO OUTFILE ', '"results/Synonym', ClientCodeParam, '.txt"',
"FIELDS TERMINATED BY '", FieldsTerminated,
"' OPTIONALLY ENCLOSED BY '' ESCAPED BY '' LINES TERMINATED BY '",
LinesTerminated, "'");
PREPARE QueryTXT FROM @a;
EXECUTE QueryTXT;
insert into ready_client_files values(null, ClientCodeParam, 'Synonym', 0);
SET @a:=concat(
'select currency, exchange ',
'from farm.catalogcurrency  where currency="$" or currency="Eu" ',
' INTO OUTFILE ', '"results/CatalogCurrency', ClientCodeParam, '.txt"',
"FIELDS TERMINATED BY '", FieldsTerminated,
"' OPTIONALLY ENCLOSED BY '' ESCAPED BY '' LINES TERMINATED BY '",
LinesTerminated, "'");
PREPARE QueryTXT FROM @a;
EXECUTE QueryTXT;
insert into ready_client_files values(null, ClientCodeParam, 'CatalogCurrency', 0);
if Cumulative then
UPDATE ret_update_info
        SET
        ReclameTime      =default,
        RejectTime       =default,
        CalculateReject  =0,
        RegistryTime     =default,
        CalculateRegistry=0
WHERE   clientcode       =ClientCodeParam;
else
UPDATE ret_update_info SET CalculateReject=1 WHERE clientcode=ClientCodeParam;
end if;
UPDATE intersection_update_info iui,
        MaxCodesSynFirmCr,
        ActivePrices Prices
        SET UncMaxSynonymFirmCrCode    = MaxCodesSynFirmCr.synonym,
        CalculateSynonymFirmCr         = 1
WHERE   Prices.pricecode        = iui.pricecode
        and MaxCodesSynFirmCr.pricecode= Prices.pricecode
        and iui.clientcode    = ClientCodeParam;
UPDATE intersection_update_info iui,
        maxcodessyn,
        ActivePrices Prices
        SET UncMaxSynonymCode      = maxcodessyn.synonym,
        CalculateSynonym           = 1
WHERE   Prices.pricecode    = iui.pricecode
        and maxcodessyn.pricecode  = Prices.pricecode
        and iui.clientcode= ClientCodeParam;
UPDATE intersection_update_info iui,
        ActivePrices Prices
        SET CalculateDate           = 1
WHERE   Fresh
        and Prices.pricecode = iui.pricecode
        and Prices.RegionCode= iui.RegionCode
        and iui.clientcode = ClientCodeParam;
SELECT  s.OffersClientCode
INTO    SClientCode
FROM    retclientsset r,
        OrderSendRules.smart_order_rules s
WHERE   r.clientcode        =ClientCodeParam
    AND s.id                =r.smartorderruleid
    AND s.offersclientcode !=r.clientcode;
if SClientCode is null then
if (select sum(fresh)>0  from ActivePrices) or Cumulative  then
call GetOffers(ClientCodeParam, not Cumulative);
UPDATE  ActivePrices Prices,
        Core
        SET CryptCost          = replace(replace(replace(replace(replace(AES_ENCRYPT(Cost, CostsPasswordParam), char(37), '%25'), char(32), '%20'),
         char(159), '%9F'), char(161), '%A1'), char(0), '%00')
WHERE   Prices.PriceCode= Core.PriceCode
        and if(Cumulative, 1, Fresh)
        and Core.PriceCode!=2647
        ;
UPDATE Core
        SET CryptCost=concat(left(CryptCost, 1), char(round((rand()*110)+32,0)), SUBSTRING(CryptCost,2,length(CryptCost)-4), char(round((rand()*110)+32,0)), right(CryptCost, 3))
WHERE  length(CryptCost)>0
       and Core.PriceCode!=2647;
 
SET @a:=concat('SELECT MinCosts.ID, MinCosts.ProductId, MinCosts.RegionCode, if(PriceCode=2647, "", (99999900 ^ TRUNCATE((MinCost*100), 0))) FROM MinCosts',
               ' INTO OUTFILE ', '"results/MinPrices', ClientCodeParam, '.txt"',
                "FIELDS TERMINATED BY '", FieldsTerminated,
                "' OPTIONALLY ENCLOSED BY '' ESCAPED BY '' LINES TERMINATED BY '",
                LinesTerminated, "'");
PREPARE QueryTXT FROM @a;
EXECUTE QueryTXT;
insert into ready_client_files values(null, ClientCodeParam, 'MinPrices', 0);
SET @a:=concat('SELECT CT.PriceCode, CT.regioncode, CT.ProductId, Core.codefirmcr, Core.synonymcode, ',
                'Core.SynonymFirmCrCode, Core.Code, ',
                'Core.CodeCr, Core.unit, Core.volume , Core.Junk, Core.Await, Core.quantity, Core.note, Core.period, Core.doc,',
                'ifnull(Core.RegistryCost, ""), Core.VitallyImportant, ifnull(Core.RequestRatio, ""), ',
                'CT.CryptCost, ',
                'CT.ID, ifnull(OrderCost, ""), ifnull(MinOrderCount, "") from Core CT, ActivePrices AT, farm.core0 Core ',
                'where ct.pricecode=at.pricecode and ct.regioncode=at.regioncode ',
                'and Core.id=CT.id ',
                'and if(', Cumulative, ', 1, fresh) ',
                ' INTO OUTFILE ', '"results/core', ClientCodeParam, '.txt"',
                "FIELDS TERMINATED BY '", FieldsTerminated,
                "' OPTIONALLY ENCLOSED BY '' ESCAPED BY '' LINES TERMINATED BY '",
                LinesTerminated, "'");
PREPARE QueryTXT FROM @a;
EXECUTE QueryTXT;
insert into ready_client_files values(null, ClientCodeParam, 'core', 0);
drop temporary table if exists Core, MinCosts;
else
SET @a:=concat('SELECT ""',
               ' INTO OUTFILE ', '"results/MinPrices', ClientCodeParam, '.txt"',
                "FIELDS TERMINATED BY '", FieldsTerminated,
                "' OPTIONALLY ENCLOSED BY '' ESCAPED BY '' LINES TERMINATED BY ''");
PREPARE QueryTXT FROM @a;
EXECUTE QueryTXT;
insert into ready_client_files values(null, ClientCodeParam, 'MinPrices', 0);
SET @a:=concat('SELECT "" ',
                ' INTO OUTFILE ', '"results/core', ClientCodeParam, '.txt"',
                "FIELDS TERMINATED BY '", FieldsTerminated,
                "' OPTIONALLY ENCLOSED BY '' ESCAPED BY '' LINES TERMINATED BY ''");
PREPARE QueryTXT FROM @a;
EXECUTE QueryTXT;
insert into ready_client_files values(null, ClientCodeParam, 'core', 0);
end if;
else
SELECT  RegionCode
INTO    ClientRegionCode
FROM    ClientsData Cd
WHERE   firmcode=ClientCodeParam;
CALL GetActivePrices(SClientCode);
DROP TEMPORARY TABLE IF EXISTS CoreT,
        CoreTP;
CREATE TEMPORARY TABLE CoreT(ProductId  INT unsigned, CodeFirmCr INT unsigned, UNIQUE MultiK(ProductId, CodeFirmCr))engine=MEMORY;
CREATE TEMPORARY TABLE CoreTP(ProductId INT unsigned, UNIQUE MultiK(ProductId))engine                                     =MEMORY;
INSERT
INTO    CoreT
SELECT DISTINCT core0.ProductId,
        core0.codefirmcr
FROM    farm.core0,
        activeprices
WHERE   core0.pricecode=if(CostType=1, activeprices.CostCode, activeprices.pricecode);
INSERT
INTO    CoreTP
SELECT DISTINCT ProductId
FROM    CoreT;
SET @a:=concat(
'SELECT  2647                , ',
         ClientRegionCode,'  , ',
'        A.ProductId         , ',
'        A.CodeFirmCr        , ',
'        S.SynonymCode       , ',
'        SF.SynonymFirmCrCode, ',
'        ""                  , ',
'        ""                  , ',
'        ""                  , ',
'        ""                  , ',
'        0                   , ',
'        0                   , ',
'        ""                  , ',
'        ""                  , ',
'        ""                  , ',
'        ""                  , ',
'        ""                  , ',
'        0                   , ',
'        ""                  , ',
'        ""                  , ',
'        A.Id                , ',
'        ""                  , ',
'        "" ',
'FROM    Catalogs.Assortment A, ',
'        farm.Synonym S       , ',
'        farm.SynonymFirmCr SF, ',
'        CoreT ',
'WHERE   S.PriceCode     =2647 ',
'    AND SF.PriceCode    =2647 ',
'    AND S.ProductId     =A.ProductId ',
'    AND SF.CodeFirmCr   =A.CodeFirmCr ',
'    AND CoreT.ProductId =A.ProductId ',
'    AND CoreT.CodeFirmCr=A.CodeFirmCr ',
'    AND A.CodeFirmCr!=1 ',
'UNION ',
' ',
'SELECT  2647         , ',
    ClientRegionCode,', ',
'        A.ProductId  , ',
'        A.CodeFirmCr , ',
'        S.SynonymCode, ',
'        0            , ',
'        ""           , ',
'        ""           , ',
'        ""           , ',
'        ""           , ',
'        0            , ',
'        0            , ',
'        ""           , ',
'        ""           , ',
'        ""           , ',
'        ""           , ',
'        ""           , ',
'        0            , ',
'        ""           , ',
'        ""           , ',
'        A.Id         , ',
'        ""           , ',
'        "" ',
'FROM    Catalogs.Assortment A, ',
'        farm.Synonym S       , ',
'        CoreTP ',
'WHERE   S.PriceCode     =2647 ',
'    AND S.ProductId     =A.ProductId ',
'    AND CoreTP.ProductId=A.ProductId ',
'    AND A.CodeFirmCr    =1 ',
' INTO OUTFILE ', '"results/core', ClientCodeParam, '.txt"',
                 "FIELDS TERMINATED BY '", FieldsTerminated,
                "' OPTIONALLY ENCLOSED BY '' ESCAPED BY '' LINES TERMINATED BY '",
                LinesTerminated, "'");
PREPARE QueryTXT FROM @a;
EXECUTE QueryTXT;
SET @a:=concat(
'SELECT  0        , ',
'        ProductId, ',
         ClientRegionCode,', ',
'        "" ',
'FROM    CoreTP ',
               ' INTO OUTFILE ', '"results/MinPrices', ClientCodeParam, '.txt"',
                "FIELDS TERMINATED BY '", FieldsTerminated,
                "' OPTIONALLY ENCLOSED BY '' ESCAPED BY '' LINES TERMINATED BY '",
                LinesTerminated, "'");
PREPARE QueryTXT FROM @a;
EXECUTE QueryTXT;
insert into ready_client_files values(null, ClientCodeParam, 'MinPrices', 0);
insert into ready_client_files values(null, ClientCodeParam, 'core', 0);
end if;
drop temporary table if exists Core, ParentCodes, MaxCodesSynFirmCr, MinCosts, Prices, ActivePrices, tmpprd, MaxCodesSyn, CoreT, CoreTP;
END $$

DELIMITER ;


DROP PROCEDURE IF EXISTS usersettings.GetShowStat;


DELIMITER $$

CREATE PROCEDURE usersettings.GetShowStat(IN StartDateParam DATE, IN EndDateParam DATE, IN RegionMaskParam BIGINT UNSIGNED, IN UserNameParam VARCHAR(25))
BEGIN
Declare InProc,  NonProcOrdersCount mediumint unsigned;
Declare DataSize, DocSize int unsigned;
Declare MaxUpdateTime, MaxOrderTime, LastForm, LastDown varchar(20);
Declare FormCount, DownCount varchar(50);
Declare OrderSum decimal(12, 2);
Declare  OrdersCount, ReGet, UpdatesAD, UpdatesErr, OrdersErr, OrdersAD, CumulativeUpdates, Updates  varchar(10);

#SELECT  max(UpdateTime)
#INTO    MaxUpdateTime
#FROM    (usersettings.ret_update_info, usersettings.clientsdata, accessright.regionaladmins showright)
#WHERE   clientsdata.firmcode                  = ret_update_info.clientcode
#        AND RegionCode & showright.regionmask > 0
#        AND RegionCode & RegionMaskParam      > 0
#        AND UncommittedUpdateTime BETWEEN StartDateParam AND EndDateParam
#        AND username = UserNameParam;

SELECT  concat(count(DISTINCT oh.rowid), '(', count(DISTINCT oh.clientcode), ')'),
        sum(cost*quantity),
        count(DISTINCT if(processed = 0
        AND if(SubmitOrders         = 1, Submited
        AND not Deleted, 1), orderid, null)),
        Max(WriteTime)
INTO    OrdersCount,
        OrderSum, 
        NonProcOrdersCount, 
        MaxOrderTime
FROM    orders.ordershead oh,
        orders.orderslist, 
        usersettings.clientsdata cd, 
        accessright.regionaladmins showright,
        usersettings.retclientsset rcs
WHERE   oh.rowid                              = orderid
        AND cd.firmcode                       = oh.clientcode
        AND cd.billingcode                    <> 921
        AND rcs.clientcode                    = oh.clientcode
        AND firmsegment                       = 0
        AND serviceclient                     = 0 
        AND showright.regionmask & maskregion > 0
        AND oh.regioncode & RegionMaskParam   > 0
        AND not Deleted
        AND showright.username = UserNameParam
        AND WriteTime BETWEEN StartDateParam AND EndDateParam;

SELECT  sum(if(UpdateType in (1,2), resultsize, 0)),
        sum(if(UpdateType in (8), resultsize, 0)),
        sum(if(updatetype in (1,2), Commit=0, null)),
        concat(Sum(UpdateType                  IN (5)) ,'(' ,count(DISTINCT if(UpdateType  IN (5) ,p.ClientCode ,null)) ,')') UpdatesAD ,
        concat(sum(UpdateType                      = 2) ,'(' ,count(DISTINCT if(UpdateType = 2 ,p.clientcode ,null)) ,')') CumulativeUpdates              ,
        concat(sum(UpdateType                      = 1) ,'(' ,count(DISTINCT if(UpdateType = 1 ,p.clientcode ,null)) ,')') Updates
INTO    DataSize,
        DocSize,
        InProc,
        UpdatesAD,
        CumulativeUpdates,
        Updates
FROM    usersettings.clientsdata ,
        accessright.regionaladmins showright    ,
        logs.AnalitFUpdates p
WHERE   firmcode                                   = clientcode
        AND showright.regionmask & maskregion      > 0
        AND maskregion  & RegionMaskParam          > 0
        AND showright.username                     = UserNameParam
         AND RequestTime BETWEEN StartDateParam AND EndDateParam;

#SELECT  concat(count(if(resultid=2, pricecode, null)), '(', count(DISTINCT if(resultid=2, pricecode, null)), ')'),
#        max(if(resultid=2, logtime, null))
#into FormCount, LastForm
#FROM    logs.formlogs
#WHERE logtime BETWEEN StartDateParam AND EndDateParam;

#SELECT  concat(count(if(resultcode=2, pricecode, null)), '(', count(DISTINCT if(resultcode=2, pricecode, null)), ')'),
#max(if(resultcode=2, logtime, null))
#into DownCount, LastDown
#FROM    logs.downlogs
#WHERE logtime BETWEEN StartDateParam AND EndDateParam;

select
ifnull(OrdersCount, '') OrdersCount,
ifnull(OrderSum, 0) OrderSum,
ifnull(NonProcOrdersCount, 0) NonProcOrdersCount,
ifnull(MaxOrderTime, '0:0:0') MaxOrderTime,
ifnull(InProc, 0) InProc,
ifnull(MaxUpdateTime, '0:0:0') MaxUpdateTime,
ifnull(ReGet, '') ReGet,
ifnull(UpdatesAD, '') UpdatesAD,
ifnull(UpdatesErr, '') UpdatesErr,
ifnull(OrdersErr, '') OrdersErr,
ifnull(OrdersAD, '') OrdersAD,
ifnull(CumulativeUpdates, '') CumulativeUpdates,
ifnull(Updates, '') Updates,
0 NoForm,
0 NoDown,
ifnull(LastForm, '2000-01-01') LastForm,
ifnull(LastDown, '2000-01-01') LastDown,
0 QueueForm,
ifnull(DownCount, '') DownCount,
0 NoPriceCount,
ifnull(FormCount, '') FormCount,
ifnull(DataSize, 0) DataSize,
ifnull(DocSize, 0) DocSize;
END $$

DELIMITER ;
