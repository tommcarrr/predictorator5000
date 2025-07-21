IF OBJECT_ID(N'[GameWeeks]', N'U') IS NULL
BEGIN
    CREATE TABLE [GameWeeks] (
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Season] nvarchar(20) NOT NULL,
        [Number] int NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NOT NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_GameWeeks_Season_Number')
    CREATE UNIQUE INDEX [IX_GameWeeks_Season_Number] ON [GameWeeks] ([Season], [Number]);
