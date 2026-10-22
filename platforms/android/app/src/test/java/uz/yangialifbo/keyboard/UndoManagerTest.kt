package uz.yangialifbo.keyboard

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertNull
import org.junit.Test

class UndoManagerTest {

    @Test
    fun testUndoAfterConversion() {
        val undoManager = UndoManager()
        undoManager.record("shahar", "şahar", " ")

        val record = undoManager.checkUndo("Salom şahar ")
        assertNotNull(record)
        assertEquals("shahar", record!!.originalWord)
        assertEquals("şahar", record.convertedWord)
        assertEquals(" ", record.delimiter)

        // Second check should be null because undo is consumed
        assertNull(undoManager.checkUndo("Salom şahar "))
    }

    @Test
    fun testUndoNotTriggeredOnUnrelatedText() {
        val undoManager = UndoManager()
        undoManager.record("shahar", "şahar", " ")

        // User typed something else after the conversion
        val record = undoManager.checkUndo("Salom şahar yana bir so'z")
        assertNull(record)
    }

    @Test
    fun testExplicitClear() {
        val undoManager = UndoManager()
        undoManager.record("shahar", "şahar", " ")
        undoManager.clear()

        assertNull(undoManager.checkUndo("Salom şahar "))
    }
}
