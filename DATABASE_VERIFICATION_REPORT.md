# Database Table Verification Report

## ? Overall Status: **ALL TABLES ARE CORRECT**

Your database schema matches your models perfectly! Here's a detailed breakdown:

---

## Table-by-Table Verification

### 1. **Organizers Table** ?
| Model Property | SQL Column | Type | Nullable | Match |
|---|---|---|---|---|
| OrganizerId | [OrganizerId] | INT PRIMARY KEY | ? | ? |
| UserId | [UserId] | NVARCHAR(450) | ? | ? |
| FullName | [FullName] | NVARCHAR(MAX) | ? | ? |
| Email | [Email] | NVARCHAR(MAX) | ? | ? |
| CreatedAt | [CreatedAt] | DATETIME2 DEFAULT GETUTCDATE() | ? | ? |

**Relationships:** ? Cascade delete to Sessions

---

### 2. **Players Table** ?
| Model Property | SQL Column | Type | Nullable | Match |
|---|---|---|---|---|
| PlayerId | [PlayerId] | INT PRIMARY KEY | ? | ? |
| FullName | [FullName] | NVARCHAR(MAX) | ? | ? |
| SkillCategory | [SkillCategory] | NVARCHAR(MAX) | ? | ? |
| SkillLevel | [SkillLevel] | DECIMAL(18,2) | ? | ? |
| DUPR | [DUPR] | DECIMAL(18,2) | ? | ? |
| WinPercentage | [WinPercentage] | DECIMAL(18,2) DEFAULT 50.00 | ? | ? |
| GamesPlayed | [GamesPlayed] | INT DEFAULT 0 | ? | ? |
| TotalWins | [TotalWins] | INT DEFAULT 0 | ? | ? |
| CreatedAt | [CreatedAt] | DATETIME2 DEFAULT GETUTCDATE() | ? | ? |
| UpdatedAt | [UpdatedAt] | DATETIME2 | ? | ? |

**Relationships:** ? Cascade delete to SessionPlayers, QueueEntries, MatchHistory, MatchPlayers

---

### 3. **Sessions Table** ?
| Model Property | SQL Column | Type | Nullable | Match |
|---|---|---|---|---|
| SessionId | [SessionId] | INT PRIMARY KEY | ? | ? |
| OrganizerId | [OrganizerId] | INT FK | ? | ? |
| SessionName | [SessionName] | NVARCHAR(MAX) | ? | ? |
| SessionDate | [SessionDate] | DATETIME2 | ? | ? |
| StartTime | [StartTime] | TIME | ? | ? |
| EndTime | [EndTime] | TIME | ? | ? |
| NumberOfCourts | [NumberOfCourts] | INT DEFAULT 4 | ? | ? |
| Status | [Status] | NVARCHAR(MAX) DEFAULT 'Active' | ? | ? |
| CreatedAt | [CreatedAt] | DATETIME2 DEFAULT GETUTCDATE() | ? | ? |
| UpdatedAt | [UpdatedAt] | DATETIME2 | ? | ? |

**Relationships:** ? FK to Organizers (CASCADE), Cascade delete to SessionPlayers and Matches

---

### 4. **SessionPlayers Table** ?
| Model Property | SQL Column | Type | Nullable | Match |
|---|---|---|---|---|
| SessionPlayerId | [SessionPlayerId] | INT PRIMARY KEY | ? | ? |
| SessionId | [SessionId] | INT FK | ? | ? |
| PlayerId | [PlayerId] | INT FK | ? | ? |
| CheckInTime | [CheckInTime] | DATETIME2 | ? | ? |
| Status | [Status] | NVARCHAR(MAX) DEFAULT 'Registered' | ? | ? |
| BenchReason | [BenchReason] | NVARCHAR(MAX) | ? | ? |
| BenchedAt | [BenchedAt] | DATETIME2 | ? | ? |

**Constraints:** ? Unique index on (SessionId, PlayerId)
**Relationships:** ? Cascade delete from Sessions and Players

---

### 5. **QueueEntries Table** ?
| Model Property | SQL Column | Type | Nullable | Match |
|---|---|---|---|---|
| QueueId | [QueueId] | INT PRIMARY KEY | ? | ? |
| SessionId | [SessionId] | INT FK | ? | ? |
| PlayerId | [PlayerId] | INT FK | ? | ? |
| CheckInTime | [CheckInTime] | DATETIME2 DEFAULT GETUTCDATE() | ? | ? |
| PriorityScore | [PriorityScore] | DECIMAL(18,2) | ? | ? |
| Status | [Status] | NVARCHAR(MAX) DEFAULT 'Waiting' | ? | ? |
| Position | [Position] | INT | ? | ? |

**Constraints:** ? Unique index on (SessionId, PlayerId)
**Relationships:** ? Cascade delete from Sessions and Players

---

### 6. **Matches Table** ?
| Model Property | SQL Column | Type | Nullable | Match |
|---|---|---|---|---|
| MatchId | [MatchId] | INT PRIMARY KEY | ? | ? |
| SessionId | [SessionId] | INT FK | ? | ? |
| CourtNumber | [CourtNumber] | INT | ? | ? |
| StartTime | [StartTime] | DATETIME2 | ? | ? |
| EndTime | [EndTime] | DATETIME2 | ? | ? |
| MatchType | [MatchType] | NVARCHAR(MAX) DEFAULT 'Doubles' | ? | ? |
| RotationMode | [RotationMode] | NVARCHAR(MAX) | ? | ? |
| Team1Score | [Team1Score] | INT | ? | ? |
| Team2Score | [Team2Score] | INT | ? | ? |
| IsCompleted | [IsCompleted] | BIT DEFAULT 0 | ? | ? |

