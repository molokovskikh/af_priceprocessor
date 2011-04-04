ALTER TABLE documents.DocumentBodies
CHANGE SummaNds NdsAmount DECIMAL(12, 6) UNSIGNED DEFAULT NULL COMMENT 'Сумма НДС';