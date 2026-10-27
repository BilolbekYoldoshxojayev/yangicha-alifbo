"""Counts the Latin-script words of an Uzbek Wikipedia dump.

    python -I wiki_words.py <uzwiki-pages-articles.xml.bz2> <out.tsv>

Writes word<TAB>occurrences<TAB>articles for words seen at least 3 times in at
least 2 articles. Markup that is not running Uzbek text (references, templates,
tables, links' targets, URLs, HTML) is removed first. tools/compact decides
which of these words join the lexicon.
"""
import bz2
import re
import sys
import xml.etree.ElementTree as ET

APOS = "‘’ʻʼ`´‛′"
REF = re.compile(r"<ref[^>]*?/>|<ref.*?</ref>", re.S | re.I)
TAG = re.compile(r"<[^>]+>")
TEMPLATE = re.compile(r"\{\{[^{}]*\}\}")
TABLE = re.compile(r"\{\|.*?\|\}", re.S)
FILE_LINK = re.compile(r"\[\[(?:Fayl|File|Rasm|Image|Tasvir|Kategoriya|Category):[^\[\]]*(?:\[\[[^\]]*\]\][^\[\]]*)*\]\]", re.I)
LINK = re.compile(r"\[\[(?:[^|\]]*\|)?([^\]]*)\]\]")
EXT = re.compile(r"\[https?://\S+\s*([^\]]*)\]|https?://\S+")
WORD = re.compile(r"[A-Za-z][A-Za-z'-]*[A-Za-z]")


def clean(text):
    text = REF.sub(" ", text)
    for _ in range(5):                     # nested templates
        text, n = TEMPLATE.subn(" ", text)
        if not n:
            break
    text = TABLE.sub(" ", text)
    text = FILE_LINK.sub(" ", text)
    text = LINK.sub(r"\1", text)
    text = EXT.sub(r"\1", text)
    text = TAG.sub(" ", text)
    for a in APOS:
        text = text.replace(a, "'")
    return text


def main():
    src, out = sys.argv[1], sys.argv[2]
    count, arts = {}, {}
    pages = 0
    with bz2.open(src, "rb") as f:
        ns = None
        for _, el in ET.iterparse(f, events=("end",)):
            tag = el.tag.rsplit("}", 1)[-1]
            if tag == "ns":
                ns = el.text
            elif tag == "text":
                if ns == "0" and el.text and not el.text.lstrip().lower().startswith("#yo"):   # skip redirects
                    seen = set()
                    for w in WORD.findall(clean(el.text)):
                        w = w.lower()
                        count[w] = count.get(w, 0) + 1
                        seen.add(w)
                    for w in seen:
                        arts[w] = arts.get(w, 0) + 1
                    pages += 1
                    if pages % 50000 == 0:
                        print(f"{pages} articles, {len(count)} distinct words", flush=True)
            if tag == "page":
                el.clear()
    kept = sorted((w for w in count if count[w] >= 3 and arts[w] >= 2), key=lambda w: -count[w])
    with open(out, "w", encoding="utf-8", newline="\n") as o:
        for w in kept:
            o.write(f"{w}\t{count[w]}\t{arts[w]}\n")
    print(f"{pages} articles, {len(count)} distinct words, {len(kept)} kept")


if __name__ == "__main__":
    main()
