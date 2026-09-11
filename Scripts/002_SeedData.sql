-- ============================================
-- Seed Data: Company, Role, User, Navigation Routes
-- Password: Admin123!
-- ============================================
USE [secsistem];
GO

BEGIN TRANSACTION;
GO

-- 1. Company
IF NOT EXISTS (SELECT 1 FROM [Companies] WHERE [Id] = 1)
BEGIN
    SET IDENTITY_INSERT [Companies] ON;
    INSERT INTO [Companies] ([Id], [Name], [TaxId], [IsActive], [CreatedAt], [CreatedBy])
    VALUES (1, N'Empresa Demo', N'DEMO000000', 1, GETUTCDATE(), N'system');
    SET IDENTITY_INSERT [Companies] OFF;
END;
GO

-- 2. Role
IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Id] = 1)
BEGIN
    SET IDENTITY_INSERT [Roles] ON;
    INSERT INTO [Roles] ([Id], [Name], [Description], [CompanyId], [IsActive], [CreatedAt], [CreatedBy])
    VALUES (1, N'Administrator', N'Full access to all system features', 1, 1, GETUTCDATE(), N'system');
    SET IDENTITY_INSERT [Roles] OFF;
END;
GO

-- 3. User (password: Admin123!)
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Username] = N'admin' AND [CompanyId] = 1)
BEGIN
    SET IDENTITY_INSERT [Users] ON;
    INSERT INTO [Users] ([Id], [Username], [Email], [PasswordHash], [FirstName], [LastName], [PhoneNumber], [IsActive], [RoleId], [CompanyId], [CreatedAt], [CreatedBy])
    VALUES (1, N'admin', N'admin@empresademo.com', N'$2a$11$PGeqmbiBUyNR.mP1mlmg7OCYYhMSSygGLHMM/ErNCW6eu5JMVGhQ.', N'Admin', N'User', NULL, 1, 1, 1, GETUTCDATE(), N'system');
    SET IDENTITY_INSERT [Users] OFF;
END;
GO

-- 4. Navigation Routes (menu tree)
-- Parent: Dashboard
IF NOT EXISTS (SELECT 1 FROM [NavigationRoutes] WHERE [Id] = 1)
BEGIN
    SET IDENTITY_INSERT [NavigationRoutes] ON;
    INSERT INTO [NavigationRoutes] ([Id], [ParentId], [WindowName], [RoutePath], [Icon], [WindowId], [Level], [SortOrder], [CompanyId], [IsActive], [CreatedAt], [CreatedBy])
    VALUES (1, NULL, N'Dashboard', N'/dashboard', N'fas fa-home', N'win-dashboard', 0, 1, 1, 1, GETUTCDATE(), N'system');
    SET IDENTITY_INSERT [NavigationRoutes] OFF;
END;
GO

-- Parent: Administration (group)
IF NOT EXISTS (SELECT 1 FROM [NavigationRoutes] WHERE [Id] = 2)
BEGIN
    SET IDENTITY_INSERT [NavigationRoutes] ON;
    INSERT INTO [NavigationRoutes] ([Id], [ParentId], [WindowName], [RoutePath], [Icon], [WindowId], [Level], [SortOrder], [CompanyId], [IsActive], [CreatedAt], [CreatedBy])
    VALUES (2, NULL, N'Administration', N'#', N'fas fa-cogs', N'win-admin', 0, 2, 1, 1, GETUTCDATE(), N'system');
    SET IDENTITY_INSERT [NavigationRoutes] OFF;
END;
GO

-- Child: Users (under Administration)
IF NOT EXISTS (SELECT 1 FROM [NavigationRoutes] WHERE [Id] = 3)
BEGIN
    SET IDENTITY_INSERT [NavigationRoutes] ON;
    INSERT INTO [NavigationRoutes] ([Id], [ParentId], [WindowName], [RoutePath], [Icon], [WindowId], [Level], [SortOrder], [CompanyId], [IsActive], [CreatedAt], [CreatedBy])
    VALUES (3, 2, N'Users', N'/admin/users', N'fas fa-users', N'win-users', 1, 1, 1, 1, GETUTCDATE(), N'system');
    SET IDENTITY_INSERT [NavigationRoutes] OFF;
