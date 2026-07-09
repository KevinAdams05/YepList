package com.yeplist.app.di

import android.content.Context
import android.content.SharedPreferences
import com.google.gson.Gson
import com.yeplist.app.data.local.YepListDatabase
import com.yeplist.app.data.local.dao.CategoryDao
import com.yeplist.app.data.local.dao.PendingOperationDao
import com.yeplist.app.data.local.dao.SyncMetadataDao
import com.yeplist.app.data.local.dao.TodoItemDao
import com.yeplist.app.data.local.dao.TodoListDao
import com.yeplist.app.data.remote.NetworkModule
import com.yeplist.app.data.remote.YepListApiService
import com.yeplist.app.data.repository.CategoryRepository
import com.yeplist.app.data.repository.SyncRepository
import com.yeplist.app.data.repository.TodoItemRepository
import com.yeplist.app.data.repository.TodoListRepository
import com.yeplist.app.sync.ConnectivityMonitor
import com.yeplist.app.sync.IdRemapper
import com.yeplist.app.sync.PendingOperationProcessor
import com.yeplist.app.sync.SyncManager
import com.yeplist.app.sync.TempIdGenerator
import kotlinx.coroutines.runBlocking

class AppContainer(context: Context) {

    private val appContext: Context = context.applicationContext

    val prefs: SharedPreferences =
        context.getSharedPreferences("yeplist_prefs", Context.MODE_PRIVATE)

    val gson: Gson = Gson()

    // Database
    val database: YepListDatabase = YepListDatabase.getInstance(context)
    val todoListDao: TodoListDao = database.todoListDao()
    val todoItemDao: TodoItemDao = database.todoItemDao()
    val categoryDao: CategoryDao = database.categoryDao()
    val pendingOperationDao: PendingOperationDao = database.pendingOperationDao()
    val syncMetadataDao: SyncMetadataDao = database.syncMetadataDao()

    // Network
    val serverUrl: String
        get() {
            val saved = prefs.getString(PREF_SERVER_URL, null)
            return if (saved.isNullOrBlank()) DEFAULT_SERVER_URL else saved
        }

    // Stable per-install identifier sent to the server (X-Device-Id) so sync
    // activity and deletions can be attributed to this device.
    val deviceId: String by lazy {
        prefs.getString(PREF_DEVICE_ID, null)?.takeIf { it.isNotBlank() } ?: run {
            val id = java.util.UUID.randomUUID().toString().replace("-", "")
            prefs.edit().putString(PREF_DEVICE_ID, id).apply()
            id
        }
    }

    // Friendly name (X-Device-Name): the user's chosen device name if set
    // (e.g. "Taylor's Phone"), otherwise the manufacturer + model.
    val deviceName: String by lazy {
        val userName = android.provider.Settings.Global.getString(appContext.contentResolver, "device_name")
        if (!userName.isNullOrBlank()) userName else "${android.os.Build.MANUFACTURER} ${android.os.Build.MODEL}"
    }

    val apiService: YepListApiService by lazy {
        NetworkModule.createApiService(serverUrl, deviceId, deviceName)
    }

    // Connectivity
    val connectivityMonitor: ConnectivityMonitor = ConnectivityMonitor(context)

    // Repositories
    val todoListRepository: TodoListRepository by lazy {
        TodoListRepository(todoListDao, pendingOperationDao, gson)
    }
    val todoItemRepository: TodoItemRepository by lazy {
        TodoItemRepository(todoItemDao, pendingOperationDao, gson)
    }
    val categoryRepository: CategoryRepository by lazy {
        CategoryRepository(categoryDao, pendingOperationDao, gson)
    }
    val syncRepository: SyncRepository by lazy {
        SyncRepository(apiService, database, todoListDao, todoItemDao, categoryDao, pendingOperationDao, syncMetadataDao)
    }

    // Sync infrastructure
    val idRemapper: IdRemapper by lazy {
        IdRemapper(todoListDao, todoItemDao, categoryDao, pendingOperationDao)
    }
    val pendingOperationProcessor: PendingOperationProcessor by lazy {
        PendingOperationProcessor(apiService, pendingOperationDao, idRemapper, gson)
    }
    val syncManager: SyncManager by lazy {
        SyncManager(appContext, syncRepository, pendingOperationProcessor, connectivityMonitor)
    }

    init {
        connectivityMonitor.start()
        initTempIdGenerator()
    }

    private fun initTempIdGenerator() {
        runBlocking {
            val minListId = todoListDao.getMinId() ?: 0
            val minItemId = todoItemDao.getMinId() ?: 0
            val minCatId = categoryDao.getMinId() ?: 0
            val startBelow = minOf(minListId, minItemId, minCatId, 0)
            TempIdGenerator.resetTo(startBelow)
        }
    }

    companion object {
        const val DEFAULT_SERVER_URL = "http://192.168.74.122:5000"
        const val PREF_SERVER_URL = "server_url"
        const val PREF_DEVICE_ID = "device_id"
        const val PREF_DEFAULT_LIST_ID = "default_list_id"
        const val PREF_SYNC_INTERVAL = "sync_interval_seconds"
        const val DEFAULT_SYNC_INTERVAL = 30L
    }
}
