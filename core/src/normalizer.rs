//! YangiAlifbo — ChatNormalizer
//! Normalizes colloquial, informal, and abbreviated Uzbek chat words
//! into standard orthographic forms ready for reformed Latin transliteration.

use std::collections::HashSet;
use std::sync::LazyLock;

static BOL_SUFFIXES: LazyLock<HashSet<&'static str>> = LazyLock::new(|| {
    [
        "", "adi", "di", "sa", "ib", "gan", "gani", "ishi", "averadi", "sin", "maydi",
        "arkan", "ardim", "gancha", "adimi", "ardi", "ar", "masa", "ish", "yapti", "lar",
        "masin", "madi", "ganmiz", "gandik", "masam", "votti", "vommiz", "vomman", "vosiz",
        "vossiz", "vosila", "votgan", "uradi", "uramiz", "urasiz", "uring", "vurasiz", "vuramiz",
    ]
    .into_iter()
    .collect()
});

static KOR_SUFFIXES: LazyLock<HashSet<&'static str>> = LazyLock::new(|| {
    [
        "", "amiz", "amz", "ishamiz", "ishamz", "dim", "dm", "dik", "dk", "ib", "gan",
        "gancha", "ganmiz", "gandik", "ishdik", "aylik", "inadi", "indi", "ik", "asan",
        "asiz", "asz", "ish", "a", "ing", "aylikchi", "voman", "votti", "vommiz",
    ]
    .into_iter()
    .collect()
});

static OZ_SUFFIXES: LazyLock<HashSet<&'static str>> = LazyLock::new(|| {
    [
        "", "i", "im", "ing", "imiz", "iz", "lari", "bekcha", "bek", "gina", "roq",
        "ini", "icha", "iga", "ida", "idan", "imizga", "imizda", "imizni", "ingiz",
    ]
    .into_iter()
    .collect()
});

static KOP_SUFFIXES: LazyLock<HashSet<&'static str>> = LazyLock::new(|| {
    ["", "roq", "chilik", "aydi", "aytir", "ina", "dan", "ga", "ni", "da"]
        .into_iter()
        .collect()
});

static SOZ_SUFFIXES: LazyLock<HashSet<&'static str>> = LazyLock::new(|| {
    [
        "", "lar", "lashuv", "lagan", "lash", "im", "ing", "i", "imiz", "da", "dan", "ga", "ni",
    ]
    .into_iter()
    .collect()
});

static SORA_SUFFIXES: LazyLock<HashSet<&'static str>> = LazyLock::new(|| {
    [
        "", "moq", "dim", "dm", "sh", "gan", "yman", "ng", "ylik", "dik", "dk", "yapsiz",
    ]
    .into_iter()
    .collect()
});

static YOL_SUFFIXES: LazyLock<HashSet<&'static str>> = LazyLock::new(|| {
    ["", "lar", "i", "ida", "idan", "lovchi", "ga", "da", "ni", "boshchi"]
        .into_iter()
        .collect()
});

static ORT_SUFFIXES: LazyLock<HashSet<&'static str>> = LazyLock::new(|| {
    ["a", "oq", "iq", "iqcha", "ada", "asida", "iqroq", "ada", "asiga", "og'i", "og'im"]
        .into_iter()
        .collect()
});

static OCHIR_SUFFIXES: LazyLock<HashSet<&'static str>> = LazyLock::new(|| {
    ["", "ildi", "ish", "dim", "dm", "di", "ib", "ilsin", "ing", "moq"]
        .into_iter()
        .collect()
});

static OYLA_SUFFIXES: LazyLock<HashSet<&'static str>> = LazyLock::new(|| {
    [
        "", "b", "dim", "dm", "sh", "yman", "sak", "sam", "gan", "nadi", "mang", "ng", "dik",
    ]
    .into_iter()
    .collect()
});

static JONAT_SUFFIXES: LazyLock<HashSet<&'static str>> = LazyLock::new(|| {
    ["", "dim", "dm", "gan", "ildi", "ish", "ing", "adi", "aman", "amiz", "aylik"]
        .into_iter()
        .collect()
});

