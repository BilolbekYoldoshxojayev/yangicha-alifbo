package uz.yangialifbo.keyboard

import android.view.inputmethod.InputConnection
import java.lang.reflect.Proxy
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Before
import org.junit.Test

/** A text field with a cursor and a composing region, behind the real [InputConnection] interface. */
private class FakeField {
    val text = StringBuilder()
    var cursor = 0
    var compStart = -1
    var compEnd = -1

    val keys = ArrayList<Int>()

    val connection: InputConnection = Proxy.newProxyInstance(
        InputConnection::class.java.classLoader,
        arrayOf(InputConnection::class.java),
    ) { _, method, args ->
        when (method.name) {
            "setComposingText" -> {
                place(args[0].toString(), keepComposing = true)
                true
            }
            "commitText" -> {
                place(args[0].toString(), keepComposing = false)
                true
            }
            "finishComposingText" -> {
                compStart = -1
                compEnd = -1
                true
            }
            "deleteSurroundingText" -> {
                val before = args[0] as Int
                val after = args[1] as Int
                text.delete(cursor - before, cursor + after)
                cursor -= before
                compStart = -1
                compEnd = -1
                true
            }
            "getTextBeforeCursor" -> text.substring(maxOf(0, cursor - (args[0] as Int)), cursor)
            "getSelectedText" -> null
            "beginBatchEdit", "endBatchEdit" -> true
            else -> throw UnsupportedOperationException(method.name)
        }
    } as InputConnection

    private fun place(s: String, keepComposing: Boolean) {
        val start = if (compStart >= 0) compStart else cursor
        val end = if (compStart >= 0) compEnd else cursor
        text.replace(start, end, s)
        cursor = start + s.length
        if (keepComposing) {
            compStart = start
            compEnd = cursor
        } else {
            compStart = -1
            compEnd = -1
        }
    }

    /** What the Backspace key does in a real editor. */
    fun backspaceKey() {
        keys.add(67)
        if (cursor > 0) {
            text.deleteCharAt(cursor - 1)
            cursor--
        }
        compStart = -1
        compEnd = -1
    }
}

class TypingControllerTest {
    private lateinit var field: FakeField
    private lateinit var typing: TypingController

    @Before
    fun setUp() {
        field = FakeField()
        typing = TypingController(
            connection = { field.connection },
            convert = { word -> if (word == "shahar") "şahar" else null },
            sendKey = { field.backspaceKey() },
            sendChar = { error("not used") },
        )
        typing.converting = true
    }

    private fun type(s: String) = s.forEach { typing.type(it) }

    @Test
    fun aFinishedWordIsCommittedConvertedWithItsDelimiter() {
        type("shahar ")
        assertEquals("şahar ", field.text.toString())
        assertEquals(-1, field.compStart)
    }

    @Test
    fun theWordBeingTypedIsComposingText() {
        type("sha")
        assertEquals("sha", field.text.toString())
        assertEquals(0, field.compStart)
        assertEquals(3, field.compEnd)
    }

    @Test
    fun punctuationEndsTheWordLikeASpace() {
        type("shahar.")
        assertEquals("şahar.", field.text.toString())
    }

    @Test
    fun aWordTheEngineKeepsStaysAsTyped() {
        type("show ")
        assertEquals("show ", field.text.toString())
    }

    @Test
    fun backspaceInsideAWordShortensItAndTheLastOneEmptiesIt() {
        type("sh")
        typing.delete()
        assertEquals("s", field.text.toString())
        typing.delete()
        assertEquals("", field.text.toString())
        assertTrue(field.keys.isEmpty())
    }

    @Test
    fun backspaceRightAfterAConversionRestoresTheWordAsTyped() {
        type("shahar ")
        typing.delete()
        assertEquals("shahar ", field.text.toString())
        assertTrue(field.keys.isEmpty())
    }

    @Test
    fun theNextBackspaceDeletesNormally() {
        type("shahar ")
        typing.delete()
        typing.delete()
        assertEquals("shahar", field.text.toString())
        assertEquals(1, field.keys.size)
    }

    @Test
    fun backspaceDoesNotRestoreOnceTheCursorMovedAway() {
        type("shahar ")
        field.cursor = 0
        typing.delete()
        assertEquals("şahar ", field.text.toString())
    }

    @Test
    fun typingAfterTheCursorMovedAwayStartsANewWordInsteadOfDuplicatingTheOldOne() {
        type("ab")
        field.cursor = 0 // the user tapped elsewhere and the app dropped the composing region
        field.compStart = -1
        field.compEnd = -1
        typing.selectionLooksMoved()
        typing.type('c')
        assertEquals("cab", field.text.toString())
        assertEquals("c", typing.word)
    }

    @Test
    fun aLateSelectionUpdateDoesNotSplitAWordWhoseCursorIsStillBehindIt() {
        type("ab")
        typing.selectionLooksMoved()
        typing.type('c')
        assertEquals("abc", field.text.toString())
        assertEquals("abc", typing.word)
    }

    @Test
    fun withConversionOffCharactersAreCommittedOneByOne() {
        typing.converting = false
        type("shahar ")
        assertEquals("shahar ", field.text.toString())
        assertEquals(-1, field.compStart)
    }

    @Test
    fun flushCommitsTheConvertedWordBeforeEnter() {
        type("shahar")
        typing.flush()
        assertEquals("şahar", field.text.toString())
        assertEquals("", typing.word)
    }

    @Test
    fun finishingAsTypedKeepsTheWordUnconverted() {
        type("shahar")
        typing.finishAsTyped()
        assertEquals("shahar", field.text.toString())
        assertEquals(-1, field.compStart)
    }
}
