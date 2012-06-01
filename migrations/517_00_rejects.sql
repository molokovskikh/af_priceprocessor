alter table Usersettings.PricesData add column IsRejects TINYINT(1) not null default 0;
alter table Usersettings.PricesData add column IsRejectCancellations TINYINT(1) not null default 0;

    create table Farm.Rejects (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       Product VARCHAR(255),
       ProductId INTEGER UNSIGNED,
       Producer VARCHAR(255),
       ProducerId INTEGER UNSIGNED,
       Series VARCHAR(255),
       LetterNo VARCHAR(255),
       LetterDate DATETIME,
       CauseRejects VARCHAR(255),
       CancelDate DATETIME,
       primary key (Id)
    );
