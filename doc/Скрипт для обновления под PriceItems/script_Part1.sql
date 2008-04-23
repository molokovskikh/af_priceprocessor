set names cp1251;

-- ќбновл€ем таблицу Core0

drop trigger if exists farm.Core0BeforeInsert;

alter table farm.Core0
  add column `JunkNew` tinyint(1) unsigned NOT NULL DEFAULT '0' after `Doc`,
  add column `AwaitNew` tinyint(1) unsigned NOT NULL DEFAULT '0' after `JunkNew`,
  drop column `BaseCost`;

update
  farm.Core0
set
  JunkNew =  if(length(Junk) > 0, 1, 0),
  AwaitNew = if(length(Await) > 0, 1, 0);

alter table farm.Core0
  drop key `Await_IDX`,
  drop key `Junk_IDX`,
  drop column `Junk`,
  drop column `Await`;

alter table farm.Core0
  change column `JunkNew` `Junk` tinyint(1) unsigned NOT NULL DEFAULT '0',
  change column `AwaitNew` `Await` tinyint(1) unsigned NOT NULL DEFAULT '0',
  add key `Junk_IDX` (`Junk`) USING BTREE,
  add key `Await_IDX` (`Await`) USING BTREE;

drop table farm.distinctsynonymtmp;

drop table farm.distinctsynonymfirmcrtmp;


-- удал€ем триггеры дл€ работы с price_update_info

drop trigger if exists Catalogs.AssortmentAfterInsert;

drop trigger if exists farm.AssortmentAfterInsert;


-- добавл€ем к Synonym, SynonymFirmCr пол€ LastUsed
alter table farm.Synonym
  add column `LastUsed` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP;

alter table farm.SynonymFirmCr
  add column `LastUsed` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP;


-- корректируем таблицу usersettings.intersection
drop trigger if exists usersettings.IntersectionLogDelete;

drop trigger if exists usersettings.IntersectionLogInsert;

drop trigger if exists usersettings.IntersectionLogUpdate;

alter table usersettings.Intersection
  drop column `CostCorrOwner`,
  drop column `CostCorrByFirm`;

alter table logs.intersection_logs
  drop column `MaxSynonymFirmCrCode`,
  drop column `UncMaxSynonymFirmCrCode`,
  drop column `CalculateSynonymFirmCr`,
  drop column `MaxSynonymCode`,
  drop column `UncMaxSynonymCode`,
  drop column `CalculateSynonym`,
  drop column `ParentSynonymPriceCode`,
  drop column `UncommittedLastSent`,
  drop column `LastSent`,
  drop column `CalculateDate`,
  drop column `Calculate`,
  drop column `CostCorrOwner`,
  drop column `CostCorrByFirm`;


DELIMITER ;;

CREATE TRIGGER UserSettings.IntersectionLogInsert AFTER Insert ON UserSettings.Intersection FOR EACH ROW
BEGIN
        INSERT 
        INTO    `logs`.intersection_logs 
                SET LogTime = now()
                ,
                OperatorName = IFNULL(
                        @INUser, 
                        SUBSTRING_INDEX(USER(),'@',1)
                ) 
                ,
                OperatorHost = IFNULL(
                        @INHost, 
                        SUBSTRING_INDEX(USER(),'@',-1)
                ) 
                , 
                Operation               = 0, 
                IntersectionID          = NEW.Id ,
                ClientCode              = NEW.ClientCode ,
                RegionCode              = NEW.RegionCode ,
                DisabledByAgency        = NEW.DisabledByAgency ,
                AlowDisabledByClient    = NEW.AlowDisabledByClient ,
                DisabledByClient        = NEW.DisabledByClient ,
                InvisibleOnFirm         = NEW.InvisibleOnFirm ,
                InvisibleOnClient       = NEW.InvisibleOnClient ,
                PriceCode               = NEW.PriceCode ,
                CostCode                = NEW.CostCode ,
                MinReq                  = NEW.MinReq ,
                PublicCostCorr          = NEW.PublicCostCorr ,
                FirmCostCorr            = NEW.FirmCostCorr ,
                CostCorrByClient        = NEW.CostCorrByClient ;
END ;;

DELIMITER ;


DELIMITER ;;

CREATE TRIGGER UserSettings.IntersectionLogUpdate AFTER Update ON UserSettings.Intersection FOR EACH ROW
BEGIN

