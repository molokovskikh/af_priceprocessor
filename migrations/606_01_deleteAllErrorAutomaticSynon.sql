DROP TEMPORARY TABLE IF EXISTS farm.AutomaticProducerSynonymsForDelete;

CREATE TEMPORARY TABLE farm.AutomaticProducerSynonymsForDelete (
SynonymId INT unsigned) engine=MEMORY;

INSERT
INTO    farm.AutomaticProducerSynonymsForDelete

SELECT
aps.*
FROM
farm.SynonymFirmCr s
join farm.AutomaticProducerSynonyms aps on aps.ProducerSynonymId = s.SynonymFirmCrCode
where s.CodeFirmCr is not null;


delete from farm.AutomaticProducerSynonyms
where ProducerSynonymId in (select * from farm.AutomaticProducerSynonymsForDelete);