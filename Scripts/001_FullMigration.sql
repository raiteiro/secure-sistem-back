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

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830195345_CreateNavigationRoutes'
)
BEGIN
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
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830195345_CreateNavigationRoutes'
)
BEGIN
    CREATE INDEX [IX_NavigationRoutes_CompanyId] ON [NavigationRoutes] ([CompanyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830195345_CreateNavigationRoutes'
)
BEGIN
    CREATE INDEX [IX_NavigationRoutes_CompanyId_ParentId] ON [NavigationRoutes] ([CompanyId], [ParentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830195345_CreateNavigationRoutes'
)
BEGIN
    CREATE INDEX [IX_NavigationRoutes_ParentId] ON [NavigationRoutes] ([ParentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830195345_CreateNavigationRoutes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260830195345_CreateNavigationRoutes', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830200015_AddCompaniesRolesUsers'
)
BEGIN
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
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830200015_AddCompaniesRolesUsers'
)
BEGIN
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
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830200015_AddCompaniesRolesUsers'
)
BEGIN
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
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830200015_AddCompaniesRolesUsers'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Roles_CompanyId_Name] ON [Roles] ([CompanyId], [Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830200015_AddCompaniesRolesUsers'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_CompanyId_Username] ON [Users] ([CompanyId], [Username]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830200015_AddCompaniesRolesUsers'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830200015_AddCompaniesRolesUsers'
)
BEGIN
    CREATE INDEX [IX_Users_RoleId] ON [Users] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830200015_AddCompaniesRolesUsers'
)
BEGIN
    ALTER TABLE [NavigationRoutes] ADD CONSTRAINT [FK_NavigationRoutes_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830200015_AddCompaniesRolesUsers'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260830200015_AddCompaniesRolesUsers', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830203606_AddUserAndRoleNavigationRoutes'
)
BEGIN
    CREATE TABLE [RoleNavigationRoutes] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] int NOT NULL,
        [NavigationRouteId] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_RoleNavigationRoutes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RoleNavigationRoutes_NavigationRoutes_NavigationRouteId] FOREIGN KEY ([NavigationRouteId]) REFERENCES [NavigationRoutes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RoleNavigationRoutes_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830203606_AddUserAndRoleNavigationRoutes'
)
BEGIN
    CREATE TABLE [UserNavigationRoutes] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [NavigationRouteId] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_UserNavigationRoutes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserNavigationRoutes_NavigationRoutes_NavigationRouteId] FOREIGN KEY ([NavigationRouteId]) REFERENCES [NavigationRoutes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserNavigationRoutes_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830203606_AddUserAndRoleNavigationRoutes'
)
BEGIN
    CREATE INDEX [IX_RoleNavigationRoutes_NavigationRouteId] ON [RoleNavigationRoutes] ([NavigationRouteId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830203606_AddUserAndRoleNavigationRoutes'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RoleNavigationRoutes_RoleId_NavigationRouteId] ON [RoleNavigationRoutes] ([RoleId], [NavigationRouteId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830203606_AddUserAndRoleNavigationRoutes'
)
BEGIN
    CREATE INDEX [IX_UserNavigationRoutes_NavigationRouteId] ON [UserNavigationRoutes] ([NavigationRouteId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830203606_AddUserAndRoleNavigationRoutes'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserNavigationRoutes_UserId_NavigationRouteId] ON [UserNavigationRoutes] ([UserId], [NavigationRouteId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260830203606_AddUserAndRoleNavigationRoutes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260830203606_AddUserAndRoleNavigationRoutes', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906011238_AddRefreshTokens'
)
BEGIN
    CREATE TABLE [RefreshTokens] (
        [Id] int NOT NULL IDENTITY,
        [Token] nvarchar(500) NOT NULL,
        [UserId] int NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [RevokedAt] datetime2 NULL,
        [ReplacedByToken] nvarchar(500) NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906011238_AddRefreshTokens'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RefreshTokens_Token] ON [RefreshTokens] ([Token]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906011238_AddRefreshTokens'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906011238_AddRefreshTokens'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260906011238_AddRefreshTokens', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906144919_AddCompanySubscriptionLimits'
)
BEGIN
    ALTER TABLE [Companies] ADD [MaxConcurrentSessions] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906144919_AddCompanySubscriptionLimits'
)
BEGIN
    ALTER TABLE [Companies] ADD [MaxUsers] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906144919_AddCompanySubscriptionLimits'
)
BEGIN
    ALTER TABLE [Companies] ADD [SubscriptionExpiresAt] datetime2 NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906144919_AddCompanySubscriptionLimits'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260906144919_AddCompanySubscriptionLimits', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906150238_AddCompaniesAndSystemAdmin'
)
BEGIN
    ALTER TABLE [Users] ADD [IsSystemAdmin] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906150238_AddCompaniesAndSystemAdmin'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260906150238_AddCompaniesAndSystemAdmin', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906162743_AddMustChangePassword'
)
BEGIN
    ALTER TABLE [Users] ADD [MustChangePassword] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906162743_AddMustChangePassword'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260906162743_AddMustChangePassword', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906163611_AddPasswordResetTokens'
)
BEGIN
    CREATE TABLE [PasswordResetTokens] (
        [Id] int NOT NULL IDENTITY,
        [Token] nvarchar(500) NOT NULL,
        [UserId] int NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UsedAt] datetime2 NULL,
        CONSTRAINT [PK_PasswordResetTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PasswordResetTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906163611_AddPasswordResetTokens'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PasswordResetTokens_Token] ON [PasswordResetTokens] ([Token]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906163611_AddPasswordResetTokens'
)
BEGIN
    CREATE INDEX [IX_PasswordResetTokens_UserId] ON [PasswordResetTokens] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906163611_AddPasswordResetTokens'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260906163611_AddPasswordResetTokens', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906184024_AddCompanyPrimaryColor'
)
BEGIN
    ALTER TABLE [Companies] ADD [PrimaryColor] nvarchar(7) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906184024_AddCompanyPrimaryColor'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260906184024_AddCompanyPrimaryColor', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906192616_ReplacePrimaryColorWithHeaderAndButtonColor'
)
BEGIN
    EXEC sp_rename N'[Companies].[PrimaryColor]', N'HeaderColor', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906192616_ReplacePrimaryColorWithHeaderAndButtonColor'
)
BEGIN
    ALTER TABLE [Companies] ADD [ButtonColor] nvarchar(7) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906192616_ReplacePrimaryColorWithHeaderAndButtonColor'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260906192616_ReplacePrimaryColorWithHeaderAndButtonColor', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906193812_AddCompanyHeaderTextMode'
)
BEGIN
    ALTER TABLE [Companies] ADD [HeaderTextMode] nvarchar(5) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906193812_AddCompanyHeaderTextMode'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260906193812_AddCompanyHeaderTextMode', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906194753_AddCompanyReportFields'
)
BEGIN
    ALTER TABLE [Companies] ADD [Address] nvarchar(300) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906194753_AddCompanyReportFields'
)
BEGIN
    ALTER TABLE [Companies] ADD [Email] nvarchar(256) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906194753_AddCompanyReportFields'
)
BEGIN
    ALTER TABLE [Companies] ADD [LogoPath] nvarchar(500) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906194753_AddCompanyReportFields'
)
BEGIN
    ALTER TABLE [Companies] ADD [Phone] nvarchar(20) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906194753_AddCompanyReportFields'
)
BEGIN
    ALTER TABLE [Companies] ADD [Website] nvarchar(300) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906194753_AddCompanyReportFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260906194753_AddCompanyReportFields', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906235932_ReplaceColorsWithColorPreset'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Companies]') AND [c].[name] = N'ButtonColor');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Companies] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [Companies] DROP COLUMN [ButtonColor];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906235932_ReplaceColorsWithColorPreset'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Companies]') AND [c].[name] = N'HeaderColor');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Companies] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [Companies] DROP COLUMN [HeaderColor];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906235932_ReplaceColorsWithColorPreset'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Companies]') AND [c].[name] = N'HeaderTextMode');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Companies] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [Companies] DROP COLUMN [HeaderTextMode];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906235932_ReplaceColorsWithColorPreset'
)
BEGIN
    ALTER TABLE [Companies] ADD [ColorPreset] nvarchar(20) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260906235932_ReplaceColorsWithColorPreset'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260906235932_ReplaceColorsWithColorPreset', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907003337_AddCompanySocialLinks'
)
BEGIN
    ALTER TABLE [Companies] ADD [Facebook] nvarchar(300) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907003337_AddCompanySocialLinks'
)
BEGIN
    ALTER TABLE [Companies] ADD [Instagram] nvarchar(300) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907003337_AddCompanySocialLinks'
)
BEGIN
    ALTER TABLE [Companies] ADD [TikTok] nvarchar(300) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907003337_AddCompanySocialLinks'
)
BEGIN
    ALTER TABLE [Companies] ADD [WhatsApp] nvarchar(20) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260907003337_AddCompanySocialLinks'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260907003337_AddCompanySocialLinks', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911233937_AddBranches'
)
BEGIN
    CREATE TABLE [Branches] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [Address] nvarchar(300) NULL,
        [Phone] nvarchar(20) NULL,
        [CompanyId] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Branches] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Branches_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911233937_AddBranches'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Branches_CompanyId_Name] ON [Branches] ([CompanyId], [Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911233937_AddBranches'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260911233937_AddBranches', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911234949_AddWarehouses'
)
BEGIN
    CREATE TABLE [Warehouses] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [Address] nvarchar(300) NULL,
        [CompanyId] int NOT NULL,
        [BranchId] int NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Warehouses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Warehouses_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Warehouses_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911234949_AddWarehouses'
)
BEGIN
    CREATE INDEX [IX_Warehouses_BranchId] ON [Warehouses] ([BranchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911234949_AddWarehouses'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Warehouses_CompanyId_Name] ON [Warehouses] ([CompanyId], [Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260911234949_AddWarehouses'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260911234949_AddWarehouses', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912000145_AddProductCatalog'
)
BEGIN
    CREATE TABLE [Categories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Description] nvarchar(300) NULL,
        [CompanyId] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Categories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Categories_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912000145_AddProductCatalog'
)
BEGIN
    CREATE TABLE [TaxRates] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Rate] decimal(9,4) NOT NULL,
        [CompanyId] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_TaxRates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TaxRates_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912000145_AddProductCatalog'
)
BEGIN
    CREATE TABLE [Products] (
        [Id] int NOT NULL IDENTITY,
        [Sku] nvarchar(100) NULL,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(500) NULL,
        [Unit] nvarchar(20) NULL,
        [Price] decimal(18,2) NOT NULL,
        [Cost] decimal(18,2) NULL,
        [ImagePath] nvarchar(500) NULL,
        [CategoryId] int NULL,
        [TaxRateId] int NULL,
        [CompanyId] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Products_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Products_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Products_TaxRates_TaxRateId] FOREIGN KEY ([TaxRateId]) REFERENCES [TaxRates] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912000145_AddProductCatalog'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Categories_CompanyId_Name] ON [Categories] ([CompanyId], [Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912000145_AddProductCatalog'
)
BEGIN
    CREATE INDEX [IX_Products_CategoryId] ON [Products] ([CategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912000145_AddProductCatalog'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Products_CompanyId_Sku] ON [Products] ([CompanyId], [Sku]) WHERE [Sku] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912000145_AddProductCatalog'
)
BEGIN
    CREATE INDEX [IX_Products_TaxRateId] ON [Products] ([TaxRateId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912000145_AddProductCatalog'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TaxRates_CompanyId_Name] ON [TaxRates] ([CompanyId], [Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912000145_AddProductCatalog'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260912000145_AddProductCatalog', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912001233_AddIsDefaultForNewRoles'
)
BEGIN
    ALTER TABLE [NavigationRoutes] ADD [IsDefaultForNewRoles] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912001233_AddIsDefaultForNewRoles'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260912001233_AddIsDefaultForNewRoles', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912002333_AddInventory'
)
BEGIN
    CREATE TABLE [Inventories] (
        [Id] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [WarehouseId] int NOT NULL,
        [CompanyId] int NOT NULL,
        [Quantity] decimal(18,4) NOT NULL,
        [MinStock] decimal(18,4) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Inventories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Inventories_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Inventories_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Inventories_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912002333_AddInventory'
)
BEGIN
    CREATE TABLE [InventoryMovements] (
        [Id] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [WarehouseId] int NOT NULL,
        [CompanyId] int NOT NULL,
        [Type] nvarchar(20) NOT NULL,
        [Quantity] decimal(18,4) NOT NULL,
        [ResultingQuantity] decimal(18,4) NOT NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_InventoryMovements] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_InventoryMovements_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InventoryMovements_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InventoryMovements_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912002333_AddInventory'
)
BEGIN
    CREATE INDEX [IX_Inventories_CompanyId] ON [Inventories] ([CompanyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912002333_AddInventory'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Inventories_ProductId_WarehouseId] ON [Inventories] ([ProductId], [WarehouseId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912002333_AddInventory'
)
BEGIN
    CREATE INDEX [IX_Inventories_WarehouseId] ON [Inventories] ([WarehouseId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912002333_AddInventory'
)
BEGIN
    CREATE INDEX [IX_InventoryMovements_CompanyId] ON [InventoryMovements] ([CompanyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912002333_AddInventory'
)
BEGIN
    CREATE INDEX [IX_InventoryMovements_ProductId_WarehouseId] ON [InventoryMovements] ([ProductId], [WarehouseId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912002333_AddInventory'
)
BEGIN
    CREATE INDEX [IX_InventoryMovements_WarehouseId] ON [InventoryMovements] ([WarehouseId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912002333_AddInventory'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260912002333_AddInventory', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912003806_AddCustomersAndCashSessions'
)
BEGIN
    CREATE TABLE [CashRegisters] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [BranchId] int NOT NULL,
        [CompanyId] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_CashRegisters] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CashRegisters_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CashRegisters_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912003806_AddCustomersAndCashSessions'
)
BEGIN
    CREATE TABLE [Customers] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [Email] nvarchar(256) NULL,
        [Phone] nvarchar(20) NULL,
        [TaxId] nvarchar(50) NULL,
        [Address] nvarchar(300) NULL,
        [CompanyId] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Customers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Customers_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912003806_AddCustomersAndCashSessions'
)
BEGIN
    CREATE TABLE [CashSessions] (
        [Id] int NOT NULL IDENTITY,
        [CashRegisterId] int NOT NULL,
        [UserId] int NOT NULL,
        [CompanyId] int NOT NULL,
        [OpeningAmount] decimal(18,2) NOT NULL,
        [OpenedAt] datetime2 NOT NULL,
        [ClosingAmount] decimal(18,2) NULL,
        [ExpectedAmount] decimal(18,2) NULL,
        [Difference] decimal(18,2) NULL,
        [ClosedAt] datetime2 NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_CashSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CashSessions_CashRegisters_CashRegisterId] FOREIGN KEY ([CashRegisterId]) REFERENCES [CashRegisters] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CashSessions_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_CashSessions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912003806_AddCustomersAndCashSessions'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CashRegisters_BranchId_Name] ON [CashRegisters] ([BranchId], [Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912003806_AddCustomersAndCashSessions'
)
BEGIN
    CREATE INDEX [IX_CashRegisters_CompanyId] ON [CashRegisters] ([CompanyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912003806_AddCustomersAndCashSessions'
)
BEGIN
    CREATE INDEX [IX_CashSessions_CashRegisterId_ClosedAt] ON [CashSessions] ([CashRegisterId], [ClosedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912003806_AddCustomersAndCashSessions'
)
BEGIN
    CREATE INDEX [IX_CashSessions_CompanyId] ON [CashSessions] ([CompanyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912003806_AddCustomersAndCashSessions'
)
BEGIN
    CREATE INDEX [IX_CashSessions_UserId_ClosedAt] ON [CashSessions] ([UserId], [ClosedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912003806_AddCustomersAndCashSessions'
)
BEGIN
    CREATE INDEX [IX_Customers_CompanyId] ON [Customers] ([CompanyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912003806_AddCustomersAndCashSessions'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Customers_CompanyId_Email] ON [Customers] ([CompanyId], [Email]) WHERE [Email] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912003806_AddCustomersAndCashSessions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260912003806_AddCustomersAndCashSessions', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912004556_AddSales'
)
BEGIN
    CREATE TABLE [Sales] (
        [Id] int NOT NULL IDENTITY,
        [FolioNumber] int NOT NULL,
        [BranchId] int NOT NULL,
        [WarehouseId] int NOT NULL,
        [CustomerId] int NULL,
        [CashSessionId] int NOT NULL,
        [UserId] int NOT NULL,
        [CompanyId] int NOT NULL,
        [Status] nvarchar(20) NOT NULL,
        [Subtotal] decimal(18,2) NOT NULL,
        [DiscountTotal] decimal(18,2) NOT NULL,
        [TaxTotal] decimal(18,2) NOT NULL,
        [Total] decimal(18,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [ModifiedBy] nvarchar(100) NULL,
        CONSTRAINT [PK_Sales] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Sales_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Sales_CashSessions_CashSessionId] FOREIGN KEY ([CashSessionId]) REFERENCES [CashSessions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Sales_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Sales_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Sales_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Sales_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912004556_AddSales'
)
BEGIN
    CREATE TABLE [Payments] (
        [Id] int NOT NULL IDENTITY,
        [SaleId] int NOT NULL,
        [CompanyId] int NOT NULL,
        [Method] nvarchar(20) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Payments_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Payments_Sales_SaleId] FOREIGN KEY ([SaleId]) REFERENCES [Sales] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912004556_AddSales'
)
BEGIN
    CREATE TABLE [SaleItems] (
        [Id] int NOT NULL IDENTITY,
        [SaleId] int NOT NULL,
        [ProductId] int NOT NULL,
        [Quantity] decimal(18,4) NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [DiscountAmount] decimal(18,2) NOT NULL,
        [TaxRateValue] decimal(9,4) NOT NULL,
        [TaxAmount] decimal(18,2) NOT NULL,
        [Subtotal] decimal(18,2) NOT NULL,
        [Total] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_SaleItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SaleItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SaleItems_Sales_SaleId] FOREIGN KEY ([SaleId]) REFERENCES [Sales] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912004556_AddSales'
)
BEGIN
    CREATE INDEX [IX_Payments_CompanyId] ON [Payments] ([CompanyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912004556_AddSales'
)
BEGIN
    CREATE INDEX [IX_Payments_SaleId] ON [Payments] ([SaleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912004556_AddSales'
)
BEGIN
    CREATE INDEX [IX_SaleItems_ProductId] ON [SaleItems] ([ProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912004556_AddSales'
)
BEGIN
    CREATE INDEX [IX_SaleItems_SaleId] ON [SaleItems] ([SaleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912004556_AddSales'
)
BEGIN
    CREATE INDEX [IX_Sales_BranchId] ON [Sales] ([BranchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912004556_AddSales'
)
BEGIN
    CREATE INDEX [IX_Sales_CashSessionId] ON [Sales] ([CashSessionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912004556_AddSales'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Sales_CompanyId_FolioNumber] ON [Sales] ([CompanyId], [FolioNumber]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912004556_AddSales'
)
BEGIN
    CREATE INDEX [IX_Sales_CustomerId] ON [Sales] ([CustomerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912004556_AddSales'
)
BEGIN
    CREATE INDEX [IX_Sales_UserId] ON [Sales] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912004556_AddSales'
)
BEGIN
    CREATE INDEX [IX_Sales_WarehouseId] ON [Sales] ([WarehouseId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912004556_AddSales'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260912004556_AddSales', N'8.0.8');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912132542_AddReturns'
)
BEGIN
    CREATE TABLE [Returns] (
        [Id] int NOT NULL IDENTITY,
        [SaleId] int NOT NULL,
        [WarehouseId] int NOT NULL,
        [CashSessionId] int NOT NULL,
        [UserId] int NOT NULL,
        [CompanyId] int NOT NULL,
        [RefundMethod] nvarchar(20) NOT NULL,
        [Reason] nvarchar(500) NULL,
        [SubtotalRefunded] decimal(18,2) NOT NULL,
        [TaxRefunded] decimal(18,2) NOT NULL,
        [TotalRefunded] decimal(18,2) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_Returns] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Returns_CashSessions_CashSessionId] FOREIGN KEY ([CashSessionId]) REFERENCES [CashSessions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Returns_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Returns_Sales_SaleId] FOREIGN KEY ([SaleId]) REFERENCES [Sales] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Returns_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Returns_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912132542_AddReturns'
)
BEGIN
    CREATE TABLE [ReturnItems] (
        [Id] int NOT NULL IDENTITY,
        [ReturnId] int NOT NULL,
        [SaleItemId] int NOT NULL,
        [ProductId] int NOT NULL,
        [Quantity] decimal(18,4) NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [TaxRateValue] decimal(9,4) NOT NULL,
        [Subtotal] decimal(18,2) NOT NULL,
        [TaxAmount] decimal(18,2) NOT NULL,
        [Total] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_ReturnItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ReturnItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ReturnItems_Returns_ReturnId] FOREIGN KEY ([ReturnId]) REFERENCES [Returns] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ReturnItems_SaleItems_SaleItemId] FOREIGN KEY ([SaleItemId]) REFERENCES [SaleItems] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912132542_AddReturns'
)
BEGIN
    CREATE INDEX [IX_ReturnItems_ProductId] ON [ReturnItems] ([ProductId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912132542_AddReturns'
)
BEGIN
    CREATE INDEX [IX_ReturnItems_ReturnId] ON [ReturnItems] ([ReturnId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912132542_AddReturns'
)
BEGIN
    CREATE INDEX [IX_ReturnItems_SaleItemId] ON [ReturnItems] ([SaleItemId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912132542_AddReturns'
)
BEGIN
    CREATE INDEX [IX_Returns_CashSessionId] ON [Returns] ([CashSessionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912132542_AddReturns'
)
BEGIN
    CREATE INDEX [IX_Returns_CompanyId] ON [Returns] ([CompanyId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912132542_AddReturns'
)
BEGIN
    CREATE INDEX [IX_Returns_SaleId] ON [Returns] ([SaleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912132542_AddReturns'
)
BEGIN
    CREATE INDEX [IX_Returns_UserId] ON [Returns] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912132542_AddReturns'
)
BEGIN
    CREATE INDEX [IX_Returns_WarehouseId] ON [Returns] ([WarehouseId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260912132542_AddReturns'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260912132542_AddReturns', N'8.0.8');
END;
GO

COMMIT;
GO