END;
GO

-- Child: Roles (under Administration)
IF NOT EXISTS (SELECT 1 FROM [NavigationRoutes] WHERE [Id] = 4)
BEGIN
    SET IDENTITY_INSERT [NavigationRoutes] ON;
    INSERT INTO [NavigationRoutes] ([Id], [ParentId], [WindowName], [RoutePath], [Icon], [WindowId], [Level], [SortOrder], [CompanyId], [IsActive], [CreatedAt], [CreatedBy])
    VALUES (4, 2, N'Roles', N'/admin/roles', N'fas fa-user-tag', N'win-roles', 1, 2, 1, 1, GETUTCDATE(), N'system');
    SET IDENTITY_INSERT [NavigationRoutes] OFF;
END;
GO

-- Child: Navigation Routes (under Administration)
IF NOT EXISTS (SELECT 1 FROM [NavigationRoutes] WHERE [Id] = 5)
BEGIN
    SET IDENTITY_INSERT [NavigationRoutes] ON;
    INSERT INTO [NavigationRoutes] ([Id], [ParentId], [WindowName], [RoutePath], [Icon], [WindowId], [Level], [SortOrder], [CompanyId], [IsActive], [CreatedAt], [CreatedBy])
    VALUES (5, 2, N'Menu Routes', N'/admin/routes', N'fas fa-sitemap', N'win-routes', 1, 3, 1, 1, GETUTCDATE(), N'system');
    SET IDENTITY_INSERT [NavigationRoutes] OFF;
END;
GO

-- 5. Assign all routes to the Administrator role
IF NOT EXISTS (SELECT 1 FROM [RoleNavigationRoutes] WHERE [RoleId] = 1 AND [NavigationRouteId] = 1)
    INSERT INTO [RoleNavigationRoutes] ([RoleId], [NavigationRouteId], [IsActive], [CreatedAt], [CreatedBy])
    VALUES (1, 1, 1, GETUTCDATE(), N'system');
GO

IF NOT EXISTS (SELECT 1 FROM [RoleNavigationRoutes] WHERE [RoleId] = 1 AND [NavigationRouteId] = 2)
    INSERT INTO [RoleNavigationRoutes] ([RoleId], [NavigationRouteId], [IsActive], [CreatedAt], [CreatedBy])
    VALUES (1, 2, 1, GETUTCDATE(), N'system');
GO

IF NOT EXISTS (SELECT 1 FROM [RoleNavigationRoutes] WHERE [RoleId] = 1 AND [NavigationRouteId] = 3)
    INSERT INTO [RoleNavigationRoutes] ([RoleId], [NavigationRouteId], [IsActive], [CreatedAt], [CreatedBy])
    VALUES (1, 3, 1, GETUTCDATE(), N'system');
GO

IF NOT EXISTS (SELECT 1 FROM [RoleNavigationRoutes] WHERE [RoleId] = 1 AND [NavigationRouteId] = 4)
    INSERT INTO [RoleNavigationRoutes] ([RoleId], [NavigationRouteId], [IsActive], [CreatedAt], [CreatedBy])
    VALUES (1, 4, 1, GETUTCDATE(), N'system');
GO

IF NOT EXISTS (SELECT 1 FROM [RoleNavigationRoutes] WHERE [RoleId] = 1 AND [NavigationRouteId] = 5)
    INSERT INTO [RoleNavigationRoutes] ([RoleId], [NavigationRouteId], [IsActive], [CreatedAt], [CreatedBy])
    VALUES (1, 5, 1, GETUTCDATE(), N'system');
GO

COMMIT;
GO
