package uz.yangialifbo.engine

import java.io.File
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertNull
import kotlin.test.assertTrue

class EngineTest {
    companion object {
        val engine = YangiEngine()

        /** tests/data lives at the repository root; walk up from the working directory. */
        fun repoFile(rel: String): File {
            var dir: File? = File("").absoluteFile
            while (dir != null) {
                val f = File(dir, rel)
                if (f.exists()) return f
                dir = dir.parentFile
            }
            throw java.io.FileNotFoundException(rel)
        }
    }

    @Test
    fun readmeTable() {
        assertEquals("şahar", engine.convert("shahar"))
        assertEquals("çoyxona", engine.convert("choyxona"))
        assertEquals("örtoq", engine.convert("o'rtoq"))
        assertEquals("örtoq", engine.convert("o‘rtoq"))
        assertEquals("örtoq", engine.convert("oʻrtoq"))
        assertEquals("ğalaba", engine.convert("g'alaba"))
        assertEquals("ğalaba", engine.convert("g‘alaba"))
    }

    @Test
    fun foreignAndBoundaryWordsStay() {
        for (w in listOf("show", "push", "switch", "catch", "as'hob", "Is'hoq", "mus'haf", "kitob")) {
            assertNull(engine.convert(w), w)
        }
    }

    @Test
    fun casePreservedAndAggressive() {
        assertEquals("Şahar", engine.convert("Shahar"))
        assertEquals("ŞAHAR", engine.convert("SHAHAR"))
        assertEquals("şaharzzz", engine.convert("shaharzzz", Mode.AGGRESSIVE))
        assertNull(engine.convert("shaharzzz", Mode.STRICT))
        assertNull(engine.convert("show", Mode.AGGRESSIVE))   // reject list wins in every mode
    }

    @Test
    fun slangRewrite() {
        assertEquals("yaxşi", engine.convert("yaxwi"))
    }

    @Test
    fun userLists() {
        val e = YangiEngine()
        assertNull(e.convert("shaharzzz"))
        e.setUserLists(setOf("Shaharzzz"), setOf("shahar"))
        assertEquals("şaharzzz", e.convert("shaharzzz"))
        assertNull(e.convert("shahar"))
        e.setUserLists(emptySet(), emptySet())
        assertEquals("şahar", e.convert("shahar"))
    }

    @Test
    fun neuralVectorsMatchPyTorch() {
        val model = NeuralFallback.loadEmbedded()
        assertTrue(model.threshold > 0.5 && model.threshold < 1.0, "threshold ${model.threshold}")
        var n = 0
        var worst = 0.0
        for (line in repoFile("tests/data/neural_vectors.tsv").readLines()) {
            if (line.isEmpty()) continue
            val p = line.split('\t')
            worst = Math.max(worst, Math.abs(model.score(p[0]) - p[1].toDouble()))
            n++
        }
        assertTrue(n >= 1000, "only $n vectors")
        assertTrue(worst < 1e-4, "largest difference $worst")
    }

    @Test
    fun neuralEligibility() {
        assertTrue(NeuralFallback.isEligible("shahar"))
        assertTrue(NeuralFallback.isEligible("o'z-o'zidan"))
        for (w in listOf("osh", "swish", "chocolate", "push_button", "chat2", "'shahar", "kitob", "is'hoq"))
            assertTrue(!NeuralFallback.isEligible(w), w)
    }

    @Test
    fun neuralFallbackAcceptsUnknownUzbekWord() {
        assertEquals("şörvaxonalarimizdagilar", engine.convert("sho'rvaxonalarimizdagilar"))
        assertNull(engine.convert("sho'rvaxonalarimizdagilar", neural = false))
    }
}
