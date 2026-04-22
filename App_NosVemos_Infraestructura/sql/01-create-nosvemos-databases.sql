IF DB_ID(N'AutenticacionDB') IS NULL
BEGIN
    CREATE DATABASE [AutenticacionDB];
END;
GO

IF DB_ID(N'UsuariosDB') IS NULL
BEGIN
    CREATE DATABASE [UsuariosDB];
END;
GO

IF DB_ID(N'NucleoDB') IS NULL
BEGIN
    CREATE DATABASE [NucleoDB];
END;
GO

IF DB_ID(N'IADB') IS NULL
BEGIN
    CREATE DATABASE [IADB];
END;
GO

IF DB_ID(N'AuditoriaDB') IS NULL
BEGIN
    CREATE DATABASE [AuditoriaDB];
END;
GO

SELECT name
FROM sys.databases
WHERE name IN (N'AutenticacionDB', N'UsuariosDB', N'NucleoDB', N'IADB', N'AuditoriaDB')
ORDER BY name;
GO
