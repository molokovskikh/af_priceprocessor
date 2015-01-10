update Farm.AutomaticProducerSynonyms a
join Farm.SynonymFirmCr s on s.SynonymFirmCrCode = a.ProducerSynonymId
set a.SynonymText = s.Synonym, a.PriceCode = s.PriceCode
where s.CodeFirmCr is null;
