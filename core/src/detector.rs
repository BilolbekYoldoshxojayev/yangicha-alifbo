//! YangiAlifbo — LanguageDetector
//! 3-tier Uzbek/foreign word discrimination engine.
//! Tier 1: English/tech reject list (FST Set)
//! Tier 2: Uzbek lexicon (Roots FST + Exceptions FST)
//! Tier 3: Uzbek morphological affix heuristic (140+ affixes, consonant alternation, verb infinitives)

use crate::phonetics;
use crate::suffixes::{CHAINABLE_INNER_SUFFIXES, ORDINAL_SUFFIXES, UZBEK_SUFFIXES};
use byteorder::{ByteOrder, LittleEndian};
use fst::Set;
use std::collections::HashSet;
use std::sync::LazyLock;

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum ConversionMode {
    Strict,
    Aggressive,
}

static ORDINAL_SET: LazyLock<HashSet<&'static str>> = LazyLock::new(|| {
    ORDINAL_SUFFIXES.iter().copied().collect()
});

const MAGIC: &[u8; 8] = b"YANGIDAT";

/// Zero-copy LanguageDetector backed by memory slices of FST sets.
pub struct LanguageDetector<'a> {
    roots: Set<&'a [u8]>,
    exceptions: Set<&'a [u8]>,
    rejects: Set<&'a [u8]>,
    classifier: Option<crate::classifier::UnknownWordClassifier<'a>>,
}

impl<'a> LanguageDetector<'a> {
    /// Creates a LanguageDetector from raw binary slice formatted according to data/FORMAT.md.
    pub fn from_bytes(bytes: &'a [u8]) -> Result<Self, &'static str> {
        if bytes.len() < 64 {
            return Err("Binary data too short (less than 64-byte header)");
        }

        if &bytes[0..8] != MAGIC {
            return Err("Invalid magic bytes in dictionary header");
        }

        let roots_offset = LittleEndian::read_u64(&bytes[16..24]) as usize;
        let roots_len = LittleEndian::read_u64(&bytes[24..32]) as usize;

        let exceptions_offset = LittleEndian::read_u64(&bytes[32..40]) as usize;
        let exceptions_len = LittleEndian::read_u64(&bytes[40..48]) as usize;

        let reject_offset = LittleEndian::read_u64(&bytes[48..56]) as usize;
        let reject_len = LittleEndian::read_u64(&bytes[56..64]) as usize;

        if bytes.len() < roots_offset + roots_len
            || bytes.len() < exceptions_offset + exceptions_len
            || bytes.len() < reject_offset + reject_len
        {
            return Err("Binary section offsets exceed file bounds");
        }

        let roots_data = &bytes[roots_offset..roots_offset + roots_len];
        let exceptions_data = &bytes[exceptions_offset..exceptions_offset + exceptions_len];
        let reject_data = &bytes[reject_offset..reject_offset + reject_len];

        let roots = Set::new(roots_data).map_err(|_| "Failed to parse Roots FST")?;
        let exceptions = Set::new(exceptions_data).map_err(|_| "Failed to parse Exceptions FST")?;
        let rejects = Set::new(reject_data).map_err(|_| "Failed to parse Reject List FST")?;

        Ok(Self {
            roots,
            exceptions,
            rejects,
            classifier: None,
        })
    }

    /// Attaches an unknown-word classifier (default is OFF/None).
    pub fn with_classifier(mut self, classifier: crate::classifier::UnknownWordClassifier<'a>) -> Self {
        self.classifier = Some(classifier);
        self
    }

}

impl LanguageDetector<'static> {
    /// Creates an embedded instance using the bundled binary data file.
    #[cfg(feature = "embedded-data")]
    pub fn embedded() -> LanguageDetector<'static> {
        const BYTES: &[u8] = include_bytes!("../../data/yangi_data.bin");
        LanguageDetector::from_bytes(BYTES).expect("Failed to initialize embedded YangiAlifbo dictionary")
    }

    /// Creates an embedded instance with the embedded unknown-word classifier attached.
    #[cfg(feature = "embedded-data")]
    pub fn embedded_with_classifier() -> LanguageDetector<'static> {
        let detector = Self::embedded();
        let classifier = crate::classifier::UnknownWordClassifier::embedded();
        detector.with_classifier(classifier)
    }
}

impl<'a> LanguageDetector<'a> {

    /// Normalizes word for dictionary lookup: converts apostrophes and lowercases.
    pub fn normalize_for_lookup(word: &str) -> String {
        phonetics::normalize_apostrophes(word).to_lowercase()
    }

