//! YangiAlifbo — UnknownWordClassifier
//! Pure integer char 1-5 gram logistic regression classifier.
//! Zero ML runtime, int8 quantized weights, ~16 KB footprint.

use byteorder::{ByteOrder, LittleEndian};

const MAGIC: &[u8; 8] = b"YANGIMOD";
const NUM_FEATURES: usize = 16384;

#[derive(Clone)]
pub struct UnknownWordClassifier<'a> {
    pub threshold: i32,
    pub bias: i32,
    pub weights: &'a [i8],
}

impl<'a> UnknownWordClassifier<'a> {
    /// Loads a classifier from a slice of binary model data.
    pub fn from_bytes(bytes: &'a [u8]) -> Result<Self, &'static str> {
        if bytes.len() < 24 + NUM_FEATURES {
            return Err("Model binary too short");
        }

        if &bytes[0..8] != MAGIC {
            return Err("Invalid magic header in unknown-word model");
        }

        let num_features = LittleEndian::read_u32(&bytes[12..16]) as usize;
        if num_features != NUM_FEATURES {
            return Err("Unexpected feature count in model header");
        }

        let threshold = LittleEndian::read_i32(&bytes[16..20]);
        let bias = LittleEndian::read_i32(&bytes[20..24]);

        let weights_raw = &bytes[24..24 + NUM_FEATURES];
        // Cast &[u8] to &[i8] safely (same size and alignment)
        let weights: &'a [i8] = unsafe {
            std::slice::from_raw_parts(weights_raw.as_ptr() as *const i8, NUM_FEATURES)
        };

        Ok(Self {
            threshold,
            bias,
            weights,
        })
    }

    /// Creates an embedded instance using the bundled model binary.
    #[cfg(feature = "embedded-data")]
    pub fn embedded() -> UnknownWordClassifier<'static> {
        const BYTES: &[u8] = include_bytes!("../../data/unknown_model.bin");
        Self::from_bytes(BYTES).expect("Failed to initialize embedded unknown-word model")
    }

    /// Computes the integer score for an input word.
    pub fn score(&self, word: &str) -> i32 {
        let mut score: i32 = self.bias;
        let lower = word.to_lowercase();
        let bounded = format!("^{}$", lower);
        let chars: Vec<char> = bounded.chars().collect();
        let len = chars.len();

        for n in 1..=5 {
            if n > len {
                break;
            }
            for i in 0..=(len - n) {
                let ngram: String = chars[i..i + n].iter().collect();
                let hash_idx = (fnv1a_hash(ngram.as_bytes()) % (NUM_FEATURES as u32)) as usize;
                score += self.weights[hash_idx] as i32;
            }
        }

        score
    }

    /// Predicts whether an unknown word is likely Uzbek.
    pub fn predict(&self, word: &str) -> bool {
        self.score(word) >= self.threshold
    }
}

#[inline]
fn fnv1a_hash(bytes: &[u8]) -> u32 {
    let mut h: u32 = 2166136261;
    for &b in bytes {
        h ^= b as u32;
        h = h.wrapping_mul(16777619);
    }
    h
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_classifier_embedded() {
        let bytes = include_bytes!("../../data/unknown_model.bin");
        let classifier = UnknownWordClassifier::from_bytes(bytes).expect("Valid model");

        // Common Uzbek stems should score high
        assert!(classifier.score("yaxshilik") > classifier.threshold);

        // English words should score below threshold
        assert!(classifier.score("school") < classifier.threshold);
        assert!(classifier.score("character") < classifier.threshold);
        assert!(!classifier.predict("schedule"));
    }
}
