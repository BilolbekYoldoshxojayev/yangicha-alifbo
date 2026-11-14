package uz.yangialifbo.engine

import java.io.BufferedOutputStream
import java.io.File
import java.io.FileOutputStream
import java.security.MessageDigest

/** Cross-platform parity run: main(<words.txt>, <out.tsv>) mirrors tools/parity/Program.cs. */
fun main(args: Array<String>) {
    val engine = YangiEngine()
    val text = String(File(args[0]).readBytes(), Charsets.UTF_8)
    val sha = MessageDigest.getInstance("SHA-256")
    var count = 0
    BufferedOutputStream(FileOutputStream(args[1]), 1 shl 16).use { out ->
        val sb = StringBuilder()
        var pos = 0
        while (pos <= text.length) {
            var end = text.indexOf('\n', pos)
            if (end < 0) end = text.length
            // tools/parity writes the list with the platform line ending (CRLF on Windows).
            val stop = if (end > pos && text[end - 1] == '\r') end - 1 else end
            if (stop > pos) {
                val w = text.substring(pos, stop)
                sb.setLength(0)
                sb.append(w).append('\t')
                    .append(engine.convert(w, Mode.STRICT, slang = true, neural = true) ?: "").append('\t')
                    .append(engine.convert(w, Mode.STRICT, slang = false, neural = true) ?: "").append('\t')
                    .append(engine.convert(w, Mode.AGGRESSIVE, slang = true, neural = true) ?: "").append('\n')
                val b = sb.toString().toByteArray(Charsets.UTF_8)
                out.write(b)
                sha.update(b)
                count++
            }
            pos = end + 1
        }
    }
    val hex = sha.digest().joinToString("") { "%02x".format(it) }
    println("$count words, sha256 $hex")
}