if (NEW.ClientCode <> OLD.ClientCode) or
(NEW.RegionCode <> OLD.RegionCode) or
(NEW.DisabledByAgency <> OLD.DisabledByAgency) or
(NEW.AlowDisabledByClient <> OLD.AlowDisabledByClient) or
(NEW.DisabledByClient <> OLD.DisabledByClient) or
(NEW.InvisibleOnFirm <> OLD.InvisibleOnFirm) or
(NEW.InvisibleOnClient <> OLD.InvisibleOnClient) or
(NEW.PriceCode <> OLD.PriceCode) or
(NEW.CostCode <> OLD.CostCode) or
(NEW.FirmClientCode <> OLD.FirmClientCode) or
(NEW.FirmClientCode2 <> OLD.FirmClientCode2) or
(NEW.FirmClientCode3 <> OLD.FirmClientCode3) or
(NEW.MinReq <> OLD.MinReq) or
(NEW.ControlMinReq <> OLD.ControlMinReq) or
(NEW.FirmCategory <> OLD.FirmCategory) or
(NEW.PublicCostCorr <> OLD.PublicCostCorr) or
(NEW.FirmCostCorr <> OLD.FirmCostCorr) or
(NEW.CostCorrByClient <> OLD.CostCorrByClient)
then
INSERT
        INTO    `logs`.intersection_logs
                SET LogTime = now()
                ,
                OperatorName = IFNULL(
                        @INUser,
                        SUBSTRING_INDEX(USER(),'@',1)
                ) 
                , 
                OperatorHost = IFNULL(
                        @INHost,
                        SUBSTRING_INDEX(USER(),'@',-1)
                )
                , 
                Operation      = 1, 
                IntersectionID = OLD.Id ,
                ClientCode     = IFNULL(
                         NEW.ClientCode,
                        OLD.ClientCode
                ) 
                ,
                RegionCode = IFNULL(
                        NEW.RegionCode,
                        OLD.RegionCode
                ) 
                ,
                DisabledByAgency = NULLIF(
                        NEW.DisabledByAgency,
                        OLD.DisabledByAgency
                ) 
                ,
                AlowDisabledByClient = NULLIF(
                        NEW.AlowDisabledByClient,
                        OLD.AlowDisabledByClient
                ) 
                ,
                DisabledByClient = NULLIF(
                        NEW.DisabledByClient,
                        OLD.DisabledByClient
                )
                ,
                InvisibleOnFirm = NULLIF(
                        NEW.InvisibleOnFirm,
                        OLD.InvisibleOnFirm
                )
                ,
                InvisibleOnClient = NULLIF(
                        NEW.InvisibleOnClient,
                        OLD.InvisibleOnClient
                )
                ,
                PriceCode = IFNULL(
                        NEW.PriceCode,
                        OLD.PriceCode
                )
                ,
                CostCode = NULLIF(
                        NEW.CostCode,
                        OLD.CostCode
                )
                ,
                FirmClientCode = NULLIF(
                        NEW.FirmClientCode,
                        OLD.FirmClientCode
                )
                ,
                FirmClientCode2 = NULLIF(
                        NEW.FirmClientCode2,
                        OLD.FirmClientCode2
                )
                ,
                FirmClientCode3 = NULLIF(
                        NEW.FirmClientCode3,
                        OLD.FirmClientCode3
                )
                ,
                MinReq = NULLIF(
                        NEW.MinReq,
                        OLD.MinReq
                )
                ,
                ControlMinReq = NULLIF(
                        NEW.ControlMinReq,
                        OLD.ControlMinReq
                )
                ,
                FirmCategory = NULLIF(
                        NEW.FirmCategory,
                        OLD.FirmCategory
                )
                ,
                PublicCostCorr = NULLIF(
                        NEW.PublicCostCorr,
                        OLD.PublicCostCorr
                )
                ,
                FirmCostCorr = NULLIF(
                        NEW.FirmCostCorr,
                        OLD.FirmCostCorr
                )
                ,
                CostCorrByClient = NULLIF(
                        NEW.CostCorrByClient,
                        OLD.CostCorrByClient
                );
 end if;
 if (NEW.CostCode <> OLD.CostCode) or
  (NEW.PublicCostCorr <> OLD.PublicCostCorr) or
  (NEW.FirmCostCorr <> OLD.FirmCostCorr) or
  (NEW.DisabledByClient <> OLD.DisabledByClient) or
  (NEW.DisabledByAgency <> OLD.DisabledByAgency) or
  (NEW.InvisibleOnClient <> OLD.InvisibleOnClient)
  then
