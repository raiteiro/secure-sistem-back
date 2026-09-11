-- ============================================
-- Seed: Navigation Routes de prueba
-- Estructura del menú:
--   Dashboard
--   Administration
--     ├── Users
--     ├── Roles
--     └── Menu Routes
--   Operations
--     ├── Orders
--     ├── Inventory
--     └── Reports
--         ├── Sales Report
--         └── Inventory Report
--   Settings
--     ├── General
--     └── Notifications
-- ============================================
USE [secsistem];
GO

BEGIN TRANSACTION;
GO

-- ========== Parent: Operations (group) ==========
IF NOT EXISTS (SELECT 1 FROM [NavigationRoutes] WHERE [Id] = 6)
BEGIN
    SET IDENTITY_INSERT [NavigationRoutes] ON;
    INSERT INTO [NavigationRoutes] ([Id], [ParentId], [WindowName], [RoutePath], [Icon], [WindowId], [Level], [SortOrder], [CompanyId], [IsActive], [CreatedAt], [CreatedBy])
    VALUES (6, NULL, N'Operations', N'#', N'fas fa-briefcase', N'win-operations', 0, 3, 1, 1, GETUTCDATE(), N'system');
    SET IDENTITY_INSERT [NavigationRoutes] OFF;
END;
GO

-- Child: Orders
IF NOT EXISTS (SELECT 1 FROM [NavigationRoutes] WHERE [Id] = 7)
BEGIN
    SET IDENTITY_INSERT [NavigationRoutes] ON;
    INSERT INTO [NavigationRoutes] ([Id], [ParentId], [WindowName], [RoutePath], [Icon], [WindowId], [Level], [SortOrder], [CompanyId], [IsActive], [CreatedAt], [CreatedBy])
    VALUES (7, 6, N'Orders', N'/operations/orders', N'fas fa-shopping-cart', N'win-orders', 1, 1, 1, 1, GETUTCDATE(), N'system');
    SET IDENTITY_INSERT [NavigationRoutes] OFF;
END;
GO

-- Child: Inventory
IF NOT EXISTS (SELECT 1 FROM [NavigationRoutes] WHERE [Id] = 8)
BEGIN
    SET IDENTITY_INSERT [NavigationRoutes] ON;
    INSERT INTO [NavigationRoutes] ([Id], [ParentId], [WindowName], [RoutePath], [Icon], [WindowId], [Level], [SortOrder], [CompanyId], [IsActive], [CreatedAt], [CreatedBy])
    VALUES (8, 6, N'Inventory', N'/operations/inventory', N'fas fa-boxes', N'win-inventory', 1, 2, 1, 1, GETUTCDATE(), N'system');
    SET IDENTITY_INSERT [NavigationRoutes] OFF;
END;
GO

-- Child: Reports (sub-group)
IF NOT EXISTS (SELECT 1 FROM [NavigationRoutes] WHERE [Id] = 9)
BEGIN
    SET IDENTITY_INSERT [NavigationRoutes] ON;
    INSERT INTO [NavigationRoutes] ([Id], [ParentId], [WindowName], [RoutePath], [Icon], [WindowId], [Level], [SortOrder], [CompanyId], [IsActive], [CreatedAt], [CreatedBy])
    VALUES (9, 6, N'Reports', N'#', N'fas fa-chart-bar', N'win-reports', 1, 3, 1, 1, GETUTCDATE(), N'system');
    SET IDENTITY_INSERT [NavigationRoutes] OFF;
END;
GO

-- Grandchild: Sales Report
IF NOT EXISTS (SELECT 1 FROM [NavigationRoutes] WHERE [Id] = 10)
BEGIN
    SET IDENTITY_INSERT [NavigationRoutes] ON;
    INSERT INTO [NavigationRoutes] ([Id], [ParentId], [WindowName], [RoutePath], [Icon], [WindowId], [Level], [SortOrder], [CompanyId], [IsActive], [CreatedAt], [CreatedBy])
    VALUES (10, 9, N'Sales Report', N'/operations/reports/sales', N'fas fa-dollar-sign', N'win-report-sales', 2, 1, 1, 1, GETUTCDATE(), N'system');
    SET IDENTITY_INSERT [NavigationRoutes] OFF;
