package com.yeplist.app.sync

import android.content.Context
import androidx.work.CoroutineWorker
import androidx.work.WorkerParameters
import com.yeplist.app.YepListApp

class SyncWorker(
    context: Context,
    params: WorkerParameters
) : CoroutineWorker(context, params) {

    override suspend fun doWork(): Result {
        val app = applicationContext as? YepListApp
            ?: return finishAndRequeue(Result.failure())
        val container = app.container

        return try {
            container.syncManager.sync()
            finishAndRequeue(Result.success())
        } catch (e: Exception) {
            // Re-enqueue even on failure so the chain doesn't break.
            finishAndRequeue(Result.retry())
        }
    }

    private fun finishAndRequeue(result: Result): Result {
        SyncScheduler.scheduleNextSync(applicationContext)
        return result
    }

    companion object {
        const val WORK_NAME = "yeplist_sync"
    }
}