UPDATE intersection_update_info
        SET lastsent  ='2003-01-01 00:00:00',
        UncommittedLastSent='2003-01-01 00:00:00'
WHERE   pricecode     =OLD.PriceCode 
        AND clientcode=OLD.ClientCode 
        AND regioncode=OLD.RegionCode;
        end if;
END ;;

DELIMITER ;

DELIMITER ;;

CREATE TRIGGER UserSettings.IntersectionLogDelete AFTER Delete ON UserSettings.Intersection FOR EACH ROW
BEGIN

INSERT INTO `logs`.intersection_logs
SET LogTime = now() ,
OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)) ,
OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)) ,
Operation = 2,
IntersectionID = OLD.Id
,ClientCode = OLD.ClientCode
,RegionCode = OLD.RegionCode
,DisabledByAgency = OLD.DisabledByAgency
,DisabledByFirm = OLD.DisabledByFirm
,AlowDisabledByClient = OLD.AlowDisabledByClient
,DisabledByClient = OLD.DisabledByClient
,InvisibleOnFirm = OLD.InvisibleOnFirm
,InvisibleOnClient = OLD.InvisibleOnClient
,PriceCode = OLD.PriceCode
,CostCode = OLD.CostCode
,FirmClientCode = OLD.FirmClientCode
,FirmClientCode2 = OLD.FirmClientCode2
,FirmClientCode3 = OLD.FirmClientCode3
,MinReq = OLD.MinReq
,ControlMinReq = OLD.ControlMinReq
,FirmCategory = OLD.FirmCategory
,PublicCostCorr = OLD.PublicCostCorr
,FirmCostCorr = OLD.FirmCostCorr
,CostCorrByClient = OLD.CostCorrByClient;
END ;;

DELIMITER ;


-- обновл€ем таблицу CostFormRules
-- потом не забыть восстановить логирование CostFormRules
drop trigger if exists farm.CostFormRulesLogDelete;

drop trigger if exists farm.CostFormRulesLogInsert;

drop trigger if exists farm.CostFormRulesLogUpdate;

alter table farm.CostFormRules
  drop primary key,
  drop key `PC_CostCode_IDX`,
  drop foreign key `CostFormRules_ibfk_1`,
  drop column `FR_Id`,
  change column `PC_CostCode` `CostCode` int(11) unsigned NOT NULL;

delete from farm.CostFormRules
where
  not exists(select * from usersettings.pricescosts pc where pc.CostCode = CostFormRules.CostCode);

alter table farm.CostFormRules
  add primary key(`CostCode`),
  add constraint `CostCode_FK` foreign key (`CostCode`) REFERENCES `usersettings`.`pricescosts` (`CostCode`) ON DELETE cascade ON UPDATE CASCADE;


-- обновл€ем таблицу PricesRetrans
-- потом не забыть восстановить логирование PricesRetrans
drop trigger if exists logs.PricesRetransAfterInsert;

alter table logs.PricesRetrans
  add column `PriceItemId` int(11) unsigned DEFAULT NULL after `OperatorHost`;

-- обновл€ем таблицу PricesCosts
-- потом не забыть восстановить логирование PricesCosts
drop trigger if exists usersettings.pricescostsLogDelete;

drop trigger if exists usersettings.pricescostsLogInsert;

drop trigger if exists usersettings.pricescostsLogUpdate;

alter table usersettings.PricesCosts
  add column `PriceItemId` int(11) unsigned DEFAULT NULL after `PriceCode`;

alter table logs.prices_costs_logs
  add column `PriceItemId` int(11) unsigned DEFAULT NULL after `PriceCode`,
  drop column `ShowPriceCode`;


-- обновл€ем таблицу PricesData
-- потом не забыть восстановить логирование PricesData
drop trigger if exists usersettings.pricesdataLogDelete;

drop trigger if exists usersettings.pricesdataLogInsert;

drop trigger if exists usersettings.pricesdataLogUpdate;

alter table logs.prices_data_logs
  drop column WaitingDownloadInterval,
  add column ParentSynonym INT(11) UNSIGNED DEFAULT NULL;

alter table usersettings.pricesdata
  add column ParentSynonym INT(11) UNSIGNED DEFAULT NULL;



