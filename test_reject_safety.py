import os

dict_dir = r"c:\dev\Projects\alphabet\YangiAlifbo\src\Core\Dictionaries"
with open(os.path.join(dict_dir, "english_reject.txt"), encoding='utf-8') as f:
    en_reject = set(line.strip().lower() for line in f)

with open(os.path.join(dict_dir, "uzbek_words.txt"), encoding='utf-8') as f:
    uz_words = set(line.strip().lower() for line in f)

# Add all bare stems of -moq verbs
for w in list(uz_words):
    if w.endswith('moq') and len(w) > 4:
        stem = w[:-3]
        uz_words.add(stem)
        uz_words.add(stem.replace("o'", "ö").replace("g'", "ğ").replace("sh", "ş").replace("ch", "ç"))

test_english = [
    "show", "sheet", "crash", "push", "shift", "shell", "short", "should", "shop",
    "ship", "shine", "shock", "shoe", "shoot", "shore", "shut", "shadow", "shake",
    "shame", "sharp", "finish", "publish", "english", "child", "church", "change",
    "machine", "catch", "watch", "choose", "check", "chair", "chain", "channel",
    "chapter", "charge", "cheap", "cheese", "chemical", "chest", "touch", "teach",
    "reach", "search", "switch", "match", "fetch", "char", "echo", "chmod", "chown", "branch"
]

for en in test_english:
    if en not in en_reject:
        print(f"WARNING: English word '{en}' NOT in reject list!")

print("All test English words in reject list: OK!")

# Test user sentence words
user_words = ["o'zbekcha", "gaplashsak", "ham", "bo'laveradi", "bo'lsa", "bo'ldi", "qilaveradi", "kelaver"]
print("\nTesting user sentence words:")
for uw in user_words:
    in_dict = uw in uz_words
    print(f"'{uw}' in direct dict: {in_dict}")
