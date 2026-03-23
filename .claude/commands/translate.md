# Translate Localization Files

Translate a source localization file into all Playnite-supported languages using parallel agent teams with QA review.

## Usage

```
/translate [source_file]
```

If no source file is provided, default to `Localization/en_US.xaml` in the current project.

## Instructions

You are a translation orchestrator. Follow these steps exactly:

### Step 1: Read the source file

Read the source localization file specified by the argument (or `Localization/en_US.xaml` by default). Identify the file format (XAML ResourceDictionary, JSON, etc.) and extract all translatable strings. Note any placeholders like `{0}`, `{1}` that must be preserved.

### Step 2: Check for existing translations

Use Glob to find any existing translation files in the same directory. Note which languages already have translations so the QA reviewer can check for regressions.

### Step 3: Dispatch parallel translation agents

Launch **3 translation agents in parallel** using the Agent tool. Each agent handles a batch of languages. Provide each agent with:
- The full source file content
- The exact list of target locales assigned to it
- Instructions to preserve the file format, XML structure, keys, and all placeholders (`{0}`, `{1}`, etc.)
- The output file naming convention: same directory as source, with locale code replacing the source locale (e.g., `en_US.xaml` -> `es_ES.xaml`)

**Agent 1 — Romance languages:**
`es_ES, fr_FR, it_IT, pt_BR, pt_PT, ro_RO`

**Agent 2 — Germanic, Nordic & Slavic languages:**
`de_DE, nl_NL, sv_SE, pl_PL, cs_CZ, ru_RU, uk_UA`

**Agent 3 — East Asian & Turkish:**
`ja_JP, ko_KR, zh_CN, zh_TW, tr_TR`

Tier 1 only (18 languages). Tier 2 and 3 languages are skipped due to lower translation confidence.

Each agent prompt should be:

```
You are a professional translator for software UI strings. Translate the following source localization file into the specified target languages.

CRITICAL RULES:
1. Preserve ALL placeholders exactly as-is: {0}, {1}, etc.
2. Preserve the exact file format, XML structure, namespaces, and x:Key attributes
3. Only translate the string VALUES, never the keys or XML attributes
4. Use natural, idiomatic phrasing appropriate for software UI (concise, clear)
5. Keep brand names untranslated (e.g., "PlayniteSound", "Playnite")
6. For RTL languages (Arabic, Hebrew, Persian), keep the same XML structure — RTL rendering is handled by the UI framework

SOURCE FILE CONTENT:
[paste full source file here]

TARGET LOCALES: [list of locales for this agent]

For each locale, write the complete translated file using the Write tool to:
[output directory]/[locale_code].xaml

Example: If source is Localization/en_US.xaml, write Spanish to Localization/es_ES.xaml
```

### Step 4: QA Review

After ALL translation agents complete, launch a **QA reviewer agent** that:

1. Reads the source file and ALL generated translation files
2. Checks every file for:
   - **Placeholder integrity**: All `{0}`, `{1}` placeholders from the source exist in translations
   - **Valid XML**: Proper XAML ResourceDictionary structure, no broken tags
   - **Key completeness**: Every key from the source file exists in every translation
   - **No untranslated strings**: Flag any values identical to English (except brand names and technical terms like "px")
   - **Consistent terminology**: Same term translated consistently within each language file
3. Reports a summary of issues found
4. Fixes any issues directly by editing the affected files

The QA agent prompt should be:

```
You are a localization QA reviewer. Read the source file and all translation files in the Localization directory. Check each translation for quality and correctness.

Source file: [path]
Translation directory: [directory path]

CHECK EACH FILE FOR:
1. PLACEHOLDER INTEGRITY: Every {0}, {1} etc. from the source must appear in the translation. This is CRITICAL — missing placeholders cause runtime crashes.
2. VALID XML: Must be a valid XAML ResourceDictionary with correct namespaces.
3. KEY COMPLETENESS: Every x:Key from the source must exist in every translation.
4. UNTRANSLATED STRINGS: Flag values identical to English (ignore brand names like "PlayniteSound", technical terms like "px", and format strings).
5. TERMINOLOGY CONSISTENCY: Same English term should be translated the same way throughout each file.

For each file, report: PASS or FAIL with specific issues.
Fix any issues you find by editing the files directly.
Provide a final summary of all languages and their status.
```

### Step 5: Summary

After the QA agent completes, provide the user with:
- Total languages translated
- Any QA issues found and fixed
- Any languages that may need human review (especially low-resource languages)
