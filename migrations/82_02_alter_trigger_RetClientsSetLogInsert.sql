USE usersettings;

DROP TRIGGER IF EXISTS RetClientsSetLogInsert;
CREATE
DEFINER = 'RootDBMS'@'127.0.0.1'
TRIGGER RetClientsSetLogInsert
AFTER INSERT
ON RetClientsSet
FOR EACH ROW
BEGIN
  INSERT INTO `logs`.RetClientsSetLogs
  SET
    LogTime = NOW(), 
    OperatorName = IFNULL(@INUser, SUBSTRING_INDEX(USER(), '@', 1)), 
    OperatorHost = IFNULL(@INHost, SUBSTRING_INDEX(USER(), '@', -1)), 
    Operation = 0, 
    ClientCode = NEW.ClientCode, 
    InvisibleOnFirm = NEW.InvisibleOnFirm, 
    BaseFirmCategory = NEW.BaseFirmCategory, 
    RetUpCost = NEW.RetUpCost, 
    OverCostPercent = NEW.OverCostPercent, 
    DifferenceCalculation = NEW.DifferenceCalculation, 
    AlowRegister = NEW.AlowRegister, 
    AlowRejection = NEW.AlowRejection, 
    AlowDocuments = NEW.AlowDocuments, 
    MultiUserLevel = NEW.MultiUserLevel, 
    AdvertisingLevel = NEW.AdvertisingLevel, 
    AlowWayBill = NEW.AlowWayBill, 
    AllowDocuments = NEW.AllowDocuments, 
    AlowChangeSegment = NEW.AlowChangeSegment, 
    ShowPriceName = NEW.ShowPriceName, 
    WorkRegionMask = NEW.WorkRegionMask, 
    OrderRegionMask = NEW.OrderRegionMask, 
    EnableUpdate = NEW.EnableUpdate, 
    CheckCopyID = NEW.CheckCopyID, 
    AlowCumulativeUpdate = NEW.AlowCumulativeUpdate, 
    CheckCumulativeUpdateStatus = NEW.CheckCumulativeUpdateStatus, 
    ServiceClient = NEW.ServiceClient, 
    SubmitOrders = NEW.SubmitOrders, 
    AllowSubmitOrders = NEW.AllowSubmitOrders, 
    BasecostPassword = NEW.BasecostPassword, 
    OrdersVisualizationMode = NEW.OrdersVisualizationMode, 
    CalculateLeader = NEW.CalculateLeader, 
    AllowPreparatInfo = NEW.AllowPreparatInfo, 
    AllowPreparatDesc = NEW.AllowPreparatDesc, 
    SmartOrderRuleId = NEW.SmartOrderRuleId, 
    FirmCodeOnly = NEW.FirmCodeOnly, 
    MaxWeeklyOrdersSum = NEW.MaxWeeklyOrdersSum, 
    CheckWeeklyOrdersSum = NEW.CheckWeeklyOrdersSum, 
    AllowDelayOfPayment = NEW.AllowDelayOfPayment, 
    Spy = NEW.Spy, SpyAccount = NEW.SpyAccount, 
    ShowNewDefecture = NEW.ShowNewDefecture, 
    MigrateToPrgDataService = NEW.MigrateToPrgDataService, 
    ManualComparison = NEW.ManualComparison, 
    ParseWaybills = NEW.ParseWaybills, 
    SendRetailMarkup = NEW.SendRetailMarkup, 
    ShowAdvertising = NEW.ShowAdvertising, 
    IgnoreNewPrices = NEW.IgnoreNewPrices, 
    SendWaybillsFromClient = NEW.SendWaybillsFromClient, 
    OnlyParseWaybills = NEW.OnlyParseWaybills, 
    UpdateToTestBuild = NEW.UpdateToTestBuild, 
    EnableSmartOrder = NEW.EnableSmartOrder, 
    BuyingMatrixPriceId = NEW.BuyingMatrixPriceId, 
    BuyingMatrixType = NEW.BuyingMatrixType,
    WarningOnBuyingMatrix = NEW.WarningOnBuyingMatrix, 
    EnableImpersonalPrice = NEW.EnableImpersonalPrice,
    IsConvertFormat = NEW.IsConvertFormat;
END
