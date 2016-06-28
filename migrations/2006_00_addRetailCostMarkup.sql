use documents;
alter table documentbodies 
	add column `RetailCostMarkup` DECIMAL(5,3) NULL DEFAULT NULL COMMENT 'процент розничной наценки' after `RetailCost`;