-- =====================================================================
-- Event Registration System - demonstration data
--
-- Run ONCE, on an EMPTY database that already has migrations
-- 001 to 004 applied (in numeric order).
--
-- All dates are relative to the moment the script runs, so the data
-- always looks "current" (upcoming, past, closed and full events).
--
-- Contents: 5 categories (1 inactive), 8 events, 12 participants
-- (2 inactive), 12 active registrations and 3 cancelled registrations.
-- =====================================================================

-- ---------------------------------------------------------------------
-- Categories
-- ---------------------------------------------------------------------
INSERT INTO Categories (Id, Name, Description, IsActive) VALUES
(1, 'Technology',        'Workshops, meetups and talks about software and technology.', 1),
(2, 'Business',          'Networking, startups and leadership events.',                 1),
(3, 'Arts & Culture',    'Exhibitions, performances and creative workshops.',           1),
(4, 'Health & Wellness', 'Fitness, mindfulness and healthy living sessions.',           1),
(5, 'Archived Topics',   'Old category kept for history.',                              0);

-- ---------------------------------------------------------------------
-- Events
--   1 Cloud Computing Workshop  : FULL (5 of 5 seats taken)
--   2 Startup Pitch Night       : plenty of seats
--   3 Digital Art Exhibition    : plenty of seats
--   4 Mindfulness Morning       : ONE seat left (2 of 3 taken)
--   5 Frontend Fundamentals     : no registrations (can be deleted)
--   6 Leadership Summit         : already finished (past event)
--   7 Wellness Retreat          : INACTIVE event
--   8 Photography Basics        : registration deadline already passed
-- ---------------------------------------------------------------------
INSERT INTO Events
    (Id, CategoryId, Name, Description, Location, StartAt, EndAt, RegistrationDeadline, Capacity, IsActive)
VALUES
(1, 1, 'Cloud Computing Workshop',
    'Hands-on introduction to cloud services and deployment.',
    'Innovation Lab',
    UTC_TIMESTAMP() + INTERVAL 10 DAY,
    UTC_TIMESTAMP() + INTERVAL 10 DAY + INTERVAL 4 HOUR,
    UTC_TIMESTAMP() + INTERVAL 8 DAY,
    5, 1),
(2, 2, 'Startup Pitch Night',
    'Founders pitch their ideas to a panel of mentors and investors.',
    'City Conference Hall',
    UTC_TIMESTAMP() + INTERVAL 14 DAY,
    UTC_TIMESTAMP() + INTERVAL 14 DAY + INTERVAL 3 HOUR,
    UTC_TIMESTAMP() + INTERVAL 12 DAY,
    20, 1),
(3, 3, 'Digital Art Exhibition',
    'A showcase of digital paintings, animation and interactive art.',
    'Cultural Center Gallery',
    UTC_TIMESTAMP() + INTERVAL 21 DAY,
    UTC_TIMESTAMP() + INTERVAL 21 DAY + INTERVAL 6 HOUR,
    UTC_TIMESTAMP() + INTERVAL 19 DAY,
    30, 1),
(4, 4, 'Mindfulness Morning',
    'A calm morning of guided breathing and meditation.',
    'Garden Studio',
    UTC_TIMESTAMP() + INTERVAL 5 DAY,
    UTC_TIMESTAMP() + INTERVAL 5 DAY + INTERVAL 2 HOUR,
    UTC_TIMESTAMP() + INTERVAL 3 DAY,
    3, 1),
(5, 1, 'Frontend Fundamentals Workshop',
    'HTML, CSS and React basics for beginners.',
    'Training Room 2',
    UTC_TIMESTAMP() + INTERVAL 30 DAY,
    UTC_TIMESTAMP() + INTERVAL 30 DAY + INTERVAL 5 HOUR,
    UTC_TIMESTAMP() + INTERVAL 28 DAY,
    25, 1),
(6, 2, 'Leadership Summit',
    'A full-day summit about leading modern teams.',
    'Grand Hotel Ballroom',
    UTC_TIMESTAMP() - INTERVAL 20 DAY,
    UTC_TIMESTAMP() - INTERVAL 20 DAY + INTERVAL 6 HOUR,
    UTC_TIMESTAMP() - INTERVAL 23 DAY,
    40, 1),
(7, 4, 'Wellness Retreat',
    'A weekend retreat (postponed, currently inactive).',
    'Mountain Lodge',
    UTC_TIMESTAMP() + INTERVAL 40 DAY,
    UTC_TIMESTAMP() + INTERVAL 42 DAY,
    UTC_TIMESTAMP() + INTERVAL 38 DAY,
    12, 0),