    /// Checks if a word is in the English reject list or compound with English components.
    pub fn is_foreign_or_reject(&self, word: &str) -> bool {
        if word.trim().is_empty() {
            return false;
        }

        let normalized = Self::normalize_for_lookup(word);

        if let Some(hyphen_idx) = normalized.find('-') {
            if hyphen_idx > 0 {
                let part1 = &normalized[..hyphen_idx];
                let part2 = &normalized[hyphen_idx + 1..];

                if part1.chars().all(|c| c.is_ascii_digit()) {
                    if ORDINAL_SET.contains(part2) {
                        return false;
                    }
                    return self.rejects.contains(part2);
                }

                if part2 == "chi" {
                    return self.rejects.contains(part1);
                }

                if self.rejects.contains(part1) || self.rejects.contains(part2) {
                    return true;
                }
            }
        }

        self.rejects.contains(&normalized)
    }

    /// Determines whether a word is Uzbek.
    pub fn is_uzbek(&self, word: &str, mode: ConversionMode) -> bool {
        if word.trim().len() < 2 {
            return false;
        }

        let normalized = Self::normalize_for_lookup(word);
        self.compute_is_uzbek(&normalized, mode)
    }

    fn compute_is_uzbek(&self, normalized: &str, mode: ConversionMode) -> bool {
        if mode == ConversionMode::Aggressive {
            return true;
        }

        // Tier 1: English / foreign reject fast path
        if self.rejects.contains(normalized) {
            return false;
        }

        // Compound & Ordinal Number Handling
        if let Some(hyphen_idx) = normalized.find('-') {
            if hyphen_idx > 0 {
                let part1 = &normalized[..hyphen_idx];
                let part2 = &normalized[hyphen_idx + 1..];

                if part1.chars().all(|c| c.is_ascii_digit()) {
                    if ORDINAL_SET.contains(part2) {
                        return true;
                    }
                    if self.compute_is_uzbek(part2, mode) {
                        return true;
                    }
                } else {
                    if normalized.ends_with("-chi") {
                        let stem = &normalized[..normalized.len() - 4];
                        if stem.len() >= 2 && self.compute_is_uzbek(stem, mode) {
                            return true;
                        }
                    }

                    if !self.rejects.contains(part1) && !self.rejects.contains(part2) {
                        if self.compute_is_uzbek(part1, mode) || self.compute_is_uzbek(part2, mode) {
                            return true;
                        }
                    }
                }
            }
        }

        // Tier 2: Uzbek lexicon exact match (roots or exceptions)
        if self.contains_lexicon(normalized) {
            return true;
        }

        let converted = phonetics::convert(normalized);
        if converted != normalized && self.contains_lexicon(&converted) {
            return true;
        }

        // Tier 3: Morphological affix stripping
        if self.try_morphological_match(normalized) {
            return true;
        }

        if converted != normalized && self.try_morphological_match(&converted) {
            return true;
        }

        // Tier 4: Unknown-word ML classifier (if enabled, default OFF)
        if let Some(ref classifier) = self.classifier {
            return classifier.predict(normalized);
        }

        false
    }

    #[inline]
    fn contains_lexicon(&self, s: &str) -> bool {
        self.roots.contains(s) || self.exceptions.contains(s)
    }

    /// Determines whether a word should be converted.
    pub fn should_convert(&self, word: &str, mode: ConversionMode) -> bool {
        if word.trim().len() < 2 {
            return false;
        }

        if !phonetics::has_convertible_patterns(word) {
            return false;
        }

        if mode == ConversionMode::Aggressive {
            return true;
        }

        self.is_uzbek(word, mode)
    }

