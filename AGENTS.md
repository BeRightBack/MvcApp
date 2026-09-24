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
  `MvcApp_logs` (Serilog logs DB, from `appsettings.json` → `Serilog:ConnectionStrings:Logs`)
  does NOT exist on the server — unverified whether the sink creates it on first write.
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

## Key settings (SystemSettings table: Key, Value, Description, Group, UpdatedAt, UpdatedBy)
- `SiteTemplate` = active template (currently `Default`; exact template Name, compared
  OrdinalIgnoreCase).
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
- SiteNav.* rows are created only by the admin saving nav in the editor; code (NavDefaults)
  is the source of truth until then — do NOT reintroduce SiteNav seeding (it freezes
  defaults and leaves them stale). Data migrations deleting stale rows must be no-ops on
  fresh DBs.
- **Production mode never seeds:** `Program.cs` runs `SeedLanguageAsync`/`SeedSettingsAsync`/
  `SeedChatRoomsAsync`/`app.SeedData()` ONLY in `IsDevelopment()` — Production just runs.
  Before running a generated app in Production, apply migrations for BOTH contexts manually:
  `dotnet ef database update -c UserDbContext` AND
  `dotnet ef database update -c LocalizationDbContext`. Otherwise you get 500s and empty
  `SystemSettings`. (Migrations/seed need MySQL — see Databases.)
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
    `Module.Gamification.Enabled` but nothing consumed them. Source-only: the VSIX template
    has NO Events/Gamification controllers, no nav entries, and does NOT seed these settings
    — do NOT add the feature to the template. Verified: anonymous 302 (Authorize challenge),
    logged-in Mulva → 200.
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
- Working log: `%TEMP%\opencode\summary.md` — update it after each major block of work
  (sessions start with fresh memory; it is also wiped on reboot, so graduate durable facts
  into this file).
