package com.yeplist.app.sync

import com.yeplist.app.data.repository.SyncRepository
import com.yeplist.app.debug.RemoteLogger
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock

class SyncManager(
    private val syncRepository: SyncRepository,
    private val pendingProcessor: PendingOperationProcessor,
    private val connectivityMonitor: ConnectivityMonitor
) {

    private val mutex = Mutex()

    private val _syncState = MutableStateFlow(SyncState.IDLE)
    val syncState: StateFlow<SyncState> = _syncState.asStateFlow()

    /**
     * Full sync cycle: push pending operations then pull from server.
     * Mutex-protected so only one sync runs at a time.
     */
    suspend fun sync() {
        if (!connectivityMonitor.isOnline.value) {
            _syncState.value = SyncState.OFFLINE
            return
        }

        mutex.withLock {
            try {
                _syncState.value = SyncState.SYNCING
                RemoteLogger.d(TAG, "sync: starting push+pull cycle")

                // Push phase: send pending local changes to server
                pendingProcessor.processAll()

                // Pull phase: fetch server changes into Room
                syncRepository.pullFromServer()

                _syncState.value = SyncState.SYNCED
                RemoteLogger.d(TAG, "sync: complete")
            } catch (e: Exception) {
                RemoteLogger.e(TAG, "Sync failed", e)
                _syncState.value = SyncState.ERROR
            }
        }
    }

    /**
     * Push-only: process pending operations without pulling.
     * Used when a local write happens while online.
     */
    suspend fun pushOnly() {
        if (!connectivityMonitor.isOnline.value) return

        mutex.withLock {
            try {
                pendingProcessor.processAll()
            } catch (e: Exception) {
                RemoteLogger.e(TAG, "Push failed", e)
            }
        }
    }

    enum class SyncState {
        IDLE, SYNCING, SYNCED, OFFLINE, ERROR
    }

    companion object {
        private const val TAG = "SyncManager"
    }
}
