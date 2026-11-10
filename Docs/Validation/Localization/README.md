# Localization validation — 2026-11-10

## Implemented

29 locales from the requested list: en, fr, de, it, es, pt, ro, bg, cs, da, nl, fi, el, hu, id, ja, ko, ms, no, pl, ru, zh-Hans, sv, zh-Hant, th, tr, uk, vi, ar. Spanish/Portuguese regional duplicates collapse; Chinese scripts stay separate. The Language page is available in Settings and initial comfort setup. Selection applies immediately and persists in the local profile; career reset retains it.

Unity Localization 1.5.14 supplies Locale/String/Asset Table APIs. Local providers and the custom initialization subclass bypass Addressables catalog initialization. All text/fonts ship locally; no external translation service, web font or online locale dependency. Automatic Addressables player-content builds and catalog updates are disabled. No builds were run.

410 shared concepts were authored directly in each of 28 non-English languages. Reviewed source mappings adapt longer help to UI space; formatted sentences retain runtime arguments. Structured part descriptions retain every original signed percentage, mass, speed, force and cable threshold. All24 site titles and recovery situations, including mandatory tree removal, are covered. Brands, part/model design identifiers, units, key identities, original legal notices and discoverable English language-picker names remain unchanged. UnchangedEntries.json lists retained strings with contexts; nonempty tables are a structural check, not a claim that brand names need translating.

## Evidence

- Compile.json: native C# compilation, zero errors/warnings.
- TestsResult.json:4 native directly invoked NUnit methods passed: regional normalization/script split, saved language and preference copy, canonical text/user-data protection, numerical stats/tree instructions.
- AuditFinal.json:29 locales ×1,781 keys =51,649 nonempty native table values; zero gaps. No player or Addressables content build.
- FontCoverage.json: every codepoint in all28 translation files and uppercase variants covered by the selected font source. Native basic probe also loaded the correct body/display font for29 locales. Arabic presentation shaping checked in rendered UI.
- FinalRenderSummary.json:435 final menu pages across29 languages at130% text, zero reported exceptions/overflows after corrections. Initial435-page audit exposed two long RU/UK winch footnotes; they were shortened and58 affected pages rerendered. English Starter flags came from the disposable fixture using body typography for titles; rerender with the real Fredoka title face passed, without changing English layout.
- Selected PNGs show actual native UI captures on a flat preview background; source scene/car arrangement was not rewritten. Scripts used disposable preview scenes, profiles and font assets.
- PlayCheckFinal.json: actual isolated offline Editor Play,29 language buttons; Japanese chosen through its button; heading renders 設定 and font Japanese; RecoveryProfile.Load restores ja; initialization done, zero Addressables resource locators, RecoveryOfflineTransport, no co-op menu entry.
- RestoreFinal.json: Play stopped; scope released; source OpenWorldRecovery scene and preexisting dirty state preserved; real save.json, backup and previous hashes unchanged. Switch socket/network auto-init flags remain false; final Editor quality PC.
- ConsoleFinalScope.json retains six SerializedObjectNotCreatableException messages from stale Better Fog/URP Volume inspectors during domain transitions. No localization/Addressables runtime error in the final scope. Historical early setup Addressables errors remain in the Console; it was not cleared. Do not report the entire Editor Console as clean.

## Repeatable authoring and limits

See Assets/TowTrouble/Localization/README.md. compose_tables.py is local and uses only Python's standard library; Refresh local tables saves only localization assets. SourceInventory.json records the extraction/omission contexts; canonical TMP strings remain English so focus matching, display-confirmation matching and controller glyph resolution stay stable. Rendered output is translated through the package-selected table. Names, IP fields, typed drafts and legal notices bypass translation.

Source fonts, hashes, OFL files and RTLTMPro MIT provenance are retained; full required notices are also in Credits. Selected script fonts are loaded lazily, never a global CJK fallback stack. Dynamic multi-atlas fonts are expected to populate from embedded font files offline.

