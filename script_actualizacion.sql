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
CREATE TABLE [FoodLogs] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [OriginalText] nvarchar(max) NOT NULL,
    [EstimatedCalories] int NOT NULL,
    [LoggedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_FoodLogs] PRIMARY KEY ([Id])
);

CREATE TABLE [Users] (
    [Id] uniqueidentifier NOT NULL,
    [Email] nvarchar(255) NOT NULL,
    [PasswordHash] nvarchar(512) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [HeightCm] float NOT NULL,
    [CurrentWeightKg] float NOT NULL,
    [TargetWeightKg] float NOT NULL,
    [Age] int NOT NULL,
    [BiologicalSex] nvarchar(1) NOT NULL,
    [ActivityLevel] int NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260401133055_InitialCreate', N'9.0.0');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260401142325_AddFoodLogTable', N'9.0.0');

ALTER TABLE [Users] ADD [DailyCaloricTarget] int NOT NULL DEFAULT 0;

ALTER TABLE [Users] ADD [Goal] nvarchar(max) NOT NULL DEFAULT N'';

CREATE TABLE [UserProfileHistory] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Weight] float NOT NULL,
    [Height] float NOT NULL,
    [ActivityLevel] int NOT NULL,
    [RecordedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_UserProfileHistory] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260402142648_AgregarHistorialPerfil', N'9.0.0');

COMMIT;
GO

