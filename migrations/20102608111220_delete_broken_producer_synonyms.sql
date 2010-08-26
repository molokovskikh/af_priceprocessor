delete farm.s FROM farm.SynonymFirmCr S
left join farm.AutomaticProducerSynonyms aps on aps.ProducerSynonymId = s.SynonymFirmCrCode
left join farm.Excludes e on e.ProducerSynonym = s.Synonym and e.PriceCode = s.PriceCode
where codefirmcr is null and aps.ProducerSynonymId is null and e.Id is null;
