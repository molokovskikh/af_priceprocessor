USE usersettings;

DROP TRIGGER IF EXISTS RetClientsSetLogDelete;

CREATE
DEFINER = 'RootDBMS'@'127.0.0.1'
TRIGGER RetClientsSetLogDelete
AFTER DELETE
ON RetClientsSet
FOR EACH ROW
BEGIN
  INSERT INTO `logs`.RetClientsSetLogs
  SET
    LogTime = NOW(),
    OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(), '@', 1)),
    OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(), '@', -1)),
    Operation = 2, 
    ClientCode = OLD.ClientCode, 
    InvisibleOnFirm = OLD.InvisibleOnFirm, 
    BaseFirmCategory = OLD.BaseFirmCategory, 
    RetUpCost = OLD.RetUpCost, 
    OverCostPercent = OLD.OverCostPercent, 
    DifferenceCalculation = OLD.DifferenceCalculation, 
    AlowRegister = OLD.AlowRegister, 
    AlowRejection = OLD.AlowRejection, 
    AlowDocuments = OLD.AlowDocuments, 
    MultiUserLevel = OLD.MultiUserLevel, 
    AdvertisingLevel = OLD.AdvertisingLevel, 
    AlowWayBill = OLD.AlowWayBill, 
    AllowDocuments = OLD.AllowDocuments, 
    AlowChangeSegment = OLD.AlowChangeSegment, 
    ShowPriceName = OLD.ShowPriceName, 
    WorkRegionMask = OLD.WorkRegionMask, 
    OrderRegionMask = OLD.OrderRegionMask, 
    EnableUpdate = OLD.EnableUpdate, 
    CheckCopyID = OLD.CheckCopyID, 
    AlowCumulativeUpdate = OLD.AlowCumulativeUpdate, 
    CheckCumulativeUpdateStatus = OLD.CheckCumulativeUpdateStatus, 
    ServiceClient = OLD.ServiceClient, 
    SubmitOrders = OLD.SubmitOrders, 
    AllowSubmitOrders = OLD.AllowSubmitOrders, 
    BasecostPassword = OLD.BasecostPassword, 
    OrdersVisualizationMode = OLD.OrdersVisualizationMode, 
    CalculateLeader = OLD.CalculateLeader, 
    AllowPreparatInfo = OLD.AllowPreparatInfo, 
    AllowPreparatDesc = OLD.AllowPreparatDesc, 
    SmartOrderRuleId = OLD.SmartOrderRuleId, 
    FirmCodeOnly = OLD.FirmCodeOnly, 
    MaxWeeklyOrdersSum = OLD.MaxWeeklyOrdersSum, 
    CheckWeeklyOrdersSum = OLD.CheckWeeklyOrdersSum, 
    AllowDelayOfPayment = OLD.AllowDelayOfPayment, 
    Spy = OLD.Spy, SpyAccount = OLD.SpyAccount, 
    ShowNewDefecture = OLD.ShowNewDefecture, 
    MigrateToPrgDataService = OLD.MigrateToPrgDataService, 
    ManualComparison = OLD.ManualComparison, 
    ParseWaybills = OLD.ParseWaybills, 
    SendRetailMarkup = OLD.SendRetailMarkup, 
    ShowAdvertising = OLD.ShowAdvertising, 
    IgnoreNewPrices = OLD.IgnoreNewPrices, 
    SendWaybillsFromClient = OLD.SendWaybillsFromClient, 
    OnlyParseWaybills = OLD.OnlyParseWaybills, 
    UpdateToTestBuild = OLD.UpdateToTestBuild, 
    EnableSmartOrder = OLD.EnableSmartOrder, 
    BuyingMatrixPriceId = OLD.BuyingMatrixPriceId, 
    BuyingMatrixType = OLD.BuyingMatrixType, 
    WarningOnBuyingMatrix = OLD.WarningOnBuyingMatrix, 
    EnableImpersonalPrice = OLD.EnableImpersonalPrice,
    IsConvertFormat = OLD.IsConvertFormat;
END