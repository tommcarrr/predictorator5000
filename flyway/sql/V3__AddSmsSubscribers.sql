CREATE TABLE IF NOT EXISTS "SmsSubscribers" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "PhoneNumber" TEXT NOT NULL,
    "IsVerified" INTEGER NOT NULL,
    "VerificationToken" TEXT NOT NULL,
    "UnsubscribeToken" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL
);
