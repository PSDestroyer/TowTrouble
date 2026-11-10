# Tow Trouble localization

29 locales, one runtime language per saved profile. Spanish and Portuguese regional variants share one locale each; Norwegian nb/nn map to no. Simplified and Traditional Chinese remain separate writing systems. UI and recovery text are localized; audio is unchanged.

## Authoring

- Source.json: canonical English keys and extraction contexts.
- Authored: directly authored translations for 410 shared concepts in 28 languages, plus reviewed mappings for existing longer UI text. No external translation service is used.
- Translations: composed per-locale data, including explicit formatted UI sentences and structured part statistics. All signs, numerical values, and units remain intact.
- Tables/Locales: native Unity Localization String/Asset Tables and Locale assets.

After editing Authored data, run `python3 Assets/TowTrouble/Editor/Localization/compose_tables.py` from the project root. Then, outside Play, choose **Tow Trouble > Localization > Refresh local tables**. This refreshes only owned localization assets; it does not save scenes or build content. The Python script uses the standard library only. Inspect Source contexts when adding a new visible string and give dynamic sentences an explicit `{0}` template instead of translating individual grammar fragments.

## Runtime and offline behavior

LocalizationSettings.SelectedLocale owns the active locale. The Language page saves the preference in RecoveryProfile. Normalization preserves existing en/ro saves and merges supported regional codes. Resetting the career retains language preferences.

RecoveryLocalizationSettings supplies completed local initialization and public ILocalesProvider/ITableProvider implementations. String Tables are ordinary serialized references. Font Asset Tables use public SetAssetOverride and Resources to load the selected script font. This avoids Addressables initialization, runtime catalogs, bundles, URLs, and content builds. Addressables remains a package dependency of Unity Localization; automatic content building with the player and catalog updates are disabled.

Text preprocessing leaves canonical TMP text, control names, focus identity and English source matching stable. Rendering translates and swaps the font. Input glyphs are resolved before translation; player names, address fields, typed drafts and legal notices bypass translation. Arabic shaping preserves rich-text markup and numbers. New scene TMP signs are bound by the HUD at startup. No scenes were batch-rewritten.

Original Western body/display faces remain for supported Latin scripts. Other scripts use lazy, locale-specific dynamic multi-atlas fonts via the Font Asset Table; CJK fonts are not added to a global fallback stack. Cyrillic and extended Latin use Nunito Sans, Greek Noto Sans, Arabic Noto Naskh Arabic, Thai Noto Sans Thai, and each CJK writing system its own Noto CJK face. Font provenance, hashes and OFL licenses are in Fonts. The game Credits includes the full required font and shaping notices.

See Docs/Validation/Localization for validation evidence and every intentionally unchanged source entry. Part/model design identifiers, brands, key names, international units and discoverable English language names are retained. Long help text is adapted for the available UI space. Speaker review of terminology/grammar and hardware font/performance acceptance remain separate from functional Editor validation.
