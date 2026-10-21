import re

with open(r'c:\dev\Projects\alphabet\YangiAlifbo\src\Core\LanguageDetector.cs', encoding='utf-8') as f:
    content = f.read()

match = re.search(r'UzbekSuffixes\s*=\s*\[(.*?)\];', content, re.DOTALL)
if match:
    items = [x.strip(' "\n\r\t,') for x in match.group(1).split(',') if x.strip(' "\n\r\t,')]
    print('Total suffixes:', len(items))
    for check in ['sak', 'sa', 'sam', 'sang', 'salar', 'averadi', 'veradi', 'aver', 'ver', 'laveradi', 'adi', 'di', 'k']:
        print(f"'{check}' in suffixes? {check in items}")

# Test simulated TryMorphologicalMatch for 'gaplashsak' and 'bo\'laveradi'
with open(r'c:\dev\Projects\alphabet\YangiAlifbo\src\Core\Dictionaries\uzbek_words.txt', encoding='utf-8') as f:
    uz_words = set(line.strip().lower() for line in f)

def check_stem(stem):
    if len(stem) < 2: return False
    if stem in uz_words: return True
    if (stem + "moq") in uz_words: return True
    return False

def try_morph(word):
    for s1 in items:
        if len(word) > len(s1) + 1 and word.endswith(s1):
            stem1 = word[:-len(s1)]
            if check_stem(stem1): return (True, s1, stem1)
            for s2 in items:
                if len(stem1) > len(s2) + 1 and stem1.endswith(s2):
                    stem2 = stem1[:-len(s2)]
                    if check_stem(stem2): return (True, f"{s2}+{s1}", stem2)
    return (False, None, None)

print("\nTryMorph for 'gaplashsak':", try_morph("gaplashsak"))
print("TryMorph for \"bo'laveradi\":", try_morph("bo'laveradi"))
