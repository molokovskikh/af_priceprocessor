ALTER TABLE `logs`.`form_rules_logs`	ADD COLUMN `FOptimizationSkip` VARCHAR(20) NULL DEFAULT NULL;
ALTER TABLE `logs`.`form_rules_logs`	ADD COLUMN `TxtOptimizationSkipBegin` INT(11) NULL DEFAULT NULL;
ALTER TABLE `logs`.`form_rules_logs`	ADD COLUMN `TxtOptimizationSkipEnd` INT(11) NULL DEFAULT NULL;

USE farm;
DROP TRIGGER IF EXISTS FormRulesLogInsert;
CREATE DEFINER=`RootDBMS`@`127.0.0.1` TRIGGER `FormRulesLogInsert` AFTER INSERT ON `FormRules` FOR EACH ROW BEGIN
  INSERT
  INTO `logs`.form_rules_logs
  SET
    LogTime = NOW(), OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(), '@', 1)), OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(), '@', -1)), Operation = 0, FormRulesID = NEW.Id
    , PriceFormatId = NEW.PriceFormatId
    , MaxOld = NEW.MaxOld
    , Delimiter = NEW.Delimiter
    , ParentFormRules = NEW.ParentFormRules
    , FormByCode = NEW.FormByCode
    , NameMask = NEW.NameMask
    , ForbWords = NEW.ForbWords
    , JunkPos = NEW.JunkPos
    , AwaitPos = NEW.AwaitPos
    , StartLine = NEW.StartLine
    , ListName = NEW.ListName
    , TxtCodeBegin = NEW.TxtCodeBegin
    , TxtCodeEnd = NEW.TxtCodeEnd
    , TxtCodeCrBegin = NEW.TxtCodeCrBegin
    , TxtCodeCrEnd = NEW.TxtCodeCrEnd
    , TxtNameBegin = NEW.TxtNameBegin
    , TxtNameEnd = NEW.TxtNameEnd
    , TxtFirmCrBegin = NEW.TxtFirmCrBegin
    , TxtFirmCrEnd = NEW.TxtFirmCrEnd
    , TxtBaseCostBegin = NEW.TxtBaseCostBegin
    , TxtBaseCostEnd = NEW.TxtBaseCostEnd
    , TxtMinBoundCostBegin = NEW.TxtMinBoundCostBegin
    , TxtMinBoundCostEnd = NEW.TxtMinBoundCostEnd
    , TxtUnitBegin = NEW.TxtUnitBegin
    , TxtUnitEnd = NEW.TxtUnitEnd
    , TxtVolumeBegin = NEW.TxtVolumeBegin
    , TxtVolumeEnd = NEW.TxtVolumeEnd
    , TxtQuantityBegin = NEW.TxtQuantityBegin
    , TxtQuantityEnd = NEW.TxtQuantityEnd
    , TxtNoteBegin = NEW.TxtNoteBegin
    , TxtNoteEnd = NEW.TxtNoteEnd
    , TxtPeriodBegin = NEW.TxtPeriodBegin
    , TxtPeriodEnd = NEW.TxtPeriodEnd
    , TxtDocBegin = NEW.TxtDocBegin
    , TxtDocEnd = NEW.TxtDocEnd
    , TxtJunkBegin = NEW.TxtJunkBegin
    , TxtJunkEnd = NEW.TxtJunkEnd
    , TxtAwaitBegin = NEW.TxtAwaitBegin
    , TxtAwaitEnd = NEW.TxtAwaitEnd
    , FCode = NEW.FCode
    , FCodeCr = NEW.FCodeCr
    , FName1 = NEW.FName1
    , FName2 = NEW.FName2
    , FName3 = NEW.FName3
    , FFirmCr = NEW.FFirmCr
    , FBaseCost = NEW.FBaseCost
    , FMinBoundCost = NEW.FMinBoundCost
    , FUnit = NEW.FUnit
    , FVolume = NEW.FVolume
    , FQuantity = NEW.FQuantity
    , FNote = NEW.FNote
    , FPeriod = NEW.FPeriod
    , FDoc = NEW.FDoc
    , FJunk = NEW.FJunk
    , FAwait = NEW.FAwait
    , Memo = NEW.Memo
    , TxtVitallyImportantBegin = NEW.TxtVitallyImportantBegin
    , TxtVitallyImportantEnd = NEW.TxtVitallyImportantEnd
    , FVitallyImportant = NEW.FVitallyImportant
    , VitallyImportantMask = NEW.VitallyImportantMask
    , TxtRequestRatioBegin = NEW.TxtRequestRatioBegin
    , TxtRequestRatioEnd = NEW.TxtRequestRatioEnd
    , FRequestRatio = NEW.FRequestRatio
    , TxtRegistryCostBegin = NEW.TxtRegistryCostBegin
    , TxtRegistryCostEnd = NEW.TxtRegistryCostEnd
    , FRegistryCost = NEW.FRegistryCost
    , FMaxBoundCost = NEW.FMaxBoundCost
    , TxtMaxBoundCostBegin = NEW.TxtMaxBoundCostBegin
    , TxtMaxBoundCostEnd = NEW.TxtMaxBoundCostEnd
    , FOrderCost = NEW.FOrderCost
    , TxtOrderCostBegin = NEW.TxtOrderCostBegin
    , TxtOrderCostEnd = NEW.TxtOrderCostEnd
    , FMinOrderCount = NEW.FMinOrderCount
    , TxtMinOrderCountBegin = NEW.TxtMinOrderCountBegin
    , TxtMinOrderCountEnd = NEW.TxtMinOrderCountEnd
	, FOptimizationSkip = NEW.FOptimizationSkip
	, TxtOptimizationSkipBegin = NEW.TxtOptimizationSkipBegin
	, TxtOptimizationSkipEnd = NEW.TxtOptimizationSkipEnd
  ;
