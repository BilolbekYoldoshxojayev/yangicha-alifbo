with open(r'c:\dev\Projects\alphabet\YangiAlifbo\src\Core\Dictionaries\uzbek_words.txt', encoding='utf-8') as f:
    words = set(line.strip().lower() for line in f)

print('Current word count:', len(words))
moq_stems = set()
for w in words:
    if w.endswith('moq') and len(w) > 4:
        stem = w[:-3]
        moq_stems.add(stem)
        reformed = stem.replace("o'", "ö").replace("g'", "ğ").replace("sh", "ş").replace("ch", "ç")
        moq_stems.add(reformed)

print('New verb stems to add:', len(moq_stems))
words.update(moq_stems)
print('Total new words:', len(words))