static QOSH_SUFFIXES: LazyLock<HashSet<&'static str>> = LazyLock::new(|| {
    [
        "", "imcha", "ish", "ildi", "ilgan", "moq", "ilsin", "amiz", "amz", "adi", "di", "dim",
        "dm",
    ]
    .into_iter()
    .collect()
});

static ORIN_SUFFIXES: LazyLock<HashSet<&'static str>> = LazyLock::new(|| {
    ["", "diq", "bosar", "li", "da", "lar", "i", "ga", "dan", "iga", "ida"]
        .into_iter()
        .collect()
});

static TOL_SUFFIXES: LazyLock<HashSet<&'static str>> = LazyLock::new(|| {
    ["ov", "ash", "adi", "angan", "ayman", "dir", "dim", "dm", "di", "dirildi", "iq"]
        .into_iter()
        .collect()
});

static OT_SUFFIXES: LazyLock<HashSet<&'static str>> = LazyLock::new(|| {
    ["gan", "ib", "ish", "ganlar", "adi", "moqda", "dim", "dm", "aylik", "ibdi", "dik", "dk"]
        .into_iter()
        .collect()
});

/// Normalizes a chat word into standard orthography, preserving casing.
pub fn normalize(word: &str) -> String {
    if word.trim().len() < 2 {
        return word.to_string();
    }

    let lower = word.to_lowercase();
    let mut norm = lower.clone();

    // 0. Hyphenated numbers (e.g. 5-ci -> 5-chi, 1-orin -> 1-o'rin)
    if let Some(hyphen_idx) = norm.find('-') {
        if hyphen_idx > 0 && norm[..hyphen_idx].chars().all(|c| c.is_ascii_digit()) {
            let prefix = &norm[..=hyphen_idx];
            let suffix = &norm[hyphen_idx + 1..];
            if suffix == "ci" || suffix == "c" {
                norm = format!("{}chi", prefix);
            } else {
                let norm_suffix = normalize(suffix);
                if norm_suffix != suffix {
                    norm = format!("{}{}", prefix, norm_suffix);
                }
            }
        }
    }

    // 1. Russian keyboard 'w' substitutions
    if norm == "wu" {
        norm = "shu".to_string();
    } else if norm.starts_with("wunaqa")
        || norm.starts_with("wunaqangi")
        || norm.starts_with("wuni")
        || norm.starts_with("wunga")
        || norm.starts_with("wunday")
        || norm.starts_with("wunde")
        || norm.starts_with("wuncha")
        || norm.starts_with("wunda")
        || norm.starts_with("wuning")
        || norm.starts_with("wunisi")
    {
        norm = format!("sh{}", &norm[1..]);
    } else if norm.starts_with("owa") || norm.starts_with("uwa") {
        norm = format!("o'sha{}", &norm[3..]);
    } else if norm == "awqam" {
        norm = "oqshom".to_string();
    } else {
        if norm.contains("yaxw") {
            norm = norm.replace("yaxw", "yaxsh");
        }
        if norm.contains("tuw") {
            norm = norm.replace("tuw", "tush");
        }
        if norm.contains("bowq") {
            norm = norm.replace("bowq", "boshq");
        }
        if norm.contains("bowlan") {
            norm = norm.replace("bowlan", "boshlan");
        }
        if norm.contains("yow") {
            norm = norm.replace("yow", "yosh");
        }
        if norm.ends_with("iwi") && norm.len() >= 5 {
            norm = format!("{}ishi", &norm[..norm.len() - 3]);
        }
        if norm == "iwim" || norm == "iwing" || norm == "iwi" {
            norm = norm.replace('w', "sh");
        }
        if norm == "quwiladi" {
            norm = "qo'shiladi".to_string();
        }
        if norm.contains("tawavor") {
            norm = norm.replace("tawavor", "tashavor");
        }
    }

    // 2. 'c' for 'ch' in high-frequency chat abbreviations
    if norm == "ucun" {
        norm = "uchun".to_string();
    } else if norm == "qacon" {
        norm = "qachon".to_string();
    } else if norm == "hec" {
        norm = "hech".to_string();
    } else if norm == "qanca" {
        norm = "qancha".to_string();
    } else if norm == "necu" {
        norm = "nechun".to_string();
    }

    // 3. Dialectal & spoken stems
    if norm.starts_with("osha") {
        norm = format!("o'sha{}", &norm[4..]);
    } else if norm.starts_with("bopti") {
        norm = format!("bo'pti{}", &norm[5..]);
    } else if norm == "bomasam" || norm == "bomasa" || norm == "bomasin" {
        norm = "bo'lmasam".to_string();
    } else if norm == "bosa" {
        norm = "bo'lsa".to_string();
    } else if norm == "bovotti" || norm == "bovot" {
        norm = "bo'votti".to_string();
    } else if norm.starts_with("bovot") && norm.len() >= 6 {
        norm = format!("bo'vott{}", &norm[6..]);
    } else if norm == "lekn" {
        norm = "lekin".to_string();
    } else if norm == "blan" || norm == "bln" {
        norm = "bilan".to_string();
    } else if norm == "nma" {
        norm = "nima".to_string();
    } else if norm == "nmaga" {
        norm = "nimaga".to_string();
    } else if norm == "nmadur" || norm == "nmadir" {
        norm = "nimadir".to_string();
    } else if norm == "qalesz" {
        norm = "qaleysiz".to_string();
    } else if norm.starts_with("qattansz") {
        norm = "qattansiz".to_string();
    } else if norm == "yaxwmsz" {
        norm = "yaxshimisiz".to_string();
    }

    // 4. Suffix contractions (-amz, -asz, -dm, -dk, -misz)
    if norm.ends_with("amz") && norm.len() >= 5 {
        norm = format!("{}amiz", &norm[..norm.len() - 3]);
    } else if norm.ends_with("asz") && norm.len() >= 5 {
        norm = format!("{}asiz", &norm[..norm.len() - 3]);
    } else if norm.ends_with("misz") && norm.len() >= 6 {
        norm = format!("{}misiz", &norm[..norm.len() - 4]);
    } else if norm.ends_with("dm") && norm.len() >= 3 && norm != "adm" {
        norm = format!("{}dim", &norm[..norm.len() - 2]);
    } else if norm.ends_with("dk") && norm.len() >= 4 && norm != "idk" {
        norm = format!("{}dik", &norm[..norm.len() - 2]);
    }

    // 5. Ultra-fast unaccented 'o' expansion
    norm = expand_unaccented_roots_fast(&norm);

    if norm == lower {
        word.to_string()
    } else {
        match_casing(word, &norm)
    }
}

