/* =============================================================================
   Contoso Infrastructure - Rate Library (Azure SQL seed)
   Module 3: Foundry IQ + Azure SQL knowledge source

   Migrates the Module 2 markdown rate library
   (modules/02-build-your-first-agent/data/contoso-rate-library.md)
   into a normalized Azure SQL table so it can be served as an indexed
   Azure SQL knowledge source (kind = indexedSql) for agentic retrieval.

   Design notes
   ------------
   - LONG format: one row per (trade_item x region). Each SQL row becomes one
     logical document in the generated Azure AI Search index.
   - Single-valued primary key (rate_id) is REQUIRED by the Azure SQL knowledge
     source (composite keys are not supported; the key is auto-discovered).
   - `region`, `division`, and `owner_group` are the row-level access dimensions
     used by the two RLS demo tracks:
       * Track 1 (security filter): filter on `region` / `division` at query time.
       * Track 2 (identity RLS):    map `owner_group` to Entra security groups.
   - `content_text` is a composed sentence used for embeddings / semantic ranking.

   Idempotent: safe to re-run. Recreates the table and reseeds it.
   Target: Azure SQL Database (also works on SQL Server 2019+ / Azure SQL MI).
   ============================================================================= */

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.rate_library', N'U') IS NOT NULL
    DROP TABLE dbo.rate_library;
GO

CREATE TABLE dbo.rate_library
(
    rate_id        INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_rate_library PRIMARY KEY,   -- single-valued PK (required by indexedSql)
    category       NVARCHAR(60)   NOT NULL,        -- e.g. 'Concrete'
    division       NVARCHAR(20)   NOT NULL,        -- Civil | Structures | Plant  (secondary RLS dimension)
    trade_item     NVARCHAR(120)  NOT NULL,
    unit           NVARCHAR(20)   NOT NULL,
    region         NVARCHAR(10)   NOT NULL,        -- NSW | VIC | QLD | WA         (primary RLS dimension)
    rate_aud       DECIMAL(12,2)  NOT NULL,
    owner_group    NVARCHAR(60)   NOT NULL,        -- Entra security group that owns the row (Track 2)
    effective_date DATE           NOT NULL,
    content_text   NVARCHAR(400)  NOT NULL         -- composed text for embeddings / semantic ranking
);
GO

/* -----------------------------------------------------------------------------
   Wide staging data (mirrors the Module 2 markdown, one row per trade item).
   Rates are NSW / VIC / QLD / WA in AUD.
   ----------------------------------------------------------------------------- */
DECLARE @src TABLE
(
    category   NVARCHAR(60),
    division   NVARCHAR(20),
    trade_item NVARCHAR(120),
    unit       NVARCHAR(20),
    nsw        DECIMAL(12,2),
    vic        DECIMAL(12,2),
    qld        DECIMAL(12,2),
    wa         DECIMAL(12,2)
);

