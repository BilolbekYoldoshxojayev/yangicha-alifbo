//! YangiAlifbo — UzbekPhonetics
//! Transliteration rules: 1995 Uzbek Latin → 2024 Reformed Latin.
//!   sh → ş,  ch → ç,  oʻ → ö,  gʻ → ğ
//! Handles case preservation, tutuq / apostrophe-boundary exceptions,
//! and all 9 apostrophe/quote character variants.

/// All 9 characters recognized as apostrophe/tutuq belgisi variants in Uzbek text.
pub const APOSTROPHE_VARIANTS: [char; 9] = [
    '\'',     // 1. U+0027 APOSTROPHE
    '\u{2018}', // 2. U+2018 LEFT SINGLE QUOTATION MARK
    '\u{2019}', // 3. U+2019 RIGHT SINGLE QUOTATION MARK
    '\u{02BB}', // 4. U+02BB MODIFIER LETTER TURNED COMMA
    '\u{02BC}', // 5. U+02BC MODIFIER LETTER APOSTROPHE
    '`',      // 6. U+0060 GRAVE ACCENT
    '\u{00B4}', // 7. U+00B4 ACUTE ACCENT
    '\u{201B}', // 8. U+201B SINGLE HIGH-REVERSED-9 QUOTATION MARK
    '\u{2032}', // 9. U+2032 PRIME
];

#[inline]
pub fn is_apostrophe(c: char) -> bool {
    APOSTROPHE_VARIANTS.contains(&c)
}

/// Normalizes all 9 apostrophe variants in a string to standard ASCII apostrophe `'` (U+0027).
pub fn normalize_apostrophes(text: &str) -> String {
    let mut result = String::with_capacity(text.len());
    for c in text.chars() {
        if is_apostrophe(c) {
            result.push('\'');
        } else {
            result.push(c);
        }
    }
    result
}

/// Checks whether a word contains any convertible character sequences (`sh`, `ch`, `o'`, `g'`).
/// Syllable break sequences `s'h` and `c'h` (e.g. `Is'hoq`, `mus'haf`) are protected.
pub fn has_convertible_patterns(word: &str) -> bool {
    if word.is_empty() {
        return false;
    }

    let normalized = normalize_apostrophes(word);
    let chars: Vec<char> = normalized.chars().collect();
    if chars.len() < 2 {
        return false;
    }

    // Identify protected positions for s'h and c'h
    let mut protected = vec![false; chars.len()];
    if chars.len() >= 3 {
        for i in 0..chars.len() - 2 {
            let lo = chars[i].to_ascii_lowercase();
            let mid = chars[i + 1];
            let next = chars[i + 2].to_ascii_lowercase();

            if (lo == 's' || lo == 'c') && mid == '\'' && next == 'h' {
                protected[i] = true;
                protected[i + 1] = true;
                protected[i + 2] = true;
            }
        }
    }

    for i in 0..chars.len() - 1 {
        if protected[i] || protected[i + 1] {
            continue;
        }

        let lo = chars[i].to_ascii_lowercase();
        let next = chars[i + 1];
        let lo_next = next.to_ascii_lowercase();

        // sh or ch
        if (lo == 's' || lo == 'c') && lo_next == 'h' {
            return true;
        }

        // o' or g'
        if (lo == 'o' || lo == 'g') && next == '\'' {
            return true;
        }
    }

    false
}

/// Converts text from 1995 Uzbek Latin orthography to 2024 Reformed Latin:
///   sh → ş,  ch → ç,  oʻ → ö,  gʻ → ğ
/// Preserves letter casing and protects syllable break boundaries (s'h, c'h).
pub fn convert(word: &str) -> String {
    if word.is_empty() {
        return String::new();
    }

    let normalized = normalize_apostrophes(word);
    let chars: Vec<char> = normalized.chars().collect();
    if chars.is_empty() {
        return String::new();
    }

    // Mark protected positions (s'h and c'h)
    let mut protected = vec![false; chars.len()];
    if chars.len() >= 3 {
        for i in 0..chars.len() - 2 {
            let lo = chars[i].to_ascii_lowercase();
            let mid = chars[i + 1];
            let next = chars[i + 2].to_ascii_lowercase();

            if (lo == 's' || lo == 'c') && mid == '\'' && next == 'h' {
                protected[i] = true;
                protected[i + 1] = true;
                protected[i + 2] = true;
            }
        }
    }

    let mut result = String::with_capacity(word.len());
    let mut idx = 0;

    while idx < chars.len() {
        if protected[idx] {
            result.push(chars[idx]);
            idx += 1;
            continue;
        }

        if idx + 1 < chars.len() && !protected[idx + 1] {
            let lo = chars[idx].to_ascii_lowercase();
            let next_char = chars[idx + 1];
            let lo_next = next_char.to_ascii_lowercase();
            let is_upper1 = chars[idx].is_uppercase();

            // sh → ş
            if lo == 's' && lo_next == 'h' {
                result.push(if is_upper1 { 'Ş' } else { 'ş' });
                idx += 2;
                continue;
            }

            // ch → ç
            if lo == 'c' && lo_next == 'h' {
                result.push(if is_upper1 { 'Ç' } else { 'ç' });
                idx += 2;
                continue;
            }

            // o' → ö
            if lo == 'o' && next_char == '\'' {
                result.push(if is_upper1 { 'Ö' } else { 'ö' });
                idx += 2;
                continue;
            }

            // g' → ğ
            if lo == 'g' && next_char == '\'' {
                result.push(if is_upper1 { 'Ğ' } else { 'ğ' });
                idx += 2;
                continue;
            }
        }

        result.push(chars[idx]);
        idx += 1;
    }

    result
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_phonetics_basic() {
        assert_eq!(convert("shahar"), "şahar");
        assert_eq!(convert("Shahar"), "Şahar");
        assert_eq!(convert("SHAHAR"), "ŞAHAR");
        assert_eq!(convert("cho'l"), "çöl");
        assert_eq!(convert("Cho'l"), "Çöl");
        assert_eq!(convert("g'alaba"), "ğalaba");
        assert_eq!(convert("o'qituvchi"), "öqituvçi");
    }

    #[test]
    fn test_tutuq_protection() {
        assert_eq!(convert("Is'hoq"), "Is'hoq");
        assert_eq!(convert("mus'haf"), "mus'haf");
        assert_eq!(convert("as'hob"), "as'hob");
        assert_eq!(convert("mas'ul"), "mas'ul");
    }

    #[test]
    fn test_apostrophe_variants() {
        assert_eq!(convert("o‘zbek"), "özbek");
        assert_eq!(convert("gʻoz"), "ğoz");
        assert_eq!(convert("yo`l"), "yöl");
    }
}
