CREATE TABLE Registrations(
    Id BIGINT AUTO_INCREMENT PRIMARY KEY ,
    EventId BIGINT NOT NULL ,
    ParticipantId BIGINT NOT NULL ,
    Status TINYINT NOT NULL ,
    Notes VARCHAR(500) ,
    RegisteredAt DATETIME NOT NULL DEFAULT (UTC_TIMESTAMP()),
    CancelledAt DATETIME  ,

    CONSTRAINT FK_Registration_Events
        FOREIGN KEY (EventId)
        REFERENCES Events(Id),
    CONSTRAINT FK_Registration_Participants
        FOREIGN KEY (ParticipantId)
        REFERENCES Participants(Id),
    CONSTRAINT UQ_Registrations_Event_Participant
        UNIQUE (EventId, ParticipantId),
    CONSTRAINT CK_Registrations_Status 
        CHECK (Status IN (1, 2)),
    INDEX IX_Registrations_EventId (EventId),
    INDEX IX_Registrations_ParticipantId (ParticipantId),
    INDEX IX_Registrations_Status (Status)
)