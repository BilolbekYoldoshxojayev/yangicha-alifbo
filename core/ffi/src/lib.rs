use std::ffi::{CStr, CString};
use std::os::raw::c_char;
use yangi_core::{
    convert_text, normalize_chat, phonetics, ConversionMode, LanguageDetector,
    TransliterationMode,
};

pub struct YangiDetector {
    detector: LanguageDetector<'static>,
    data_ptr: *mut u8,
    data_len: usize,
}

#[repr(C)]
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum YangiConversionMode {
    Strict = 0,
    Aggressive = 1,
}

impl From<YangiConversionMode> for ConversionMode {
    fn from(mode: YangiConversionMode) -> Self {
        match mode {
            YangiConversionMode::Strict => ConversionMode::Strict,
            YangiConversionMode::Aggressive => ConversionMode::Aggressive,
        }
    }
}

impl From<YangiConversionMode> for TransliterationMode {
    fn from(mode: YangiConversionMode) -> Self {
        match mode {
            YangiConversionMode::Strict => TransliterationMode::Strict,
            YangiConversionMode::Aggressive => TransliterationMode::Aggressive,
        }
    }
}

#[no_mangle]
pub unsafe extern "C" fn yangi_detector_create(
    data_bytes: *const u8,
    data_len: usize,
) -> *mut YangiDetector {
    if data_bytes.is_null() || data_len < 64 {
        return std::ptr::null_mut();
    }

    let input_slice = std::slice::from_raw_parts(data_bytes, data_len);
    let mut owned_vec = input_slice.to_vec();
    let data_ptr = owned_vec.as_mut_ptr();
    let data_len = owned_vec.len();
    std::mem::forget(owned_vec);

    let static_slice: &'static [u8] = std::slice::from_raw_parts(data_ptr, data_len);
    match LanguageDetector::from_bytes(static_slice) {
        Ok(detector) => Box::into_raw(Box::new(YangiDetector {
            detector,
            data_ptr,
            data_len,
        })),
        Err(_) => {
            let _ = Vec::from_raw_parts(data_ptr, data_len, data_len);
            std::ptr::null_mut()
        }
    }
}

#[no_mangle]
pub unsafe extern "C" fn yangi_detector_free(detector: *mut YangiDetector) {
    if !detector.is_null() {
        let boxed = Box::from_raw(detector);
        let _ = Vec::from_raw_parts(boxed.data_ptr, boxed.data_len, boxed.data_len);
    }
}

#[no_mangle]
pub unsafe extern "C" fn yangi_should_convert(
    detector: *const YangiDetector,
    word_utf8: *const c_char,
    mode: YangiConversionMode,
) -> bool {
    if detector.is_null() || word_utf8.is_null() {
        return false;
    }

    let c_str = CStr::from_ptr(word_utf8);
    let word = match c_str.to_str() {
        Ok(s) => s,
        Err(_) => return false,
    };

    (*detector).detector.should_convert(word, mode.into())
}

#[no_mangle]
pub unsafe extern "C" fn yangi_is_uzbek(
    detector: *const YangiDetector,
    word_utf8: *const c_char,
    mode: YangiConversionMode,
) -> bool {
    if detector.is_null() || word_utf8.is_null() {
        return false;
    }

    let c_str = CStr::from_ptr(word_utf8);
    let word = match c_str.to_str() {
        Ok(s) => s,
        Err(_) => return false,
    };

    (*detector).detector.is_uzbek(word, mode.into())
}

#[no_mangle]
pub unsafe extern "C" fn yangi_is_foreign_or_reject(
    detector: *const YangiDetector,
    word_utf8: *const c_char,
) -> bool {
    if detector.is_null() || word_utf8.is_null() {
        return false;
    }

    let c_str = CStr::from_ptr(word_utf8);
    let word = match c_str.to_str() {
        Ok(s) => s,
        Err(_) => return false,
    };

    (*detector).detector.is_foreign_or_reject(word)
}

#[no_mangle]
pub unsafe extern "C" fn yangi_phonetics_convert(word_utf8: *const c_char) -> *mut c_char {
    if word_utf8.is_null() {
        return std::ptr::null_mut();
    }

    let c_str = CStr::from_ptr(word_utf8);
    let word = match c_str.to_str() {
        Ok(s) => s,
        Err(_) => return std::ptr::null_mut(),
    };

    let converted = phonetics::convert(word);
    match CString::new(converted) {
        Ok(c_res) => c_res.into_raw(),
        Err(_) => std::ptr::null_mut(),
    }
}

#[no_mangle]
pub unsafe extern "C" fn yangi_chat_normalize(word_utf8: *const c_char) -> *mut c_char {
    if word_utf8.is_null() {
        return std::ptr::null_mut();
    }

    let c_str = CStr::from_ptr(word_utf8);
    let word = match c_str.to_str() {
        Ok(s) => s,
        Err(_) => return std::ptr::null_mut(),
    };

    let normalized = normalize_chat(word);
    match CString::new(normalized) {
        Ok(c_res) => c_res.into_raw(),
        Err(_) => std::ptr::null_mut(),
    }
}

#[no_mangle]
pub unsafe extern "C" fn yangi_convert_text(
    detector: *const YangiDetector,
    input_utf8: *const c_char,
    mode: YangiConversionMode,
    slang: bool,
    out_changed: *mut usize,
) -> *mut c_char {
    if detector.is_null() || input_utf8.is_null() {
        return std::ptr::null_mut();
    }

    let c_str = CStr::from_ptr(input_utf8);
    let input = match c_str.to_str() {
        Ok(s) => s,
        Err(_) => return std::ptr::null_mut(),
    };

    let (converted, changed) = convert_text(input, &(*detector).detector, mode.into(), slang);

    if !out_changed.is_null() {
        *out_changed = changed;
    }

    match CString::new(converted) {
        Ok(c_res) => c_res.into_raw(),
        Err(_) => std::ptr::null_mut(),
    }
}

#[no_mangle]
pub unsafe extern "C" fn yangi_string_free(s: *mut c_char) {
    if !s.is_null() {
        let _ = CString::from_raw(s);
    }
}
