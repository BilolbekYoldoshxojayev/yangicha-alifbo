with open(r'c:\dev\Projects\alphabet\YangiAlifbo\src\Core\Dictionaries\uzbek_words.txt', encoding='utf-8') as f:
    uz_words = set(line.strip().lower() for line in f)

print("bo'l + moq in uz_words:", ("bo'l" + "moq") in uz_words)
print("gaplash + moq in uz_words:", ("gaplash" + "moq") in uz_words)
print("gap + lash + moq in uz_words:", "gaplashmoq" in uz_words)
