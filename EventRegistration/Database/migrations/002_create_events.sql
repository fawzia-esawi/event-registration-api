CREATE TABLE Events (
    Id BIGINT AUTO_INCREMENT PRIMARY KEY ,
    CategoryId BIGINT NOT NULL ,
    Name VARCHAR(150) NOT NULL ,
    Description VARCHAR(1000) ,
    Location VARCHAR(200) NOT NULL ,
    StartAt DATETIME NOT NULL,
    EndAt DATETIME NOT NULL,
    RegistrationDeadline DATETIME NOT NULL,
    Capacity INT NOT NULL,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    UpdatedAt DATETIME NULL,

    CONSTRAINT FK_Events_Categories
        FOREIGN KEY (CategoryId)
        REFERENCES Categories(Id),


    INDEX idx_events_start_at (StartAt),
    INDEX idx_events_is_active (IsActive),
    INDEX idx_events_category_id (CategoryId),

    CHECK (Capacity BETWEEN 1 AND 10000)
)ENGINE=InnoDB
  DEFAULT CHARACTER SET utf8mb4
  COLLATE=utf8mb4_unicode_ci;