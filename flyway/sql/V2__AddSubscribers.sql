CREATE TABLE IF NOT EXISTS "Subscribers" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "Email" TEXT NOT NULL,
    "IsVerified" INTEGER NOT NULL,
    "VerificationToken" TEXT NOT NULL,
    "UnsubscribeToken" TEXT NOT NULL,
    "CreatedAt" TEXT NOT NULL
);
