package uz.yangialifbo.keyboard

import android.text.InputType
import android.view.inputmethod.EditorInfo
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class SecurityFilterTest {

    private val service = YangiKeyboardService()

    private fun createInfo(inputType: Int): EditorInfo {
        return EditorInfo().apply { this.inputType = inputType }
    }

    @Test
    fun testPasswordFieldsMustBeIgnored() {
        val pwInfo = createInfo(InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_VARIATION_PASSWORD)
        assertTrue(service.checkIsSecureField(pwInfo))

        val visiblePwInfo = createInfo(InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_VARIATION_VISIBLE_PASSWORD)
        assertTrue(service.checkIsSecureField(visiblePwInfo))

        val webPwInfo = createInfo(InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_VARIATION_WEB_PASSWORD)
        assertTrue(service.checkIsSecureField(webPwInfo))
    }

    @Test
    fun testSensitiveInputsMustBeIgnored() {
        val emailInfo = createInfo(InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_VARIATION_EMAIL_ADDRESS)
        assertTrue(service.checkIsSecureField(emailInfo))

        val uriInfo = createInfo(InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_VARIATION_URI)
        assertTrue(service.checkIsSecureField(uriInfo))

        val numberInfo = createInfo(InputType.TYPE_CLASS_NUMBER)
        assertTrue(service.checkIsSecureField(numberInfo))

        val phoneInfo = createInfo(InputType.TYPE_CLASS_PHONE)
        assertTrue(service.checkIsSecureField(phoneInfo))
    }

    @Test
    fun testStandardTextInputIsAllowed() {
        val normalText = createInfo(InputType.TYPE_CLASS_TEXT)
        assertFalse(service.checkIsSecureField(normalText))

        val multiLineText = createInfo(InputType.TYPE_CLASS_TEXT or InputType.TYPE_TEXT_FLAG_MULTI_LINE)
        assertFalse(service.checkIsSecureField(multiLineText))
    }

    @Test
    fun testNullEditorInfoHandledSafely() {
        assertFalse(service.checkIsSecureField(null))
    }
}
