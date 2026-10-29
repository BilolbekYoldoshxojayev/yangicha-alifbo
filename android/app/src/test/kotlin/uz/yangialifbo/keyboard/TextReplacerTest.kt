package uz.yangialifbo.keyboard

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Test

class TextReplacerTest {
    private val convert: (String) -> String? = { w ->
        when (w) {
            "shahar" -> "şahar"
            "5-chi" -> "5-çi"
            else -> null
        }
    }

    private fun plan(snapshot: String, from: Int, added: Int, current: String = snapshot, sel: Int = current.length) =
        TextReplacer.plan(snapshot, from, added, current, sel, sel, convert)

    /** What the field holds after applying the plan. */
    private fun apply(current: String, p: TextReplacer.Plan) =
        current.replaceRange(p.start, p.end, p.replacement)

    @Test
    fun aSpaceAfterAWordReplacesTheWord() {
        val s = "bu shahar "
        val p = plan(s, from = 9, added = 1)!!
        assertEquals("bu şahar ", apply(s, p))
        assertEquals(9, p.selStart) // the word got one letter shorter: the cursor follows
    }

    @Test
    fun punctuationEndsAWordLikeASpace() {
        val p = plan("shahar.", from = 6, added = 1)!!
        assertEquals("şahar.", apply("shahar.", p))
    }

    @Test
    fun aLetterDoesNotEndAWord() {
        assertNull(plan("shahar", from = 5, added = 1))
    }

    @Test
    fun aHyphenInsideAWordDoesNotEndIt() {
        val s = "5-chi "
        assertEquals("5-çi ", apply(s, plan(s, from = 5, added = 1)!!))
        assertNull(plan("5-", from = 1, added = 1))
    }

    @Test
    fun aWordTheEngineKeepsIsLeftAlone() {
        assertNull(plan("show ", from = 4, added = 1))
    }

    @Test
    fun autocorrectReplacingTheWholeWordAndSpaceIsHandled() {
        val s = "shahar "
        assertEquals("şahar ", apply(s, plan(s, from = 0, added = 7)!!))
    }

    @Test
    fun aPasteIsNotTyping() {
        val s = "x".repeat(30) + " "
        assertNull(plan(s, from = 0, added = 31))
    }

    @Test
    fun textTypedAfterTheSpaceWhileTheEventWasLateIsKeptAndTheCursorMoves() {
        val snapshot = "shahar "
        val current = "shahar bu"
        val p = plan(snapshot, from = 6, added = 1, current = current, sel = 9)!!
        assertEquals("şahar bu", apply(current, p))
        assertEquals(8, p.selStart)
    }

    @Test
    fun aFieldThatChangedUnderTheWordIsLeftAlone() {
        assertNull(plan("shahar ", from = 6, added = 1, current = "shahor "))
    }

    @Test
    fun aCursorBeforeTheWordStaysWhereItIs() {
        val s = "bu shahar "
        val p = TextReplacer.plan(s, 9, 1, s, 0, 0, convert)
        assertNotNull(p)
        assertEquals(0, p!!.selStart)
    }

    @Test
    fun anUnknownSelectionStaysUnknown() {
        val s = "shahar "
        val p = TextReplacer.plan(s, 6, 1, s, -1, -1, convert)!!
        assertEquals(-1, p.selStart)
    }
}
