CREATE TABLE IF NOT EXISTS "GameWeeks" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "Season" TEXT NOT NULL,
    "Number" INTEGER NOT NULL,
    "StartDate" TEXT NOT NULL,
    "EndDate" TEXT NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_GameWeeks_Season_Number" ON "GameWeeks" ("Season", "Number");
