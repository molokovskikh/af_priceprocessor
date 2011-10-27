
    create table Documents.ProtekDocs (
        Id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,
       DocId INTEGER not null,
       Line INTEGER UNSIGNED not null,
       constraint `FK_ProtekDocs_DocId` foreign key (Line) references Documents.DocumentBodies(Id) on delete cascade,
       primary key (Id)
    );
