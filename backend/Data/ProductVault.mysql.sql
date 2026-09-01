CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

ALTER DATABASE CHARACTER SET utf8mb4;

CREATE TABLE `AspNetRoles` (
    `Id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Name` varchar(256) CHARACTER SET utf8mb4 NULL,
    `NormalizedName` varchar(256) CHARACTER SET utf8mb4 NULL,
    `ConcurrencyStamp` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_AspNetRoles` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `AspNetUsers` (
    `Id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `UserName` varchar(256) CHARACTER SET utf8mb4 NULL,
    `NormalizedUserName` varchar(256) CHARACTER SET utf8mb4 NULL,
    `Email` varchar(256) CHARACTER SET utf8mb4 NULL,
    `NormalizedEmail` varchar(256) CHARACTER SET utf8mb4 NULL,
    `EmailConfirmed` tinyint(1) NOT NULL,
    `PasswordHash` longtext CHARACTER SET utf8mb4 NULL,
    `SecurityStamp` longtext CHARACTER SET utf8mb4 NULL,
    `ConcurrencyStamp` longtext CHARACTER SET utf8mb4 NULL,
    `PhoneNumber` longtext CHARACTER SET utf8mb4 NULL,
    `PhoneNumberConfirmed` tinyint(1) NOT NULL,
    `TwoFactorEnabled` tinyint(1) NOT NULL,
    `LockoutEnd` datetime(6) NULL,
    `LockoutEnabled` tinyint(1) NOT NULL,
    `AccessFailedCount` int NOT NULL,
    CONSTRAINT `PK_AspNetUsers` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Categories` (
    `CategoryId` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `CategoryCode` varchar(6) CHARACTER SET utf8mb4 NOT NULL,
    `IsActive` tinyint(1) NOT NULL,
    `OwnerId` varchar(450) CHARACTER SET utf8mb4 NOT NULL,
    `CreatedBy` varchar(450) CHARACTER SET utf8mb4 NOT NULL,
    `CreatedDate` datetime(6) NOT NULL,
    `UpdatedBy` varchar(450) CHARACTER SET utf8mb4 NULL,
    `UpdatedDate` datetime(6) NULL,
    `RowVersion` timestamp(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    CONSTRAINT `PK_Categories` PRIMARY KEY (`CategoryId`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `AspNetRoleClaims` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `RoleId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ClaimType` longtext CHARACTER SET utf8mb4 NULL,
    `ClaimValue` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_AspNetRoleClaims` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_AspNetRoleClaims_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `AspNetUserClaims` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `ClaimType` longtext CHARACTER SET utf8mb4 NULL,
    `ClaimValue` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_AspNetUserClaims` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_AspNetUserClaims_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `AspNetUserLogins` (
    `LoginProvider` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
    `ProviderKey` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
    `ProviderDisplayName` longtext CHARACTER SET utf8mb4 NULL,
    `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_AspNetUserLogins` PRIMARY KEY (`LoginProvider`, `ProviderKey`),
    CONSTRAINT `FK_AspNetUserLogins_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `AspNetUserRoles` (
    `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `RoleId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_AspNetUserRoles` PRIMARY KEY (`UserId`, `RoleId`),
    CONSTRAINT `FK_AspNetUserRoles_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_AspNetUserRoles_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `AspNetUserTokens` (
    `UserId` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `LoginProvider` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
    `Name` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
    `Value` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_AspNetUserTokens` PRIMARY KEY (`UserId`, `LoginProvider`, `Name`),
    CONSTRAINT `FK_AspNetUserTokens_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `Products` (
    `ProductId` int NOT NULL AUTO_INCREMENT,
    `ProductCode` varchar(10) CHARACTER SET utf8mb4 NOT NULL,
    `Name` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `Description` varchar(2000) CHARACTER SET utf8mb4 NULL,
    `Price` decimal(18,2) NOT NULL,
    `ImagePath` longtext CHARACTER SET utf8mb4 NULL,
    `CategoryId` int NOT NULL,
    `OwnerId` varchar(450) CHARACTER SET utf8mb4 NOT NULL,
    `CreatedBy` varchar(450) CHARACTER SET utf8mb4 NOT NULL,
    `CreatedDate` datetime(6) NOT NULL,
    `UpdatedBy` varchar(450) CHARACTER SET utf8mb4 NULL,
    `UpdatedDate` datetime(6) NULL,
    `RowVersion` timestamp(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
    CONSTRAINT `PK_Products` PRIMARY KEY (`ProductId`),
    CONSTRAINT `FK_Products_Categories_CategoryId` FOREIGN KEY (`CategoryId`) REFERENCES `Categories` (`CategoryId`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_AspNetRoleClaims_RoleId` ON `AspNetRoleClaims` (`RoleId`);

CREATE UNIQUE INDEX `RoleNameIndex` ON `AspNetRoles` (`NormalizedName`);

CREATE INDEX `IX_AspNetUserClaims_UserId` ON `AspNetUserClaims` (`UserId`);

CREATE INDEX `IX_AspNetUserLogins_UserId` ON `AspNetUserLogins` (`UserId`);

CREATE INDEX `IX_AspNetUserRoles_RoleId` ON `AspNetUserRoles` (`RoleId`);

CREATE INDEX `EmailIndex` ON `AspNetUsers` (`NormalizedEmail`);

CREATE UNIQUE INDEX `UserNameIndex` ON `AspNetUsers` (`NormalizedUserName`);

CREATE UNIQUE INDEX `IX_Categories_OwnerId_CategoryCode` ON `Categories` (`OwnerId`, `CategoryCode`);

CREATE INDEX `IX_Products_CategoryId` ON `Products` (`CategoryId`);

CREATE INDEX `IX_Products_OwnerId_CategoryId` ON `Products` (`OwnerId`, `CategoryId`);

CREATE UNIQUE INDEX `IX_Products_ProductCode` ON `Products` (`ProductCode`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260829104814_InitialMySql', '8.0.13');

COMMIT;

START TRANSACTION;

ALTER TABLE `AspNetUserTokens` MODIFY COLUMN `Name` varchar(255) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `AspNetUserTokens` MODIFY COLUMN `LoginProvider` varchar(255) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `AspNetUserLogins` MODIFY COLUMN `ProviderKey` varchar(255) CHARACTER SET utf8mb4 NOT NULL;

ALTER TABLE `AspNetUserLogins` MODIFY COLUMN `LoginProvider` varchar(255) CHARACTER SET utf8mb4 NOT NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260830163102_AlignIdentityKeyColumnLengths', '8.0.13');

COMMIT;

START TRANSACTION;

ALTER TABLE `AspNetUsers` ADD `FirstName` varchar(100) CHARACTER SET utf8mb4 NULL;

ALTER TABLE `AspNetUsers` ADD `Surname` varchar(100) CHARACTER SET utf8mb4 NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260901084750_AddApplicationUserProfile', '8.0.13');

COMMIT;

