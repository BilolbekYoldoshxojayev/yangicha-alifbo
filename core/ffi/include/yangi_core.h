#ifndef YANGI_CORE_H
#define YANGI_CORE_H

#include <stddef.h>
#include <stdint.h>
#include <stdbool.h>

#ifdef __cplusplus
extern "C" {
#endif

/**
 * Transliteration and linguistic discrimination mode.
 */
typedef enum {
    YANGI_MODE_STRICT = 0,
    YANGI_MODE_AGGRESSIVE = 1
} YangiConversionMode;

/**
 * Opaque handle to the YangiAlifbo Language Detector.
 */
typedef struct YangiDetector YangiDetector;

/**
 * Creates a YangiDetector from a binary dictionary buffer (yangi_data.bin).
 * The caller retains ownership of data_bytes, which must remain valid for the
 * lifetime of the detector, or this function copies internal references.
 */
YangiDetector* yangi_detector_create(const uint8_t* data_bytes, size_t data_len);

/**
 * Frees a previously created YangiDetector.
 */
void yangi_detector_free(YangiDetector* detector);

/**
 * Checks whether a word should be converted from 1995 Latin to 2024 Reformed Latin.
 */
bool yangi_should_convert(const YangiDetector* detector, const char* word_utf8, YangiConversionMode mode);

/**
 * Checks whether a word is recognized as Uzbek.
 */
bool yangi_is_uzbek(const YangiDetector* detector, const char* word_utf8, YangiConversionMode mode);

/**
 * Checks whether a word is in the English reject list or foreign list.
 */
bool yangi_is_foreign_or_reject(const YangiDetector* detector, const char* word_utf8);

/**
 * Transliterates phonetic sequences: sh->ş, ch->ç, o'->ö, g'->ğ with case preservation.
 * Returns a null-terminated UTF-8 string that must be freed with yangi_string_free().
 */
char* yangi_phonetics_convert(const char* word_utf8);

/**
 * Normalizes colloquial chat slang to standard orthography.
 * Returns a null-terminated UTF-8 string that must be freed with yangi_string_free().
 */
char* yangi_chat_normalize(const char* word_utf8);

/**
 * Converts a full text document preserving punctuation, whitespace, and non-Uzbek words.
 * Returns a null-terminated UTF-8 string that must be freed with yangi_string_free().
 * If out_changed is non-NULL, writes the number of changed words.
 */
char* yangi_convert_text(
    const YangiDetector* detector,
    const char* input_utf8,
    YangiConversionMode mode,
    bool slang,
    size_t* out_changed
);

/**
 * Frees a string returned by any yangi_* function.
 */
void yangi_string_free(char* s);

#ifdef __cplusplus
}
#endif

#endif /* YANGI_CORE_H */
