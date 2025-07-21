CREATE TABLE [GameWeeks] (
    [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Season] nvarchar(20) NOT NULL,
    [Number] int NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL
);

CREATE UNIQUE INDEX [IX_GameWeeks_Season_Number] ON [GameWeeks] ([Season], [Number]);
