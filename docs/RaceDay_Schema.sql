-- =========================================================
-- RaceDay Database Schema
-- Part 1 - System Planning and Database
-- Run on a clean SQL Server instance using SSMS
-- =========================================================

-- Drop tables if they already exist (clean re-run support)
IF OBJECT_ID('Results', 'U') IS NOT NULL DROP TABLE Results;
IF OBJECT_ID('Enrolments', 'U') IS NOT NULL DROP TABLE Enrolments;
IF OBJECT_ID('Categories', 'U') IS NOT NULL DROP TABLE Categories;
IF OBJECT_ID('Events', 'U') IS NOT NULL DROP TABLE Events;
IF OBJECT_ID('Users', 'U') IS NOT NULL DROP TABLE Users;
IF OBJECT_ID('Roles', 'U') IS NOT NULL DROP TABLE Roles;
GO

-- =========================================================
-- Table: Roles
-- =========================================================
CREATE TABLE Roles (
    RoleId INT IDENTITY(1,1) PRIMARY KEY,
    RoleName VARCHAR(20) NOT NULL UNIQUE
);
GO

-- =========================================================
-- Table: Users (Organisers and Participants share this table)
-- =========================================================
CREATE TABLE Users (
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    RoleId INT NOT NULL,
    FullName VARCHAR(100) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    ContactNumber VARCHAR(20) NULL,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
);
GO

-- =========================================================
-- Table: Events (created by an Organiser)
-- =========================================================
CREATE TABLE Events (
    EventId INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserId INT NOT NULL,
    EventName VARCHAR(150) NOT NULL,
    EventDate DATE NOT NULL,
    Location VARCHAR(150) NOT NULL,
    Description VARCHAR(500) NULL,
    RouteInfo VARCHAR(500) NULL,
    Distance DECIMAL(6,2) NOT NULL,
    EventType VARCHAR(10) NOT NULL,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Events_Users FOREIGN KEY (OrganiserId) REFERENCES Users(UserId),
    CONSTRAINT CK_Events_EventType CHECK (EventType IN ('Run', 'Walk', 'Cycle'))
);
GO

-- =========================================================
-- Table: Categories (belongs to one Event)
-- =========================================================
CREATE TABLE Categories (
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    EventId INT NOT NULL,
    CategoryName VARCHAR(50) NOT NULL,
    MaxParticipants INT NOT NULL DEFAULT 100,
    EntryFee DECIMAL(8,2) NOT NULL DEFAULT 0,
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventId) REFERENCES Events(EventId)
);
GO

-- =========================================================
-- Table: Enrolments (a Participant entering a Category)
-- =========================================================
CREATE TABLE Enrolments (
    EnrolmentId INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantId INT NOT NULL,
    CategoryId INT NOT NULL,
    EnrolmentDate DATETIME NOT NULL DEFAULT GETDATE(),
    Status VARCHAR(20) NOT NULL DEFAULT 'Confirmed',
    CONSTRAINT FK_Enrolments_Users FOREIGN KEY (ParticipantId) REFERENCES Users(UserId),
    CONSTRAINT FK_Enrolments_Categories FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId),
    CONSTRAINT UQ_Enrolments_Participant_Category UNIQUE (ParticipantId, CategoryId)
);
GO

-- =========================================================
-- Table: Results (one result per Enrolment, captured by an Organiser)
-- =========================================================
CREATE TABLE Results (
    ResultId INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentId INT NOT NULL UNIQUE,
    FinishTime TIME NOT NULL,
    Position INT NOT NULL,
    CapturedByUserId INT NOT NULL,
    CapturedDate DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentId) REFERENCES Enrolments(EnrolmentId),
    CONSTRAINT FK_Results_Users FOREIGN KEY (CapturedByUserId) REFERENCES Users(UserId)
);
GO

-- =========================================================
-- Seed Data
-- =========================================================

-- Roles
INSERT INTO Roles (RoleName) VALUES ('Organiser'), ('Participant');
GO

-- Users: 2 Organisers, 2 Participants
INSERT INTO Users (RoleId, FullName, Email, PasswordHash, ContactNumber)
VALUES
    (1, 'Thabo Nkosi', 'thabo.nkosi@raceday.co.za', 'HASHED_PASSWORD_1', '0821234567'),
    (1, 'Lerato Mokoena', 'lerato.mokoena@raceday.co.za', 'HASHED_PASSWORD_2', '0827654321'),
    (2, 'Sipho Dlamini', 'sipho.dlamini@example.com', 'HASHED_PASSWORD_3', '0731234567'),
    (2, 'Naledi Sithole', 'naledi.sithole@example.com', 'HASHED_PASSWORD_4', '0739876543');
GO

-- Events: 3 events, each created by an Organiser
INSERT INTO Events (OrganiserId, EventName, EventDate, Location, Description, RouteInfo, Distance, EventType)
VALUES
    (1, 'Kagiso Community Fun Run', '2026-11-21', 'Kagiso, Gauteng', 'Annual community road running event.', 'Flat loop route starting and ending at Kagiso Stadium.', 10.00, 'Run'),
    (2, 'Pretoria Cycle Classic', '2026-12-05', 'Pretoria, Gauteng', 'Road cycling event through the city and surrounds.', 'Point-to-point route from Pretoria CBD to Hartbeespoort.', 50.00, 'Cycle'),
    (1, 'Soweto Heritage Walk', '2026-09-27', 'Soweto, Gauteng', 'Community walking event celebrating local heritage.', 'Circular walking route through the Vilakazi Street precinct.', 5.00, 'Walk');
GO

-- Categories: age/distance groupings for each event
INSERT INTO Categories (EventId, CategoryName, MaxParticipants, EntryFee)
VALUES
    (1, 'Under 20', 150, 50.00),
    (1, 'Senior', 200, 80.00),
    (2, 'Open', 100, 250.00),
    (2, 'Masters 50+', 80, 200.00),
    (3, 'Open', 300, 30.00);
GO

-- Enrolments: participants entering categories
INSERT INTO Enrolments (ParticipantId, CategoryId, Status)
VALUES
    (3, 1, 'Confirmed'),
    (3, 3, 'Confirmed'),
    (4, 2, 'Confirmed'),
    (4, 5, 'Confirmed'),
    (3, 5, 'Confirmed');
GO

-- Results: captured by the organiser of the (past) walk, one per enrolment
INSERT INTO Results (EnrolmentId, FinishTime, Position, CapturedByUserId)
VALUES
    (4, '00:52:10', 1, 1),
    (5, '00:58:30', 2, 1);
GO
