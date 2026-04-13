package com.yeplist.app.sync

import com.yeplist.app.data.local.dao.CategoryDao
import com.yeplist.app.data.local.dao.PendingOperationDao
import com.yeplist.app.data.local.dao.TodoItemDao
import com.yeplist.app.data.local.dao.TodoListDao

class IdRemapper(
    private val listDao: TodoListDao,
    private val itemDao: TodoItemDao,
    private val categoryDao: CategoryDao,
    private val pendingOpDao: PendingOperationDao
) {

    suspend fun remapListId(tempId: Long, serverId: Long) {
        val entity = listDao.getById(tempId) ?: return
        listDao.deleteById(tempId)
        listDao.upsert(entity.copy(listId = serverId))

        // Update FK references in items
        itemDao.updateListId(tempId, serverId)

        // Update pending operations referencing this list
        pendingOpDao.updateEntityId(tempId, serverId, "list")
        pendingOpDao.updateParentId(tempId, serverId)
    }

    suspend fun remapItemId(tempId: Long, serverId: Long) {
        val entity = itemDao.getById(tempId) ?: return
        itemDao.deleteById(tempId)
        itemDao.upsert(entity.copy(itemId = serverId))

        // Update pending operations referencing this item
        pendingOpDao.updateEntityId(tempId, serverId, "item")
    }

    suspend fun remapCategoryId(tempId: Long, serverId: Long) {
        val entity = categoryDao.getById(tempId) ?: return
        categoryDao.deleteById(tempId)
        categoryDao.upsert(entity.copy(categoryId = serverId))

        // Update FK references in items
        itemDao.updateCategoryId(tempId, serverId)

        // Update pending operations referencing this category
        pendingOpDao.updateEntityId(tempId, serverId, "category")
    }
}
