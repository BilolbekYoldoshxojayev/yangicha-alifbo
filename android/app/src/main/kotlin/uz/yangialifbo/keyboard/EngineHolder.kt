package uz.yangialifbo.keyboard

import android.util.Log
import uz.yangialifbo.engine.YangiEngine

/**
 * Loads the engine (about a million word forms) once per process on a background thread.
 * Until it is ready words pass through as typed, exactly as when it failed to load.
 */
object EngineHolder {
    private const val TAG = "YangiAlifbo"

    @Volatile private var engine: YangiEngine? = null
    @Volatile private var loading = false

    fun get(): YangiEngine? = engine

    @Synchronized
    fun start(settings: Settings) {
        if (engine != null || loading) return
        loading = true
        Thread(null, {
            try {
                val e = YangiEngine()
                e.setUserLists(Settings.parseList(settings.whitelistText), Settings.parseList(settings.blacklistText))
                engine = e
            } catch (t: Throwable) {
                Log.e(TAG, "engine failed to load, keys pass through", t)
            } finally {
                loading = false
            }
        }, "yangialifbo-engine", 0).start()
    }

    fun applyLists(settings: Settings) {
        engine?.setUserLists(Settings.parseList(settings.whitelistText), Settings.parseList(settings.blacklistText))
    }
}
