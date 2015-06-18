CREATE TABLE  `logs`.`PriceItemLogs` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `LogTime` datetime NOT NULL,
  `OperatorName` varchar(50) NOT NULL,
  `OperatorHost` varchar(50) NOT NULL,
  `Operation` char(1) NOT NULL,
  `NewId` int(10) unsigned,
  `OldId` int(10) unsigned,
  `NewFormRuleId` int(10) unsigned,
  `OldFormRuleId` int(10) unsigned,
  `NewSourceId` int(10) unsigned,
  `OldSourceId` int(10) unsigned,
  `NewRowCount` int(10) unsigned,
  `OldRowCount` int(10) unsigned,
  `NewUnformCount` int(10) unsigned,
  `OldUnformCount` int(10) unsigned,
  `NewPriceDate` datetime,
  `OldPriceDate` datetime,
  `NewLastDownload` datetime,
  `OldLastDownload` datetime,
  `NewLastFormalization` datetime,
  `OldLastFormalization` datetime,
  `NewLastRetrans` datetime,
  `OldLastRetrans` datetime,
  `NewLastSynonymsCreation` datetime,
  `OldLastSynonymsCreation` datetime,
  `NewWaitingDownloadInterval` int(11) unsigned,
  `OldWaitingDownloadInterval` int(11) unsigned,
  `NewLocalLastDownload` datetime,
  `OldLocalLastDownload` datetime,

  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=cp1251;
DROP TRIGGER IF EXISTS Usersettings.PriceItemLogDelete;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Usersettings.PriceItemLogDelete AFTER DELETE ON Usersettings.PriceItems
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.PriceItemLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 'D',
		OldId = OLD.Id,
		OldFormRuleId = OLD.FormRuleId,
		OldSourceId = OLD.SourceId,
		OldRowCount = OLD.RowCount,
		OldUnformCount = OLD.UnformCount,
		OldPriceDate = OLD.PriceDate,
		OldLastDownload = OLD.LastDownload,
		OldLastFormalization = OLD.LastFormalization,
		OldLastRetrans = OLD.LastRetrans,
		OldLastSynonymsCreation = OLD.LastSynonymsCreation,
		OldWaitingDownloadInterval = OLD.WaitingDownloadInterval,
		OldLocalLastDownload = OLD.LocalLastDownload;
END;
DROP TRIGGER IF EXISTS Usersettings.PriceItemLogUpdate;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Usersettings.PriceItemLogUpdate AFTER UPDATE ON Usersettings.PriceItems
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.PriceItemLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 'U',
		NewId = NEW.Id,
		OldId = OLD.Id,
		NewFormRuleId = NEW.FormRuleId,
		OldFormRuleId = OLD.FormRuleId,
		NewSourceId = NEW.SourceId,
		OldSourceId = OLD.SourceId,
		NewRowCount = NEW.RowCount,
		OldRowCount = OLD.RowCount,
		NewUnformCount = NEW.UnformCount,
		OldUnformCount = OLD.UnformCount,
		NewPriceDate = NEW.PriceDate,
		OldPriceDate = OLD.PriceDate,
		NewLastDownload = NEW.LastDownload,
		OldLastDownload = OLD.LastDownload,
		NewLastFormalization = NEW.LastFormalization,
		OldLastFormalization = OLD.LastFormalization,
		NewLastRetrans = NEW.LastRetrans,
		OldLastRetrans = OLD.LastRetrans,
		NewLastSynonymsCreation = NEW.LastSynonymsCreation,
		OldLastSynonymsCreation = OLD.LastSynonymsCreation,
		NewWaitingDownloadInterval = NEW.WaitingDownloadInterval,
		OldWaitingDownloadInterval = OLD.WaitingDownloadInterval,
		NewLocalLastDownload = NEW.LocalLastDownload,
		OldLocalLastDownload = OLD.LocalLastDownload;
END;
DROP TRIGGER IF EXISTS Usersettings.PriceItemLogInsert;
CREATE DEFINER = RootDBMS@127.0.0.1 TRIGGER Usersettings.PriceItemLogInsert AFTER INSERT ON Usersettings.PriceItems
FOR EACH ROW BEGIN
	INSERT
	INTO `logs`.PriceItemLogs
	SET LogTime = now(),
		OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(),'@',1)),
		OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(),'@',-1)),
		Operation = 'I',
		NewId = NEW.Id,
		NewFormRuleId = NEW.FormRuleId,
		NewSourceId = NEW.SourceId,
		NewRowCount = NEW.RowCount,
		NewUnformCount = NEW.UnformCount,
		NewPriceDate = NEW.PriceDate,
		NewLastDownload = NEW.LastDownload,
		NewLastFormalization = NEW.LastFormalization,
		NewLastRetrans = NEW.LastRetrans,
		NewLastSynonymsCreation = NEW.LastSynonymsCreation,
		NewWaitingDownloadInterval = NEW.WaitingDownloadInterval,
		NewLocalLastDownload = NEW.LocalLastDownload;
END;