-- обновл€ем таблицу PricesFmts
alter table farm.`pricefmts`
  change column `Format` `Format` char(20) NOT NULL DEFAULT '',
  change column `Comment` `Comment` varchar(255) DEFAULT NULL,
  add column `ParserClassName` varchar(255) DEFAULT NULL;


-- обновл€ем таблицу FormRules
-- потом не забыть восстановить логирование formrules
drop trigger if exists farm.UpdatePriceCode;

update
  farm.formrules
set
  delimiter = null
where
  formrules.delimiter = ''
and (formrules.PriceFmt in ('DOS', 'WIN'));


-- запол€ем таблицу PriceFmts
delete from farm.`pricefmts`;

insert into farm.`pricefmts`
  (Id, `Format`, `FileExtention`, `Comment`, `ParserClassName`)
  values
  (1, 'DelimWIN', '.txt', '“екстовый файл с разделител€ми кодировки Windows', 'TXTWinDelimiterPriceParser'),
  (2, 'DelimDOS', '.txt', '“екстовый файл с разделител€ми кодировки DOS', 'TXTDosDelimiterPriceParser'),
  (3, 'XLS', '.xls', 'Excel-файл', 'ExcelPriceParser'),
  (4, 'DBF', '.dbf', 'DBF-файл', 'DBFPriceParser'),
  (5, 'XML', '.xml', 'CommerceML-файл', 'CommerceMLParser'),
  (6, 'FixedWIN', '.txt', '“екстовый файл с фиксированной шириной колонок кодировки Windows', 'TXTWinFixedPriceParser'),
  (7, 'FixedDOS', '.txt', '“екстовый файл с фиксированной шириной колонок кодировки DOS', 'TXTDosFixedPriceParser');


alter table farm.formrules
  add column `Id` int(11) unsigned NOT NULL first,
  add column `PriceFormatId` int(11) unsigned DEFAULT NULL after `Id`,
  drop column `Segment`,
  drop column `Flag`;

alter table logs.form_rules_logs
  add column `PriceFormatId` int(11) unsigned DEFAULT NULL after `FormRulesId`,
  drop column `FirmName`,
  drop column `FullName`,
  drop column `FirmURL`,
  drop column `Town`,
  drop column `FirmCr`,
  drop column `CountryCr`,
  drop column `FormPost`,
  drop column `DateLastForm`,
  drop column `DateCurPrice`,
  drop column `DatePrevPrice`,
  drop column `PayDate`,
  drop column `Segment`,
  drop column `Flag`,
  drop column `PriceFmt`,
  drop column `PriceFile`,
  drop column `PriceBLOB`,
  drop column `TxtAsFactCostBegin`,
  drop column `TxtAsFactCostEnd`,
  drop column `Txt5DayCostBegin`,
  drop column `Txt5DayCostEnd`,
  drop column `Txt10DayCostBegin`,
  drop column `Txt10DayCostEnd`,
  drop column `Txt15DayCostBegin`,
  drop column `Txt15DayCostEnd`,
  drop column `Txt20DayCostBegin`,
  drop column `Txt20DayCostEnd`,
  drop column `Txt25DayCostBegin`,
  drop column `Txt25DayCostEnd`,
  drop column `Txt30DayCostBegin`,
  drop column `Txt30DayCostEnd`,
  drop column `Txt45DayCostBegin`,
  drop column `Txt45DayCostEnd`,
  drop column `TxtReserved3Begin`,
  drop column `TxtReserved3End`,
  drop column `TxtUpCostBegin`,
  drop column `TxtUpCostEnd`,
  drop column `FAsFactCost`,
  drop column `F5DayCost`,
  drop column `F10DayCost`,
  drop column `F15DayCost`,
  drop column `F20DayCost`,
  drop column `F25DayCost`,
  drop column `F30DayCost`,
  drop column `F45DayCost`,
  drop column `FUpCost`,
  drop column `FReserved3`,
  drop column `AddField`,
  drop column `Symbol1`,
  drop column `Symbol2`,
  drop column `Symbol3`,
  drop column `Symbol4`,
  drop column `NoteText1`,
  drop column `NoteText2`,
  drop column `NoteText3`,
  drop column `NoteText4`,
  drop column `Tag1`,
  drop column `Tag2`,
  drop column `Tag3`,
  drop column `PosNum`,
  drop column `FirmCurs`,
  drop column `MMVBPlus`,
  drop column `RegionMask`,
  drop column `Addition`,
  drop column `AutoEMail`,
  add column `FOrderCost` varchar(20) default NULL,
  add column `TxtOrderCostBegin` int(11) unsigned default NULL,
  add column `TxtOrderCostEnd` int(11) unsigned default NULL,
  add column `FMinOrderCount` varchar(20) default NULL,
  add column `TxtMinOrderCountBegin` int(11) unsigned default NULL,
  add column `TxtMinOrderCountEnd` int(11) unsigned default NULL;


