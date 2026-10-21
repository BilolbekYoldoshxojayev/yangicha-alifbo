import re

with open(r'c:\dev\Projects\alphabet\YangiAlifbo\src\Core\Dictionaries\uzbek_words.txt', encoding='utf-8') as f:
    words = set(line.strip().lower() for line in f)

test_words = ['gaplashsak', 'gaplashmoq', 'gaplash', 'gap', "bo'laveradi", "bo'ladi", "bo'lmoq", "bo'l"]

for term in test_words:
    print(f"'{term}' in dict: {term in words}")

# Find any word starting with gaplash or bo'l
print("\nWords starting with 'gaplash':", [w for w in words if w.startswith('gaplash')][:10])
print("Words starting with \"bo'l\":", [w for w in words if w.startswith("bo'l")][:10])
