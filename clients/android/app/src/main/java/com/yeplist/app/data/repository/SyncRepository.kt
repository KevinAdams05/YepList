package com.yeplist.app.data.repository

import androidx.room.withTransaction
import com.yeplist.app.data.local.YepListDatabase
import com.yeplist.app.data.local.dao.CategoryDao
import com.yeplist.app.data.local.dao.PendingOperationDao
import com.yeplist.app.data.local.dao.SyncMetadataDao
import com.yeplist.app.data.local.dao.TodoItemDao
import com.yeplist.app.data.local.dao.TodoListDao
import com.yeplist.app.data.local.entity.SyncMetadataEntity
import com.yeplist.app.data.remote.YepListApiService
import com.yeplist.app.debug.RemoteLogger

class SyncRepository(
    private val apiService: YepListApiService,
    private val database: YepListDatabase,
    private val listDao: TodoListDao,
    private val itemDao: TodoItemDao,
    private val categoryDao: CategoryDao,
    private val pendingOpDao: PendingOperationDao,
    private val syncMetadataDao: SyncMetadataDao
) {

    suspend fun pullFromServer() {
        val metadata = syncMetadataDao.get()
        val since = metadata?.lastSyncTime?.takeIf { it.isNotEmpty() }

        RemoteLogger.d(TAG, "pullFromServer: since=$since")
        val response = apiService.sync(since)
        RemoteLogger.d(TAG, "pullFromServer: received ${response.lists.size} lists, " +
                "${response.items.size} items, ${response.categories.size} categories")

        for (list in response.lists) {
            RemoteLogger.d(TAG, "  list: id=${list.listId}, name='${list.name}'")
        }
        for (item in response.items) {
            RemoteLogger.d(TAG, "  item: id=${item.itemId}, listId=${item.listId}, " +
                    "title='${item.title}', completed=${item.isCompleted}")
        }

        database.withTransaction {
            // Upsert lists
            for (dto in response.lists) {
                listDao.upsert(dto.toEntity())
            }
            // Upsert categories
            for (dto in response.categories) {
                categoryDao.upsert(dto.toEntity())
            }
            // Upsert items
            for (dto in response.items) {
                itemDao.upsert(dto.toEntity())
            }
            // Process deletions
            if (response.deletedListIds.isNotEmpty()) {
                listDao.deleteByIds(response.deletedListIds)
                RemoteLogger.d(TAG, "  deleted lists: ${response.deletedListIds}")
            }
            if (response.deletedItemIds.isNotEmpty()) {
                itemDao.deleteByIds(response.deletedItemIds)
                RemoteLogger.d(TAG, "  deleted items: ${response.deletedItemIds}")
            }
            if (response.deletedCategoryIds.isNotEmpty()) {
                categoryDao.deleteByIds(response.deletedCategoryIds)
                RemoteLogger.d(TAG, "  deleted categories: ${response.deletedCategoryIds}")
            }
            // Update sync time
            syncMetadataDao.upsert(SyncMetadataEntity(id = 1, lastSyncTime = response.serverTime))
        }
        RemoteLogger.d(TAG, "pullFromServer: complete, serverTime=${response.serverTime}")
    }

    suspend fun resetSyncTime() {
        syncMetadataDao.upsert(SyncMetadataEntity(id = 1, lastSyncTime = ""))
    }

    companion object {
        private const val TAG = "SyncRepo"
    }
}