**Relationships:** ? FK to Sessions (CASCADE), Cascade delete to MatchPlayers

---

### 7. **MatchPlayers Table** ?
| Model Property | SQL Column | Type | Nullable | Match |
|---|---|---|---|---|
| MatchPlayerId | [MatchPlayerId] | INT PRIMARY KEY | ? | ? |
| MatchId | [MatchId] | INT FK | ? | ? |
| PlayerId | [PlayerId] | INT FK | ? | ? |
| TeamNumber | [TeamNumber] | INT | ? | ? |
| IsWinner | [IsWinner] | BIT | ? | ? |

**Relationships:** ? Cascade delete from Matches and Players

---

### 8. **PlayerMatchHistories Table** ?
| Model Property | SQL Column | Type | Nullable | Match |
|---|---|---|---|---|
| HistoryId | [HistoryId] | INT PRIMARY KEY | ? | ? |
| PlayerId | [PlayerId] | INT FK | ? | ? |
| MatchId | [MatchId] | INT FK | ? | ? |
| PartnerId | [PartnerId] | INT | ? | ? |
| Opponent1Id | [Opponent1Id] | INT | ? | ? |
| Opponent2Id | [Opponent2Id] | INT | ? | ? |
| PlayedAt | [PlayedAt] | DATETIME2 DEFAULT GETUTCDATE() | ? | ? |

**Relationships:** ? Cascade delete from Players, FK to Matches (no cascade)
**Index:** ? Composite index on (PlayerId, PlayedAt)

---

## Indexes Summary ?

| Index Name | Table | Columns | Type |
|---|---|---|---|
| IX_Sessions_OrganizerId | Sessions | OrganizerId | Single |
| IX_SessionPlayers_SessionId_PlayerId | SessionPlayers | SessionId, PlayerId | Composite |
| IX_QueueEntries_SessionId_PlayerId | QueueEntries | SessionId, PlayerId | Composite |
| IX_PlayerMatchHistories_PlayerId_PlayedAt | PlayerMatchHistories | PlayerId, PlayedAt | Composite |
| IX_Matches_SessionId | Matches | SessionId | Single |
| IX_MatchPlayers_MatchId | MatchPlayers | MatchId | Single |
| IX_MatchPlayers_PlayerId | MatchPlayers | PlayerId | Single |

All indexes are properly created for query optimization ?

---

## Relationship Verification ?

### Cascade Delete Chains:
1. **Organizer ? Sessions** ?
2. **Sessions ? SessionPlayers** ?
3. **Sessions ? Matches** ?
4. **Sessions ? QueueEntries** ?
5. **Players ? SessionPlayers** ?
6. **Players ? QueueEntries** ?
7. **Players ? MatchPlayers** ?
8. **Players ? PlayerMatchHistories** ?
9. **Matches ? MatchPlayers** ?

### Foreign Keys:
- Sessions.OrganizerId ? Organizers.OrganizerId ?
- SessionPlayers.SessionId ? Sessions.SessionId ?
- SessionPlayers.PlayerId ? Players.PlayerId ?
- QueueEntries.SessionId ? Sessions.SessionId ?
- QueueEntries.PlayerId ? Players.PlayerId ?
- Matches.SessionId ? Sessions.SessionId ?
- MatchPlayers.MatchId ? Matches.MatchId ?
- MatchPlayers.PlayerId ? Players.PlayerId ?
- PlayerMatchHistories.PlayerId ? Players.PlayerId ?
- PlayerMatchHistories.MatchId ? Matches.MatchId ?

---

## Data Type Mapping ?

| C# Type | SQL Type | Match |
|---|---|---|
| int | INT | ? |
| string | NVARCHAR(MAX) | ? |
| decimal | DECIMAL(18,2) | ? |
| decimal? | DECIMAL(18,2) NULL | ? |
| bool | BIT | ? |
| bool? | BIT NULL | ? |
| DateTime | DATETIME2 | ? |
| DateTime? | DATETIME2 NULL | ? |
| TimeSpan | TIME | ? |

---

## Unique Constraints ?

- **SessionPlayers:** (SessionId, PlayerId) - Prevents duplicate player registrations ?
- **QueueEntries:** (SessionId, PlayerId) - Prevents duplicate queue entries ?

---

## Summary

| Category | Status | Details |
|---|---|---|
| **Table Structure** | ? CORRECT | All 8 tables with correct columns and types |
| **Primary Keys** | ? CORRECT | All tables have appropriate primary keys |
| **Foreign Keys** | ? CORRECT | All relationships properly defined |
| **Cascade Deletes** | ? CORRECT | All cascade deletes configured correctly |
| **Indexes** | ? CORRECT | 7 performance indexes created |
| **Data Types** | ? CORRECT | All C# types map correctly to SQL types |
| **Constraints** | ? CORRECT | Unique constraints in place where needed |
| **Defaults** | ? CORRECT | All default values match model defaults |
| **Nullability** | ? CORRECT | All nullable columns marked correctly |

**Your database schema is perfectly aligned with your entity models!** ??

