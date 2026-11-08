#!/usr/bin/env python3
"""Verify fresh/legacy/repeated upgrades in a disposable SQL database."""
import os
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[1]
CONTAINER = os.getenv("SQLSERVER_CONTAINER_NAME", "micro-insurtech-sql")
DATABASE = "ComparisonSchemaVerification"


def sql(statement, database=DATABASE):
    subprocess.run(["docker", "exec", "-i", CONTAINER,
                    "/opt/mssql-tools18/bin/sqlcmd", "-S", "localhost", "-U", "sa",
                    "-P", os.environ["SQLSERVER_SA_PASSWORD"], "-C", "-b", "-d", database],
                   input=statement, text=True, check=True)


def main():
    schema = (ROOT / "database/schema.sql").read_text()
    upgrade = (ROOT / "database/upgrades/001-saved-comparisons.sql").read_text()
    sql(f"IF DB_ID('{DATABASE}') IS NOT NULL DROP DATABASE [{DATABASE}]; CREATE DATABASE [{DATABASE}];", "master")
    try:
        sql(schema)
        sql(upgrade)
        sql(upgrade)
        # Reconstruct the pre-feature schema without relying on Git or a fixture copy.
        sql("""DROP INDEX UX_Properties_QuoteReference ON dbo.Properties;
ALTER TABLE dbo.Properties DROP CONSTRAINT DF_Properties_QuoteReference, DF_Properties_CreatedAt;
ALTER TABLE dbo.Properties DROP COLUMN QuoteReference, CreatedAt;
INSERT dbo.Clients (FirstName,LastName,Email,CreatedAt) VALUES ('Legacy','Client','legacy@example.com','2025-01-02');
INSERT dbo.Properties (ClientID,Postcode,YearBuilt,RebuildCost,IsUnoccupied) VALUES (1,'SW1A 1AA',1910,750000,0),(1,'SW1A 1AA',2000,500000,1);
INSERT dbo.Quotes (PropertyID,UnderwriterName,PremiumAmount,RiskRating) VALUES (1,'Original',123.45,'Low');""")
        sql(upgrade)
        sql("SELECT QuoteReference,CreatedAt INTO dbo.BeforeRerun FROM dbo.Properties;")
        sql(upgrade)
        sql("""IF (SELECT COUNT(*) FROM dbo.Properties) <> 2 OR (SELECT COUNT(*) FROM dbo.Quotes WHERE PremiumAmount=123.45) <> 1
    THROW 50010, 'Legacy rows lost', 1;
IF (SELECT COUNT(DISTINCT QuoteReference) FROM dbo.Properties) <> 2
    THROW 50011, 'References not unique', 1;
IF EXISTS (SELECT 1 FROM dbo.Properties WHERE CreatedAt <> '2025-01-02')
    THROW 50012, 'Legacy timestamp incorrect', 1;
IF EXISTS (SELECT QuoteReference,CreatedAt FROM dbo.BeforeRerun EXCEPT SELECT QuoteReference,CreatedAt FROM dbo.Properties)
    THROW 50013, 'Repeated upgrade changed metadata', 1;
DECLARE @duplicate uniqueidentifier = (SELECT TOP 1 QuoteReference FROM dbo.Properties);
BEGIN TRY
    INSERT dbo.Properties (ClientID,Postcode,YearBuilt,RebuildCost,IsUnoccupied,QuoteReference)
    VALUES (1,'SW1A 1AA',2000,100,0,@duplicate);
    THROW 50014, 'Duplicate reference accepted', 1;
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() NOT IN (2601,2627) THROW;
END CATCH;""")
        print("PASS: fresh schema, populated legacy upgrade, repeatability, preserved prices and unique references")
    finally:
        sql(f"DROP DATABASE [{DATABASE}];", "master")


if __name__ == "__main__":
    main()
