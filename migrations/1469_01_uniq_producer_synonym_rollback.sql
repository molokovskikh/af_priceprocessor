DROP FUNCTION Farm.CreateProducerSynonym;
CREATE DEFINER=`RootDBMS`@`127.0.0.1` FUNCTION Farm.`CreateProducerSynonym`( PriceId bigint unsigned,  ProducerId bigint unsigned,  Synonym varchar(255),  IsAutomatic bool ) RETURNS bigint(20) unsigned
    DETERMINISTIC
    SQL SECURITY INVOKER
BEGIN
	declare LastProducerSynonymId bigint unsigned;

	set LastProducerSynonymId = (select SynonymFirmCrCode from farm.SynonymFirmCr ps where ps.Synonym = Synonym and ps.CodeFirmCr <=> ProducerId and ps.PriceCode=PriceId limit 1);


	if LastProducerSynonymId is null then
		insert into farm.SynonymFirmCr (PriceCode, CodeFirmCr, Synonym) values (PriceId, ProducerId, Synonym);
		set LastProducerSynonymId = last_insert_id();
		insert farm.UsedSynonymFirmCrLogs (SynonymFirmCrCode) values (LastProducerSynonymId);

		if IsAutomatic then
			insert into AutomaticProducerSynonyms(ProducerSynonymId) values(LastProducerSynonymId);
		end if;
	end if;

	return LastProducerSynonymId;
END;
