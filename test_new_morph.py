with open(r'c:\dev\Projects\alphabet\YangiAlifbo\src\Core\Dictionaries\uzbek_words.txt', encoding='utf-8') as f:
    uz_words = set(line.strip().lower() for line in f)

# Add bare stems of all -moq verbs to uz_words
moq_stems = set()
for w in uz_words:
    if w.endswith('moq') and len(w) > 4:
        moq_stems.add(w[:-3])
uz_words.update(moq_stems)

# Test new suffixes
new_suffixes = [
    # Continuative aspect
    "averadilar", "averaman", "averamiz", "averasan", "averasiz", "averadi",
    "averdilar", "averdim", "averdik", "averding", "averdingiz", "averdi",
    "aversalar", "aversam", "aversak", "aversang", "aversangiz", "aversa",
    "averaylik", "avermoq", "averish", "avergan", "avergach", "avergani", "aver",
    "veradilar", "veraman", "veramiz", "verasan", "verasiz", "veradi",
    "verdilar", "verdim", "verdik", "verding", "verdingiz", "verdi",
    "versalar", "versam", "versak", "versang", "versangiz", "versa",
    "veraylik", "vermoq", "verish", "vergan", "ver",
    # Conditional
    "saylik", "sangiz", "salar", "sak", "sam", "san", "sa",
    # Potentive
    "oladilar", "olaman", "olamiz", "olasan", "olasiz", "oladi",
    "oldilar", "oldim", "oldik", "olding", "oldingiz", "oldi",
    "olsalar", "olsam", "olsak", "olsang", "olsangiz", "olsa",
    "olmaydilar", "olmaydi", "olmaymiz", "olmaslik", "olmagan", "olmoq", "olish", "olgan",
    # Imperative/Optative
    "ayliklar", "aylik", "ayin", "ay", "gin", "gil", "inglar", "ingizlar",
    # Reciprocal / Causative
    "lashmoq", "lashgan", "lashdi", "lashadi", "lashaylik", "lashish", "lash",
    "tirish", "tirmoq", "tirdi", "tiradi", "tirgan",
    # Single letter
    "k", "q"
]

def check_stem(stem):
    if len(stem) < 2: return False
    if stem in uz_words: return True
    if (stem + "moq") in uz_words: return True
    return False

def try_morph(word):
    for s1 in sorted(new_suffixes, key=len, reverse=True):
        if len(word) > len(s1) + 1 and word.endswith(s1):
            stem1 = word[:-len(s1)]
            if check_stem(stem1): return (True, s1, stem1)
            for s2 in sorted(new_suffixes, key=len, reverse=True):
                if len(stem1) > len(s2) + 1 and stem1.endswith(s2):
                    stem2 = stem1[:-len(s2)]
                    if check_stem(stem2): return (True, f"{s2}+{s1}", stem2)
    return (False, None, None)

print("gaplashsak ->", try_morph("gaplashsak"))
print("bo'laveradi ->", try_morph("bo'laveradi"))
print("gaplashaylik ->", try_morph("gaplashaylik"))
print("yozsak ->", try_morph("yozsak"))
print("qilaveradi ->", try_morph("qilaveradi"))
print("ko'rolmaydi ->", try_morph("ko'rolmaydi"))
