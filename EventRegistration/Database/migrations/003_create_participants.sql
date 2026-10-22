CREATE TABLE IF NOT EXISTS Participants (
    Id          BIGINT       NOT NULL AUTO_INCREMENT,
    FullName    VARCHAR(150) NOT NULL,
    Email       VARCHAR(255) NOT NULL,
    Phone       VARCHAR(30)  NOT NULL,
    DateOfBirth DATE         NULL,
    IsActive    TINYINT(1)   NOT NULL DEFAULT 1,
    CreatedAt   DATETIME     NOT NULL DEFAULT (UTC_TIMESTAMP()),
    UpdatedAt   DATETIME     NULL,
    PRIMARY KEY (Id),
    CONSTRAINT UQ_Participants_Email UNIQUE (Email),
    INDEX IX_Participants_FullName (FullName),
    INDEX IX_Participants_IsActive (IsActive)
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci;