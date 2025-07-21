IF OBJECT_ID(N'[SmsSubscribers]', N'U') IS NULL
BEGIN
    CREATE TABLE [SmsSubscribers] (
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [PhoneNumber] nvarchar(20) NOT NULL,
        [IsVerified] bit NOT NULL,
        [VerificationToken] nvarchar(64) NOT NULL,
        [UnsubscribeToken] nvarchar(64) NOT NULL,
        [CreatedAt] datetime2 NOT NULL
    );
END;