No Switch build, hardware rendering/FPS/RAM measurement, IL2CPP player run, or two-peer localization test was performed. Linguistic speaker review remains advisable for release, particularly compact technical terminology and Arabic/mixed-direction text. English gameplay/scenes/PC render settings, existing soundtrack, actual career and concurrent wheel-dust/banner work were not redesigned by this change. No scene save, commit or push.

## Follow-up: text sizes and diacritics

Rechecked 29 languages × 15 menu states × 5 text scales (85%, 100%, 110%, 120%, 130%) = 2,175 native rendered preview cases at 1280×720 (1600×900 layout). MultiSizeReport.json records zero TMP text overflows, zero exceptions and preservation of the active scene and its preexisting dirty state. RenderMultiSize.cs.txt retains the disposable preview harness. Cases include main menu, language, audio, controls, accessibility, gameplay settings, first setup, confirmation, starter selection, recovery board, three help tabs and assembly/winch garage pages. This covers these states and five samples of the continuous slider, not every possible runtime text or player-entered string.

Font source coverage was rerun for every character in all 28 non-English translation files and their uppercase variants: no missing codepoints, including diacritics. This verifies glyph availability; it does not substitute for native-speaker review of spelling or complex-script typography. Previous native captures provide visual samples.

Fixed three paths that lost the non-English autosizing allowance: live slider changes, garage control label resizing and waypoint objective resizing. English sizing remains unchanged. Native recompile passed with zero errors/warnings. No scene save, save-file change, Play session or build was needed for this follow-up. Hardware rendering remains unverified.

## Follow-up: gameplay, recovery descriptions and garage parts

Audited the current 384-part catalogue, all 24 open-world recoveries plus three tutorial recoveries, and dynamic mission phases. Kept source model/style identifiers and technical units; descriptions, recovery titles and instructions are translated. The 56 unique part descriptions all have nonempty non-English translations. Authored 12 more precise concepts in each non-English locale, plus two compact Greek recovery descriptions; chassis descriptions now retain light/balanced/heavy distinctions, the 22 kN winch retains the recommended 26 kN cable, and driving controls no longer call driving "pull".

- FastContentReport.json: 137,750 native TMP measurements (950 distinct content/geometry specifications × 29 locales × five text scales), zero overflow/errors. Includes every part name/description, card stats, recovery description/title, delivery name and winch state. Measurements use shipped fonts, localization preprocessors and production dimensions; repeated identical text/geometry pairs are deduplicated. Initial full native summary rendering also covered all 384 parts.
- GameplayLayoutReport.json: 145 native HUD fixtures (29 locales × five scales), all mission-instruction branches, all recovery titles, guidance and reset hints exercised through actual resizing methods; zero final overflow/errors. Initial real HUD pass caught German/Polish/Russian/Ukrainian tree instructions at 100%.
- Fixed the cause: TMP GetPreferredValues(string, ...) bypasses ITextPreprocessor. Shared GetLocalizedPreferredValues now measures the translated/shaped text and control glyphs. Updated HUD, waypoints, hitch/winch hints, garage review scrolls, confirmation panels and settings help measurements. No-argument TMP measurement already parses the current label and stays unchanged.
- MultiSizeReport.json rerun after the shared measurement fix: 2,175 menu cases, zero overflow/errors. ContentCompile.json: zero C# errors/warnings.
- Refresh local tables now checks for existing shared keys rather than AddKey unconditionally creating numbered aliases. It removes only generated aliases unreferenced by any locale table and retains canonical IDs. ContentFinalReport.json verifies stable repeated refreshes and full table coverage.
- Scripts use disposable preview scenes/fonts and suppressed-save profiles. Source scene and original dirty state preserved. No Play, scene save, build, commit or push. Reports distinguish font glyph coverage from linguistic/native-speaker quality assurance. No hardware verification or exhaustive player-entered-name test is claimed.

Final verification: 51,765 nonempty values, 1,785 canonical shared text keys, two font keys, stable IDs after two refreshes. Four native localization regression tests passed. Git whitespace checking reports four Unity-generated quoted YAML line-wrap whitespace cases; these are serializer formatting, not C# compilation errors.
