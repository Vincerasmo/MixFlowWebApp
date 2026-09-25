IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706084726_Init'
)
BEGIN
    CREATE TABLE [Leaderboards] (
        [LeaderboardId] int NOT NULL IDENTITY,
        [Type] nvarchar(max) NOT NULL,
        [SessionId] int NULL,
        [GeneratedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Leaderboards] PRIMARY KEY ([LeaderboardId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706084726_Init'
)
BEGIN
    CREATE TABLE [Organizers] (
        [OrganizerId] int NOT NULL IDENTITY,
        [UserId] nvarchar(max) NOT NULL,
        [FullName] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Organizers] PRIMARY KEY ([OrganizerId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706084726_Init'
)
BEGIN
    CREATE TABLE [Players] (
        [PlayerId] int NOT NULL IDENTITY,
        [FullName] nvarchar(max) NOT NULL,
        [SkillCategory] nvarchar(max) NOT NULL,
        [SkillLevel] decimal(18,2) NOT NULL,
        [DUPR] decimal(18,2) NULL,
        [WinPercentage] decimal(18,2) NOT NULL,
        [GamesPlayed] int NOT NULL,
        [TotalWins] int NOT NULL,
        [TotalLosses] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Players] PRIMARY KEY ([PlayerId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706084726_Init'
)
BEGIN
    CREATE TABLE [Sessions] (
        [SessionId] int NOT NULL IDENTITY,
        [OrganizerId] int NOT NULL,
        [SessionName] nvarchar(max) NOT NULL,
        [SessionDate] datetime2 NOT NULL,
        [StartTime] time NOT NULL,
        [EndTime] time NOT NULL,
        [NumberOfCourts] int NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Sessions] PRIMARY KEY ([SessionId]),
        CONSTRAINT [FK_Sessions_Organizers_OrganizerId] FOREIGN KEY ([OrganizerId]) REFERENCES [Organizers] ([OrganizerId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706084726_Init'
)
BEGIN
    CREATE TABLE [LeaderboardEntries] (
        [LeaderboardEntryId] int NOT NULL IDENTITY,
        [LeaderboardId] int NOT NULL,
        [PlayerId] int NOT NULL,
        [WinPercentage] decimal(18,2) NOT NULL,
        [GamesPlayed] int NOT NULL,
        [SkillLevel] decimal(18,2) NOT NULL,
        [Wins] int NOT NULL,
        [Losses] int NOT NULL,
        [Rank] int NOT NULL,
        CONSTRAINT [PK_LeaderboardEntries] PRIMARY KEY ([LeaderboardEntryId]),
        CONSTRAINT [FK_LeaderboardEntries_Leaderboards_LeaderboardId] FOREIGN KEY ([LeaderboardId]) REFERENCES [Leaderboards] ([LeaderboardId]) ON DELETE CASCADE,
        CONSTRAINT [FK_LeaderboardEntries_Players_PlayerId] FOREIGN KEY ([PlayerId]) REFERENCES [Players] ([PlayerId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706084726_Init'
)
BEGIN
    CREATE TABLE [PlayerMatchHistories] (
        [HistoryId] int NOT NULL IDENTITY,
        [PlayerId] int NOT NULL,
        [MatchId] int NOT NULL,
        [PartnerId] int NULL,
        [Opponent1Id] int NULL,
        [Opponent2Id] int NULL,
        [PlayedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_PlayerMatchHistories] PRIMARY KEY ([HistoryId]),
        CONSTRAINT [FK_PlayerMatchHistories_Players_PlayerId] FOREIGN KEY ([PlayerId]) REFERENCES [Players] ([PlayerId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706084726_Init'
)
BEGIN
    CREATE TABLE [Matches] (
        [MatchId] int NOT NULL IDENTITY,
        [SessionId] int NOT NULL,
        [CourtNumber] int NULL,
        [StartTime] datetime2 NULL,
        [EndTime] datetime2 NULL,
        [MatchType] nvarchar(max) NOT NULL,
        [RotationMode] nvarchar(max) NULL,
        [Team1Score] int NULL,
        [Team2Score] int NULL,
        [IsCompleted] bit NOT NULL,
        CONSTRAINT [PK_Matches] PRIMARY KEY ([MatchId]),
        CONSTRAINT [FK_Matches_Sessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [Sessions] ([SessionId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706084726_Init'
)
BEGIN
    CREATE TABLE [QueueEntries] (
        [QueueId] int NOT NULL IDENTITY,
        [SessionId] int NOT NULL,
        [PlayerId] int NOT NULL,
        [CheckInTime] datetime2 NOT NULL,
        [PriorityScore] decimal(18,2) NULL,
        [Status] nvarchar(max) NOT NULL,
        [Position] int NULL,
        CONSTRAINT [PK_QueueEntries] PRIMARY KEY ([QueueId]),
        CONSTRAINT [FK_QueueEntries_Players_PlayerId] FOREIGN KEY ([PlayerId]) REFERENCES [Players] ([PlayerId]) ON DELETE CASCADE,
        CONSTRAINT [FK_QueueEntries_Sessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [Sessions] ([SessionId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706084726_Init'
)
BEGIN
    CREATE TABLE [SessionPlayers] (
        [SessionPlayerId] int NOT NULL IDENTITY,
        [SessionId] int NOT NULL,
        [PlayerId] int NOT NULL,
        [CheckInTime] datetime2 NULL,
        [Status] nvarchar(max) NOT NULL,
        [BenchReason] nvarchar(max) NULL,
        [BenchedAt] datetime2 NULL,
        CONSTRAINT [PK_SessionPlayers] PRIMARY KEY ([SessionPlayerId]),
        CONSTRAINT [FK_SessionPlayers_Players_PlayerId] FOREIGN KEY ([PlayerId]) REFERENCES [Players] ([PlayerId]) ON DELETE CASCADE,
        CONSTRAINT [FK_SessionPlayers_Sessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [Sessions] ([SessionId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706084726_Init'
)
BEGIN
    CREATE TABLE [MatchPlayers] (
        [MatchPlayerId] int NOT NULL IDENTITY,
        [MatchId] int NOT NULL,
        [PlayerId] int NOT NULL,
        [TeamNumber] int NOT NULL,
        [IsWinner] bit NULL,
        CONSTRAINT [PK_MatchPlayers] PRIMARY KEY ([MatchPlayerId]),
        CONSTRAINT [FK_MatchPlayers_Matches_MatchId] FOREIGN KEY ([MatchId]) REFERENCES [Matches] ([MatchId]) ON DELETE CASCADE,
        CONSTRAINT [FK_MatchPlayers_Players_PlayerId] FOREIGN KEY ([PlayerId]) REFERENCES [Players] ([PlayerId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706084726_Init'
)
BEGIN
    CREATE INDEX [IX_LeaderboardEntries_LeaderboardId] ON [LeaderboardEntries] ([LeaderboardId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706084726_Init'
)
BEGIN
    CREATE INDEX [IX_LeaderboardEntries_PlayerId] ON [LeaderboardEntries] ([PlayerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706084726_Init'
)
BEGIN
    CREATE INDEX [IX_Matches_SessionId] ON [Matches] ([SessionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706084726_Init'
)
BEGIN
    CREATE INDEX [IX_MatchPlayers_MatchId] ON [MatchPlayers] ([MatchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706084726_Init'
)
BEGIN
    CREATE INDEX [IX_MatchPlayers_PlayerId] ON [MatchPlayers] ([PlayerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706084726_Init'
)
BEGIN
    CREATE INDEX [IX_PlayerMatchHistories_PlayerId_PlayedAt] ON [PlayerMatchHistories] ([PlayerId], [PlayedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706084726_Init'
)
BEGIN
    CREATE INDEX [IX_QueueEntries_PlayerId] ON [QueueEntries] ([PlayerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706084726_Init'
)
BEGIN
    CREATE UNIQUE INDEX [IX_QueueEntries_SessionId_PlayerId] ON [QueueEntries] ([SessionId], [PlayerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706084726_Init'
)
BEGIN
    CREATE INDEX [IX_SessionPlayers_PlayerId] ON [SessionPlayers] ([PlayerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706084726_Init'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SessionPlayers_SessionId_PlayerId] ON [SessionPlayers] ([SessionId], [PlayerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706084726_Init'
)
BEGIN
    CREATE INDEX [IX_Sessions_OrganizerId] ON [Sessions] ([OrganizerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706084726_Init'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260706084726_Init', N'9.0.17');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706085551_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260706085551_InitialCreate', N'9.0.17');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706142329_UpdatePlayerSkillCategoryAndLevel'
)
BEGIN
    DECLARE @var sysname;
    SELECT @var = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Players]') AND [c].[name] = N'DUPR');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Players] DROP CONSTRAINT [' + @var + '];');
    ALTER TABLE [Players] DROP COLUMN [DUPR];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260706142329_UpdatePlayerSkillCategoryAndLevel'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260706142329_UpdatePlayerSkillCategoryAndLevel', N'9.0.17');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260712055048_AddLockedPartnerToSessionPlayer'
)
BEGIN
    ALTER TABLE [SessionPlayers] ADD [LockedPartnerId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260712055048_AddLockedPartnerToSessionPlayer'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260712055048_AddLockedPartnerToSessionPlayer', N'9.0.17');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260714060730_AddMatchStatus'
)
BEGIN
    ALTER TABLE [Matches] ADD [Status] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260714060730_AddMatchStatus'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260714060730_AddMatchStatus', N'9.0.17');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723053219_RemoveLeaderboardSnapshotFeature'
)
BEGIN
    DROP TABLE [LeaderboardEntries];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723053219_RemoveLeaderboardSnapshotFeature'
)
BEGIN
    DROP TABLE [Leaderboards];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723053219_RemoveLeaderboardSnapshotFeature'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260723053219_RemoveLeaderboardSnapshotFeature', N'9.0.17');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904092656_AddOrganizerIdToPlayer'
)
BEGIN
    ALTER TABLE [Players] ADD [OrganizerId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904092656_AddOrganizerIdToPlayer'
)
BEGIN

            UPDATE Players
            SET OrganizerId = (SELECT MIN(OrganizerId) FROM Organizers)
            WHERE OrganizerId IS NULL;
        
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904092656_AddOrganizerIdToPlayer'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Players]') AND [c].[name] = N'OrganizerId');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Players] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [Players] ALTER COLUMN [OrganizerId] int NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904092656_AddOrganizerIdToPlayer'
)
BEGIN
    CREATE INDEX [IX_Players_OrganizerId] ON [Players] ([OrganizerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904092656_AddOrganizerIdToPlayer'
)
BEGIN
    ALTER TABLE [Players] ADD CONSTRAINT [FK_Players_Organizers_OrganizerId] FOREIGN KEY ([OrganizerId]) REFERENCES [Organizers] ([OrganizerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904092656_AddOrganizerIdToPlayer'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260904092656_AddOrganizerIdToPlayer', N'9.0.17');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904094018_SyncOrganizerPlayerFkBehavior'
)
BEGIN
    ALTER TABLE [Players] DROP CONSTRAINT [FK_Players_Organizers_OrganizerId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904094018_SyncOrganizerPlayerFkBehavior'
)
BEGIN
    ALTER TABLE [Players] ADD CONSTRAINT [FK_Players_Organizers_OrganizerId] FOREIGN KEY ([OrganizerId]) REFERENCES [Organizers] ([OrganizerId]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260904094018_SyncOrganizerPlayerFkBehavior'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260904094018_SyncOrganizerPlayerFkBehavior', N'9.0.17');
END;

COMMIT;
GO