(8, 3, 'Photography Basics',
    'Learn composition, light and camera settings.',
    'Photo Studio',
    UTC_TIMESTAMP() + INTERVAL 2 DAY,
    UTC_TIMESTAMP() + INTERVAL 2 DAY + INTERVAL 3 HOUR,
    UTC_TIMESTAMP() - INTERVAL 1 DAY,
    10, 1);

-- ---------------------------------------------------------------------
-- Participants (10 active, 2 inactive)
-- ---------------------------------------------------------------------
INSERT INTO Participants (Id, FullName, Email, Phone, DateOfBirth, IsActive) VALUES
(1,  'Ahmad Ali',      'ahmad.ali@example.com',      '0599000001', '1995-03-14', 1),
(2,  'Lina Haddad',    'lina.haddad@example.com',    '0599000002', '1998-07-22', 1),
(3,  'Omar Khalil',    'omar.khalil@example.com',    '0599000003', '1990-11-05', 1),
(4,  'Sara Nasser',    'sara.nasser@example.com',    '0599000004', NULL,         1),
(5,  'Yousef Abbas',   'yousef.abbas@example.com',   '0599000005', '1988-01-30', 1),
(6,  'Maya Saleh',     'maya.saleh@example.com',     '0599000006', '2000-09-18', 1),
(7,  'Khaled Mansour', 'khaled.mansour@example.com', '0599000007', '1993-05-09', 1),
(8,  'Rana Odeh',      'rana.odeh@example.com',      '0599000008', NULL,         1),
(9,  'Tariq Zaid',     'tariq.zaid@example.com',     '0599000009', '1997-12-01', 1),
(10, 'Dina Hamdan',    'dina.hamdan@example.com',    '0599000010', '1994-04-27', 1),
(11, 'Layth Barakat',  'layth.barakat@example.com',  '0599000011', '1985-08-16', 0),
(12, 'Noor Qasem',     'noor.qasem@example.com',     '0599000012', '2001-02-11', 0);

-- ---------------------------------------------------------------------
-- Registrations: 12 active (Status = 1) and 3 cancelled (Status = 2)
-- ---------------------------------------------------------------------
INSERT INTO Registrations
    (EventId, ParticipantId, Status, Notes, RegisteredAt, CancelledAt)
VALUES
-- Event 1: Cloud Computing Workshop (full: 5 active, 1 cancelled)
(1, 1, 1, 'Registered by phone',      UTC_TIMESTAMP() - INTERVAL 9 DAY, NULL),
(1, 2, 1, NULL,                       UTC_TIMESTAMP() - INTERVAL 8 DAY, NULL),
(1, 3, 1, NULL,                       UTC_TIMESTAMP() - INTERVAL 8 DAY, NULL),
(1, 4, 1, 'Needs a power outlet',     UTC_TIMESTAMP() - INTERVAL 6 DAY, NULL),
(1, 5, 1, NULL,                       UTC_TIMESTAMP() - INTERVAL 6 DAY, NULL),
(1, 6, 2, 'Could not attend',         UTC_TIMESTAMP() - INTERVAL 7 DAY, UTC_TIMESTAMP() - INTERVAL 5 DAY),

-- Event 2: Startup Pitch Night (2 active)
(2, 1, 1, NULL,                       UTC_TIMESTAMP() - INTERVAL 5 DAY, NULL),
(2, 7, 1, 'Will bring a guest',       UTC_TIMESTAMP() - INTERVAL 3 DAY, NULL),

-- Event 3: Digital Art Exhibition (1 active)
(3, 2, 1, NULL,                       UTC_TIMESTAMP() - INTERVAL 2 DAY, NULL),

-- Event 4: Mindfulness Morning (2 active, 1 cancelled, one seat left)
(4, 3, 1, NULL,                       UTC_TIMESTAMP() - INTERVAL 4 DAY, NULL),
(4, 10, 1, 'First time attending',    UTC_TIMESTAMP() - INTERVAL 2 DAY, NULL),
(4, 4, 2, 'Schedule conflict',        UTC_TIMESTAMP() - INTERVAL 6 DAY, UTC_TIMESTAMP() - INTERVAL 3 DAY),

-- Event 6: Leadership Summit (past event: 1 active, 1 cancelled before it started)
(6, 5, 1, NULL,                       UTC_TIMESTAMP() - INTERVAL 26 DAY, NULL),
(6, 7, 2, 'Travelling that week',     UTC_TIMESTAMP() - INTERVAL 27 DAY, UTC_TIMESTAMP() - INTERVAL 24 DAY),

-- Event 8: Photography Basics (registration already closed)
(8, 9, 1, 'Bringing own camera',      UTC_TIMESTAMP() - INTERVAL 4 DAY, NULL);