END;

DROP TRIGGER IF EXISTS FormRulesLogUpdate;
CREATE DEFINER=`RootDBMS`@`127.0.0.1` TRIGGER `FormRulesLogUpdate` AFTER UPDATE ON `FormRules` FOR EACH ROW BEGIN
  INSERT
  INTO `logs`.form_rules_logs
  SET
    LogTime = NOW(), OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(), '@', 1)), OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(), '@', -1)), Operation = 1, FormRulesID = OLD.Id
    , PriceFormatId = IFNULL(NEW.PriceFormatId, OLD.PriceFormatId)
    , MaxOld = NULLIF(NEW.MaxOld, OLD.MaxOld)
    , Delimiter = NULLIF(NEW.Delimiter, OLD.Delimiter)
    , ParentFormRules = IFNULL(NEW.ParentFormRules, OLD.ParentFormRules)
    , FormByCode = NULLIF(NEW.FormByCode, OLD.FormByCode)
    , NameMask = NULLIF(NEW.NameMask, OLD.NameMask)
    , ForbWords = NULLIF(NEW.ForbWords, OLD.ForbWords)
    , JunkPos = NULLIF(NEW.JunkPos, OLD.JunkPos)
    , AwaitPos = NULLIF(NEW.AwaitPos, OLD.AwaitPos)
    , StartLine = NULLIF(NEW.StartLine, OLD.StartLine)
    , ListName = NULLIF(NEW.ListName, OLD.ListName)
    , TxtCodeBegin = NULLIF(NEW.TxtCodeBegin, OLD.TxtCodeBegin)
    , TxtCodeEnd = NULLIF(NEW.TxtCodeEnd, OLD.TxtCodeEnd)
    , TxtCodeCrBegin = NULLIF(NEW.TxtCodeCrBegin, OLD.TxtCodeCrBegin)
    , TxtCodeCrEnd = NULLIF(NEW.TxtCodeCrEnd, OLD.TxtCodeCrEnd)
    , TxtNameBegin = NULLIF(NEW.TxtNameBegin, OLD.TxtNameBegin)
    , TxtNameEnd = NULLIF(NEW.TxtNameEnd, OLD.TxtNameEnd)
    , TxtFirmCrBegin = NULLIF(NEW.TxtFirmCrBegin, OLD.TxtFirmCrBegin)
    , TxtFirmCrEnd = NULLIF(NEW.TxtFirmCrEnd, OLD.TxtFirmCrEnd)
    , TxtBaseCostBegin = NULLIF(NEW.TxtBaseCostBegin, OLD.TxtBaseCostBegin)
    , TxtBaseCostEnd = NULLIF(NEW.TxtBaseCostEnd, OLD.TxtBaseCostEnd)
    , TxtMinBoundCostBegin = NULLIF(NEW.TxtMinBoundCostBegin, OLD.TxtMinBoundCostBegin)
    , TxtMinBoundCostEnd = NULLIF(NEW.TxtMinBoundCostEnd, OLD.TxtMinBoundCostEnd)
    , TxtUnitBegin = NULLIF(NEW.TxtUnitBegin, OLD.TxtUnitBegin)
    , TxtUnitEnd = NULLIF(NEW.TxtUnitEnd, OLD.TxtUnitEnd)
    , TxtVolumeBegin = NULLIF(NEW.TxtVolumeBegin, OLD.TxtVolumeBegin)
    , TxtVolumeEnd = NULLIF(NEW.TxtVolumeEnd, OLD.TxtVolumeEnd)
    , TxtQuantityBegin = NULLIF(NEW.TxtQuantityBegin, OLD.TxtQuantityBegin)
    , TxtQuantityEnd = NULLIF(NEW.TxtQuantityEnd, OLD.TxtQuantityEnd)
    , TxtNoteBegin = NULLIF(NEW.TxtNoteBegin, OLD.TxtNoteBegin)
    , TxtNoteEnd = NULLIF(NEW.TxtNoteEnd, OLD.TxtNoteEnd)
    , TxtPeriodBegin = NULLIF(NEW.TxtPeriodBegin, OLD.TxtPeriodBegin)
    , TxtPeriodEnd = NULLIF(NEW.TxtPeriodEnd, OLD.TxtPeriodEnd)
    , TxtDocBegin = NULLIF(NEW.TxtDocBegin, OLD.TxtDocBegin)
    , TxtDocEnd = NULLIF(NEW.TxtDocEnd, OLD.TxtDocEnd)
    , TxtJunkBegin = NULLIF(NEW.TxtJunkBegin, OLD.TxtJunkBegin)
    , TxtJunkEnd = NULLIF(NEW.TxtJunkEnd, OLD.TxtJunkEnd)
    , TxtAwaitBegin = NULLIF(NEW.TxtAwaitBegin, OLD.TxtAwaitBegin)
    , TxtAwaitEnd = NULLIF(NEW.TxtAwaitEnd, OLD.TxtAwaitEnd)
    , FCode = NULLIF(NEW.FCode, OLD.FCode)
    , FCodeCr = NULLIF(NEW.FCodeCr, OLD.FCodeCr)
    , FName1 = NULLIF(NEW.FName1, OLD.FName1)
    , FName2 = NULLIF(NEW.FName2, OLD.FName2)
    , FName3 = NULLIF(NEW.FName3, OLD.FName3)
    , FFirmCr = NULLIF(NEW.FFirmCr, OLD.FFirmCr)
    , FBaseCost = NULLIF(NEW.FBaseCost, OLD.FBaseCost)
    , FMinBoundCost = NULLIF(NEW.FMinBoundCost, OLD.FMinBoundCost)
    , FUnit = NULLIF(NEW.FUnit, OLD.FUnit)
    , FVolume = NULLIF(NEW.FVolume, OLD.FVolume)
    , FQuantity = NULLIF(NEW.FQuantity, OLD.FQuantity)
    , FNote = NULLIF(NEW.FNote, OLD.FNote)
    , FPeriod = NULLIF(NEW.FPeriod, OLD.FPeriod)
    , FDoc = NULLIF(NEW.FDoc, OLD.FDoc)
    , FJunk = NULLIF(NEW.FJunk, OLD.FJunk)
    , FAwait = NULLIF(NEW.FAwait, OLD.FAwait)
    , Memo = NULLIF(NEW.Memo, OLD.Memo)
    , TxtVitallyImportantBegin = NULLIF(NEW.TxtVitallyImportantBegin, OLD.TxtVitallyImportantBegin)
    , TxtVitallyImportantEnd = NULLIF(NEW.TxtVitallyImportantEnd, OLD.TxtVitallyImportantEnd)
    , FVitallyImportant = NULLIF(NEW.FVitallyImportant, OLD.FVitallyImportant)
    , VitallyImportantMask = NULLIF(NEW.VitallyImportantMask, OLD.VitallyImportantMask)
    , TxtRequestRatioBegin = NULLIF(NEW.TxtRequestRatioBegin, OLD.TxtRequestRatioBegin)
    , TxtRequestRatioEnd = NULLIF(NEW.TxtRequestRatioEnd, OLD.TxtRequestRatioEnd)
    , FRequestRatio = NULLIF(NEW.FRequestRatio, OLD.FRequestRatio)
    , TxtRegistryCostBegin = NULLIF(NEW.TxtRegistryCostBegin, OLD.TxtRegistryCostBegin)
    , TxtRegistryCostEnd = NULLIF(NEW.TxtRegistryCostEnd, OLD.TxtRegistryCostEnd)
    , FRegistryCost = NULLIF(NEW.FRegistryCost, OLD.FRegistryCost)
    , FMaxBoundCost = NULLIF(NEW.FMaxBoundCost, OLD.FMaxBoundCost)
    , TxtMaxBoundCostBegin = NULLIF(NEW.TxtMaxBoundCostBegin, OLD.TxtMaxBoundCostBegin)
    , TxtMaxBoundCostEnd = NULLIF(NEW.TxtMaxBoundCostEnd, OLD.TxtMaxBoundCostEnd)
    , FOrderCost = NULLIF(NEW.FOrderCost, OLD.FOrderCost)
    , TxtOrderCostBegin = NULLIF(NEW.TxtOrderCostBegin, OLD.TxtOrderCostBegin)
    , TxtOrderCostEnd = NULLIF(NEW.TxtOrderCostEnd, OLD.TxtOrderCostEnd)
    , FMinOrderCount = NULLIF(NEW.FMinOrderCount, OLD.FMinOrderCount)
    , TxtMinOrderCountBegin = NULLIF(NEW.TxtMinOrderCountBegin, OLD.TxtMinOrderCountBegin)
    , TxtMinOrderCountEnd = NULLIF(NEW.TxtMinOrderCountEnd, OLD.TxtMinOrderCountEnd)
	, FOptimizationSkip = NULLIF(NEW.FOptimizationSkip, OLD.FOptimizationSkip)
	, TxtOptimizationSkipBegin = NULLIF(NEW.TxtOptimizationSkipBegin, OLD.TxtOptimizationSkipBegin)
	, TxtOptimizationSkipEnd = NULLIF(NEW.TxtOptimizationSkipEnd, OLD.TxtOptimizationSkipEnd)
  ;
