DROP FUNCTION Farm.CreateproducerSynonym;
CREATE DEFINER=`RootDBMS`@`127.0.0.1` FUNCTION Farm.`CreateproducerSynonym`( PriceId bigint unsigned,  ProducerId bigint unsigned,  Synonym varchar(255),  IsAutomatic bool ) RETURNS bigint(20) unsigned
    DETERMINISTIC
    SQL SECURITY INVOKER
BEGIN
declare LastProducerSynonymId bigint unsigned;


set LastProducerSynonymId=(select SynonymFirmCrCode from farm.SynonymFirmCr ps where ps.Synonym=Synonym and if(ProducerId is null, ps.CodeFirmCr is null, ps.CodeFirmCr=ProducerId) and ps.PriceCode=PriceId limit 1);


if LastProducerSynonymId is null then

if IsAutomatic then
insert into AutomaticProducerSynonyms(SynonymText, PriceId) values(Synonym, PriceId);
end if;

insert into farm.SynonymFirmCr (PriceCode, CodeFirmCr, Synonym) values (PriceId, ProducerId, Synonym);
set LastProducerSynonymId= last_insert_id();
insert farm.UsedSynonymFirmCrLogs (SynonymFirmCrCode) values (LastProducerSynonymId);

if IsAutomatic then
update farm.AutomaticProducerSynonyms A  set A.ProducerSynonymId=LastProducerSynonymId where A.PriceId=PriceId and A.SynonymText=Synonym;
end if;

end if;

return LastProducerSynonymId;
END;
