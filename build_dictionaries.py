# ──────────────────────────────────────────────────────────────────────
# YangiAlifbo — build_dictionaries.py
# Compiles expanded, linguistically accurate dictionaries:
# 1. uzbek_words.txt (~80,000+ words/stems) with dual 1995 & 2024 indexing,
#    syllable break protection (s'h -> Is'hoq, mus'haf, as'hob),
#    clean grammatical noun declensions (no ungrammatical chaining),
#    and verbal inflections.
# 2. english_reject.txt (10,000+ entries) with high-frequency English,
#    tech terms, programming keywords, CLI tools, and homographs.
# ──────────────────────────────────────────────────────────────────────

import os
import urllib.request

print("=== Building YangiAlifbo Dictionaries ===")

dict_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), "src", "YangiAlifbo.Core", "Dictionaries")
os.makedirs(dict_dir, exist_ok=True)

raw_path = os.path.join(dict_dir, "uzwords_raw.txt")

# 1. Load Cyrillic words (online fetch with local fallback)
raw_cyrillic_words = []
if os.path.exists(raw_path):
    print("Reading local uzwords_raw.txt...")
    with open(raw_path, "r", encoding="utf-8") as f:
        raw_cyrillic_words = [line.strip().lower() for line in f if line.strip()]
else:
    url_uz = "https://raw.githubusercontent.com/anvarnarz/uzwords/master/uzwords.txt"
    print("Fetching uzwords from remote...")
    try:
        req = urllib.request.Request(url_uz, headers={'User-Agent': 'Mozilla/5.0'})
        cyrillic_data = urllib.request.urlopen(req, timeout=20).read().decode('utf-8')
        raw_cyrillic_words = [line.strip().lower() for line in cyrillic_data.splitlines() if line.strip()]
        with open(raw_path, "w", encoding="utf-8") as f:
            for w in raw_cyrillic_words:
                f.write(w + "\n")
    except Exception as e:
        print(f"Error fetching uzwords: {e}")

print(f"Loaded {len(raw_cyrillic_words)} Cyrillic words")

vowels = set("аеёиоуэюяў")

def cyrillic_to_latin(w):
    res = []
    i = 0
    while i < len(w):
        # Syllable break protection: Cyrillic сҳ -> 1995 s'h (Is'hoq, mus'haf, as'hob)
        if w[i:i+2] == "сҳ":
            res.append("s'h")
            i += 2
            continue
        ch = w[i]
        if ch == 'е':
            if i == 0 or (i > 0 and w[i-1] in vowels):
                res.append("ye")
            else:
                res.append("e")
        elif ch == 'ё': res.append("yo")
        elif ch == 'ю': res.append("yu")
        elif ch == 'я': res.append("ya")
        elif ch == 'ш': res.append("sh")
        elif ch == 'ч': res.append("ch")
        elif ch == 'ў': res.append("o'")
        elif ch == 'ғ': res.append("g'")
        elif ch == 'қ': res.append("q")
        elif ch == 'ҳ': res.append("h")
        elif ch == 'х': res.append("x")
        elif ch == 'ж': res.append("j")
        elif ch == 'ц': res.append("ts")
        elif ch == 'ъ': res.append("'")
        elif ch == 'ь': pass
        elif ch == 'а': res.append("a")
        elif ch == 'б': res.append("b")
        elif ch == 'в': res.append("v")
        elif ch == 'г': res.append("g")
        elif ch == 'д': res.append("d")
        elif ch == 'з': res.append("z")
        elif ch == 'и': res.append("i")
        elif ch == 'й': res.append("y")
        elif ch == 'к': res.append("k")
        elif ch == 'л': res.append("l")
        elif ch == 'м': res.append("m")
        elif ch == 'н': res.append("n")
        elif ch == 'о': res.append("o")
        elif ch == 'п': res.append("p")
        elif ch == 'р': res.append("r")
        elif ch == 'с': res.append("s")
        elif ch == 'т': res.append("t")
        elif ch == 'у': res.append("u")
        elif ch == 'ф': res.append("f")
        elif ch == 'э': res.append("e")
        else:
            if ch.isalpha() or ch in "'-":
                res.append(ch)
        i += 1
    return "".join(res)

