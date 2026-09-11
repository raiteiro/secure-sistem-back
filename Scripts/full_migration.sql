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
GO

CREATE TABLE [NavigationRoutes] (
    [Id] int NOT NULL IDENTITY,
    [ParentId] int NULL,
    [WindowName] nvarchar(150) NOT NULL,
    [RoutePath] nvarchar(500) NOT NULL,
    [Icon] nvarchar(100) NULL,
    [WindowId] nvarchar(100) NOT NULL,
    [Level] int NOT NULL,
    [SortOrder] int NOT NULL,
    [CompanyId] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(100) NOT NULL,
    [ModifiedAt] datetime2 NULL,
    [ModifiedBy] nvarchar(100) NULL,
    CONSTRAINT [PK_NavigationRoutes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_NavigationRoutes_NavigationRoutes_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [NavigationRoutes] ([Id]) ON DELETE NO ACTION
);
GO

CREATE INDEX [IX_NavigationRoutes_CompanyId] ON [NavigationRoutes] ([CompanyId]);
GO

CREATE INDEX [IX_NavigationRoutes_CompanyId_ParentId] ON [NavigationRoutes] ([CompanyId], [ParentId]);
GO

CREATE INDEX [IX_NavigationRoutes_ParentId] ON [NavigationRoutes] ([ParentId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260830195345_CreateNavigationRoutes', N'8.0.8');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Companies] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [TaxId] nvarchar(50) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(100) NOT NULL,
    [ModifiedAt] datetime2 NULL,
    [ModifiedBy] nvarchar(100) NULL,
    CONSTRAINT [PK_Companies] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Roles] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(300) NULL,
    [CompanyId] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(100) NOT NULL,
    [ModifiedAt] datetime2 NULL,
    [ModifiedBy] nvarchar(100) NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Roles_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [Username] nvarchar(100) NOT NULL,
    [Email] nvarchar(256) NOT NULL,
    [PasswordHash] nvarchar(500) NOT NULL,
    [FirstName] nvarchar(100) NOT NULL,
    [LastName] nvarchar(100) NOT NULL,
    [PhoneNumber] nvarchar(20) NULL,
    [IsActive] bit NOT NULL,
    [RoleId] int NOT NULL,
    [CompanyId] int NOT NULL,
    [LastLoginAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(100) NOT NULL,
    [ModifiedAt] datetime2 NULL,
    [ModifiedBy] nvarchar(100) NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Users_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Users_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION
);
GO

CREATE UNIQUE INDEX [IX_Roles_CompanyId_Name] ON [Roles] ([CompanyId], [Name]);
GO

CREATE UNIQUE INDEX [IX_Users_CompanyId_Username] ON [Users] ([CompanyId], [Username]);
GO

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
GO

CREATE INDEX [IX_Users_RoleId] ON [Users] ([RoleId]);
GO

ALTER TABLE [NavigationRoutes] ADD CONSTRAINT [FK_NavigationRoutes_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260830200015_AddCompaniesRolesUsers', N'8.0.8');
GO

COMMIT;
GO

