package com.yeplist.app.ui

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.yeplist.app.YepListApp
import com.yeplist.app.data.local.entity.CategoryEntity
import com.yeplist.app.data.local.entity.TodoListEntity
import com.yeplist.app.di.AppContainer
import com.yeplist.app.sync.SyncManager
import com.yeplist.app.sync.SyncScheduler
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch

class MainViewModel(application: Application) : AndroidViewModel(application) {

    private val container: AppContainer = (application as YepListApp).container

    val lists: StateFlow<List<TodoListEntity>> = container.todoListRepository.getAll()
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), emptyList())

    val categories: StateFlow<List<CategoryEntity>> = container.categoryRepository.getAll()
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), emptyList())

    val syncState: StateFlow<SyncManager.SyncState> = container.syncManager.syncState

    private val _selectedListId = MutableStateFlow<Long?>(null)
    val selectedListId: StateFlow<Long?> = _selectedListId.asStateFlow()

    private var syncJob: Job? = null

    init {
        // Load default list from preferences
        val defaultId = container.prefs.getLong(AppContainer.PREF_DEFAULT_LIST_ID, -1)
        if (defaultId > 0) {
            _selectedListId.value = defaultId
        }

        // Schedule background sync via WorkManager
        SyncScheduler.schedulePeriodicSync(application)

        // Start foreground 30-second sync loop
        startForegroundSync()

        // Trigger immediate sync on connectivity restore
        viewModelScope.launch {
            container.connectivityMonitor.isOnline.collect { online ->
                if (online) {
                    sync()
                }
            }
        }
    }

    fun selectList(listId: Long) {
        _selectedListId.value = listId
    }

    fun sync() {
        viewModelScope.launch {
            container.syncManager.sync()
        }
    }

    private fun startForegroundSync() {
        syncJob = viewModelScope.launch {
            while (true) {
                val intervalMs = container.prefs.getLong(
                    AppContainer.PREF_SYNC_INTERVAL,
                    AppContainer.DEFAULT_SYNC_INTERVAL
                ) * 1000
                delay(intervalMs)
                container.syncManager.sync()
            }
        }
    }

    fun restartForegroundSync() {
        syncJob?.cancel()
        startForegroundSync()
    }

    fun getDefaultListId(): Long {
        return container.prefs.getLong(AppContainer.PREF_DEFAULT_LIST_ID, -1)
    }

    fun setDefaultListId(listId: Long) {
        container.prefs.edit().putLong(AppContainer.PREF_DEFAULT_LIST_ID, listId).apply()
    }

    fun clearDefaultListId() {
        container.prefs.edit().remove(AppContainer.PREF_DEFAULT_LIST_ID).apply()
    }

    suspend fun createList(name: String): TodoListEntity {
        val entity = container.todoListRepository.create(name)
        pushIfOnline()
        return entity
    }

    suspend fun updateList(listId: Long, name: String, sortOrder: Int) {
        container.todoListRepository.update(listId, name, sortOrder)
        pushIfOnline()
    }

    suspend fun deleteList(listId: Long) {
        container.todoListRepository.delete(listId)
        if (_selectedListId.value == listId) {
            _selectedListId.value = null
        }
        pushIfOnline()
    }

    private fun pushIfOnline() {
        viewModelScope.launch {
            container.syncManager.pushOnly()
        }
    }
}