def latin_to_reformed(w):
    res = []
    i = 0
    norm = []
    for c in w:
        if c in "\u2018\u2019\u02bb\u02bc`´\u201b\u2032":
            norm.append("'")
        else:
            norm.append(c)
    w = "".join(norm)
    while i < len(w):
        # Syllable break protection: s'h and c'h never merge
        if w[i:i+3].lower() in ("s'h", "c'h"):
            res.append(w[i:i+3])
            i += 3
            continue
        if w[i:i+2].lower() == "sh":
            res.append("ş" if w[i].islower() else "Ş")
            i += 2
            continue
        if w[i:i+2].lower() == "ch":
            res.append("ç" if w[i].islower() else "Ç")
            i += 2
            continue
        if w[i:i+2].lower() == "o'":
            res.append("ö" if w[i].islower() else "Ö")
            i += 2
            continue
        if w[i:i+2].lower() == "g'":
            res.append("ğ" if w[i].islower() else "Ğ")
            i += 2
            continue
        res.append(w[i])
        i += 1
    return "".join(res)

uzbek_words = set()

# Base latin words
for w in raw_cyrillic_words:
    lat = cyrillic_to_latin(w)
    if lat and len(lat) > 1:
        uzbek_words.add(lat)

# Essential base Uzbek vocabulary
base_uzbek = [
    # sh words
    "shahar", "shaharlar", "shaharda", "shahardan", "shaharga", "shahari", "shahrida", "shaharimiz",
    "shaxs", "shaxslar", "shaxsiy", "shart", "shartlar", "shartnoma", "shartli",
    "shirin", "shirinlik", "sharq", "sharqiy", "shu", "shunday", "shunda", "shuning",
    "shuningdek", "shundayki", "sher", "sherlar", "sherik", "sherigi", "sheriklar",
    "shorva", "shoshilmoq", "shoshilinch", "shisha", "shifo", "shifoxona", "shifokor",
    "shikoyat", "shijoat", "shior", "shiypon", "shoshmoq", "shod", "shodlik", "shox",
    "shoxlar", "shoxobcha", "shoira", "shoir", "shoirlar", "shikast", "shukr", "shukur",
    "shubha", "shubhali", "shum", "shumlik", "shon", "shonli", "shovqin", "shovqinli",
    "mashina", "ish", "ishchi", "ishxona", "ishlamoq", "ishladi", "ishlaydi", "ishlash",
    "yaxshi", "yaxshilik", "yosh", "yoshlar", "yoshlik", "tosh", "toshlar", "qosh",
    "bosh", "boshliq", "boshlamoq", "boshlandi", "boshlanish", "boshqaruv", "tush",
    "tushmoq", "tushdi", "tushunmoq", "tushundi", "tushuntirish", "uchrashuv", "uchrashmoq",
    "kush", "kushlar", "pichoq", "qo'shiq", "qo'shiqchi", "qo'shilmoq", "qo'shmoq",
    "qishloq", "qishloqlar", "qishlog'i", "qishlog'imiz", "qishlog'ida", "qishlog'idan",
    "boshlamoqchiman", "boshlayapti", "boshlagan", "boshlasa", "boshladik", "boshlaymiz",
    
    # ch words
    "choy", "choyxona", "choynak", "chorva", "chorvachilik", "chiqish", "chiqmoq",
    "chiqdi", "chiqadi", "chorshamba", "chet", "chetel", "chegara", "chiroq", "chivin",
    "chiziq", "cho'l", "cho'pon", "cho'qqi", "chuchuk", "chidamoq", "chirkin", "chopmoq",
    "chora", "choralar", "charchoq", "charchamoq", "chiroyli", "chim", "chala",
    "kecha", "kechqurun", "burchak", "burch", "uchun", "ochmoq", "ochildi", "ochiq",
    "kochmoq", "kocha", "kochalar", "kichik", "kichkina", "kechikmoq", "sekin",
    "ichmoq", "ichdi", "ichida", "ichidan", "ichiga", "barcha", "parcha", "archa",
    "qochmoq", "qochdi", "yechmoq", "yechdi", "yechim", "yechimlar", "o'chmoq",
    "chuchvara", "chevar", "chavandoz", "chambil", "chanoq", "charx", "chertmoq",
    "chindan", "chinakam", "chindagi", "chopildi", "chopish", "gapirmoq", "gapiryapti",
    "chin", "chop", "ship",
    
    # o' words
    "o'zbek", "o'zbekiston", "o'zbekcha", "o'rtoq", "o'rtoqlar", "o'qituvchi", "o'qituvchilar",
    "o'quvchi", "o'quvchilar", "o'rmon", "o'rmonlar", "o'rik", "o'g'il", "o'g'li",
    "o'yin", "o'yinlar", "o'ynamoq", "o'ynadi", "o'rta", "o'rtasida", "o'sar", "o'smoq",
    "o'tmoq", "o'tdi", "o'tadi", "o'tgan", "o'tov", "o'choq", "o'pka", "o'z", "o'zi",
    "o'ziga", "o'zida", "o'zidan", "o'zini", "o'zining", "o'zlari", "o'zgarish",
    "o'zgardi", "o'zgarmoq", "o'lchamoq", "o'lchov", "o'lim", "o'lmoq", "o'ldi",
    "o'xshash", "o'xshamoq", "o'tirmoq", "o'tirdi", "o'tirib", "o'rnashmoq", "o'rin",
    "o'ringa", "o'rinda", "o'rindiq", "ko'r", "ko'rmoq", "ko'rdi", "ko'radi", "ko'z",
    "ko'zlar", "ko'p", "ko'plik", "bo'lmoq", "bo'ldi", "bo'ladi", "bo'lgan", "bo'lsa",
    "do'st", "do'stlar", "do'stlik", "to'rt", "to'rtinchi", "go'zal", "go'zallik",
    "bo'y", "bo'yi", "bo'yicha", "to'g'ri", "to'g'risida", "so'z", "so'zlar", "so'zlashuv",
    "o'rtog'i", "o'rtog'imiz", "o'rtog'im", "o'rtog'ing", "o'rtog'iga", "o'rtog'ida", "o'rtog'idan",
    "yo'l", "yo'llar", "yo'lchi", "ko'ngil", "ko'rkam", "bo'ston", "to'lqin", "do'kon",
    
    # g' words
    "g'alaba", "g'alabalar", "g'arb", "g'arbiy", "g'azab", "g'azabli", "g'arib",
    "g'ayrat", "g'ayratli", "g'ildirak", "g'ildiraklar", "g'isht", "g'ishtlar", "g'oz",
    "g'o'za", "g'urur", "g'ururli", "g'uncha", "g'amgin", "g'am", "g'ayri", "g'oyib",
    "g'oya", "g'oyalar", "g'oyaviy", "g'or", "g'orlar", "g'alati", "g'alla", "g'amxo'r",
    "tog'", "tog'lar", "bog'", "bog'lar", "bog'bon", "bog'cha", "bog'lanish", "bog'liq",
    "sog'", "sog'lik", "sog'inch", "sog'inmoq", "yog'", "yog'och", "yog'moq", "yog'di",
    "tug'", "tug'ilmoq", "tug'ildi", "tug'ilgan", "bug'", "bug'doy", "sig'moq",
    "sig'im", "yig'moq", "yig'di", "yig'ilish", "yig'loq", "yig'lamoq", "yig'ladi",
    
    # Exceptions (must preserve apostrophe)
    "is'hoq", "mus'haf", "as-hob", "as'hob", "mas'ul", "mas'uliyat", "mas'uliyatli", "san'at",
    "san'atkor", "sur'at", "ta'lim", "ta'sir", "ta'rif", "ma'no", "ma'rifat", "e'tibor",
    "e'lon", "in'om", "mo''jiza", "me'mor", "ne'mat", "sho''la", "jur'at", "a'lo", "ta'zim",
    "osh", "oshmoq", "oshyapti", "yurak", "eshik"
]

