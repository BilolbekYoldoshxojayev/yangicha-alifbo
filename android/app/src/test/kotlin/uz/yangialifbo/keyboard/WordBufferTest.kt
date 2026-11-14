package uz.yangialifbo.keyboard

import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Test

class WordBufferTest {
    private fun buffer() = WordBuffer { if (it == "shahar") "şahar" else null }

    private fun WordBuffer.type(s: String) = s.forEach { add(it) }

    @Test
    fun hyphenBelongsToAWordOnlyInsideIt() {
        val b = buffer()
        assertFalse(b.accepts('-'))
        b.add('5')
        assertTrue(b.accepts('-'))
        assertTrue(b.accepts('c'))
    }

    @Test
    fun allApostropheShapesBelongToAWord() {
        val b = buffer()
        for (ch in "'‘’ʻʼ`´‛′") assertTrue("U+%04X".format(ch.code), b.accepts(ch))
        assertFalse(b.accepts(' '))
        assertFalse(b.accepts('.'))
    }

    @Test
    fun takeGivesTypedAndConvertedAndEmptiesTheBuffer() {
        val b = buffer()
        b.type("shahar")
        val t = b.take()
        assertEquals("shahar", t.original)
        assertEquals("şahar", t.converted)
        assertTrue(b.isEmpty)
    }

    @Test
    fun aWordTheEngineKeepsIsReturnedAsTyped() {
        val b = buffer()
        b.type("show")
        assertEquals("show", b.take().converted)
    }

    @Test
    fun backspaceShortensTheWordAndReportsEmpty() {
        val b = buffer()
        b.type("ab")
        assertTrue(b.backspace())
        assertEquals("a", b.word)
        assertTrue(b.backspace())
        assertFalse(b.backspace())
    }
}
