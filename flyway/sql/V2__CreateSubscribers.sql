CREATE TABLE IF NOT EXISTS "Subscribers" (
    "Id" INTEGER PRIMARY KEY AUTOINCREMENT,
    "Email" TEXT NOT NULL,
    "Verified" INTEGER NOT NULL,
    "Token" TEXT NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Subscribers_Email" ON "Subscribers" ("Email");