END;

DROP TRIGGER IF EXISTS FormRulesLogDelete;
CREATE DEFINER=`RootDBMS`@`127.0.0.1` TRIGGER `FormRulesLogDelete` AFTER DELETE ON `FormRules` FOR EACH ROW BEGIN
  INSERT
  INTO `logs`.form_rules_logs
  SET
    LogTime = NOW(), OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(), '@', 1)), OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(), '@', -1)), Operation = 2, FormRulesID = OLD.Id
    , PriceFormatId = OLD.PriceFormatId
    , MaxOld = OLD.MaxOld
    , Delimiter = OLD.Delimiter
    , ParentFormRules = OLD.ParentFormRules
    , FormByCode = OLD.FormByCode
    , NameMask = OLD.NameMask
    , ForbWords = OLD.ForbWords
    , JunkPos = OLD.JunkPos
    , AwaitPos = OLD.AwaitPos
    , StartLine = OLD.StartLine
    , ListName = OLD.ListName
    , TxtCodeBegin = OLD.TxtCodeBegin
    , TxtCodeEnd = OLD.TxtCodeEnd
    , TxtCodeCrBegin = OLD.TxtCodeCrBegin
    , TxtCodeCrEnd = OLD.TxtCodeCrEnd
    , TxtNameBegin = OLD.TxtNameBegin
    , TxtNameEnd = OLD.TxtNameEnd
    , TxtFirmCrBegin = OLD.TxtFirmCrBegin
    , TxtFirmCrEnd = OLD.TxtFirmCrEnd
    , TxtBaseCostBegin = OLD.TxtBaseCostBegin
    , TxtBaseCostEnd = OLD.TxtBaseCostEnd
    , TxtMinBoundCostBegin = OLD.TxtMinBoundCostBegin
    , TxtMinBoundCostEnd = OLD.TxtMinBoundCostEnd
    , TxtUnitBegin = OLD.TxtUnitBegin
    , TxtUnitEnd = OLD.TxtUnitEnd
    , TxtVolumeBegin = OLD.TxtVolumeBegin
    , TxtVolumeEnd = OLD.TxtVolumeEnd
    , TxtQuantityBegin = OLD.TxtQuantityBegin
    , TxtQuantityEnd = OLD.TxtQuantityEnd
    , TxtNoteBegin = OLD.TxtNoteBegin
    , TxtNoteEnd = OLD.TxtNoteEnd
    , TxtPeriodBegin = OLD.TxtPeriodBegin
    , TxtPeriodEnd = OLD.TxtPeriodEnd
    , TxtDocBegin = OLD.TxtDocBegin
    , TxtDocEnd = OLD.TxtDocEnd
    , TxtJunkBegin = OLD.TxtJunkBegin
    , TxtJunkEnd = OLD.TxtJunkEnd
    , TxtAwaitBegin = OLD.TxtAwaitBegin
    , TxtAwaitEnd = OLD.TxtAwaitEnd
    , FCode = OLD.FCode
    , FCodeCr = OLD.FCodeCr
    , FName1 = OLD.FName1
    , FName2 = OLD.FName2
    , FName3 = OLD.FName3
    , FFirmCr = OLD.FFirmCr
    , FBaseCost = OLD.FBaseCost
    , FMinBoundCost = OLD.FMinBoundCost
    , FUnit = OLD.FUnit
    , FVolume = OLD.FVolume
    , FQuantity = OLD.FQuantity
    , FNote = OLD.FNote
    , FPeriod = OLD.FPeriod
    , FDoc = OLD.FDoc
    , FJunk = OLD.FJunk
    , FAwait = OLD.FAwait
    , Memo = OLD.Memo
    , TxtVitallyImportantBegin = OLD.TxtVitallyImportantBegin
    , TxtVitallyImportantEnd = OLD.TxtVitallyImportantEnd
    , FVitallyImportant = OLD.FVitallyImportant
    , VitallyImportantMask = OLD.VitallyImportantMask
    , TxtRequestRatioBegin = OLD.TxtRequestRatioBegin
    , TxtRequestRatioEnd = OLD.TxtRequestRatioEnd
    , FRequestRatio = OLD.FRequestRatio
    , TxtRegistryCostBegin = OLD.TxtRegistryCostBegin
    , TxtRegistryCostEnd = OLD.TxtRegistryCostEnd
    , FRegistryCost = OLD.FRegistryCost
    , FMaxBoundCost = OLD.FMaxBoundCost
    , TxtMaxBoundCostBegin = OLD.TxtMaxBoundCostBegin
    , TxtMaxBoundCostEnd = OLD.TxtMaxBoundCostEnd
    , FOrderCost = OLD.FOrderCost
    , TxtOrderCostBegin = OLD.TxtOrderCostBegin
    , TxtOrderCostEnd = OLD.TxtOrderCostEnd
    , FMinOrderCount = OLD.FMinOrderCount
    , TxtMinOrderCountBegin = OLD.TxtMinOrderCountBegin
    , TxtMinOrderCountEnd = OLD.TxtMinOrderCountEnd
	, FOptimizationSkip = OLD.FOptimizationSkip
	, TxtOptimizationSkipBegin = OLD.TxtOptimizationSkipBegin
	, TxtOptimizationSkipEnd = OLD.TxtOptimizationSkipEnd
  ;
END
