with open(r'c:\dev\Projects\alphabet\YangiAlifbo\src\Core\Dictionaries\uzbek_words.txt', encoding='utf-8') as f:
    words = set(line.strip().lower() for line in f)

verbs = ["bo'l", "ko'r", "qo'y", "o't", "to'k", "so'k", "cho'z", "shosh", "och", "kech", "uch", "ich", "chiq", "tush", "osh", "tosh", "qosh", "yosh"]

for v in verbs:
    print(f"'{v}' in dict: {v in words}, '{v}moq' in dict: {(v + 'moq') in words}")
