use std::sync::LazyLock;
use wasm_bindgen::prelude::*;
use yangi_core::{
    convert_text as core_convert_text, convert_word as core_convert_word,
    normalize_chat as core_normalize_chat, phonetics as core_phonetics,
    ConversionMode, LanguageDetector, TransliterationMode,
};

static DETECTOR: LazyLock<LanguageDetector<'static>> = LazyLock::new(|| {
    LanguageDetector::embedded()
});

fn parse_mode(mode: &str) -> TransliterationMode {
    if mode.eq_ignore_ascii_case("aggressive") {
        TransliterationMode::Aggressive
    } else {
        TransliterationMode::Strict
    }
}

fn parse_conv_mode(mode: &str) -> ConversionMode {
    if mode.eq_ignore_ascii_case("aggressive") {
        ConversionMode::Aggressive
    } else {
        ConversionMode::Strict
    }
}

#[wasm_bindgen(js_name = convertText)]
pub fn convert_text(input: &str, mode: Option<String>, slang: Option<bool>) -> String {
    let trans_mode = parse_mode(mode.as_deref().unwrap_or("strict"));
    let use_slang = slang.unwrap_or(false);
    let (converted, _) = core_convert_text(input, &DETECTOR, trans_mode, use_slang);
    converted
}

#[wasm_bindgen(js_name = convertWord)]
pub fn convert_word(word: &str, mode: Option<String>, slang: Option<bool>) -> String {
    let trans_mode = parse_mode(mode.as_deref().unwrap_or("strict"));
    let use_slang = slang.unwrap_or(false);
    core_convert_word(word, &DETECTOR, trans_mode, use_slang)
}

#[wasm_bindgen(js_name = shouldConvert)]
pub fn should_convert(word: &str, mode: Option<String>) -> bool {
    let conv_mode = parse_conv_mode(mode.as_deref().unwrap_or("strict"));
    DETECTOR.should_convert(word, conv_mode)
}

#[wasm_bindgen(js_name = isUzbek)]
pub fn is_uzbek(word: &str, mode: Option<String>) -> bool {
    let conv_mode = parse_conv_mode(mode.as_deref().unwrap_or("strict"));
    DETECTOR.is_uzbek(word, conv_mode)
}

#[wasm_bindgen(js_name = isForeignOrReject)]
pub fn is_foreign_or_reject(word: &str) -> bool {
    DETECTOR.is_foreign_or_reject(word)
}

#[wasm_bindgen(js_name = phoneticsConvert)]
pub fn phonetics_convert(word: &str) -> String {
    core_phonetics::convert(word)
}

#[wasm_bindgen(js_name = chatNormalize)]
pub fn chat_normalize(word: &str) -> String {
    core_normalize_chat(word)
}