fn expand_unaccented_roots_fast(norm: &str) -> String {
    // Root: bol- -> bo'l-
    if norm.starts_with("bol") && BOL_SUFFIXES.contains(&norm[3..]) {
        return format!("bo'l{}", &norm[3..]);
    }

    // Root: kor- -> ko'r-
    if norm.starts_with("kor") && KOR_SUFFIXES.contains(&norm[3..]) {
        return format!("ko'r{}", &norm[3..]);
    }

    // Root: oz- -> o'z-
    if norm.starts_with("oz") && OZ_SUFFIXES.contains(&norm[2..]) {
        return format!("o'z{}", &norm[2..]);
    }

    // Words: togri, zor, hop, yoq
    if norm == "togri" || norm.starts_with("togri") {
        return format!("to'g'ri{}", &norm[5..]);
    }
    if norm == "zor" || norm.starts_with("zor") {
        return format!("zo'r{}", &norm[3..]);
    }
    if norm == "hop" || norm == "xop" {
        return "xo'p".to_string();
    }
    if norm == "yoq" || norm == "yoqmi" || norm == "yoqligi" || norm == "yoqku" {
        return format!("yo'q{}", &norm[3..]);
    }

    // Root: kop- -> ko'p-
    if norm.starts_with("kop") && KOP_SUFFIXES.contains(&norm[3..]) {
        return format!("ko'p{}", &norm[3..]);
    }

    // Root: soz- -> so'z-
    if norm.starts_with("soz") && SOZ_SUFFIXES.contains(&norm[3..]) {
        return format!("so'z{}", &norm[3..]);
    }

    // Root: sora- -> so'ra-
    if norm.starts_with("sora") && SORA_SUFFIXES.contains(&norm[4..]) {
        return format!("so'ra{}", &norm[4..]);
    }

    // Root: yol- -> yo'l-
    if norm.starts_with("yol") && YOL_SUFFIXES.contains(&norm[3..]) {
        return format!("yo'l{}", &norm[3..]);
    }

    // Root: ort- -> o'rt-
    if norm.starts_with("ort") && ORT_SUFFIXES.contains(&norm[3..]) {
        return format!("o'rt{}", &norm[3..]);
    }

    // Root: ochir- -> o'chir-
    if norm.starts_with("ochir") && OCHIR_SUFFIXES.contains(&norm[5..]) {
        return format!("o'chir{}", &norm[5..]);
    }

    // Root: oyla- -> o'yla-
    if norm.starts_with("oyla") && OYLA_SUFFIXES.contains(&norm[4..]) {
        return format!("o'yla{}", &norm[4..]);
    }

    // Root: jonat- -> jo'nat-
    if norm.starts_with("jonat") && JONAT_SUFFIXES.contains(&norm[5..]) {
        return format!("jo'nat{}", &norm[5..]);
    }

    // Root: qosh- -> qo'sh-
    if norm.starts_with("qosh") && QOSH_SUFFIXES.contains(&norm[4..]) {
        return format!("qo'sh{}", &norm[4..]);
    }

    // Root: orin- -> o'rin-
    if norm.starts_with("orin") && ORIN_SUFFIXES.contains(&norm[4..]) {
        return format!("o'rin{}", &norm[4..]);
    }

    // Root: orgat- -> o'rgat-, organ- -> o'rgan-
    if norm.starts_with("orgat") {
        return format!("o'rgat{}", &norm[5..]);
    }
    if norm.starts_with("organ") {
        return format!("o'rgan{}", &norm[5..]);
    }

    // Root: tol- -> to'l-
    if norm.starts_with("tol") && TOL_SUFFIXES.contains(&norm[3..]) {
        return format!("to'l{}", &norm[3..]);
    }

    // Root: ot- -> o't-
    if norm.starts_with("ot") && OT_SUFFIXES.contains(&norm[2..]) {
        return format!("o't{}", &norm[2..]);
    }

    norm.to_string()
}

/// Matches the casing pattern of the original word.
pub fn match_casing(original: &str, transformed: &str) -> String {
    if transformed.is_empty() {
        return String::new();
    }

    let all_upper = original
        .chars()
        .filter(|c| c.is_alphabetic())
        .all(|c| c.is_uppercase());

    if all_upper {
        return transformed.to_uppercase();
    }

    if let Some(first) = original.chars().next() {
        if first.is_uppercase() {
            let mut chars = transformed.chars();
            if let Some(t_first) = chars.next() {
                return format!("{}{}", t_first.to_uppercase(), chars.as_str());
            }
        }
    }

    transformed.to_lowercase()
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_chat_normalization() {
        assert_eq!(normalize("yaxwi"), "yaxshi");
        assert_eq!(normalize("Yaxwi"), "Yaxshi");
        assert_eq!(normalize("YAXWI"), "YAXSHI");
        assert_eq!(normalize("boldi"), "bo'ldi");
        assert_eq!(normalize("koramz"), "ko'ramiz");
        assert_eq!(normalize("qalesz"), "qaleysiz");
        assert_eq!(normalize("5-ci"), "5-chi");
    }
}
