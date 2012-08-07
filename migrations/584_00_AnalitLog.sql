DROP TABLE IF EXISTS `analit`.`Log`;
CREATE TABLE  `analit`.`Log` (
  `Id` int unsigned NOT NULL AUTO_INCREMENT,
  `Date` datetime NOT NULL,
  `Level` varchar(50) NOT NULL,
  `Logger` varchar(255) NOT NULL,
  `Host` varchar(255) DEFAULT NULL,
  `User` varchar(255) DEFAULT NULL,
  `Message` text,
  `Exception` text,
  `Source` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `Date` (`Date`)
) ENGINE=MyISAM AUTO_INCREMENT=2463 DEFAULT CHARSET=cp1251;