for w in base_uzbek:
    uzbek_words.add(w.lower())

# Verb conjugations for all verbs
verbs_moq = [w for w in list(uzbek_words) if w.endswith("moq") and len(w) > 4]

verb_inflections = [
    # Past & Perfect
    "di", "dim", "dik", "ding", "dingiz", "dilar",
    "gan", "ganman", "ganmiz", "gansan", "gansiz", "ganlar", "gandim", "gandik",
    # Present / Continuous
    "yapti", "yapman", "yapmiz", "yapsan", "yapsiz", "yaptilar",
    "moqda", "moqdaman", "moqdamiz", "moqdasan", "moqdasiz", "moqdalar",
    # Present-Future
    "adi", "aman", "amiz", "asan", "asiz", "adilar",
    "ydi", "yman", "ymiz", "ysan", "ysiz", "ydilar",
    # Negative forms
    "madi", "madim", "madik", "mading", "madingiz", "madilar",
    "magan", "maganman", "maganmiz", "magansan", "magansiz", "maganlar",
    "maydi", "mayman", "maymiz", "maysan", "maysiz", "maydilar",
    "maslik", "masdan",
    # Conditional (-sa)
    "sa", "sam", "sak", "sang", "sangiz", "salar", "saylik",
    # Continuative (-aver / -ver)
    "averadi", "averadilar", "averamiz", "averasan", "averasiz",
    "averdi", "averdim", "averdik", "averding", "averdingiz", "averdilar",
    "aversa", "aversak", "aversam", "aversang", "aversangiz", "aversalar",
    "avermoq", "averish", "avergan", "aver",
    "veradi", "veradilar", "veramiz", "verasan", "verasiz",
    "verdi", "verdim", "verdik", "verding", "verdingiz", "verdilar",
    "versa", "versak", "versam", "versang", "versangiz", "versalar",
    "vermoq", "verish", "vergan", "ver",
    # Potentive (-ol)
    "oladi", "oladilar", "olamiz", "olasan", "olasiz",
    "oldi", "oldim", "oldik", "olding", "oldingiz", "oldilar",
    "olsa", "olsak", "olsam", "olsang", "olsangiz", "olsalar",
    "olmaydi", "olmaydilar", "olmaymiz", "olmaslik", "olmoq", "olgan",
    # Imperative / Optative
    "ish", "ishlik", "ishni", "ishga", "ishda", "ishdan",
    "aylik", "ayliklar", "ay", "ayin", "gin", "gil",
    "ing", "ingiz", "inglar", "sin", "sinlar",
    # Participles & converbs
    "gach", "gani", "guncha", "gancha", "gandek", "ganday"
]

