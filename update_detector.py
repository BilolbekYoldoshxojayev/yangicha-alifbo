with open(r'c:\dev\Projects\alphabet\YangiAlifbo\generated_suffixes.cs', encoding='utf-8') as f:
    new_decl = f.read()

with open(r'c:\dev\Projects\alphabet\YangiAlifbo\src\Core\LanguageDetector.cs', encoding='utf-8') as f:
    code = f.read()

start = code.find("    private static readonly string[] UzbekSuffixes = new[]")
end = code.find("    /// <summary>\n    /// AppData directory for user configuration files.", start)

if start != -1 and end != -1:
    updated_code = code[:start] + new_decl + "\n\n" + code[end:]
    with open(r'c:\dev\Projects\alphabet\YangiAlifbo\src\Core\LanguageDetector.cs', 'w', encoding='utf-8') as f:
        f.write(updated_code)
    print("LanguageDetector.cs updated successfully!")
else:
    print(f"Error locating block: start={start}, end={end}")
