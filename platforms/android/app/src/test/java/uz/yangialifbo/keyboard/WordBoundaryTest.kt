package uz.yangialifbo.keyboard

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class WordBoundaryTest {

    @Test
    fun testPhoneticRules() {
        assertEquals("şahar", YangiNativeBridge.applyPhonetics("shahar"))
        assertEquals("çiroq", YangiNativeBridge.applyPhonetics("chiroq"))
        assertEquals("öquvçi", YangiNativeBridge.applyPhonetics("o'quvchi"))
        assertEquals("ğalaba", YangiNativeBridge.applyPhonetics("g'alaba"))
        assertEquals("Şahar", YangiNativeBridge.applyPhonetics("Shahar"))
    }

    @Test
    fun testHardRule4_EnglishPreservation() {
        // English words must never be converted
        assertFalse(YangiNativeBridge.canConvert("school"))
        assertFalse(YangiNativeBridge.canConvert("chat"))
        assertFalse(YangiNativeBridge.canConvert("share"))
        assertFalse(YangiNativeBridge.canConvert("google"))
        assertFalse(YangiNativeBridge.canConvert("switch"))
    }

    @Test
    fun testTutuqSignProtection() {
        // Syllable break exceptions must be protected
        assertFalse(YangiNativeBridge.canConvert("Is'hoq"))
        assertEquals("Is'hoq", YangiNativeBridge.applyPhonetics("Is'hoq"))
    }

    @Test
    fun testWordExtraction() {
        val service = YangiKeyboardService()
        assertEquals("shahar", service.extractLastWord("Salom shahar "))
        assertEquals("chiroq", service.extractLastWord("Bu chiroq."))
        assertEquals("o'quvchi", service.extractLastWord("A'lochi o'quvchi, "))
    }
}