for vm in verbs_moq:
    root = vm[:-3]
    uzbek_words.add(root)  # Add bare verb root (e.g. bo'l, gaplash, qil, bor, kel)
    for suf in verb_inflections:
        uzbek_words.add(root + suf)

# Grammatical noun inflections for core vocabulary
core_stems = [
    "shahar", "shaxs", "shart", "o'rtoq", "choyxona", "g'alaba", "mashina",
    "o'zbek", "do'st", "ko'z", "ish", "yo'l", "so'z", "tog'", "bog'", "bog'cha",
    "maktab", "kitob", "qalam", "yurak", "eshik", "bola", "daraxt", "uy",
    "suv", "havo", "quyosh", "oy", "yulduz", "daryo", "tengiz", "dala",
    "qishloq", "viloyat", "tuman", "mahalla", "ko'cha", "bino", "xona",
    "inson", "odam", "xalq", "millat", "vatan", "davlat", "tarix", "fan",
    "adabiyot", "madaniyat", "iqtisod", "siyosat", "qonun", "huquq", "burch",
    "ishchi", "dehqon", "ziyoli", "yoshlar", "ota", "ona", "aka", "uka",
    "opa", "singil", "farzand", "oila", "xotin", "er", "qiz", "o'g'il",
    "bosh", "qo'l", "oyoq", "til", "og'iz", "burun", "quloq", "yuz"
]

noun_clean_suffixes = [
    "lar",
    "im", "ing", "i", "si", "imiz", "ingiz", "lari",
    "ning", "ni", "ga", "ka", "qa", "da", "dan",
    "larim", "laring", "lari", "larimiz", "laringiz",
    "larga", "larda", "lardan", "larni", "larning",
    "imga", "imda", "imdan", "imni", "imning",
    "ingga", "ingda", "ingdan", "ingni", "ingning",
    "iga", "ida", "idan", "ini", "ining",
    "imizga", "imizda", "imizdan", "imizni", "imizning",
    "ingizga", "ingizda", "ingizdan", "ingizni", "ingizning",
    "larimizga", "larimizda", "larimizdan", "larimizni", "larimizning",
    "lariga", "larida", "laridan", "larini", "larining",
    "larimga", "larimda", "larimdan", "larimni", "larimning",
    "laringga", "laringda", "laringdan", "laringni", "laringning",
    "laringizga", "laringizda", "laringizdan", "laringizni", "laringizning",
    "lik", "likka", "likda", "likdan", "likni", "likning", "liklar",
    "siz", "sizlik",
    "chi", "chilar", "chilik", "chilikka", "chilikda", "chilikdan", "chilikni", "chilikning",
    "dagi", "dagilar"
]