INSERT INTO @src (category, division, trade_item, unit, nsw, vic, qld, wa) VALUES
-- 1. Earthworks & Civil  -> Civil
(N'Earthworks & Civil', N'Civil', N'Bulk excavation (rock)',          N'm3',    45.00,   42.00,   48.00,   52.00),
(N'Earthworks & Civil', N'Civil', N'Bulk excavation (soil)',          N'm3',    18.50,   17.00,   19.50,   21.00),
(N'Earthworks & Civil', N'Civil', N'Fill and compact (imported)',     N'm3',    32.00,   30.00,   34.00,   36.00),
(N'Earthworks & Civil', N'Civil', N'Fill and compact (on-site)',      N'm3',    12.00,   11.50,   13.00,   14.00),
(N'Earthworks & Civil', N'Civil', N'Trenching (up to 1.5m)',          N'lin m', 85.00,   80.00,   88.00,   92.00),
(N'Earthworks & Civil', N'Civil', N'Road base (150mm thick)',         N'm2',    28.00,   26.00,   30.00,   32.00),
(N'Earthworks & Civil', N'Civil', N'Asphalt (40mm wearing course)',   N'm2',    35.00,   33.00,   37.00,   39.00),
-- 2. Concrete  -> Structures
(N'Concrete', N'Structures', N'Concrete supply & pour (32MPa)', N'm3',    285.00,  270.00,  295.00,  310.00),
(N'Concrete', N'Structures', N'Concrete supply & pour (40MPa)', N'm3',    320.00,  305.00,  330.00,  345.00),
(N'Concrete', N'Structures', N'Concrete supply & pour (50MPa)', N'm3',    380.00,  365.00,  395.00,  410.00),
(N'Concrete', N'Structures', N'Reinforcement (supply & fix)',   N'tonne', 3200.00, 3100.00, 3300.00, 3450.00),
(N'Concrete', N'Structures', N'Formwork (standard)',            N'm2',    95.00,   90.00,   98.00,   105.00),
(N'Concrete', N'Structures', N'Formwork (complex/curved)',      N'm2',    145.00,  138.00,  150.00,  160.00),
(N'Concrete', N'Structures', N'Post-tensioning',                N'tonne', 4500.00, 4350.00, 4650.00, 4800.00),
-- 3. Structural Steel  -> Structures
(N'Structural Steel', N'Structures', N'Structural steel (supply)',    N'tonne', 3800.00, 3650.00, 3900.00, 4100.00),
(N'Structural Steel', N'Structures', N'Structural steel (erection)',  N'tonne', 2200.00, 2100.00, 2300.00, 2400.00),
(N'Structural Steel', N'Structures', N'Steel fabrication (standard)', N'tonne', 4500.00, 4300.00, 4600.00, 4800.00),
(N'Structural Steel', N'Structures', N'Steel fabrication (complex)',  N'tonne', 6200.00, 5900.00, 6400.00, 6700.00),
(N'Structural Steel', N'Structures', N'Bolted connections',          N'each',  85.00,   80.00,   88.00,   92.00),
(N'Structural Steel', N'Structures', N'Welded connections',          N'lin m', 120.00,  115.00,  125.00,  130.00),
-- 4. Piling & Foundations  -> Structures
(N'Piling & Foundations', N'Structures', N'Bored piles (600mm dia)',  N'lin m', 450.00,  430.00,  465.00,  490.00),
(N'Piling & Foundations', N'Structures', N'Bored piles (900mm dia)',  N'lin m', 680.00,  650.00,  700.00,  735.00),
(N'Piling & Foundations', N'Structures', N'Bored piles (1200mm dia)', N'lin m', 950.00,  910.00,  980.00,  1030.00),
(N'Piling & Foundations', N'Structures', N'Sheet piling (temporary)', N'm2',    180.00,  170.00,  185.00,  195.00),
(N'Piling & Foundations', N'Structures', N'Pad footings (average)',   N'm3',    350.00,  335.00,  360.00,  380.00),
-- 5. Labor Rates (all-in)  -> Civil
(N'Labor Rates', N'Civil', N'General laborer',        N'hr', 65.00,  62.00,  68.00,  72.00),
(N'Labor Rates', N'Civil', N'Skilled tradesperson',   N'hr', 95.00,  90.00,  98.00,  105.00),
(N'Labor Rates', N'Civil', N'Plant operator (< 20t)', N'hr', 85.00,  80.00,  88.00,  95.00),
(N'Labor Rates', N'Civil', N'Plant operator (> 20t)', N'hr', 110.00, 105.00, 115.00, 120.00),
(N'Labor Rates', N'Civil', N'Crane operator',         N'hr', 130.00, 125.00, 135.00, 140.00),
(N'Labor Rates', N'Civil', N'Site supervisor',        N'hr', 120.00, 115.00, 125.00, 130.00),
(N'Labor Rates', N'Civil', N'Project engineer',       N'hr', 145.00, 140.00, 150.00, 155.00),
-- 6. Plant & Equipment (dry hire)  -> Plant
(N'Plant & Equipment', N'Plant', N'Excavator 20t',                 N'day', 1200.00, 1150.00, 1250.00, 1350.00),
(N'Plant & Equipment', N'Plant', N'Excavator 30t',                 N'day', 1800.00, 1700.00, 1850.00, 2000.00),
(N'Plant & Equipment', N'Plant', N'Bulldozer D6',                  N'day', 1500.00, 1450.00, 1550.00, 1650.00),
(N'Plant & Equipment', N'Plant', N'Mobile crane 50t',              N'day', 3500.00, 3300.00, 3600.00, 3800.00),
(N'Plant & Equipment', N'Plant', N'Mobile crane 100t',             N'day', 5500.00, 5200.00, 5700.00, 6000.00),
(N'Plant & Equipment', N'Plant', N'Concrete pump (truck-mounted)', N'day', 2800.00, 2650.00, 2900.00, 3100.00),
(N'Plant & Equipment', N'Plant', N'Compactor (vibratory roller)',  N'day', 800.00,  750.00,  850.00,  900.00);

/* -----------------------------------------------------------------------------
   Unpivot the 4 regional columns into normalized rows and compute derived cols.
   ----------------------------------------------------------------------------- */
INSERT INTO dbo.rate_library
    (category, division, trade_item, unit, region, rate_aud, owner_group, effective_date, content_text)
SELECT
    s.category,
    s.division,
    s.trade_item,
    s.unit,
    r.region,
    r.rate_aud,
    r.owner_group,
    CONVERT(DATE, '2026-04-01') AS effective_date,
    CONCAT(
        s.trade_item, N' - ', s.category, N' - ', r.region,
        N' - ', r.rate_aud, N' AUD per ', s.unit,
        N' (division: ', s.division, N', effective 2026-04-01).'
    ) AS content_text
FROM @src AS s
CROSS APPLY (VALUES
    (N'NSW', s.nsw, N'grp-estimating-nsw'),
    (N'VIC', s.vic, N'grp-estimating-vic'),
    (N'QLD', s.qld, N'grp-estimating-qld'),
    (N'WA',  s.wa,  N'grp-estimating-wa')
) AS r(region, rate_aud, owner_group);
GO

/* Helpful non-clustered index for region/division filtering (Track 1). */
CREATE INDEX IX_rate_library_region_division
    ON dbo.rate_library (region, division) INCLUDE (trade_item, rate_aud);
GO

/* Quick verification (expected: 156 rows = 39 items x 4 regions). */
SELECT region, COUNT(*) AS rows_per_region
FROM dbo.rate_library
GROUP BY region
ORDER BY region;
GO

/* -----------------------------------------------------------------------------
   REQUIRED - enable SQL integrated change tracking. The Foundry IQ Azure SQL
   knowledge source (indexedSql) uses the SQL integrated change-tracking policy
   for tables, so the generated indexer fails at create time with
   "Integrated change tracking is not enabled for table '<name>'" unless both
   the DATABASE and the TABLE have change tracking turned on. Requires
   ALTER DATABASE permission (run as the SQL Entra admin). Safe to re-run.
   ----------------------------------------------------------------------------- */
ALTER DATABASE CURRENT SET CHANGE_TRACKING = ON
    (CHANGE_RETENTION = 2 DAYS, AUTO_CLEANUP = ON);
GO
ALTER TABLE dbo.rate_library ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = OFF);
GO