END;
GO

-- Grandchild: Inventory Report
IF NOT EXISTS (SELECT 1 FROM [NavigationRoutes] WHERE [Id] = 11)
BEGIN
    SET IDENTITY_INSERT [NavigationRoutes] ON;
    INSERT INTO [NavigationRoutes] ([Id], [ParentId], [WindowName], [RoutePath], [Icon], [WindowId], [Level], [SortOrder], [CompanyId], [IsActive], [CreatedAt], [CreatedBy])
    VALUES (11, 9, N'Inventory Report', N'/operations/reports/inventory', N'fas fa-warehouse', N'win-report-inventory', 2, 2, 1, 1, GETUTCDATE(), N'system');
    SET IDENTITY_INSERT [NavigationRoutes] OFF;
END;
GO

-- ========== Parent: Settings (group) ==========
IF NOT EXISTS (SELECT 1 FROM [NavigationRoutes] WHERE [Id] = 12)
BEGIN
    SET IDENTITY_INSERT [NavigationRoutes] ON;
    INSERT INTO [NavigationRoutes] ([Id], [ParentId], [WindowName], [RoutePath], [Icon], [WindowId], [Level], [SortOrder], [CompanyId], [IsActive], [CreatedAt], [CreatedBy])
    VALUES (12, NULL, N'Settings', N'#', N'fas fa-sliders-h', N'win-settings', 0, 4, 1, 1, GETUTCDATE(), N'system');
    SET IDENTITY_INSERT [NavigationRoutes] OFF;
END;
GO

-- Child: General
IF NOT EXISTS (SELECT 1 FROM [NavigationRoutes] WHERE [Id] = 13)
BEGIN
    SET IDENTITY_INSERT [NavigationRoutes] ON;
    INSERT INTO [NavigationRoutes] ([Id], [ParentId], [WindowName], [RoutePath], [Icon], [WindowId], [Level], [SortOrder], [CompanyId], [IsActive], [CreatedAt], [CreatedBy])
    VALUES (13, 12, N'General', N'/settings/general', N'fas fa-cog', N'win-settings-general', 1, 1, 1, 1, GETUTCDATE(), N'system');
    SET IDENTITY_INSERT [NavigationRoutes] OFF;
END;
GO

-- Child: Notifications
IF NOT EXISTS (SELECT 1 FROM [NavigationRoutes] WHERE [Id] = 14)
BEGIN
    SET IDENTITY_INSERT [NavigationRoutes] ON;
    INSERT INTO [NavigationRoutes] ([Id], [ParentId], [WindowName], [RoutePath], [Icon], [WindowId], [Level], [SortOrder], [CompanyId], [IsActive], [CreatedAt], [CreatedBy])
    VALUES (14, 12, N'Notifications', N'/settings/notifications', N'fas fa-bell', N'win-settings-notifications', 1, 2, 1, 1, GETUTCDATE(), N'system');
    SET IDENTITY_INSERT [NavigationRoutes] OFF;
END;
GO

-- ========== Assign new routes to Administrator role ==========
DECLARE @routeId INT = 6;
WHILE @routeId <= 14
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [RoleNavigationRoutes] WHERE [RoleId] = 1 AND [NavigationRouteId] = @routeId)
        INSERT INTO [RoleNavigationRoutes] ([RoleId], [NavigationRouteId], [IsActive], [CreatedAt], [CreatedBy])
        VALUES (1, @routeId, 1, GETUTCDATE(), N'system');
    SET @routeId = @routeId + 1;
END;
GO

COMMIT;
GO