for s in core_stems:
    for suf in noun_clean_suffixes:
        uzbek_words.add(s + suf)
    # Consonant alternation for q/k
    if s.endswith("q"):
        alt = s[:-1] + "g'"
        for suf in ["im", "ing", "i", "imiz", "ingiz", "iga", "ida", "idan", "ini", "ining", "imizga", "imizda", "imizdan", "imizni", "imizning"]:
            uzbek_words.add(alt + suf)
    elif s.endswith("k"):
        alt = s[:-1] + "g"
        for suf in ["im", "ing", "i", "imiz", "ingiz", "iga", "ida", "idan", "ini", "ining", "imizga", "imizda", "imizdan", "imizni", "imizning"]:
            uzbek_words.add(alt + suf)

# Add 2024 Reformed orthography equivalents (dual indexing)
reformed_words = set()
for w in uzbek_words:
    ref = latin_to_reformed(w)
    if ref != w:
        reformed_words.add(ref)

uzbek_words.update(reformed_words)

print(f"Total compiled Uzbek words: {len(uzbek_words)}")
uz_path = os.path.join(dict_dir, "uzbek_words.txt")
with open(uz_path, "w", encoding="utf-8") as f:
    for w in sorted(uzbek_words):
        f.write(w + "\n")
print(f"Saved uzbek_words.txt ({os.path.getsize(uz_path)} bytes, {len(uzbek_words)} entries)")


# 2. Build English Reject List (10,000+ entries)
print("Building English reject list...")
en_path = os.path.join(dict_dir, "english_reject.txt")
en_words = set()

if os.path.exists(en_path):
    with open(en_path, "r", encoding="utf-8") as f:
        for line in f:
            t = line.strip().lower()
            if t and not t.startswith("#"):
                en_words.add(t)

# Fetch top English words if needed
if len(en_words) < 5000:
    url_en = "https://raw.githubusercontent.com/first20hours/google-10000-english/master/google-10000-english-no-swears.txt"
    try:
        req = urllib.request.Request(url_en, headers={'User-Agent': 'Mozilla/5.0'})
        en_data = urllib.request.urlopen(req, timeout=20).read().decode('utf-8')
        for w in en_data.splitlines():
            t = w.strip().lower()
            if t:
                en_words.add(t)
    except Exception as e:
        print(f"Error fetching english list: {e}")