update farm.formrules
set
  Id = FirmCode;

update
  farm.formrules
set
  Id = PriceCode,
  PriceFormatId = 
    if(PriceFMT = 'XLS', 3, 
       if(PriceFMT = 'DBF', 4, 
          if(PriceFMT = 'XML', 5, 
             if(PriceFMT = 'WIN' and Delimiter is not null, 1,
                if(PriceFMT = 'DOS' and Delimiter is not null, 2,
                   if(PriceFMT = 'WIN' and Delimiter is null, 6, 
                      if(PriceFMT = 'DOS' and Delimiter is null, 7, null)
                   )
                )
             )
          )
       )
    );


-- обновл€ем таблицу SourceTypes
alter table farm.sourcetypes
  add column `Id` int(11) unsigned NOT NULL DEFAULT '0' first;

update farm.sourcetypes set Id = 1 where Type = 'EMail';
update farm.sourcetypes set Id = 2 where Type = 'HTTP';
update farm.sourcetypes set Id = 3 where Type = 'FTP';
update farm.sourcetypes set Id = 4 where Type = 'LAN';

alter table farm.sourcetypes
  add primary key (`Id`);


-- обновл€ем таблицу Sources
-- потом не забыть восстановить логирование sources
drop trigger if exists farm.sourcesLogDelete;

drop trigger if exists farm.sourcesLogInsert;

drop trigger if exists farm.sourcesLogUpdate;

alter table farm.sources
  add column `Id` int(11) unsigned NOT NULL first,
  add column `SourceTypeId` int(11) unsigned DEFAULT NULL after `Id`;

update
  farm.sources
set
  sources.Id = sources.FirmCode;

update
  farm.sources,
  farm.sourcetypes
set
  sources.SourceTypeId = sourcetypes.Id
where
  sourcetypes.Type = sources.SourceType;


-- обновл€ем таблицы BlockedPrice, Forb, Zero, UnrecExp
alter table farm.`blockedPrice`
  add column `PriceItemId` int(11) unsigned DEFAULT NULL first;

alter table farm.`forb`
  add column `PriceItemId` int(11) unsigned DEFAULT NULL after `RowId`;

alter table farm.`zero`
  add column `PriceItemId` int(11) unsigned DEFAULT NULL after `RowId`;

alter table farm.`UnrecExp`
  add column `PriceItemId` int(11) unsigned DEFAULT NULL after `RowId`,
  drop column BlockBy;


-- обновл€ем таблицу FormLogs
alter table logs.formlogs
  add column `PriceItemId` int(11) unsigned DEFAULT NULL after `LogTime`,
  add column `Host` varchar(50) DEFAULT NULL after `PriceItemId`;

update logs.formlogs
set
  Host = if(AppCode = 7, 'fms', 'prg1');

-- обновл€ем таблицу DownLogs
alter table logs.downlogs
  add column `PriceItemId` int(11) unsigned DEFAULT NULL after `LogTime`,
  add column `Host` varchar(50) DEFAULT NULL after `PriceItemId`;

update logs.downlogs
set
  Host = if(AppCode = 3, 'fms', 'prg1');


