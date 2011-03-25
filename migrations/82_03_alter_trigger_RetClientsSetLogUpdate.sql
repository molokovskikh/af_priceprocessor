USE usersettings;
DROP TRIGGER IF EXISTS RetClientsSetLogUpdate;
CREATE
DEFINER = 'RootDBMS'@'127.0.0.1'
TRIGGER RetClientsSetLogUpdate
AFTER UPDATE
ON RetClientsSet
FOR EACH ROW
BEGIN

  INSERT INTO `logs`.RetClientsSetLogs
  SET
    LogTime = NOW()
    , OperatorName = IFNULL
    (
    @INUser,
    SUBSTRING_INDEX(USER(), '@', 1)
    )
    , OperatorHost = IFNULL
    (
    @INHost,
    SUBSTRING_INDEX(USER(), '@', -1)
    )
    , Operation = 1, ClientCode = IFNULL
    (
    NEW.ClientCode,
    OLD.ClientCode
    )
    , InvisibleOnFirm = NULLIF
    (
    NEW.InvisibleOnFirm,
    OLD.InvisibleOnFirm
    )
    , BaseFirmCategory = NULLIF
    (
    NEW.BaseFirmCategory,
    OLD.BaseFirmCategory
    )
    , RetUpCost = NULLIF
    (
    NEW.RetUpCost,
    OLD.RetUpCost
    )
    , OverCostPercent = NULLIF
    (
    NEW.OverCostPercent,
    OLD.OverCostPercent
    )
    , DifferenceCalculation = NULLIF
    (
    NEW.DifferenceCalculation,
    OLD.DifferenceCalculation
    )
    , AlowRegister = NULLIF
    (
    NEW.AlowRegister,
    OLD.AlowRegister
    )
    , AlowRejection = NULLIF
    (
    NEW.AlowRejection,
    OLD.AlowRejection
    )
    , AlowDocuments = NULLIF
    (
    NEW.AlowDocuments,
    OLD.AlowDocuments
    )
    , MultiUserLevel = NULLIF
    (
    NEW.MultiUserLevel,
    OLD.MultiUserLevel
    )
    , AdvertisingLevel = NULLIF
    (
    NEW.AdvertisingLevel,
    OLD.AdvertisingLevel
    )
    , AlowWayBill = NULLIF
    (
    NEW.AlowWayBill,
    OLD.AlowWayBill
    )
    , AllowDocuments = NULLIF
    (
    NEW.AllowDocuments,
    OLD.AllowDocuments
    )
    , AlowChangeSegment = NULLIF
    (
    NEW.AlowChangeSegment,
    OLD.AlowChangeSegment
    )
    , ShowPriceName = NULLIF
    (
    NEW.ShowPriceName,
    OLD.ShowPriceName
    )
    , WorkRegionMask = NULLIF
    (
    NEW.WorkRegionMask,
    OLD.WorkRegionMask
    )
    , OrderRegionMask = NULLIF
    (
    NEW.OrderRegionMask,
    OLD.OrderRegionMask
    )
    , EnableUpdate = NULLIF
    (
    NEW.EnableUpdate,
    OLD.EnableUpdate
    )
    , CheckCopyID = NULLIF
    (
    NEW.CheckCopyID,
    OLD.CheckCopyID
    )
    , AlowCumulativeUpdate = NULLIF
    (
    NEW.AlowCumulativeUpdate,
    OLD.AlowCumulativeUpdate
    )
    , CheckCumulativeUpdateStatus = NULLIF
    (
    NEW.CheckCumulativeUpdateStatus,
    OLD.CheckCumulativeUpdateStatus
    )
    , ServiceClient = NULLIF
    (
    NEW.ServiceClient,
    OLD.ServiceClient
    )
    , SubmitOrders = NULLIF
    (
    NEW.SubmitOrders,
    OLD.SubmitOrders
    )
    , AllowSubmitOrders = NULLIF
    (
    NEW.AllowSubmitOrders,
    OLD.AllowSubmitOrders
    )
    , BasecostPassword = NULLIF
    (
    NEW.BasecostPassword,
    OLD.BasecostPassword
    )
    , OrdersVisualizationMode = NULLIF
    (
    NEW.OrdersVisualizationMode,
    OLD.OrdersVisualizationMode
    )
    , CalculateLeader = NULLIF
    (
    NEW.CalculateLeader,
    OLD.CalculateLeader
    )
    , AllowPreparatInfo = NULLIF
    (
    NEW.AllowPreparatInfo,
    OLD.AllowPreparatInfo
    )
    , AllowPreparatDesc = NULLIF
    (
    NEW.AllowPreparatDesc,
    OLD.AllowPreparatDesc
    )
    , SmartOrderRuleId = NULLIF
    (
    NEW.SmartOrderRuleId,
    OLD.SmartOrderRuleId
    )
    , FirmCodeOnly = NULLIF
    (
    NEW.FirmCodeOnly,
    OLD.FirmCodeOnly
    )
    , MaxWeeklyOrdersSum = NULLIF
    (
    NEW.MaxWeeklyOrdersSum,
    OLD.MaxWeeklyOrdersSum
    )
    , CheckWeeklyOrdersSum = NULLIF
    (
    NEW.CheckWeeklyOrdersSum,
    OLD.CheckWeeklyOrdersSum
    )
    , AllowDelayOfPayment = NULLIF
    (
    NEW.AllowDelayOfPayment,
    OLD.AllowDelayOfPayment
    )
    , Spy = NULLIF
    (
    NEW.Spy,
    OLD.Spy
    )
    , SpyAccount = NULLIF
    (
    NEW.SpyAccount,
    OLD.SpyAccount
    )
    , ShowNewDefecture = NULLIF
    (
    NEW.ShowNewDefecture,
    OLD.ShowNewDefecture
    )
    , MigrateToPrgDataService = NULLIF
    (
    NEW.MigrateToPrgDataService,
    OLD.MigrateToPrgDataService
    )
    , ManualComparison = NULLIF
    (
    NEW.ManualComparison,
    OLD.ManualComparison
    )
    , ParseWaybills = NULLIF
    (
    NEW.ParseWaybills,
    OLD.ParseWaybills
    )
    , SendRetailMarkup = NULLIF
    (
    NEW.SendRetailMarkup,
    OLD.SendRetailMarkup
    )
    , ShowAdvertising = NULLIF
    (
    NEW.ShowAdvertising,
    OLD.ShowAdvertising
    )
    , IgnoreNewPrices = NULLIF
    (
    NEW.IgnoreNewPrices,
    OLD.IgnoreNewPrices
    )
    , SendWaybillsFromClient = NULLIF
    (
    NEW.SendWaybillsFromClient,
    OLD.SendWaybillsFromClient
    )
    , OnlyParseWaybills = NULLIF
    (
    NEW.OnlyParseWaybills,
    OLD.OnlyParseWaybills
    )
    , UpdateToTestBuild = NULLIF
    (
    NEW.UpdateToTestBuild,
    OLD.UpdateToTestBuild
    )
    , EnableSmartOrder = NULLIF
    (
    NEW.EnableSmartOrder,
    OLD.EnableSmartOrder
    )
    , BuyingMatrixPriceId = NULLIF
    (
    NEW.BuyingMatrixPriceId,
    OLD.BuyingMatrixPriceId
    )
    , BuyingMatrixType = NULLIF
    (
    NEW.BuyingMatrixType,
    OLD.BuyingMatrixType
    )
    , WarningOnBuyingMatrix = NULLIF
    (
    NEW.WarningOnBuyingMatrix,
    OLD.WarningOnBuyingMatrix
    )
    , EnableImpersonalPrice = NULLIF
    (
    NEW.EnableImpersonalPrice,
    OLD.EnableImpersonalPrice
    )
    , IsConvertFormat = NULLIF
    (
    NEW.IsConvertFormat,
    OLD.IsConvertFormat
    )
  ;

END