special_reject = [
    # sh words
    "show", "shows", "showed", "showing", "shower", "showcase", "sheet", "sheets",
    "crash", "crashed", "crashes", "crashing", "push", "pushed", "pushes", "pushing",
    "ship", "ships", "shipped", "shipping", "shop", "shops", "shopped", "shopping",
    "she", "shall", "should", "short", "shorter", "shortest", "shortly", "share",
    "shared", "shares", "sharing", "shape", "shapes", "shaped", "shaping", "shift",
    "shifts", "shifted", "shifting", "shell", "shells", "shine", "shined", "shining",
    "shiny", "shock", "shocks", "shocked", "shocking", "shoe", "shoes", "shoot",
    "shoots", "shooting", "shot", "shots", "shore", "shores", "shut", "shuts",
    "shutting", "shutdown", "shadow", "shadows", "shake", "shakes", "shaking", "shook",
    "shaken", "shame", "sharp", "sharper", "sharpest", "sharply", "shave", "shaved",
    "shaving", "shelf", "shelves", "shelter", "shelters", "shield", "shields", "shirt",
    "shirts", "shout", "shouts", "shouted", "shouting", "shrink", "shrinks", "shrunk",
    "shrug", "shrugged", "shrugs", "shy", "shoulder", "shoulders", "shatter", "shred",
    "slash", "slashed", "slashes", "smash", "splash", "squash", "wish", "wishes",
    "wished", "wishing", "fish", "fishes", "fishing", "dish", "dishes", "wash",
    "washes", "washed", "washing", "rush", "rushes", "rushed", "rushing", "crush",
    "brush", "bush", "cash", "dash", "flash", "hash", "rash", "trash", "ash",
    "clash", "flesh", "fresh", "mesh", "establish", "established", "finish", "finished",
    "finishes", "finishing", "publish", "published", "publisher", "punish", "vanish",
    "worship", "english", "spanish", "polish", "swedish", "turkish", "british",
    
    # ch words
    "child", "children", "childhood", "church", "churches", "change", "changes",
    "changed", "changing", "charge", "charges", "charged", "charging", "chain",
    "chains", "chair", "chairs", "chalk", "chamber", "chambers", "champion",
    "champions", "chance", "chances", "channel", "channels", "chapter", "chapters",
    "character", "characters", "charity", "charm", "chart", "charts", "chase",
    "chased", "chat", "chats", "chatting", "cheap", "cheaper", "cheat", "check",
    "checks", "checked", "checking", "checkout", "checksum", "cheek", "cheer",
    "cheese", "chef", "chemical", "chemicals", "chemistry", "cherry", "chess",
    "chest", "chew", "chief", "chiefs", "chin", "chip", "chips", "chocolate",
    "choice", "choices", "choose", "chooses", "choosing", "chose", "chosen", "chop",
    "chunk", "chunks", "each", "beach", "breach", "coach", "approach", "approaches",
    "approached", "attach", "attached", "attachment", "catch", "catches", "caught",
    "catching", "detach", "detached", "dispatch", "enrich", "fetch", "fetches",
    "fetched", "fetching", "hatch", "latch", "match", "matches", "matched", "matching",
    "notch", "patch", "patches", "patched", "peach", "preach", "reach", "reaches",
    "reached", "reaching", "rich", "richer", "richest", "sandwich", "scratch",
    "search", "searches", "searched", "searching", "sketch", "snatch", "stitch",
    "stretch", "stretched", "switch", "switches", "switched", "switching", "teach",
    "teaches", "teacher", "teachers", "teaching", "touch", "touches", "touched",
    "touching", "watch", "watches", "watched", "watching", "witch", "much", "such",
    "which", "arch", "echo", "echoes", "scheme", "schemes", "school", "schools",
    "scholar", "anchor", "archive", "archives", "chaos", "epoch", "mechanic",
    "mechanism", "monarch", "orchestra", "stomach", "technical", "technology",
    "architecture", "architect",
    
    # Programming keywords & CLI tools
    "char", "chars", "character", "varchar", "nvarchar", "echo", "push", "shift",
    "unshift", "switch", "case", "default", "match", "fetch", "catch", "throw",
    "try", "finally", "class", "struct", "interface", "enum", "namespace", "using",
    "import", "export", "from", "public", "private", "protected", "internal",
    "static", "void", "int", "string", "bool", "float", "double", "decimal",
    "async", "await", "yield", "return", "break", "continue", "goto", "const",
    "let", "var", "function", "def", "lambda", "select", "where", "group", "by",
    "order", "having", "join", "left", "right", "inner", "outer", "insert",
    "update", "delete", "create", "alter", "drop", "table", "index", "view",
    "git", "commit", "pull", "checkout", "branch", "merge", "rebase",
    "cherry-pick", "stash", "clone", "init", "status", "diff", "log", "tag",
    "chmod", "chown", "chroot", "curl", "wget", "ssh", "scp", "bash", "zsh", "sh",
    "npm", "npx", "yarn", "pnpm", "pip", "dotnet", "cargo", "rustc", "gcc", "clang",
    "python", "node", "docker", "kubectl", "compose", "build", "run", "exec",
    "kubernetes", "terraform", "ansible", "webpack", "vite", "esbuild", "prettier",
    "eslint", "typescript", "javascript", "csharp", "golang", "powershell", "cmd",
    "github", "gitlab", "sudo", "apt", "yum", "brew", "grep", "sed", "awk"
]

for w in special_reject:
    en_words.add(w.lower().strip())

print(f"Total English reject words: {len(en_words)}")
with open(en_path, "w", encoding="utf-8") as f:
    for w in sorted(en_words):
        if w:
            f.write(w + "\n")

print(f"Saved english_reject.txt ({os.path.getsize(en_path)} bytes, {len(en_words)} entries)")
print("=== Dictionaries build complete! ===")
