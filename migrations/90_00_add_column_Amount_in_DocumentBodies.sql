ALTER TABLE documents.DocumentBodies
ADD COLUMN Amount DECIMAL(12, 6) UNSIGNED DEFAULT NULL COMMENT 'Сумма с НДС';