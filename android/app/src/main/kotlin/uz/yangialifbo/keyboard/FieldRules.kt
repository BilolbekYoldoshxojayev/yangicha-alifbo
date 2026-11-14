package uz.yangialifbo.keyboard

import android.text.InputType

object FieldRules {
    private val NO_CONVERSION = intArrayOf(
        InputType.TYPE_TEXT_VARIATION_PASSWORD,
        InputType.TYPE_TEXT_VARIATION_VISIBLE_PASSWORD,
        InputType.TYPE_TEXT_VARIATION_WEB_PASSWORD,
        InputType.TYPE_TEXT_VARIATION_EMAIL_ADDRESS,
        InputType.TYPE_TEXT_VARIATION_WEB_EMAIL_ADDRESS,
        InputType.TYPE_TEXT_VARIATION_URI,
    )

    /** A plain text field: not a number, password, e-mail address or URL field. */
    fun isPlainText(inputType: Int): Boolean {
        val cls = inputType and InputType.TYPE_MASK_CLASS
        val variation = inputType and InputType.TYPE_MASK_VARIATION
        return cls == InputType.TYPE_CLASS_TEXT && variation !in NO_CONVERSION
    }
}
