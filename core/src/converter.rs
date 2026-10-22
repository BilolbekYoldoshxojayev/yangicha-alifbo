//! YangiAlifbo — TextConverter
//! Converts an entire text document or paragraph the way typed words are converted at boundaries.
//! Preserves whitespace, punctuation, brackets, quotes, and URLs.

use crate::detector::{ConversionMode, LanguageDetector};
use crate::normalizer;
use crate::phonetics;

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum TransliterationMode {
    Strict,
    Aggressive,
}

impl From<TransliterationMode> for ConversionMode {
    fn from(mode: TransliterationMode) -> Self {
        match mode {
            TransliterationMode::Strict => ConversionMode::Strict,
            TransliterationMode::Aggressive => ConversionMode::Aggressive,
        }
    }
}

/// Converts a full text and returns the converted text along with the count of changed words.
pub fn convert_text(
    input: &str,
    detector: &LanguageDetector,
    mode: TransliterationMode,
    slang: bool,
) -> (String, usize) {
    if input.is_empty() {
        return (String::new(), 0);
    }

    let mut result = String::with_capacity(input.len());
    let mut changed_count = 0;

    let mut start = 0;
    let chars: Vec<(usize, char)> = input.char_indices().collect();
    let len = input.len();

    let mut i = 0;
    while i < chars.len() {
        let (byte_idx, ch) = chars[i];
        if is_delimiter(ch) {
            // Found delimiter: if there was a preceding word token, process it
            if byte_idx > start {
                let token = &input[start..byte_idx];
                let converted = convert_word(token, detector, mode, slang);
                if converted != token {
                    changed_count += 1;
                }
                result.push_str(&converted);
            }

            // Append consecutive delimiters
            let delim_start = byte_idx;
            while i < chars.len() && is_delimiter(chars[i].1) {
                i += 1;
            }
            let delim_end = if i < chars.len() { chars[i].0 } else { len };
            result.push_str(&input[delim_start..delim_end]);
            start = delim_end;
            continue;
        }
        i += 1;
    }

    // Trailing word token
    if start < len {
        let token = &input[start..len];
        let converted = convert_word(token, detector, mode, slang);
        if converted != token {
            changed_count += 1;
        }
        result.push_str(&converted);
    }

    (result, changed_count)
}

/// Converts a single word token according to linguistic rules, chat normalization, and mode.
pub fn convert_word(
    token: &str,
    detector: &LanguageDetector,
    mode: TransliterationMode,
    slang: bool,
) -> String {
    if token.len() < 2 {
        return token.to_string();
    }

    let first_char = match token.chars().next() {
        Some(c) => c,
        None => return token.to_string(),
    };

    if !first_char.is_alphabetic() && !first_char.is_ascii_digit() {
        return token.to_string();
    }

    if detector.is_foreign_or_reject(token) {
        return token.to_string();
    }

    let work = if slang {
        normalizer::normalize(token)
    } else {
        token.to_string()
    };

    let conv_mode = ConversionMode::from(mode);

    if phonetics::has_convertible_patterns(&work) {
        if mode == TransliterationMode::Aggressive || detector.is_uzbek(&work, conv_mode) {
            return phonetics::convert(&work);
        }
    } else if work != token && detector.is_uzbek(&work, conv_mode) {
        return work;
    }

    token.to_string()
}

#[inline]
fn is_delimiter(c: char) -> bool {
    c.is_whitespace() || matches!(c, '.' | ',' | '!' | '?' | ':' | ';' | '"' | '(' | ')' | '[' | ']' | '{' | '}' | '/' | '\\')
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_delimiters() {
        assert!(is_delimiter(' '));
        assert!(is_delimiter('\n'));
        assert!(is_delimiter('.'));
        assert!(is_delimiter(','));
        assert!(!is_delimiter('-')); // Hyphen is NOT a delimiter
    }
}
