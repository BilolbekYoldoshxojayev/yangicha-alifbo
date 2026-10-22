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
pub unsafe extern "C" fn yangi_detector_create_embedded() -> *mut YangiDetector {
    let detector = LanguageDetector::embedded();
    Box::into_raw(Box::new(YangiDetector {
        detector,
        data_ptr: std::ptr::null_mut(),
        data_len: 0,
    }))
}

#[no_mangle]
pub unsafe extern "C" fn yangi_detector_free(detector: *mut YangiDetector) {
    if !detector.is_null() {
        let boxed = Box::from_raw(detector);
        if !boxed.data_ptr.is_null() && boxed.data_len > 0 {
            let _ = Vec::from_raw_parts(boxed.data_ptr, boxed.data_len, boxed.data_len);
        }
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

pub mod android_jni {
    use super::*;
    use jni::objects::{JClass, JString};
    use jni::sys::{jboolean, jstring, JNI_FALSE, JNI_TRUE};
    use jni::JNIEnv;

    static DETECTOR: std::sync::LazyLock<LanguageDetector<'static>> =
        std::sync::LazyLock::new(|| LanguageDetector::embedded());

    static DETECTOR_WITH_ML: std::sync::LazyLock<LanguageDetector<'static>> =
        std::sync::LazyLock::new(|| LanguageDetector::embedded_with_classifier());

    fn get_detector(use_ml: bool) -> &'static LanguageDetector<'static> {
        if use_ml {
            &DETECTOR_WITH_ML
        } else {
            &DETECTOR
        }
    }

    fn parse_mode_str(mode: &str) -> TransliterationMode {
        if mode.eq_ignore_ascii_case("aggressive") {
            TransliterationMode::Aggressive
        } else {
            TransliterationMode::Strict
        }
    }

    fn parse_conv_mode_str(mode: &str) -> ConversionMode {
        if mode.eq_ignore_ascii_case("aggressive") {
            ConversionMode::Aggressive
        } else {
            ConversionMode::Strict
        }
    }

    #[no_mangle]
    pub unsafe extern "system" fn Java_uz_yangialifbo_keyboard_YangiNativeBridge_convertWord(
        mut env: JNIEnv,
        _class: JClass,
        word: JString,
        mode: JString,
        slang: jboolean,
        use_ml: jboolean,
    ) -> jstring {
        let word_str: String = match env.get_string(&word) {
            Ok(s) => s.into(),
            Err(_) => return std::ptr::null_mut(),
        };
        let mode_str: String = match env.get_string(&mode) {
            Ok(s) => s.into(),
            Err(_) => "strict".to_string(),
        };

        let detector = get_detector(use_ml != 0);
        let trans_mode = parse_mode_str(&mode_str);
        let result = yangi_core::convert_word(&word_str, detector, trans_mode, slang != 0);

        match env.new_string(result) {
            Ok(js) => js.into_raw(),
            Err(_) => std::ptr::null_mut(),
        }
    }

    #[no_mangle]
    pub unsafe extern "system" fn Java_uz_yangialifbo_keyboard_YangiNativeBridge_convertText(
        mut env: JNIEnv,
        _class: JClass,
        text: JString,
        mode: JString,
        slang: jboolean,
        use_ml: jboolean,
    ) -> jstring {
        let text_str: String = match env.get_string(&text) {
            Ok(s) => s.into(),
            Err(_) => return std::ptr::null_mut(),
        };
        let mode_str: String = match env.get_string(&mode) {
            Ok(s) => s.into(),
            Err(_) => "strict".to_string(),
        };

        let detector = get_detector(use_ml != 0);
        let trans_mode = parse_mode_str(&mode_str);
        let (result, _) = yangi_core::convert_text(&text_str, detector, trans_mode, slang != 0);

        match env.new_string(result) {
            Ok(js) => js.into_raw(),
            Err(_) => std::ptr::null_mut(),
        }
    }

    #[no_mangle]
    pub unsafe extern "system" fn Java_uz_yangialifbo_keyboard_YangiNativeBridge_shouldConvert(
        mut env: JNIEnv,
        _class: JClass,
        word: JString,
        mode: JString,
        use_ml: jboolean,
    ) -> jboolean {
        let word_str: String = match env.get_string(&word) {
            Ok(s) => s.into(),
            Err(_) => return JNI_FALSE,
        };
        let mode_str: String = match env.get_string(&mode) {
            Ok(s) => s.into(),
            Err(_) => "strict".to_string(),
        };

        let detector = get_detector(use_ml != 0);
        let conv_mode = parse_conv_mode_str(&mode_str);
        if detector.should_convert(&word_str, conv_mode) {
            JNI_TRUE
        } else {
            JNI_FALSE
        }
    }

    #[no_mangle]
    pub unsafe extern "system" fn Java_uz_yangialifbo_keyboard_YangiNativeBridge_isUzbek(
        mut env: JNIEnv,
        _class: JClass,
        word: JString,
        mode: JString,
        use_ml: jboolean,
    ) -> jboolean {
        let word_str: String = match env.get_string(&word) {
            Ok(s) => s.into(),
            Err(_) => return JNI_FALSE,
        };
        let mode_str: String = match env.get_string(&mode) {
            Ok(s) => s.into(),
            Err(_) => "strict".to_string(),
        };

        let detector = get_detector(use_ml != 0);
        let conv_mode = parse_conv_mode_str(&mode_str);
        if detector.is_uzbek(&word_str, conv_mode) {
            JNI_TRUE
        } else {
            JNI_FALSE
        }
    }

    #[no_mangle]
    pub unsafe extern "system" fn Java_uz_yangialifbo_keyboard_YangiNativeBridge_isForeignOrReject(
        mut env: JNIEnv,
        _class: JClass,
        word: JString,
    ) -> jboolean {
        let word_str: String = match env.get_string(&word) {
            Ok(s) => s.into(),
            Err(_) => return JNI_FALSE,
        };

        let detector = get_detector(false);
        if detector.is_foreign_or_reject(&word_str) {
            JNI_TRUE
        } else {
            JNI_FALSE
        }
    }

    #[no_mangle]
    pub unsafe extern "system" fn Java_uz_yangialifbo_keyboard_YangiNativeBridge_phoneticsConvert(
        mut env: JNIEnv,
        _class: JClass,
        word: JString,
    ) -> jstring {
        let word_str: String = match env.get_string(&word) {
            Ok(s) => s.into(),
            Err(_) => return std::ptr::null_mut(),
        };

        let result = yangi_core::phonetics::convert(&word_str);
        match env.new_string(result) {
            Ok(js) => js.into_raw(),
            Err(_) => std::ptr::null_mut(),
        }
    }

    #[no_mangle]
    pub unsafe extern "system" fn Java_uz_yangialifbo_keyboard_YangiNativeBridge_chatNormalize(
        mut env: JNIEnv,
        _class: JClass,
        word: JString,
    ) -> jstring {
        let word_str: String = match env.get_string(&word) {
            Ok(s) => s.into(),
            Err(_) => return std::ptr::null_mut(),
        };

        let result = yangi_core::normalize_chat(&word_str);
        match env.new_string(result) {
            Ok(js) => js.into_raw(),
            Err(_) => std::ptr::null_mut(),
        }
    }
}

