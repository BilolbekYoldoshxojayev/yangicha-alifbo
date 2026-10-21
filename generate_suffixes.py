import re

# Existing suffixes from LanguageDetector.cs
with open(r'c:\dev\Projects\alphabet\YangiAlifbo\src\Core\LanguageDetector.cs', encoding='utf-8') as f:
    code = f.read()

existing = set(re.findall(r'"([a-z\'-]+)"', code[code.find("UzbekSuffixes"):code.find("AppDataDir")]))

# Add new suffixes
additional = [
    # Continuative (-aver / -ver)
    "averadilar", "averasizlar", "averdilar", "aversalar", "averganlar",
    "averaman", "averamiz", "averasan", "averasiz", "averadi",
    "averdilar", "averdim", "averdik", "averding", "averdingiz", "averdi",
    "aversalar", "aversam", "aversak", "aversang", "aversangiz", "aversa",
    "averaylik", "avermoq", "averish", "avergan", "avergach", "avergani", "aver",
    "veradilar", "verasizlar", "verdilar", "versalar", "verganlar",
    "veraman", "veramiz", "verasan", "verasiz", "veradi",
    "verdilar", "verdim", "verdik", "verding", "verdingiz", "verdi",
    "versalar", "versam", "versak", "versang", "versangiz", "versa",
    "veraylik", "vermoq", "verish", "vergan", "ver",

    # Potentive (-ol)
    "olmaydilar", "olmaslik", "oladilar", "olganlar", "olmaymiz", "olmaydi",
    "olmasdan", "olmagan", "olaman", "olamiz", "olasan", "olasiz", "oladi",
    "oldilar", "oldim", "oldik", "olding", "oldingiz", "oldi",
    "olsalar", "olsam", "olsak", "olsang", "olsangiz", "olsa",
    "olmoq", "olish", "olgan",

    # Conditional
    "sayliklar", "saylik", "sangiz", "salar", "sak", "sam", "san",

    # Imperative/Optative
    "ayliklar", "aylik", "ayin", "ay", "gin", "gil", "ingizlar", "inglar", "sinlar",

    # Participles & converbs
    "guncha", "kuncha", "quncha",
    "gandek", "kandek", "qandek",
    "ganday", "kanday", "qanday",
    "gancha", "kancha", "qancha",
    "yotir", "yotibdi",

    # Clitics & particles
    "mikan", "mish", "choq", "chak",

    # 1-letter
    "k", "q"
]

all_suffixes = existing.union(additional)
print(f"Total unique suffixes: {len(all_suffixes)}")

# Sort by length descending, then alphabetically ascending
sorted_suffixes = sorted(all_suffixes, key=lambda s: (-len(s), s))

# Group by length
by_len = {}
for s in sorted_suffixes:
    by_len.setdefault(len(s), []).append(s)

lines = []
lines.append("    private static readonly string[] UzbekSuffixes = new[]")
lines.append("    {")
for l in sorted(by_len.keys(), reverse=True):
    lines.append(f"        // {l}-letter suffixes")
    items = by_len[l]
    # chunk into lines of 5
    for i in range(0, len(items), 5):
        chunk = items[i:i+5]
        s_chunk = ", ".join(f'"{x}"' for x in chunk)
        lines.append(f"        {s_chunk},")

lines.append("    };")

result = "\n".join(lines)
# Remove trailing comma on last line before };
result = re.sub(r',(\s+};)', r'\1', result)

with open(r'c:\dev\Projects\alphabet\YangiAlifbo\generated_suffixes.cs', 'w', encoding='utf-8') as f:
    f.write(result)

print("Generated generated_suffixes.cs successfully!")
