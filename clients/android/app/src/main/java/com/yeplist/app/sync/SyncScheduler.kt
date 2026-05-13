package com.yeplist.app.sync

import android.content.Context
import androidx.work.Constraints
import androidx.work.ExistingWorkPolicy
import androidx.work.NetworkType
import androidx.work.OneTimeWorkRequestBuilder
import androidx.work.WorkManager
import java.util.concurrent.TimeUnit

object SyncScheduler {

    // Re-enqueue interval. Android's PeriodicWorkRequest enforces a 15-minute
    // minimum, so we use a self-rescheduling OneTimeWorkRequest chain instead
    // to refresh the widget more often. Doze mode may still delay execution
    // when the device is idle.
    private const val INTERVAL_MINUTES = 5L

    /**
     * Enqueue (or re-enqueue) the next background sync. Called once at app
     * startup to bootstrap the chain, and again from SyncWorker.doWork() so
     * each completed sync schedules the next one.
     */
    fun scheduleNextSync(context: Context) {
        val constraints = Constraints.Builder()
            .setRequiredNetworkType(NetworkType.CONNECTED)
            .build()

        val request = OneTimeWorkRequestBuilder<SyncWorker>()
            .setConstraints(constraints)
            .setInitialDelay(INTERVAL_MINUTES, TimeUnit.MINUTES)
            .build()

        WorkManager.getInstance(context).enqueueUniqueWork(
            SyncWorker.WORK_NAME,
            ExistingWorkPolicy.REPLACE,
            request
        )
    }

    fun cancelPeriodicSync(context: Context) {
        WorkManager.getInstance(context).cancelUniqueWork(SyncWorker.WORK_NAME)
    }

    /**
     * @deprecated Kept for callers; delegates to scheduleNextSync().
     */
    fun schedulePeriodicSync(context: Context) {
        scheduleNextSync(context)
    }
}
