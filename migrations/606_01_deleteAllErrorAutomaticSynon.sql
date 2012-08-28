DROP TEMPORARY TABLE IF EXISTS farm.AutomaticProducerSynonymsForDelete;

CREATE TEMPORARY TABLE farm.AutomaticProducerSynonymsForDelete (
SynonymId INT unsigned) engine=MEMORY;

INSERT
INTO    farm.AutomaticProducerSynonymsForDelete

SELECT
aps.*
FROM
farm.SynonymFirmCr s
join catalogs.assortment a on a.ProducerId = s.CodeFirmCr
join farm.AutomaticProducerSynonyms aps on aps.ProducerSynonymId = s.SynonymFirmCrCode
group by s.SynonymFirmCrCode
having count(a.ProducerId) = 1;


delete from farm.AutomaticProducerSynonyms
where ProducerSynonymId in (select * from farm.AutomaticProducerSynonymsForDelete);