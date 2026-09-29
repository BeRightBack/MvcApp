# AGENTS.md — MvcApp (source app, port 9001)

Persistent context for any coding session on this repo. Read before changing anything.
Behavior standards (verify-before-done, no improvised data, error-first diagnosis, small
steps, reuse-over-rewrite, sub-agent delegation) live in the global
`~/.config/opencode/AGENTS.md` — this file holds only project facts and project-specific rules.

## Project non-negotiables (user-mandated)
1. **Never improvise data or content** — when seeding, generating, or porting, use the REAL
   data that already exists (canonical MySQL DB, the actual files in this repo, real image
   files). Do NOT invent banner copy/HTML, product names/prices, placeholder content, or
   markup the user didn't request. If real data is missing: STOP and ask the user first.
2. **User-visible changes** — prove them on real output (build, run, load pages,
   screenshot, check DB rows) and confirm with the user before finalizing. Never ship on
   "should be fine".
3. **Port 9001 belongs to the user** — the user launches the app themselves; the port must
   never be left occupied by a background instance.
4. **Prove in the source app first**, port to the VSIX template only after the user
   confirms — never work in the template first. See "Template & porting".
5. **Never edit** numbered disposable generation outputs (`MvcApp1`–`MvcApp5`, none under
   `E:\` today but treat any as disposable), `MvcApp.Template\` (stale v1.0.0-era copy), or
   `bin\Release\net472\ProjectTemplates\` copies (stale build artifacts).
6. If the user references rules or decisions you cannot find on disk, ASK — never assume.

## What this project is / layout
- Source solution root: `E:\Apps\MvcApp` — solution file `MvcApp.slnx`.
- Projects: MvcApp.Core, .Services, .Common, .Infrastructure, .Identity, .Razor,
  .Localization, .Web, .Tests, plus modules
  MvcApp.Module.{Blog, Chat, Forum, Messages, Store, IPTV, Pages, Ads, Utility, Video, Shared}.
- Run commands from the root with `workdir`; never `cd` inside commands.
- Kestrel binds `http://localhost:9001` via `appsettings.json` → `Kestrel:Endpoints:http:Url`.

## App lifecycle (MANDATORY — on every task that changed .cs/.cshtml)
1. Kill anything on 9001:
   `$p = Get-NetTCPConnection -LocalPort 9001 -State Listen -ErrorAction SilentlyContinue; if ($p) { Stop-Process -Id $p.OwningProcess -Force }`
2. `dotnet build MvcApp.Web` → must end with 0 errors (10 pre-existing warnings are OK —
   verified 2026-09-23). Do NOT use `--no-incremental`: it deletes the BundlerMinifier
   `wwwroot\css\site.min.css` before `DefineStaticWebAssets` runs → 1 error. A plain
   `dotnet build` regenerates it.
3. Start detached:
   `Start-Process -FilePath "dotnet" -ArgumentList "run","--project","MvcApp.Web","--no-build","--urls","http://localhost:9001" -WorkingDirectory "E:\Apps\MvcApp" -WindowStyle Hidden`
4. Verify by POLLING, not fixed sleep: `Invoke-WebRequest http://localhost:9001/` until
   HTTP 200 (~4-10s warm; up to ~60s if seeding ran fresh). The app listens BEFORE
   background seeding finishes — early requests 500 with `Table 'identity_db.<x>'
   doesn't exist` by design. Never kill the app before it returns 200 (see Databases).
5. Behavior-affecting changes also need: `dotnet test MvcApp.Tests` → 13/13 passed, ~24s,
   no DB/environment required (verified 2026-09-23).
6. When done: STOP the instance and FREE port 9001, then confirm
   `Get-NetTCPConnection -LocalPort 9001 -State Listen` returns nothing.

CSS-only changes under `wwwroot` need no rebuild/restart.

## Databases (MySQL 8.0.46 — reinstalled 2026-09-23, app DBs rebuilt from scratch)
- Server: service `MySQL80` (Running, Automatic); binary
  `C:\Program Files\MySQL\MySQL Server 8.0\bin\mysqld.exe`,
  defaults-file `C:\ProgramData\MySQL\MySQL Server 8.0\my.ini`.
- Client: `C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe` — NOT on PATH;
  always use the full path (matches the historical record in this repo).
- Schemas: `identity_db`, `localisation_db` (lowercased on disk —
  `lower_case_table_names` is active, names written as `Identity_db` in
  `appsettings.json` resolve fine). EF contexts: `UserDbContext`,
  `LocalizationDbContext` (NOT `LocalisationDbContext`).
- **Serilog log DB — CORRECTED 2026-09-27 (verified):** config key is
  `appsettings.json` → `ConnectionStrings:SerilogLogs` (it was `Serilog:ConnectionString:Logs`;
  the code, its comment and its exception message all said `…ConnectionStrings…` — plural, which
  never existed, so the documented env-var override was wrong; `Program.cs` now reads the
  singular-correct `ConnectionStrings:SerilogLogs` and the dead `ConnectionStrings:MvcAppLogs`
  entry + the whole `Serilog` section were deleted, which also cleared the VS warning
  "Property name is not allowed by the schema" at the old line 58 — the `$schema`
  (json.schemastore.org/appsettings.json) defines only Kestrel/Logging/AllowedHosts/
  ConnectionStrings, so the custom `Serilog` section was flagged; custom names INSIDE
  `ConnectionStrings` are fine. Override env var = `ConnectionStrings__SerilogLogs`.
  The connection string points at the **REMOTE** `mysql.xtrasvr.com`, database
  **`serilogsDb`** (keys `Server|Database|Uid|Pwd`) — NOT local `MvcApp_logs` as previously
  documented. That database did not exist (`SHOW DATABASES`, `lower_case_table_names=0` so
  names are case-sensitive) and MySQL log writes were failing silently. **FIXED 2026-09-27:**
  `CREATE DATABASE serilogsDb CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;` run with the
  app user (has `GRANT ALL`); the sink auto-created its table. Verified live: 19 rows written
  seconds after 3 home-page hits, `Message` = `HTTP "GET" "/" responded 200 in …`,
  levels Information+Warning. **Table shape gotcha:** the auto-created `Logs` table uses
  `Timestamp varchar(100)` — NOT the `@Timestamp` name the docs imply — plus
  `Level|Template|Message|Exception|Properties` and an auto `_ts timestamp` column; order/filter
  on `_ts` (`MAX(_ts)`, `MIN(_ts)`), never on `@Timestamp` (ERROR 1054). Backticked SQL is
  fragile through the PowerShell pipe — write the .sql file and pipe `Get-Content -Raw`.
- **Seeding is BACKGROUND on dev start** (`Program.cs`: "Starting background seeding"
  before `Now listening`): migrations + `SettingsSeeder`/`SeedLanguage`/chat/forum etc.
  run concurrently while the app already serves. On a fresh DB allow ~20-40s before /
  returns 200; poll, never fixed-sleep, and NEVER kill the app mid-seed: a partial run
  leaves tables without `__EFMigrationsHistory` rows and the next start fails with
  `FTL Error during background seeding ... Table 'role' already exists`. Recovery
  (verified): `DROP DATABASE identity_db; DROP DATABASE localisation_db;` then start
  once and let it reach 200.
- Old pre-reinstall data is GONE (previous MySQL install was removed before 2026-09-23);
  the current DBs contain exactly what the code seeders create (26 SystemSettings rows,
  57 likes, 57 messages, test users, languages).
- Uid/pwd live in `MvcApp.Web\appsettings.json` → `ConnectionStrings` (gitignored).
  **Never copy credential values into this file.**
- PowerShell inline backtick-escaping of SQL column `Key` FAILS. ALWAYS write SQL to a
  temp file (write tool), then pipe:
  `Get-Content -Raw "$env:TEMP\opencode\<file>.sql" | & "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe" -u <uid> -D identity_db`
  with the password supplied via `$env:MYSQL_PWD` (never on the command line or in this file).
- `Key`/`Group`/`Value` are reserved in MySQL — always backtick-quote in raw SQL.
- **MySQL casing (`lower_case_table_names`):** after a MySQL service restart, databases
  created with capital letters can become invisible/orphaned while the app looks for the
  lowercased name and fails with `Unknown database ...`. Diagnose with `SHOW DATABASES`.

## Remote MariaDB (mysql.xtrasvr.com / svr1.xtrasvr.com) — FIXED + VERIFIED 2026-09-26
- `mysql.xtrasvr.com` and `svr1.xtrasvr.com` are the same box (149.56.102.60, Debian 13, ISPConfig,
  OVH). MariaDB runs there and the source app's gitignored `appsettings.json` points its
  ConnectionStrings at it (uid `swan3344`, DBs `Identity_db`/`Localisation_db`).
- **2026-09-26 fix:** access was killed by a raw runtime iptables rule `DROP tcp dpt:3306 !lo`
  sitting at INPUT position 1, ABOVE the ufw chains — ufw already had `3306/tcp ALLOW`
  (persisted in `/etc/ufw/user.rules`), but the raw rule overrode it (host firewall, NOT a
  provider firewall: mariadbd was listening on 0.0.0.0:3306 all along). Fix was
  `iptables -D INPUT 1`. Nothing re-adds it: no `iptables-persistent`/`/etc/iptables`, no
  cron/rc.local/systemd, survived a full ISPConfig `server.sh` minute cycle. If 3306 ever
  dies again, check `iptables -L INPUT -n | grep 3306` on the server first.
- **Secondaries discovered same day (both fixed):** (a) client IP 45.44.236.68 had no PTR record and
  MariaDB resolves client hostnames per new connection (default config) → every fresh connect stalled
  ~8-10s in reverse-DNS; fixed by adding `45.44.236.68 client-45-44-236-68.xtrasvr.com` to the server's
  `/etc/hosts`. (b) page latency ~7.4s is NOT server-side: pooled queries are ~26-37ms warm
  (`Threads_created` delta 0) — the page fires many sequential DB round-trips at ~35ms WAN RTT
  (nav per-item module checks etc.); on localhost it'd be ~200ms. Code-level
  caching (settings/nav/module flags) would fix it if ever needed; login and pages DO work against the
  remote now. **RE-MEASURED 2026-09-28: the home page is 88 round-trips, not 209** (that figure was
  a stale, different measurement context). 88 x ~26ms ~= 2.3s, matching EF's reported DB time
  (2307 ms) inside an ~8s page. Breakdown: **37 SystemSettings lookups** (no caching in
  `SettingsService.GetAsync`; the nav asks `IsModuleEnabledAsync` per item across navbar + footer +
  social + profile dropdowns, plus SiteTemplate/Branding), 15 Language + 15 localization-string
  queries, 8+6+2 AdZone/AdBanner, 1 user/session, 1 VisitorLogs insert, 3 for the member query
  (post-AsSplitQuery), 1 COUNT. To measure: EF command logs are suppressed by
  `MinimumLevel.Override("Microsoft", LogEventLevel.Warning)` in `Program.cs` — temporarily set it
  to Information; each `Executed DbCommand (Xms)` line is followed by its SQL on the next line.
- **Localization audit + self-translate made non-blocking (2026-09-28):**
  - `Localisation_db` holds **5 languages (en, fr, es, it, pt) x 212 keys = 1060 rows**.
    German is configured (`DeepLConfig:TargetLangDe`, mapped in `GetTargetLanguage`) but NOT
    seeded — user chose to keep the 5.
  - **218 files** still contain hardcoded user-visible text (Web 97, Identity 31, Store 16,
    Blog 12, Forum 11, Pages 9, Ads 9, Razor 8, rest across Chat/Video/IPTV/Utility/Messages),
    and **145 of the 308 distinct `@Localizer` keys used in views are missing from the
    database** — the blocking path was already being hit before this work started.
  - The inline miss path called DeepL for every culture with `.Result` inside a property
    getter, but it returned the key either way. Self-translate is now preserved exactly and
    just moved off the request path: `BackgroundTranslationService` (channel + dedup, 429
    backoff) stores the translation a moment later. Verified end-to-end on a real page.
  - `LocalizationCache` (singleton) holds Languages + StringResources: **home page 1645-3213
    ms with 20 round-trips** (88 -> 50 -> 20 across the two caches; 6904-7200 ms originally),
    localization queries 30 -> 0.
  - **Data quirk:** `StringResources` has duplicate `Name` rows per language; the cache keeps
    the first occurrence to match the old `FirstOrDefault` (a `ToDictionary` threw on this and
    500'd `/iptv`).
  - **Identity pages could not localize at all** — `MvcApp.Identity/Pages/_ViewImports.cshtml`
    had no `IStringLocalizer` injection. Added, then localized Login/Register/ForgotPassword/
    ResetPassword/ConfirmEmail/ResendEmailConfirmation/Logout/Lockout/AccessDenied, and
    switched 11 models from `[Display]` to the existing `[LocalizedDisplayName]`.
  - **form-floating quirk (pre-existing, exposed by localization):** the label and the input's
    placeholder are both rendered, so each field drew its name twice — invisible while both
    were English, visibly garbled once the placeholder was translated. Auth inputs now use
    `placeholder=" "`, which keeps `:placeholder-shown` working (label still floats) with the
    label as the only visible text.
  - **Static `LocalizationContext` — FIXED 2026-09-28 (commit `e2dd6bb`), and the earlier
    warning here was overstated.** It no longer parks a request-scoped service on a static; it
    resolves the localizer from the CURRENT request's services and returns null when there is no
    request, so `[LocalizedDisplayName]` falls back to untranslated text instead of throwing in a
    hosted service or test. `LocalizationMiddleware` now does nothing per request.
    **It was never a cross-request contamination bug**: `DbStringLocalizer` resolves the culture
    from `Thread.CurrentThread.CurrentUICulture` at lookup time, so even a stale instance returned
    the right translation. The real defect was the null `HttpContext` dereference.
  - **Front end fully localized 2026-09-28 (commit `da5b4fd`): 1900 strings across 214 views
    and components** — public views, admin area, all module views, Identity pages, `.razor`.
    The reason 218 files were hardcoded was structural, not effort: **only IPTV had an
    `IStringLocalizer` injection in `_ViewImports`, and 8 of the 10 modules did not reference
    `MvcApp.Localization` at all**, so their views could not localize even in principle (Ads
    and Pages referenced the project but had no injection). All 10 modules + `MvcApp.Razor`
    are now wired. If a new module is added, it needs the project reference AND the
    `_ViewImports` inject or its views silently render hardcoded English.
  - **Razor HTML-encodes a `LocalizedString` value** (measured, not assumed): a key containing
    an entity like `&mdash;` renders as that literal text, and DeepL would translate the
    entity characters too. Decode entities to their Unicode characters in the key (`—`
    encodes to itself and displays fine). Keep a node's leading/trailing whitespace OUTSIDE
    the call — adjacent nodes are often separated only by a space, and trimming it into the
    key glued words together (`WhereSparksFly`).
  - **A/B verification method for view changes** (used for the localization commit, worth
    reusing): `git stash push --pathspec-from-file=<the view files>`, capture each page's
    `document.body.innerText` in English, `git stash pop`, capture again, diff word by word.
    Stashing is atomic, so unlike copy/restore there is no way to compare HEAD with HEAD and
    get a false pass — an earlier copy/restore harness silently did exactly that and produced
    a meaningless "all identical". Note identical English output CANNOT prove the edits are
    live (edited code renders the same English by design) — liveness comes from a French
    capture. **Non-deterministic pages: `/Admin` (live counters), `/Admin/SystemLogs` (live log
    rows), `/Home/Test` (random weather)** — they differ between two runs of the same build,
    so exclude them from text diffs.
  - **Pre-translation done 2026-09-28 (commit `69b5cf4`): 212 -> 1350 distinct keys (5541 rows).**
    Run through the app's own `BackgroundTranslationService` (so the pacing/429-backoff/dedup
    were the app's, not a parallel script) via a **temporary, now-removed** env-gated hook in
    `Program.cs` (`MVCAPP_TRANSLATE_KEYS_FILE` enqueued the keys, then the hook was deleted —
    `Program.cs` is byte-identical to before). ~4900 translations, **1 rate-limit event, 0
    failures**. Coverage: 1345 keys referenced in source, **1318 resolve in the DB**; the other
    27 are intentionally English (setting keys, C# type names, validation-message lookup keys
    like `CompareError`, and brands). To re-run after adding copy: extract the keys, enqueue
    them, let the queue drain, then measure — do NOT call DeepL from a script, so the app stays
    the single source of truth for retry behaviour.
  - **`StringResources` collation is `utf8mb4_uca1400_ai_ci`** — case- AND accent-insensitive.
    Consequences that cost real time: (a) `Live`/`LIVE` and `Created By`/`Created by` match
    each other, so a PowerShell ordinal comparison over-reports missing keys — measure coverage
    case-insensitively or you will chase phantom gaps; (b) it is PAD SPACE, so a **leading**
    space is significant and a key stored as `' Text'` can never be found. Three keys were
    seeded with a leading space; they are trimmed now. When measuring coverage, do NOT pipe
    `mysql.exe` output through the PowerShell console (it re-encodes and breaks non-ASCII
    comparison) — redirect the process output to a file and read it as UTF-8.
  - **Nav labels are localized in `NavService.FilterAsync`**, the single funnel every navbar,
    footer and dropdown list passes through, so both `NavDefaults` (code) and the admin's
    `SiteNav` JSON (DB) are covered. This is why `MvcApp.Services` references
    `MvcApp.Localization` (no cycle: Localization references only Core).
  - **Never wrap technical identifiers in `@Localizer`** — it reaches the text scan only when
    shown inside `<code>` or an input-group prefix, but the damage is real: DeepL translated the
    icon class `bx bx-heart` into `bx bx-cœur` and it was stored, so an admin copying that
    example would paste a class that renders no icon. Same for setting keys, C# type names and
    URL path examples. A bare `...` is also left alone.
  - **Remaining English surface closed 2026-09-28 (commit `0d3f917`, ported as VSIX 1.0.59):**
    260 attribute values (`placeholder`/`title`/`alt`/`aria-label`, using the
    `placeholder="@(Localizer["Close"])"` form — `@( )` permits nested quotes inside a
    double-quoted attribute so the emitted HTML is unchanged) and 99 controller
    TempData/ModelState literals across 26 controllers (16 primary-constructor parameters, 10
    classic constructors needing a field). English verified unchanged across 30 pages; 32/32 tests.
  - **`DbStringLocalizer` ignores format arguments** — its `this[name, args]` returns `this[name]`,
    so `Localizer["{0} keys", n]` renders a literal `{0}`. Use `string.Format(Localizer["..."], n)`.
  - **No `StringResources` row is ever stored for the source language** (the localizer returns the
    key, and the key is the source text). Any coverage check MUST exclude it or every key looks
    incomplete — that mistake first reported 1149 missing keys when only 55 were real.
  - **Pre-translation is now a supported operation, not a code hook:** `/Admin/Localization`
    reports coverage and has a POST + antiforgery + confirm action that queues incomplete keys into
    `BackgroundTranslationService`. Copy added in a new release is still translated automatically
    on first render — self-translate is not replaced. The `Program.cs` `MVCAPP_TRANSLATE_KEYS_FILE`
    hook used earlier on 2026-09-28 is deleted.
  - **Three defects in the translation worker, all mine, fixed 2026-09-28 (`81839d5`, VSIX
    1.0.61).** The "Translate missing keys" button queued keys and appeared to do nothing:
    1. The worker's guard skipped any DeepL result **equal to the source text**, but "Admin",
       "RSVP", "PayPal", "Design", "Smart TV" are genuinely identical across languages, so the
       correct answer was discarded — API call made, result thrown away, no log line. Only the
       SOURCE language is skipped now. DeepL was never at fault (a direct call returned 200).
    2. The source-language guard compared DeepL codes: `TargetLangEn` is **"EN-US"** while
       `SourceLang` is **"EN"**, so it never matched and 161 junk source-language rows were
       written (en 212 -> 373). It now compares the language's `Culture`, and skips before the
       lookup so no API call is spent. Those rows were deleted after checking that only 2 of 373
       differed from their key, both merely by a leading space — **no English copy was altered**
       (13 pages byte-identical before/after).
    3. The coverage metric grouped keys **case-sensitively** while the column collation
       (`utf8mb4_uca1400_ai_ci`) compares them case- and accent-insensitively. The table holds
       both `password` and `Password`; the app serves either from the other (which is why the
       login page shows "Mot de passe"), but the metric saw two half-filled keys and could never
       reach zero. Keys are now normalized the way the database compares them. **Compare keys the
       way the database does, not the way your language's string comparer does.**
    Self-translate was never disabled at any point — the keys were being translated and dropped.
  - **`SiteTemplate` is `Dating`, not `Luxury`** (corrected 2026-09-28 by reading the live DB
    row; the earlier "Luxury" note was stale). The active home view is therefore
    `Views/Home/Index.Dating.cshtml` — check the setting before touching a template's landing
    page. All 11 templates are localized regardless.
  - **Binary files added to the VSIX template MUST use `<ProjectItem ReplaceParameters="false">`.**
    The template engine performs text replacement on files marked `true`, which corrupts binaries.
    The shipped `Resources\translations.json.gz` was declared `true` in 1.0.58/1.0.59 — a real
    latent bug that a detokenized build CANNOT catch, because it reads the file from the template
    tree rather than through the template engine. Fixed in 1.0.60. All 61 jpg/png ProjectItems in
    `MvcApp.Web.vstemplate` were already `false`, so the pattern is now consistent.
  - **Attribute/localizer passes mask `<script>`, so JS string literals get missed.** The four
    ad-banner placeholders set via `newInput.placeholder = '...'` were skipped for that reason
    (fixed 2026-09-28, `e2dd6bb`). When injecting a translation into JavaScript, emit it as JSON
    via `@Html.Raw(System.Text.Json.JsonSerializer.Serialize(Localizer["..."].Value))` — a value
    containing an apostrophe would otherwise terminate a single-quoted JS literal.
  - **Sentence fragments split at inline markup are a known, accepted limitation.** Two keys begin
    with punctuation (", and restarting the application.", ", as it can result...") because a
    `<code>` element sits mid-sentence. English renders correctly and the French reads naturally
    (", puis de redémarrer l'application."). Merging them into one key would mean moving or
    dropping the inline styling — a copy decision, not a bug.
- SSH: `ssh root@svr1.xtrasvr.com` (root password is user-held, not on this machine). MySQL root shell
  on the server needs its own password; `swan3344` (the app user) has `GRANT ALL ON *.* WITH GRANT
  OPTION` including `mysql.*` — usable for server-side MySQL inspections instead of root.
- The app's Serilog `Logs` sink reads `ConnectionStrings:SerilogLogs`, which points at the
  REMOTE `mysql.xtrasvr.com` / database `serilogsDb` (see the corrected Databases entry —
  the earlier "local `MvcApp_logs`" note was wrong). `serilogsDb` was created 2026-09-27 and
  the sink now persists rows there (file logging under `MvcApp.Web\logs\` also stays on).

## Key settings (SystemSettings table: Key, Value, Description, Group, UpdatedAt, UpdatedBy)
- `SiteTemplate` = active template (currently **`Dating`** — read from the live DB 2026-09-28;
  the earlier "Luxury" note was stale. Exact template Name, compared OrdinalIgnoreCase). The
  active landing view follows it, so `/` renders `Views/Home/Index.Dating.cshtml`.
- `Module.*.Enabled` = module toggles (Ads, Blog, Chat, Forum, Iptv, Messages, Pages, Store,
  Utility, Video). Modules deselected at generation time have NO `Module.<X>.Enabled` row and
  are hidden from the admin Modules list (`ModuleManager.GetAllModulesAsync` skips modules
  without a row).
- `SiteNav.{template}.Navbar` / `.Footer` = per-template nav JSON (`List<NavItem>`), written
  ONLY when an admin saves in Admin → Templates → Navigation. NOT seeded at app startup —
  `NavDefaults` (code, MvcApp.Core) is the fallback. Migration
  `20260810010252_RemoveStaleSiteNavSnapshots` deleted the stale seeded rows. Legacy
  `SiteNav.{template}` (single list) is still read as a navbar fallback.
- `Template.{template}.LandingEnabled` = per-template landing-page toggle (default true).
- Nav architecture: navbar = `NavService.GetNavItemsAsync` reads `SiteNav.{template}.Navbar`
  (legacy `SiteNav.{template}` fallback) then `NavDefaults.GetNavbar(template)` then
  `GetGenericNavbar()`; footer = `GetFooterItemsAsync` reads `SiteNav.{template}.Footer` then
  `NavDefaults.GetFooter(template)` then `GetGenericFooter()`. `FilterAsync` drops disabled
  modules and auth/admin-restricted items.
- Nav defaults: `NavDefaults._byTemplate` holds one curated per-template set shared by navbar
  and footer; generic fallback derives from `NavDefaults.ModuleHome`. Catalog of linkable
  module pages: `NavCatalog` (keyed `|controller|action|`).

## Admin access for verification
- Login: `/Account/Login`, user `admin@frenzyzone.com` (or `admin`); password is in
  `appsettings.json` → `Administrator` section (gitignored — do not copy here).
- Admin pages: `/Admin/Templates`, `/Admin/TemplateNav?template=<Name>`, `/Admin/Modules`,
  `/Admin/SystemSettings`.
- Browser automation: puppeteer-core + Edge probes in `%TEMP%\opencode\puppet\probe*.js`;
  node runs from that dir. Vision subagent can verify screenshots.

## Module wiring (do not regress)
- Module `AddX()` signatures: `AddAdsModule()`, `AddBlog()`, `AddChat()`, `AddForum()`,
  `AddMessages()`, `AddStore()`, `AddIptv()`, `AddPages()`, `AddUtility()`, `AddVideo()`.
- `ModuleConfigurationRegistry` (Infrastructure, static) collects module entity-config
  assemblies; each module's `ServiceRegistration` calls `AddEntityConfigurationAssembly(...)`.
  The 23 entity configs of Blog/Chat/Forum/IPTV/Pages/Store live in
  `Infrastructure\Data\EntityConfigurations\`; Ads/Utility/Video keep theirs in-module
  (moving them would create Infrastructure→module circular references — do NOT move them).

## Generation-time deselection MUST stay consistent end-to-end
When a module is deselected at generation, ALL of these must hold:
1. Module project folder is removed by the wizard (DTE cleanup) and not referenced by Web.
2. No `AddX()`/usings/application-parts for it in Program.cs / Web.csproj (guards + wizard).
3. `SettingsSeeder._moduleDefaults` must NOT seed `Module.<X>.Enabled=true` for it
   (guarded per-module via `$ext_includeX$`). **This is what hides its nav/admin entries.**
4. NavCatalog / NavDefaults may still list its items (Core has no per-module tokens) — they
   are filtered at render time by `NavService.FilterAsync` via
   `IModuleManager.IsModuleEnabledAsync`. So the settings seed (3) is the gate. Never
   hard-enable a module the generation can omit.

## Known pitfalls / watch list
- EF warnings `Model[10632] No instantiatable types ... Blog/Chat/Forum/Store` are benign
  (configs were moved to Infrastructure).
- **MariaDB migration lock + raw-SQL semicolons (FIXED + VERIFIED 2026-09-25):**
  1. Oracle MySql.EntityFrameworkCore's `MySQLHistoryRepository.AcquireDatabaseLock` runs
     `SELECT GET_LOCK('__EFMigrationsLock',-1)` and casts to `Int64`; MariaDB (11.8.6,
     remote) returns NULL for negative timeouts → `InvalidCastException` on ANY
     `MigrateAsync`/`Update-Database`, even with nothing pending. Root cause verified by
     DLL string-scan + provider source. `dotnet ef database update` therefore can NEVER
     work against the remote; use `dotnet ef migrations script` (no lock involved) + apply
     via `mysql.exe -e "source <file>.sql"`. Pomelo keeps the lock but uses a positive 72h
     timeout (would work on MariaDB) — not needed, provider swap not done.
  2. `migrationBuilder.Sql("...")` calls WITHOUT a trailing `;` break script generation:
     Oracle's script generator appends no batch terminator inside `START TRANSACTION`, so
     the raw DELETE swallowed the following history INSERT → ERROR 1064 at mysql.exe line
     1081. Exactly three existed: `20260810010252_RemoveStaleSiteNavSnapshots.cs:13` and
     the two Member-role DELETEs in `20260815000205_AddUserBans.cs:66-67` — all three now
     end with `;` (source + template copy of RemoveStaleSiteNavSnapshots; AddUserBans is
     source-only, template stops at RemoveStaleSiteNavSnapshots = 18 of 31 migrations).
     Any NEW migration with raw SQL must terminate it with `;`.
  3. After the fix, the full script grew exactly +3 bytes; continuation script
     (`-From 20260806200416_AddUtilityEntities`) applied to the remote: `Identity_db`
     67 tables / 31 history rows, `Localisation_db` 3 tables / 1 history row — verified via
     information_schema. Production mode on the remote: clean start, HTTP 200, DB-backed
     pages render (no seeding). Source build 0E/10W; tests 13/13.
  4. **Development seeding on MariaDB FIXED 2026-09-25** (commit `7a925eb`, pushed
     `71ef7e8..7a925eb`): the 7 seed steps in Program.cs called `MigrateAsync()`
     unconditionally → MariaDB lock crash → NOTHING seeded → remote had 0 users / 0
     SystemSettings / 0 Languages → login impossible ("i cant login"). New
     `EnsureMigratedAsync(db)` helper (mirrors `Seeder.cs:51` pending-guard) only calls
     `MigrateAsync()` when `GetPendingMigrationsAsync()` returns any; all 7 seed steps
     (SeedLanguageAsync/Settings/Forum/Blog/PageSnippets/EventCategories/InterestTags)
     use it. VERIFIED live on the remote (own instance, port 9001, then freed):
     Development seeding COMPLETED — "Background seeding completed.", remote rows =
     27 users / 26 SystemSettings / 5 Languages / 2 Roles; `POST /Account/Login`
     (Mulva / Passw0rd123!!) → 302 + "User Mulva logged in."; wrong password → 200
     re-render (auth correctly fails). Template Program.cs got the identical change →
     VSIX 1.0.49.
  5. **Template CSS sync — dropdown contrast (2026-09-25):** template tree's
     `layout-override.css` ended at line 98 WITHOUT the "Dropdown contrast" block (source
     has it lines 81–138, and `_Layout.cshtml:41` loads layout-override.css LAST, after
     template CSS). Pre-existing template sync gap — the fix is in source since the
     initial commit, never ported ("contrast on templates wrong, that was fixed long
     ago"). Overwrote template copy with the source file — SHA-256 identical (159 lines,
     dropdown block present). VSIX 1.0.49 built 0W/0E, payload verified (manifest 1.0.49,
     `EnsureMigratedAsync` in Program.cs, dropdown block in css). NOT installed — user
     defers VS install.
- SiteNav.* rows are created only by the admin saving nav in the editor; code (NavDefaults)
  is the source of truth until then — do NOT reintroduce SiteNav seeding (it freezes
  defaults and leaves them stale). Data migrations deleting stale rows must be no-ops on
  fresh DBs.
- **Production mode never seeds:** `Program.cs` runs `SeedLanguageAsync`/`SeedSettingsAsync`/
  `SeedChatRoomsAsync`/`app.SeedData()` ONLY in `IsDevelopment()` — Production just runs.
  Before running a generated app in Production, apply migrations for BOTH contexts manually.
  On **local MySQL 8.0.46** that works via `dotnet ef database update -c UserDbContext` AND
  `dotnet ef database update -c LocalizationDbContext` (context name is
  `LocalizationDbContext`, NOT `LocalisationDbContext`). On **remote MariaDB
  (mysql.xtrasvr.com) `dotnet ef database update` can NEVER work** — the Oracle provider
  runs `SELECT GET_LOCK('__EFMigrationsLock',-1)` and casts the result to `Int64`; MariaDB
  returns NULL for a negative timeout (verified: `-1`→NULL, `10`→1) → every
  `MigrateAsync`/`Update-Database` dies with `InvalidCastException: DBNull→Int64` at
  `MySQLHistoryRepository.AcquireDatabaseLock`, even on a fully-migrated DB (the migrator
  takes the lock UNCONDITIONALLY before the pending-migration check). Use script generation
  instead (no lock involved): `dotnet ef migrations script --project
  MvcApp.Infrastructure --startup-project MvcApp.Web -c UserDbContext --output <file>.sql`
  (same for `-c LocalizationDbContext`) then apply via `mysql.exe -e "source <file>.sql"`
  (creds via `$env:MYSQL_PWD`). Verified 2026-09-25: remote `Identity_db` = 67 tables /
  31 history rows, `Localisation_db` = 3 tables / 1 history row. Development mode on the
  remote now WORKS on a fully-migrated MariaDB — all seed-step `MigrateAsync()` calls are
  guarded by a pending-check (2026-09-25, see watchlist item 4; verified live: seeding
  completed, login POST 302). A schema-less MariaDB still needs scripted migrations first.
  `launchSettings.json` forces
  `ASPNETCORE_ENVIRONMENT=Development` — to run Production locally use
  `dotnet run --no-launch-profile`. (Migrations/seed need MySQL — see Databases.)
- **Likes/messages seeder bug — FIXED + PORTED 2026-09-23:**
  `SeedLikesAndMessagesAsync` (MvcApp.Identity\Seeder.cs) re-added the same composite-key
  `UserLike` before SaveChanges → tracking conflict, seeding silently failed on every
  fresh DB. Fix: `pendingPairs` HashSet dedupe guard. Verified in source: 57 likes + 57
  messages, build 0W/0E, tests 13/13. Ported to the live VSIX template tree
  (`E:\Apps\MvcApp.Templates\...\ProjectTemplates\MvcApp\MvcApp.Identity\Seeder.cs`);
  rebuilt package = **1.0.40** (manifest bumped, payload extraction verified: fix present,
  800 files). The stale `MvcApp.Template\ProjectTemplates` copy in this repo was NOT touched
  (protected artifact). VSIX install: DEFERRED by user 2026-09-23 — keep the package updated
  when porting, but the focus is this source app; don't chase VS-install/generated-app tests.
- **Rate limiter:** generated apps throttle rapid requests (`Too many requests. Retrying in
  1000ms...`). Pace automated probes (~600ms+ between requests) or they hang/ERR.
- **Attribute-routed module pages:** IPTV and Utility controllers use attribute routes, not
  conventional ones — IPTV home = `/iptv` (NOT `/IptvHome`), IPTV Store = `/iptv-store`,
  Utility home = `/todos` (NOT `/Utility`). `Url.Action` in the navbar resolves these
  correctly; only manual probes get them wrong. Other modules use conventional routes.
- Nav entries with `RequiresAuth = true` (Utility, Discover, Likes) are hidden for anonymous
  visitors — a missing navbar item is not necessarily a bug.
- Attribute routes take precedence over conventional routes; keep generated route patterns
  exact (`/iptv-store`).
- `/Admin/SystemSettings` shows `Value` from the DB, not the code default — verify via SQL
  when in doubt.
- Keep the module list in this file in sync: adding/removing a module affects
  `_knownModules` (ModuleManager), `SettingsSeeder` rows, `NavCatalog`/`NavDefaults`, and
  the generated projects.
- `dotnet clean MvcApp` (bare name) fails with MSB1009 — doesn't resolve the `.slnx`.
- **Login accepts email or username (fixed 2026-09-23):** `MvcApp.Identity\Pages\Account\Login.cshtml.cs`
  previously did `FindByNameAsync` only — typing the email gave "Invalid login attempt."
  Now resolves via `_userManager.Users.SingleOrDefaultAsync(u => u.NormalizedUserName == id
  || u.NormalizedEmail == id)` (needs `using Microsoft.EntityFrameworkCore;`), label +
  placeholder say "Username or email". Verified live: email `admin@frenzyzone.com` and
  username `admin` both log in (302, "User admin logged in."). Ported to VSIX 1.0.41.
- **Test-user avatars show their main photo (fixed 2026-09-23):**
  `MvcApp.Web\Controllers\AccountController.cs` `GetProfilePicture` now falls back to the
  user's **main approved photo** (`Photos` where `IsMain && IsApproved`, first by new Id)
  when a user has no `ProfilePicturePath` file and no `ProfilePicture` BLOB — seeded test
  users have neither, so their navbar/admin avatar was a 404 (silhouette fallback). Remote
  http(s) filenames → `Redirect`; local files → `PhysicalFile` under
  `wwwroot/Photos/{UserName}/{filename}` (mirrors `MemberModel.MainPhotoUrl`). Original two
  branches untouched (admin with uploaded photo unchanged). Verified live (own instance,
  port 9001): Mulva login → navbar avatar renders her main photo 200×161, direct
  `GetProfilePicture` → 200 → `https://static.wikia.nocookie.net/.../Mulva.jpg`. Ported
  to VSIX 1.0.42 (template `Photo` has NO `IsApproved` — port filters on `IsMain` only,
  matching the template's own `EntityMapper` convention).
- **All 33 test-user photos now local & license-free (2026-09-23):** every seeded photo
  (all 26 test users, 33 photo rows — Mulva has 8) was a remote hotlink (wikia, twimg,
  amazon, wikimedia, nyt, reddit...). Replaced ALL with locally hosted randomuser.me
  portraits — `https://randomuser.me/api/portraits/{men|women}/{N}.jpg`, which come from
  the "authorized section of UI Faces" per randomuser.me's copyright notice (free to use,
  no attribution — no license restrictions). Files live at
  `MvcApp.Web\wwwroot\Photos\{UserName}\{n}.jpg` (bare filename `women1..19.jpg` /
  `men1..14.jpg`, 128×128 JPEG, magic `FF D8 FF` verified on download). This includes
  **Kramer** — his earlier local CC BY 2.0 photo (`kramer.jpg`) was deleted in both trees
  and replaced with `men2.jpg` so 100% of test-user photos are restriction-free.
  `UserSeedData.json` (source + VSIX template) now stores only bare filenames (seeder
  writes `Filename` verbatim; `MainPhotoUrl`/`PhotoUrl` build `/Photos/{UserName}/{filename}`
  for non-http values). Live DB: all 33 `Photos` rows updated directly (photo id 14 =
  Kramer → `men2.jpg`) since the seeder skips existing test users. Verified live (own
  instance, port 9001, then freed): Mulva/Kramer/George/Elaine login → avatar renders
  local 128×128 (complete), `/Photos/{User}/{file}` → 200, `GetProfilePicture` → 200,
  Discover page 41 imgs / 0 broken. Source build 0W/0E; bin seed JSON refreshed (`women1`,
  `men2` present, 0 remote URLs). VSIX 1.0.44 (payload verified: 33 photo entries in zip,
  33 `<ProjectItem>` entries in `MvcApp.Web.vstemplate`, 0 remote URLs in shipped JSON).
- **Layout cleanup done 2026-09-23:** `_Layout.cshtml` fixed — bootstrap-icons link corrected
  to `~/lib/bootstrap-icons/font/bootstrap-icons.min.css` (was 404 on every page), and the
  dead classic-SSR trio removed (`<base href="~/" />` in body = spec-inert; `HeadOutlet`
  component = no-op in MVC; `_framework/blazor.server.js` = 404 on net10). Verified: zero
  console/4xx errors on /, /Store, /Home/Test; ServerPrerendered components on the Test page
  still render statically. `AddServerSideBlazor`/`MapBlazorHub` left in Program.cs
  (harmless, removing is a separate cleanup). NOTE: the template's
  `_Layout.cshtml` (`MvcApp.Template\ProjectTemplates` — protected, and the live VSIX copy)
  still had all three dead lines; PORTED to the live VSIX tree in 1.0.45 (2026-09-23:
  `font/` path + dead trio removed; payload verified in the zip; `MvcApp.Template\ProjectTemplates`
  protected copy left untouched).
- **UI correctness + module-gating fixes (2026-09-23, all verified live on port 9001 then freed):**
  - `Middlewares\VisitorLoggingMiddleware.cs`: inline `new HttpClient()` had NO timeout
    (default ~100s) calling `http://ip-api.com/json/{ip}` on `/`, `/Home/*`, `/Subscription*`
    — now `HttpClient { Timeout = 3s }`, loopback IPs skip the external lookup
    (`IPAddress.IsLoopback` guard), and `IpGeolocation:ApiKey` is OPTIONAL (warn, no longer
    throws — the key is unused since the ipgeolocation.io call is commented out). NOTE: the
    52s `/iptv` hit was NOT this middleware (iptv path isn't matched); it was
    first-request-after-seeding contention. Applied to source AND VSIX template.
  - `Views\Shared\Components\CartBadges\Default.cshtml`: pill always rendered `badge.Count`
    (e.g. "0") — now rendered only when `badge.Count > 0`. Applied to both trees.
  - `Views\Shared\_Layout.cshtml`: head lacked `<meta name="description">`/keywords even
    though views set `ViewData["Description"]`/`["Keywords"]` (only `_MobileLayout` and
    `_IptvLayout` emitted them) — added conditional metas. Applied to both trees.
  - `Controllers\HomeController.cs` `Contact` GET+POST: removed `[Authorize]` (the Contact
    nav item is anonymous-visible; CAPTCHA + antiforgery already protect POST) and
    neutralized "Contact - Quebec Iptv" title + IPTV keywords → "Contact" + generic
    keywords. Also added `@Html.AntiForgeryToken()` to `Views\Home\Contact.Default.cshtml`
    and `Contact.Iptv.cshtml` (POST had `[ValidateAntiForgeryToken]` but the plain
    `<form method="post">` never emitted a token → latent 400 on submit). IPTV-module page
    branding (IptvHomeController etc.) intentionally kept — HomeController is the
    template-agnostic shell. Applied to both trees.
  - `Controllers\EventsController.cs` / `GamificationController.cs`: added
    `[ModuleEnabledFilter("Events")]` / `[ModuleEnabledFilter("Gamification")]`
    (+`using MvcApp.Common.Filters;`) — `SettingsSeeder` seeds `Module.Events.Enabled`/
    `Module.Gamification.Enabled` but nothing consumed them. Verified: anonymous 302
    (Authorize challenge), logged-in Mulva → 200. NOTE: this feature was SOURCE-ONLY until
    2026-09-26 — the full-parity template port (see below) added Events + Gamification to
    the live VSIX tree (controllers, nav, settings, migrations) so the two trees are now
    in sync on this feature.
  - Documented no-change decisions: `Template.Default.LandingEnabled` is an intended no-op
    (both branches of `HomeController.Index` resolve to `Index.Default` for the Default
    template); placeholder `Encryption:Key` and dev `Branding` values are config-only,
    gitignored, parameterized by the wizard — no code change, do not touch `appsettings.json`.
  - All ports shipped as VSIX **1.0.46** (payload verified in the zip; install still
    deferred by user).
- **Mulva's 8 photos are now the SAME person (2026-09-24):** the seed set
  `wwwroot\Photos\Mulva\women1-8.jpg` previously pointed to 8 DIFFERENT randomuser.me
  portraits (my earlier bulk-download defect). Replaced the 8 file CONTENTS with Pexels
  photos of Anna Tarazevich (`https://www.pexels.com/@anntarazevich/`) — she photographs
  herself, so every portrait is the same woman; Pexels license (free use, no attribution,
  modifiable, verified at `https://www.pexels.com/license/`). Originals were 5400×3600
  ~2MB each → downscaled to 800×533 JPEG q82 (49–104KB). **Filenames kept** (`women1-8.jpg`)
  so DB rows, `UserSeedData.json`, and the VSIX vstemplate file list remain valid — a
  pure content swap, no reseed/DB/manifest changes. Candidate #14751276 (cucumber-over-
  eyes, face hidden) skipped; main `women1.jpg` = reading-book portrait (Id 14751277).
  Live verified (own instance, 9001, then freed): `/Photos/Mulva/women1.jpg` + `women3.jpg`
  → 200 image/jpeg at exact new sizes. Source build 0E/10W (baseline). Ported to VSIX
  **1.0.47** (manifest bumped, Release 0W/0E, payload verified: 8 new sizes in the zip).
  Committed `9cc437a` + pushed to `github.com/BeRightBack/MvcApp` (public) — photos only,
  runtime log churn left unstaged. Follow-up if the user wants: swap other test users'
  128×128 randomuser portraits (1 each) for the same-person treatment at better res.

- **System Logs admin page (2026-09-28):** `/Admin/SystemLogs` browses + manages the Serilog
  `Logs` table (Admin sidebar -> System -> System Logs, beside the older Audit Logs).
  `ISystemLogService` (`MvcApp.Core\Abstractions`) + `SystemLogService`
  (`MvcApp.Services`, **MySqlConnector 2.5.0 — package added to MvcApp.Services.csproj, NOT
  MvcApp.Web**) uses raw SQL because the sink's table is in `serilogsDb`, a different database
  from the EF model. Filters: level (with live counts), text search (message/template/exception/
  properties), From/To date, page size 25/50/100/200. Detail page shows local + as-written +
  UTC timestamps, template, message, exception, pretty-printed JSON properties. Purge buttons
  (7/30/90/180/365 days + Delete all) are POST + antiforgery + confirm; the days value is
  allow-listed server-side (anything else = 400). `SystemLogRetentionHostedService` purges at
  startup then daily using `Logging:RetentionDays` (0 = off) and logs
  "System log retention active: purging rows older than N day(s)" so its execution is
  observable. **Timestamps: the log server runs UTC, this box is UTC-4** — the service runs
  `SET time_zone='+00:00'`, filters convert local->UTC, and the UI shows local time parsed from
  the sink's own `Timestamp` string (millisecond precision) because `_ts` only stores whole
  seconds. Indexes `idx_logs_ts` + `idx_logs_level_ts` exist on `Logs` (EXPLAIN verified:
  level filter = `ref`, paging = index scan of exactly the LIMIT).
  **File sink is now conditional** (`Logging:FileSink`: unset = on in Development, off in
  Production; Console + MySQL always on). Verified live: 297 rows, 0.2 MB, level filter and
  search, detail, exception rendering, purge guard (400 on days=13), retention log line, 0
  console errors; build 0E, tests 13/13. Screenshots:
  `E:\Pictures\Screenshots\MvcApp-admin-{systemlogs,systemlogs-filtered,logdetail,logexception}.png`.
  **VSIX 1.0.52 (2026-09-28) carries this feature** — and the first GENERATED-app test of a
  template feature (detokenized tree, real remote DBs, own port 9002, then freed): HTTP 200,
  admin login, `/Admin/SystemLogs` with 709 rows / level counts / filters / purge / detail all
  working. Generated apps need their own `<ProjectName>_logs` database (`CREATE DATABASE …`)
  or the MySQL sink and the retention job only warn; `Logging:RetentionDays` and
  `Logging:FileSink` are the two knobs. Wizard tokens are NOT valid JSON — validate template
  config files by detokenizing first (a scripted `-replace` corrupted appsettings.json once;
  recovered by extracting the pristine file from the previous .vsix).
  **Log DB is auto-created (2026-09-28, VSIX 1.0.53):** `SystemLogDatabaseInitializer`
  creates it from `Program.cs` **before** the logger is built — `Serilog.Sinks.MySQL`
  connects eagerly, so a missing DB at that moment silently kills the sink for the whole
  process (verified: creating it later never produced a table or rows). The sink still
  creates the `Logs` table on first write. `MySqlErrorCode.NoSuchTable` is treated as
  "empty", not a failure. 19 new unit tests (32 total). Verified by dropping the DB and
  restarting: recreated, table auto-created, 11 rows — and the same test in a generated app
  against a deliberately absent database (28 rows).
  **Seeder admin lookup fixed (2026-09-28, VSIX 1.0.54):** `Seeder.cs` `SeedAdminUserAsync`
  looked the admin up by EMAIL only while Identity enforces unique USERNAMES, so a mismatch
  between `Administrator:User` and `Administrator:Username` retried creation on every start
  and logged `Failed to create admin user` + `DuplicateUserName` for a healthy DB (this
  actually happened in the generated-app test - my harness injected `AdminEmail` into the
  `Administrator:User` field). It now falls back to `FindByNameAsync`, treats
  DuplicateUserName / DuplicateEmail as "already exists" (still adding the role if missing),
  and uses `ConfigOrDefault` instead of `??` (which never fires on an empty string). Verified
  with the mismatch deliberately in place: 0 errors, config restored byte-for-byte after the
  test. The admin in the remote DB is `UserName=admin`, `Email=admin@frenzyzone.com`.
  **EF multiple-collection warning fixed (2026-09-28, VSIX 1.0.55):** the
  `MultipleCollectionIncludeWarning` came from `MemberRepository.GetMembersAsync:91` via
  `HomeController.Index:83` (home page, once per request) - found by temporarily throwing
  that EF event to read the stack, then reverting. Loads `Photos` + `UserInterestTags`, both
  genuinely used by `MapToMemberModel`, so neither Include is droppable; now
  `.AsNoTracking().AsSplitQuery()`. **Measured neutral** (6501-7697 ms before, 6364-7696 ms
  after) - the home page is bound by 88 sequential round-trips at ~35ms WAN RTT, so this
  removes the growth risk, not today's latency. HTML byte-identical apart from antiforgery
  tokens; warning 1 -> 0 per page. NOTE: EF command logging is invisible in the logs because
  `Program.cs` has `MinimumLevel.Override("Microsoft", LogEventLevel.Warning)`.
  **Seeding split into bootstrap + demo (2026-09-28, VSIX 1.0.56):** the whole seeding block
  was `if (app.Environment.IsDevelopment())`, so a **Production** deployment had no admin user,
  no roles, no SystemSettings, no `SiteTemplate` and no `Module.*.Enabled` rows (every module
  read as disabled) — generated apps were undeployable. Bootstrap now always runs (languages,
  settings, chat/forum/blog structure, event categories, interest tags, badges, VIP plans,
  Blazor widget registry, roles, admin user); demo content (26 test users + photos, likes,
  messages, page snippets) is gated on **`Seeding:IncludeDemoData`**, defaulting to
  `app.Environment.IsDevelopment()` (on in dev, off in production unless set explicitly —
  the key is deliberately absent from appsettings.json so the environment decides).
  `Seeder.SeedData(app, includeDemoData)` overload added. Verified on a fresh
  prodtest_Identity/prodtest_Localisation (scripted migrations, 67 tables + 32 history rows):
  Production -> 1 user (admin), roles Admin+Moderator, 26 settings, 12 module rows,
  SiteTemplate=Default, 5 languages, 3 plans/12 details, **0 likes/messages/photos/snippets**,
  admin login works, /Admin + /Admin/SystemLogs render, anonymous /Admin still redirects to
  login. Same fresh DB in Development -> 27 users, 43 likes, 43 messages, 33 photos, 14
  snippets. Both test DBs dropped afterwards.
  **SystemSettings cache (2026-09-28, VSIX 1.0.57):** `SettingsCache` (singleton over
  `IMemoryCache`) holds the whole `SystemSettings` table as a dictionary; `SettingsService.GetAsync`
  reads from it instead of a SELECT per key. **Every writer invalidates** — `SetAsync`, the admin
  `SystemSettingsController` Edit/Create/Delete (writes via the DbContext, so it bypasses the
  service and would go stale), and the seeder via `SettingsSeeder.InvalidateCache`. 10-minute
  sliding+absolute expiry is the safety net. Measured with an identical script, warm requests:
  **6904/7200 ms before, 4428/4549 ms after** (~2.5s, 36% faster); round-trips **88 -> 50**,
  SystemSettings queries **37 -> 0**. Invalidation verified end-to-end (module disabled in the
  admin panel changed the nav on the very next request). Remaining home-page round-trips:
  ~15 Language + ~15 localization strings, 16 AdZone/AdBanner, plus user/session/visitor rows —
  localization is the next biggest target if more speed is wanted.
- **On "each template as its own module":** declined, deliberately. A generated app has exactly
  ONE active template (`SiteTemplate` is a single setting), so templates are alternatives, not
  simultaneous features — per-template module boundaries would add guard tokens and settings
  rows for something that can never be enabled alongside another. Per-app independence is
  already real: the wizard gives each app its own `<ProjectName>_Identity` / `_Localisation`
  databases (`ModulePickerWindow.xaml.cs:27`), its own connection strings, migrations and
  seeding.

## Coding conventions
- No code comments unless asked.
- Follow existing patterns (service abstractions in MvcApp.Core.Abstractions, DI
  registration, Razor view layout conventions).
- Reuse existing services (`ISettingsService`, `ITemplateService`, `IModuleManager`,
  `IAuditService`) instead of new DB access in controllers.
- After any code change, run the build; fix warnings in changed files (pre-existing
  warnings are acceptable).

## Template & porting
- Live VSIX template project: `E:\Apps\MvcApp.Templates\MvcApp.Templates\`
  (CONFIRMED new canonical path by user 2026-09-23; old `C:\Users\...\Documents\...` record
  dead). `MvcApp.Templates.slnx` with subprojects `.VSIX` + `.Wizard`; its own context in
  `E:\Apps\MvcApp.Templates\AGENTS.md` and `CHECKLIST.md`.
- Manifest Identity GUID `MvcApp.Templates.VSIX.5d477f24-489e-4492-b1e7-bbcdbb90a5ee` —
  matches the historical record. On-disk source manifest says **Version 1.0.39**
  (newest Release artifact 2026-08-10); the older "1.0.40 built 2026-08-13" note remains
  unconfirmed on disk — trust 1.0.39 until the installed-VS copy says otherwise.
- The stale copy at `E:\Apps\MvcApp\MvcApp.Template` must NOT be edited.
- **Full-parity template port 2026-09-26 (VSIX 1.0.50):** the live VSIX tree
  (`E:\Apps\MvcApp.Templates\MvcApp.Templates\MvcApp.Templates.VSIX\ProjectTemplates\MvcApp\`)
  was brought to full parity with this source app. 226 files ported from source (tokenized:
  every `MvcApp` occurrence → `$ext_safeprojectname$`, including string literals) — all
  missing features restored: Events, Gamification, Members, Gallery, Matches, SuperLike,
  VIP (VipController/VipPayPalService + SeedVipPlans), VideoUpload, Notifications
  (NotificationHub + badge), Report, Verification (SecureVerificationService),
  moderation admin (Bans: BansController/Admin area), InterestTags, email queue
  (EmailQueueHostedService), plus Observers count = full feature parity. Hand-merges:
  - `UserDbContext.cs`: source content + template's own-assembly ending
    (`ApplyConfigurationsFromAssembly(typeof(UserDbContext).Assembly)` BEFORE the
    `ModuleConfigurationRegistry.Assemblies` loop) so the 23 relocated module configs +
    5 feature configs (Event/InterestTag/Report/VerificationRequest/VideoUpload) apply —
    required because the template relocates module configs into Infrastructure.
  - `Program.cs`: source wiring merged into guarded template structure (AddHttpClient
    VipPayPalService FQN, AddHealthChecks DatabaseHealthCheck, AddRateLimiter "auth" policy,
    AddResponseCompression, AddSerilog/UseSerilogRequestLogging, BannedUserMiddleware,
    MapHub NotificationHub "/notificationhub", MapHealthChecks "/health", background
    Task.Run seeding with all seed steps incl. SeedEventCategories/SeedInterestTags/
    SeedGamification/SeedVipPlans + app.SeedData(), EnsureMigratedAsync pending-guard).
  - `SettingsSeeder.cs`: live's 10 guarded module rows + 2 UNGUARDED
    `Module.Events.Enabled`/`Module.Gamification.Enabled` rows (Events/Gamification are
    always-on features, not wizard-deselectable).
  - `Seeder.cs` (Identity): dropped `"Member"` role (roleNames = Admin/Moderator) — parity
    with source.
  - 160 `<ProjectItem>` entries added across 12 `.vstemplate` files for all new files.
  Verification: detokenized in-place build of the live tree → **0E/10W** (= source
  baseline); VSIX 1.0.50 built 0W/0E; payload verified (manifest version, all feature
  files present, 25 photos upgraded 800×800, no stray raw `MvcApp` in shipped
  .cs/.cshtml/.razor/.json, all 20 vstemplates parse). **Port ④ (photo upgrade) included:**
  the 25 non-Mulva test-user photos upgraded 128×128 → 800×800 (filenames preserved, same
  content as source — hash-identical after copy). VS install still deferred by user.
- Working log: `%TEMP%\opencode\summary.md` — update it after each major block of work
  (sessions start with fresh memory; it is also wiped on reboot, so graduate durable facts
  into this file).
- **Plans / Discover blur / admin template thumbnails (2026-09-27, source app only, not
  ported/not committed — user confirmation pending):**
  - **VIP plans = feature tiers, each with its full 1/3/6/12-month duration ladder
    (old-project model).** `SubscriptionPlan.Features` (newline-separated `string?` —
    source-only migration `20260927105307_AddSubscriptionPlanFeatures`, single
    AddColumn, applied to remote → 32nd `__EFMigrationsHistory` row) + multi-detail
    restructure mirroring `E:\Apps\IptvWebProject`: a PLAN is a tier (what you get),
    months are options INSIDE every plan — never the tier definition. NOTE: the 3 old
    single-duration prices (Premium 24.99/3mo, Platinum 39.99/6mo) were RESTATED to
    clean tier rates (tune in `SeedVipPlansAsync`):
    Basic "Essential VIP" 9.99/26.97/47.95/83.92, Premium "Most Popular"
    14.99/40.47/71.95/125.92, Platinum "Ultimate VIP" 19.99/53.97/95.95/167.92
    (1/3/6/12 mo; save 10/20/30% on 3/6/12 — exact math, all 12 rows in the seeder +
    live DB via idempotent UPDATE/INSERT). Feature lists = ONLY code-verified VIP
    gates (crown badge, Likes-you photo reveal, SuperLikes 5 vs 1/day, ChatHub
    `VipOnly` rooms, 2 vs 1 boosts/day); the unbacked old bullets (Priority in
    Search / Unlimited Messaging / Advanced Filters) were dropped. CAVEAT: backend
    still grants EVERY VIP feature to ANY tier (binary `IsVip` via
    `PremiumExpiryDate`; no tier column) — tier lists are honest SUBSETS; per-tier
    enforcement (tier column + checkpoints) is a possible follow-up if the user
    wants it. `Views/Vip/Index.cshtml` = per-card duration pills (Bootstrap
    btn-check) + price/per-month/hidden-detailId updating via inline JS;
    `VipController.Checkout`/`PaymentSuccess` now take `detailId` (were
    FirstOrDefault).
  - **IPTV store aligned to the same model** (shares the SAME
    `SubscriptionPlans`/`SubscriptionDetails` tables; `Module.Iptv.Enabled=true` in
    live DB): `IptvStore/Index.cshtml` now lists every duration with its OWN Add to
    Cart (posts `subscriptionDetailId` — was FirstOrDefault) under "Durations &
    Pricing" (+ the missing `@Html.AntiForgeryToken()` — latent 400, same class of
    bug as the Contact fix), a "from $cheapest" header, and sidebar badges switched
    to `bg-light text-primary` (luxury.css `.bg-primary` gradient made `bg-primary`
    badges gold-on-gold illegible — pre-existing). IPTV cart/checkout already handled
    detailId; IPTV home page already listed all durations. `SubscriptionService`
    FirstOrDefault helpers are dead code (no callers) — untouched. Test cart row
    added during verification was deleted.
  - **Discover blur removed for browsing.** `DiscoverController.Index` now passes
    `isPhotoBlurred: false` (was `!currentUser.IsVip` on EVERY card — you can't like a
    photo you can't see). The intended VIP reveal gate REMAINS only on the Likes "who
    liked you" list (`LikesController.cs` blurs when `predicate == "liked" && !IsVip` =
    the "See Who Liked You" upsell). Also recorded: the login form field name is
    `Input.Username` (label says "Username or email"; NOT `Input.UsernameOrEmail`).
  - **Admin → UI Templates shows REAL thumbnails instead of emoji.** Generated
    `wwwroot/images/templates/{name}-thumb.png` for all 11 templates (640×221, navbar+hero
    crop of the real per-template audit screenshots via puppeteer canvas from
    `%TEMP%\opencode\puppet\tpl-*.png`); `Areas/Admin/Views/Templates/Index.cshtml` renders
    `UiTemplateInfo.Thumbnail` (`<img>` + hidden 🎨 fallback span shown only onerror) —
    replaces the 3-emoji switch where 8 of 11 templates shared 🎨.
  Verified live on port 9001 (then freed): all 11 `*-thumb.png` → HTTP 200, naturalWidth
  640, no broken/emoji placeholders (vision); /Vip = 3 cards × 4 duration pills
  (1/3/6/12 mo, save 10/20/30%, distinct feature lists, Most Popular banner) and the
  price/per-month/hidden-detailId update on pill click (DOM + vision, $47.95/6mo =
  "That's just $7.99/mo"); /iptv-store = 4 durations per plan each with its own Add to
  Cart → adding Premium 6mo put "6 Month Service, save 20% · $71.95" in the cart
  (DOM + OCR + end-to-end cart POST), sidebar badges legible after `bg-light` fix;
  /Discover as non-VIP George = 21 cards, 12 sampled imgs `filter:none`, 0 "Like to
  reveal" overlays (DOM + vision, sharp photos). Build 0E; tests 13/13. Screenshots:
  `E:\Pictures\Screenshots\MvcApp-fixed3-{admin-templates,vip-plans,vip-durations,
  iptv-store-durations,iptv-cart,discover}.png`.
  **STATUS (2026-09-27): committed `f833034` + pushed to origin/main; ported to the live
  VSIX tree as **1.0.51** (detokenized build 0E/10W, VSIX Release 0W/0E, payload verified —
  incl. the Default-navbar / NavGrouping / layout-override prior-session work). NOT
  installed — user defers VS install.**

## Port history - VSIX 1.0.60 (2026-09-28, three defect fixes)
Ported 5 files from source commit `e2dd6bb` (port base `0d3f917`). Manifest 1.0.59 -> 1.0.60.
- **The translation payload's `ProjectItem` was fixed from `ReplaceParameters="true"` to
  `"false"`.** This was a real latent bug in 1.0.58/1.0.59: the template engine does text
  replacement on files marked `true`, so a 97 KB gzipped binary would very likely have been
  corrupted in a generated app. It would fail to decompress, the seeder would log a warning, and
  the app would start untranslated and burst-translate itself - exactly what the payload exists to
  prevent. **A detokenized build cannot catch this**, because it reads the file from the template
  tree instead of through the template engine. Every other binary in this template is `false`
  (verified in the built payload: 61 jpg/png ProjectItems in MvcApp.Web.vstemplate, all `false`,
  zero `true`), and there were NO binary ProjectItems at all before the payload, so the pattern
  was unproven here. **When adding a binary to this template, use `ReplaceParameters="false"`.**
- **`LocalizationContext` no longer stores a request-scoped service on a static.** It resolves the
  localizer from the current request's services and returns null with no current request, so
  callers fall back to untranslated text instead of throwing. `LocalizationMiddleware` now does
  nothing per request. Severity correction: this was NOT cross-request contamination - the
  localizer resolves culture itself at lookup time - the real defect was the null case.
- **The four ad-banner placeholders inside `<script>` blocks are localized**, injected once as
  JSON rather than into single-quoted JavaScript literals (a translation containing an apostrophe
  would terminate the string). The server-rendered placeholder for the same field, a Razor ternary,
  is localized too. The attribute pass masked script content and so missed these.
- **Verified:** detokenized build **0E/10W** (= source baseline); VSIX Release 0W/0E; payload
  manifest **1.0.60**, 1018 entries, all 20 vstemplates parsing, `translations.json.gz` present
  (97430 bytes, 1353 keys, `Log in` -> `Se connecter`) and now declared `ReplaceParameters="false"`.
  NOT INSTALLED (user defers), and **no wizard-generated app has been created from
  1.0.58 / 1.0.59 / 1.0.60** - the template is verified to compile and to contain the right
  files, not verified to produce a working app through the wizard.

## Session continuity (IMPORTANT)
