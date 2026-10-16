IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = N'secsistem')
BEGIN
    CREATE DATABASE [secsistem];
END;
GO

-- AT TIME ZONE (usado por 001_FullMigration.sql para las migraciones que
-- siembran datos con hora de México) requiere compatibility level 130+
-- (SQL Server 2016 en adelante). Si el servidor corre una versión más
-- vieja o el modelo de la instancia quedó en un nivel menor, súbelo aquí
-- antes de correr 001_FullMigration.sql.
ALTER DATABASE [secsistem] SET COMPATIBILITY_LEVEL = 150;
GO
