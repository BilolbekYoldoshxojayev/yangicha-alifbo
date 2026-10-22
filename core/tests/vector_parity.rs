use flate2::read::GzDecoder;
use serde::Deserialize;
use std::fs::File;
use std::io::{BufRead, BufReader};
use std::path::PathBuf;
use yangi_core::{
    convert_text, convert_word, normalize_chat, phonetics, ConversionMode, LanguageDetector,
    TransliterationMode,
};

#[derive(Debug, Deserialize)]
struct VectorEntry {
    input: String,
    should_convert_strict: bool,
    should_convert_aggressive: bool,
    chat_normalized: String,
    phonetics_converted: String,
    text_converted_strict: String,
    #[serde(default)]
    text_converted_aggressive: String,
    #[serde(default)]
    text_converted_slang: String,
    #[serde(default)]
    category: String,
}

fn find_repo_root() -> PathBuf {
    let mut dir = std::env::current_dir().unwrap_or_default();
    loop {
        if dir.join("YangiAlifbo.sln").exists() || dir.join(".agent").exists() {
            return dir;
        }
        if !dir.pop() {
            break;
        }
    }
    std::env::current_dir().unwrap_or_default()
}

#[test]
fn test_vector_parity_full() {
    let repo_root = find_repo_root();
    let data_bin_path = repo_root.join("data").join("yangi_data.bin");
    assert!(data_bin_path.exists(), "yangi_data.bin must exist");

    let bin_bytes = std::fs::read(&data_bin_path).expect("Read yangi_data.bin");
    let detector = LanguageDetector::from_bytes(&bin_bytes).expect("Initialize LanguageDetector");

    let vectors_gz_path = repo_root
        .join("tools")
        .join("vectors")
        .join("test_vectors.jsonl.gz");
    assert!(vectors_gz_path.exists(), "test_vectors.jsonl.gz must exist");

    println!("Testing against {}...", vectors_gz_path.display());
    let file = File::open(&vectors_gz_path).expect("Open test_vectors.jsonl.gz");
    let gz = GzDecoder::new(file);
    let reader = BufReader::new(gz);

    let mut total = 0;
    let mut mismatches = Vec::new();

    for line_res in reader.lines() {
        let line = match line_res {
            Ok(l) => l,
            Err(e) => panic!("Error reading line {}: {}", total, e),
        };

        let trimmed_line = line.trim_start_matches('\u{feff}').trim();
        if trimmed_line.is_empty() {
            continue;
        }

        total += 1;
        let entry: VectorEntry = match serde_json::from_str(trimmed_line) {
            Ok(v) => v,
            Err(e) => panic!("JSON parse error at line {}: {}", total, e),
        };

        // 1. Phonetics Convert parity
        let actual_phonetics = phonetics::convert(&entry.input);
        if actual_phonetics != entry.phonetics_converted {
            mismatches.push((
                total,
                entry.input.clone(),
                "phonetics_converted",
                entry.phonetics_converted.clone(),
                actual_phonetics,
            ));
            if mismatches.len() > 20 {
                break;
            }
            continue;
        }

        // 2. Chat Normalizer parity
        let actual_chat = normalize_chat(&entry.input);
        if actual_chat != entry.chat_normalized {
            mismatches.push((
                total,
                entry.input.clone(),
                "chat_normalized",
                entry.chat_normalized.clone(),
                actual_chat,
            ));
            if mismatches.len() > 20 {
                break;
            }
            continue;
        }

        // 3. ShouldConvert(Strict) parity
        let actual_strict = detector.should_convert(&entry.input, ConversionMode::Strict);
        if actual_strict != entry.should_convert_strict {
            mismatches.push((
                total,
                entry.input.clone(),
                "should_convert_strict",
                entry.should_convert_strict.to_string(),
                actual_strict.to_string(),
            ));
            if mismatches.len() > 20 {
                break;
            }
            continue;
        }

        // 4. ShouldConvert(Aggressive) parity
        let actual_agg = detector.should_convert(&entry.input, ConversionMode::Aggressive);
        if actual_agg != entry.should_convert_aggressive {
            mismatches.push((
                total,
                entry.input.clone(),
                "should_convert_aggressive",
                entry.should_convert_aggressive.to_string(),
                actual_agg.to_string(),
            ));
            if mismatches.len() > 20 {
                break;
            }
            continue;
        }

        // 5. TextConverter parity (Sentence case vs single word)
        let actual_text_strict = if entry.category == "sentence_case" {
            let (conv, _) = convert_text(&entry.input, &detector, TransliterationMode::Strict, false);
            conv
        } else {
            convert_word(&entry.input, &detector, TransliterationMode::Strict, false)
        };

        if actual_text_strict != entry.text_converted_strict {
            mismatches.push((
                total,
                entry.input.clone(),
                "text_converted_strict",
                entry.text_converted_strict.clone(),
                actual_text_strict,
            ));
            if mismatches.len() > 20 {
                break;
            }
            continue;
        }
    }

    println!("\n=== Vector Parity Summary ===");
    println!("Total Vectors Tested: {}", total);
    println!("Mismatches: {}", mismatches.len());

    if !mismatches.is_empty() {
        for m in &mismatches {
            println!(
                "  Mismatch #{}: input='{}', field='{}', expected='{}', actual='{}'",
                m.0, m.1, m.2, m.3, m.4
            );
        }
        panic!(
            "Failed vector parity with {} mismatches out of {} vectors",
            mismatches.len(),
            total
        );
    }

    assert_eq!(mismatches.len(), 0, "100% Vector Parity Required!");
}
