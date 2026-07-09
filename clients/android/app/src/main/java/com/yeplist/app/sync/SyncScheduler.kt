package com.yeplist.app.sync

import android.content.Context
import androidx.work.Constraints
import androidx.work.ExistingPeriodicWorkPolicy
import androidx.work.ExistingWorkPolicy
import androidx.work.NetworkType
import androidx.work.OneTimeWorkRequestBuilder
import androidx.work.PeriodicWorkRequestBuilder
import androidx.work.WorkManager
import java.util.concurrent.TimeUnit

object SyncScheduler {

    // Fast cadence while the app is alive. Android's PeriodicWorkRequest
    // enforces a 15-minute minimum, so we use a self-rescheduling
    // OneTimeWorkRequest chain to refresh more often. The chain can be broken
    // by the OS (force-stop, "Sleeping apps", restricted standby bucket), which
    // is why a durable periodic backstop also runs — see schedulePeriodicBackstop.
    private const val INTERVAL_MINUTES = 5L

    // Durable safety net. A PeriodicWorkRequest is persisted by WorkManager and
    // re-scheduled by the system across process death and reboots, so even when
    // the OneTimeWork chain is killed the widget/app still refresh on this floor.
    private const val BACKSTOP_MINUTES = 15L

    private fun connectedConstraints(): Constraints =
        Constraints.Builder()
            .setRequiredNetworkType(NetworkType.CONNECTED)
            .build()

    /**
     * Enqueue (or re-enqueue) the next chained background sync. Called once at
     * app startup to bootstrap the chain, and again from SyncWorker.doWork() so
     * each completed sync schedules the next one.
     */
    fun scheduleNextSync(context: Context) {
        val request = OneTimeWorkRequestBuilder<SyncWorker>()
            .setConstraints(connectedConstraints())
            .setInitialDelay(INTERVAL_MINUTES, TimeUnit.MINUTES)
            .build()

        WorkManager.getInstance(context).enqueueUniqueWork(
            SyncWorker.WORK_NAME,
            ExistingWorkPolicy.REPLACE,
            request
        )
    }

    /**
     * Enqueue the durable periodic backstop. KEEP means we never disturb an
     * already-scheduled instance, so calling this on every startup is safe.
     */
    fun schedulePeriodicBackstop(context: Context) {
        val request = PeriodicWorkRequestBuilder<SyncWorker>(BACKSTOP_MINUTES, TimeUnit.MINUTES)
            .setConstraints(connectedConstraints())
            .build()

        WorkManager.getInstance(context).enqueueUniquePeriodicWork(
            PERIODIC_WORK_NAME,
            ExistingPeriodicWorkPolicy.KEEP,
            request
        )
    }

    fun cancelPeriodicSync(context: Context) {
        WorkManager.getInstance(context).cancelUniqueWork(SyncWorker.WORK_NAME)
        WorkManager.getInstance(context).cancelUniqueWork(PERIODIC_WORK_NAME)
    }

    /**
     * Bootstrap both schedulers. Safe to call repeatedly.
     */
    fun schedulePeriodicSync(context: Context) {
        scheduleNextSync(context)
        schedulePeriodicBackstop(context)
    }

    private const val PERIODIC_WORK_NAME = "yeplist_sync_periodic"
}