CREATE TABLE usersettings.`PriceItems` (
  `Id` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `FormRuleId` int(10) unsigned NOT NULL,
  `SourceId` int(10) unsigned NOT NULL,
  `RowCount` int(10) unsigned DEFAULT NULL COMMENT ' оличество позиций в \r\nформализованном прайс-листе',
  `UnformCount` int(10) unsigned DEFAULT NULL COMMENT ' оличество \r\nнеформализованных позиций в прайсе.',
  `PriceDate` datetime DEFAULT NULL,
  `LastFormalization` datetime DEFAULT NULL,
  `LastRetrans` datetime DEFAULT NULL COMMENT 'ƒата последнего перепроведени€ \r\nпрайс-листа обработкой.',
  `LastSynonymsCreation` datetime DEFAULT NULL COMMENT 'ƒата последнего \r\nсоздани€ синонимов наименований или производителей обработкой.',
  `WaitingDownloadInterval` int(11) unsigned NOT NULL DEFAULT '1' COMMENT 'интервал (в часах), во врем€ которого должен прийти свежий прайс-лист',
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;


drop procedure if exists usersettings.FillPriceItems;

DELIMITER ;;

CREATE PROCEDURE usersettings.`FillPriceItems`()
BEGIN
  DECLARE done INT DEFAULT 0;
  DECLARE PriceCode, CostCode, CostParentSynonym INT Unsigned;
  DECLARE PriceRowCount, PriceUnformCount, CostRowCount, CostUnformCount, PriceWaitingDownloadInterval INT Unsigned;
  Declare PriceDate, PriceLastRetrans, PriceLastSynonymsCreation, CostDate, CostLastRetrans, CostLastSynonymsCreation, PriceLastFormalization, CostLastFormalization datetime;
  DECLARE CostType, BaseCost Tinyint;
  declare LastPriceItemId Int unsigned;

  DECLARE Prices CURSOR FOR select
p.PriceCode,
p.CostType,
pc.CostCode,
pc.basecost,
pui.RowCount,
pui.UnformCount,
pui.LastRetrans,
pui.LastSynonymsCreation,
pui.DateCurPrice,
pui.DateLastForm,
cui.RowCount,
cui.UnformCount,
cui.LastRetrans,
cui.LastSynonymsCreation,
cui.DateCurPrice,
cui.DateLastForm,
p.WaitingDownloadInterval,
fr.ParentSynonym
from
(
usersettings.pricesdata p,
usersettings.pricescosts pc,
usersettings.price_update_info pui
)
left join usersettings.price_update_info cui on cui.PRICECODE = pc.PriceCode
left join farm.formrules fr on fr.FirmCode = pc.PriceCode
where
    pc.SHOWPRICECODE = p.PriceCode
and ( ( ((p.COSTTYPE = 0) or (p.CostType is null)) and (pc.BaseCost = 1) ) or (p.CostType = 1) )
and pui.PRICECODE = p.PriceCode
;
  DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = 1;

  OPEN Prices;

  REPEAT
    FETCH Prices INTO PriceCode, CostType, CostCode, BaseCost,
          PriceRowCount, PriceUnformCount, PriceLastRetrans, PriceLastSynonymsCreation, PriceDate, PriceLastFormalization,
          CostRowCount, CostUnformCount, CostLastRetrans, CostLastSynonymsCreation, CostDate, CostLastFormalization, PriceWaitingDownloadInterval, CostParentSynonym;
    IF NOT done THEN

      -- ¬ставка
			if ((CostType = 0) or (CostType is null))
			then
        insert into usersettings.PriceItems
          (FormRuleId, SourceId, RowCount, UnformCount, PriceDate, LastRetrans, LastSynonymsCreation, WaitingDownloadInterval, LastFormalization)
          values
          (PriceCode, PriceCode, PriceRowCount, PriceUnformCount, PriceDate, PriceLastRetrans, PriceLastSynonymsCreation, PriceWaitingDownloadInterval, PriceLastFormalization);
      else
        insert into usersettings.PriceItems
          (FormRuleId, SourceId, RowCount, UnformCount, PriceDate, LastRetrans, LastSynonymsCreation, WaitingDownloadInterval, LastFormalization)
          values
          (CostCode, CostCode, CostRowCount, CostUnformCount, CostDate, CostLastRetrans, CostLastSynonymsCreation, PriceWaitingDownloadInterval, CostLastFormalization);
      end if;
      -- обновл€ем ParentSynonym у прайсов, только у базовых ценовых колонок
      if ((BaseCost = 1) and (CostParentSynonym is not null)) then
        if (PriceCode != CostParentSynonym) then
          update usersettings.pricesdata set ParentSynonym = CostParentSynonym where pricesdata.PriceCode = PriceCode;
        end if;
      end if;
      select last_insert_id() into LastPriceItemId;
      update usersettings.pricescosts set PriceItemId = LastPriceItemId where pricescosts.CostCode = CostCode;

    END IF;
  UNTIL done END REPEAT;

  CLOSE Prices;

END ;;

DELIMITER ;

call usersettings.FillPriceItems;


alTER TABLE `usersettings`.`price_update_info` 
  RENAME TO `usersettings`.`price_update_info_OLD`;

drop procedure usersettings.FillPriceItems;