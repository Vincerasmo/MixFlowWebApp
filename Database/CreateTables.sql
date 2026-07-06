-- MixFlow Database Schema
-- This script creates all tables based on the entity models

-- Create Organizer Table
CREATE TABLE [Organizers] (
    [OrganizerId] INT PRIMARY KEY IDENTITY(1,1),
    [UserId] NVARCHAR(450) NOT NULL,
    [FullName] NVARCHAR(MAX) NOT NULL,
    [Email] NVARCHAR(MAX) NOT NULL,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Create Player Table
CREATE TABLE [Players] (
    [PlayerId] INT PRIMARY KEY IDENTITY(1,1),
    [FullName] NVARCHAR(MAX) NOT NULL,
    [SkillCategory] NVARCHAR(MAX) NOT NULL,
    [SkillLevel] DECIMAL(18,2) NOT NULL,
    [DUPR] DECIMAL(18,2) NULL,
    [WinPercentage] DECIMAL(18,2) NOT NULL DEFAULT 50.00,
    [GamesPlayed] INT NOT NULL DEFAULT 0,
    [TotalWins] INT NOT NULL DEFAULT 0,
    [TotalLosses] INT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL
);

-- Create Session Table
CREATE TABLE [Sessions] (
    [SessionId] INT PRIMARY KEY IDENTITY(1,1),
    [OrganizerId] INT NOT NULL,
    [SessionName] NVARCHAR(MAX) NOT NULL,
    [SessionDate] DATETIME2 NOT NULL,
    [StartTime] TIME NOT NULL,
    [EndTime] TIME NOT NULL,
    [NumberOfCourts] INT NOT NULL DEFAULT 4,
    [Status] NVARCHAR(MAX) NOT NULL DEFAULT 'Active',
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NULL,
    CONSTRAINT [FK_Sessions_Organizers] FOREIGN KEY ([OrganizerId]) REFERENCES [Organizers]([OrganizerId]) ON DELETE CASCADE
);

-- Create SessionPlayer Table (Junction Table)
CREATE TABLE [SessionPlayers] (
    [SessionPlayerId] INT PRIMARY KEY IDENTITY(1,1),
    [SessionId] INT NOT NULL,
    [PlayerId] INT NOT NULL,
    [CheckInTime] DATETIME2 NULL,
    [Status] NVARCHAR(MAX) NOT NULL DEFAULT 'Registered',
    [BenchReason] NVARCHAR(MAX) NULL,
    [BenchedAt] DATETIME2 NULL,
    CONSTRAINT [FK_SessionPlayers_Sessions] FOREIGN KEY ([SessionId]) REFERENCES [Sessions]([SessionId]) ON DELETE CASCADE,
    CONSTRAINT [FK_SessionPlayers_Players] FOREIGN KEY ([PlayerId]) REFERENCES [Players]([PlayerId]) ON DELETE CASCADE,
    CONSTRAINT [UQ_SessionPlayer_SessionId_PlayerId] UNIQUE ([SessionId], [PlayerId])
);

-- Create QueueEntry Table
CREATE TABLE [QueueEntries] (
    [QueueId] INT PRIMARY KEY IDENTITY(1,1),
    [SessionId] INT NOT NULL,
    [PlayerId] INT NOT NULL,
    [CheckInTime] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [PriorityScore] DECIMAL(18,2) NULL,
    [Status] NVARCHAR(MAX) NOT NULL DEFAULT 'Waiting',
    [Position] INT NULL,
    CONSTRAINT [FK_QueueEntries_Sessions] FOREIGN KEY ([SessionId]) REFERENCES [Sessions]([SessionId]) ON DELETE CASCADE,
    CONSTRAINT [FK_QueueEntries_Players] FOREIGN KEY ([PlayerId]) REFERENCES [Players]([PlayerId]) ON DELETE CASCADE,
    CONSTRAINT [UQ_QueueEntry_SessionId_PlayerId] UNIQUE ([SessionId], [PlayerId])
);

-- Create Match Table
CREATE TABLE [Matches] (
    [MatchId] INT PRIMARY KEY IDENTITY(1,1),
    [SessionId] INT NOT NULL,
    [CourtNumber] INT NULL,
    [StartTime] DATETIME2 NULL,
    [EndTime] DATETIME2 NULL,
    [MatchType] NVARCHAR(MAX) NOT NULL DEFAULT 'Doubles',
    [RotationMode] NVARCHAR(MAX) NULL,
  [Team1Score] INT NULL,
    [Team2Score] INT NULL,
    [IsCompleted] BIT NOT NULL DEFAULT 0,
    CONSTRAINT [FK_Matches_Sessions] FOREIGN KEY ([SessionId]) REFERENCES [Sessions]([SessionId]) ON DELETE CASCADE
);

-- Create MatchPlayer Table (Junction Table)
CREATE TABLE [MatchPlayers] (
    [MatchPlayerId] INT PRIMARY KEY IDENTITY(1,1),
    [MatchId] INT NOT NULL,
    [PlayerId] INT NOT NULL,
    [TeamNumber] INT NOT NULL,
    [IsWinner] BIT NULL,
    CONSTRAINT [FK_MatchPlayers_Matches] FOREIGN KEY ([MatchId]) REFERENCES [Matches]([MatchId]) ON DELETE CASCADE,
    CONSTRAINT [FK_MatchPlayers_Players] FOREIGN KEY ([PlayerId]) REFERENCES [Players]([PlayerId]) ON DELETE CASCADE
);

-- Create PlayerMatchHistory Table
CREATE TABLE [PlayerMatchHistories] (
    [HistoryId] INT PRIMARY KEY IDENTITY(1,1),
[PlayerId] INT NOT NULL,
    [MatchId] INT NOT NULL,
    [PartnerId] INT NULL,
    [Opponent1Id] INT NULL,
    [Opponent2Id] INT NULL,
    [PlayedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [FK_PlayerMatchHistories_Players] FOREIGN KEY ([PlayerId]) REFERENCES [Players]([PlayerId]) ON DELETE CASCADE,
    CONSTRAINT [FK_PlayerMatchHistories_Matches] FOREIGN KEY ([MatchId]) REFERENCES [Matches]([MatchId])
);

-- Create Leaderboard Table
CREATE TABLE [Leaderboards] (
    [LeaderboardId] INT PRIMARY KEY IDENTITY(1,1),
    [Type] NVARCHAR(50) NOT NULL,          -- "Session" or "Overall"
    [SessionId] INT NULL,                  -- FK to Sessions if session leaderboard
    [GeneratedAt] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [FK_Leaderboards_Sessions] FOREIGN KEY ([SessionId]) REFERENCES [Sessions]([SessionId]) ON DELETE CASCADE
);

-- Create LeaderboardEntry Table
CREATE TABLE [LeaderboardEntries] (
    [LeaderboardEntryId] INT PRIMARY KEY IDENTITY(1,1),
    [LeaderboardId] INT NOT NULL,          -- FK to Leaderboards
    [PlayerId] INT NOT NULL,               -- FK to Players
    [WinPercentage] DECIMAL(18,2) NOT NULL,
    [GamesPlayed] INT NOT NULL,
    [SkillLevel] DECIMAL(18,2) NOT NULL,
    [Wins] INT NOT NULL,
    [Losses] INT NOT NULL,
    [Rank] INT NOT NULL,

    CONSTRAINT [FK_LeaderboardEntries_Leaderboards] FOREIGN KEY ([LeaderboardId]) REFERENCES [Leaderboards]([LeaderboardId]) ON DELETE CASCADE,
    CONSTRAINT [FK_LeaderboardEntries_Players] FOREIGN KEY ([PlayerId]) REFERENCES [Players]([PlayerId]) ON DELETE CASCADE
);

-- Create Indexes for Performance
CREATE INDEX [IX_Sessions_OrganizerId] ON [Sessions]([OrganizerId]);
CREATE INDEX [IX_SessionPlayers_SessionId_PlayerId] ON [SessionPlayers]([SessionId], [PlayerId]);
CREATE INDEX [IX_QueueEntries_SessionId_PlayerId] ON [QueueEntries]([SessionId], [PlayerId]);
CREATE INDEX [IX_PlayerMatchHistories_PlayerId_PlayedAt] ON [PlayerMatchHistories]([PlayerId], [PlayedAt]);
CREATE INDEX [IX_Matches_SessionId] ON [Matches]([SessionId]);
CREATE INDEX [IX_MatchPlayers_MatchId] ON [MatchPlayers]([MatchId]);
CREATE INDEX [IX_MatchPlayers_PlayerId] ON [MatchPlayers]([PlayerId]);
CREATE INDEX [IX_Leaderboards_SessionId] ON [Leaderboards]([SessionId]);
CREATE INDEX [IX_LeaderboardEntries_LeaderboardId] ON [LeaderboardEntries]([LeaderboardId]);
CREATE INDEX [IX_LeaderboardEntries_PlayerId] ON [LeaderboardEntries]([PlayerId]);