    /// Strips known Uzbek suffixes up to 3 levels deep and checks if the remaining stem exists in lexicon.
    fn try_morphological_match(&self, word: &str) -> bool {
        for suffix in UZBEK_SUFFIXES {
            if word.len() > suffix.len() + 1 && word.ends_with(suffix) {
                let stem = &word[..word.len() - suffix.len()];
                if self.check_stem(stem) {
                    return true;
                }

                // Level 2 stripping
                for suffix2 in CHAINABLE_INNER_SUFFIXES {
                    if stem.len() > suffix2.len() + 1 && stem.ends_with(suffix2) {
                        let stem2 = &stem[..stem.len() - suffix2.len()];
                        if self.check_stem(stem2) {
                            return true;
                        }

                        // Level 3 stripping
                        for suffix3 in CHAINABLE_INNER_SUFFIXES {
                            if stem2.len() > suffix3.len() + 1 && stem2.ends_with(suffix3) {
                                let stem3 = &stem2[..stem2.len() - suffix3.len()];
                                if self.check_stem(stem3) {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
        }
        false
    }

    /// Verifies whether a candidate stem is valid in the Uzbek lexicon:
    /// 1. Direct match (roots or exceptions).
    /// 2. Verb infinitive resolution (stem + "moq").
    /// 3. Root consonant alternation (g' -> q, g -> k, ğ -> q).
    /// 4. Voice derivations (passive -n, -il; causative -t, -tir, -dir).
    fn check_stem(&self, stem: &str) -> bool {
        if stem.len() < 2 {
            return false;
        }

        // 1. Direct match
        if self.contains_lexicon(stem) {
            return true;
        }

        let converted = phonetics::convert(stem);
        if converted != stem && self.contains_lexicon(&converted) {
            return true;
        }

        // 2. Verb infinitive check
        let verb_inf = format!("{}moq", stem);
        if self.contains_lexicon(&verb_inf) {
            return true;
        }
        let conv_verb = phonetics::convert(&verb_inf);
        if conv_verb != verb_inf && self.contains_lexicon(&conv_verb) {
            return true;
        }

        // 3. Consonant Alternation: g' -> q (e.g. o'rtog' -> o'rtoq, qishlog' -> qishloq)
        if stem.ends_with("g'") && stem.len() > 2 {
            let q_stem = format!("{}q", &stem[..stem.len() - 2]);
            if self.contains_lexicon(&q_stem) {
                return true;
            }
            let conv_q = phonetics::convert(&q_stem);
            if conv_q != q_stem && self.contains_lexicon(&conv_q) {
                return true;
            }
        } else if (stem.ends_with('ğ') || stem.ends_with('Ğ')) && stem.len() > 2 {
            let q_stem = format!("{}q", &stem[..stem.len() - 'ğ'.len_utf8()]);
            if self.contains_lexicon(&q_stem) {
                return true;
            }
            let conv_q = phonetics::convert(&q_stem);
            if conv_q != q_stem && self.contains_lexicon(&conv_q) {
                return true;
            }
        }

        // 4. Consonant Alternation: g -> k (e.g. yurag -> yurak, eshig -> eshik)
        if stem.ends_with('g') && stem.len() > 2 {
            let k_stem = format!("{}k", &stem[..stem.len() - 1]);
            if self.contains_lexicon(&k_stem) {
                return true;
            }
            let conv_k = phonetics::convert(&k_stem);
            if conv_k != k_stem && self.contains_lexicon(&conv_k) {
                return true;
            }
        }

        // 5. Voice Derivations:
        // Passive/Reflexive: stem ending in 'n'
        if stem.ends_with('n') && stem.len() > 2 {
            let base_stem = &stem[..stem.len() - 1];
            if self.contains_lexicon(base_stem) || self.contains_lexicon(&format!("{}moq", base_stem)) {
                return true;
            }
            let conv_base = phonetics::convert(base_stem);
            if self.contains_lexicon(&conv_base) || self.contains_lexicon(&format!("{}moq", conv_base)) {
                return true;
            }
        }

        // Passive: stem ending in 'il'
        if stem.ends_with("il") && stem.len() > 3 {
            let base_stem = &stem[..stem.len() - 2];
            if self.contains_lexicon(base_stem) || self.contains_lexicon(&format!("{}moq", base_stem)) {
                return true;
            }
            let conv_base = phonetics::convert(base_stem);
            if self.contains_lexicon(&conv_base) || self.contains_lexicon(&format!("{}moq", conv_base)) {
                return true;
            }
        }

        // Causative: stem ending in 't'
        if stem.ends_with('t') && stem.len() > 2 {
            let base_stem = &stem[..stem.len() - 1];
            if self.contains_lexicon(base_stem) || self.contains_lexicon(&format!("{}moq", base_stem)) {
                return true;
            }
            let conv_base = phonetics::convert(base_stem);
            if self.contains_lexicon(&conv_base) || self.contains_lexicon(&format!("{}moq", conv_base)) {
                return true;
            }
        }

        // Causative: stem ending in 'tir' / 'dir'
        if stem.len() > 4 && (stem.ends_with("tir") || stem.ends_with("dir")) {
            let base_stem = &stem[..stem.len() - 3];
            if self.contains_lexicon(base_stem) || self.contains_lexicon(&format!("{}moq", base_stem)) {
                return true;
            }
            let conv_base = phonetics::convert(base_stem);
            if self.contains_lexicon(&conv_base) || self.contains_lexicon(&format!("{}moq", conv_base)) {
                return true;
            }
        }

        false
    }
}
