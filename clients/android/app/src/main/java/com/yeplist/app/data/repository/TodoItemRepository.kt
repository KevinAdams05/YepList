package com.yeplist.app.data.repository

import com.google.gson.Gson
import com.yeplist.app.data.local.dao.PendingOperationDao
import com.yeplist.app.data.local.dao.TodoItemDao
import com.yeplist.app.data.local.entity.PendingOperationEntity
import com.yeplist.app.data.local.entity.TodoItemEntity
import com.yeplist.app.data.remote.dto.CreateTodoItemRequest
import com.yeplist.app.data.remote.dto.ReorderEntry
import com.yeplist.app.data.remote.dto.ReorderItemsRequest
import com.yeplist.app.data.remote.dto.ToggleCompleteRequest
import com.yeplist.app.data.remote.dto.UpdateTodoItemRequest
import com.yeplist.app.sync.TempIdGenerator
import kotlinx.coroutines.flow.Flow
import java.time.Instant

class TodoItemRepository(
    private val itemDao: TodoItemDao,
    private val pendingOpDao: PendingOperationDao,
    private val gson: Gson
) {

    fun getByListId(listId: Long): Flow<List<TodoItemEntity>> = itemDao.getByListId(listId)

    suspend fun create(
        listId: Long,
        title: String,
        notes: String? = null,
        categoryId: Long? = null,
        dueDate: String? = null,
        sortOrder: Int = 0
    ): TodoItemEntity {
        val tempId = TempIdGenerator.next()
        val now = Instant.now().toString()
        val entity = TodoItemEntity(
            itemId = tempId,
            listId = listId,
            categoryId = categoryId,
            title = title,
            notes = notes,
            isCompleted = false,
            dueDate = dueDate,
            sortOrder = sortOrder,
            createdDate = now,
            modifiedDate = now
        )
        itemDao.upsert(entity)

        val payload = gson.toJson(
            CreateTodoItemRequest(title, notes, categoryId, dueDate, sortOrder)
        )
        pendingOpDao.insert(
            PendingOperationEntity(
                entityType = "item",
                operationType = "create",
                entityId = tempId,
                parentId = listId,
                payload = payload,
                createdDate = now
            )
        )

        return entity
    }

    suspend fun update(
        itemId: Long,
        title: String,
        notes: String?,
        categoryId: Long?,
        isCompleted: Boolean,
        dueDate: String?,
        sortOrder: Int
    ) {
        val existing = itemDao.getById(itemId) ?: return
        val updated = existing.copy(
            title = title,
            notes = notes,
            categoryId = categoryId,
            isCompleted = isCompleted,
            dueDate = dueDate,
            sortOrder = sortOrder,
            modifiedDate = Instant.now().toString()
        )
        itemDao.upsert(updated)

        val payload = gson.toJson(
            UpdateTodoItemRequest(title, notes, categoryId, isCompleted, dueDate, sortOrder)
        )
        if (itemId < 0) {
            val ops = pendingOpDao.getByEntity("item", itemId)
            val createOp = ops.firstOrNull { it.operationType == "create" }
            if (createOp != null) {
                val createPayload = gson.toJson(
                    CreateTodoItemRequest(title, notes, categoryId, dueDate, sortOrder)
                )
                pendingOpDao.deleteById(createOp.id)
                pendingOpDao.insert(createOp.copy(id = 0, payload = createPayload))
            }
        } else {
            pendingOpDao.insert(
                PendingOperationEntity(
                    entityType = "item",
                    operationType = "update",
                    entityId = itemId,
                    payload = payload,
                    createdDate = Instant.now().toString()
                )
            )
        }
    }

    suspend fun toggleComplete(item: TodoItemEntity) {
        val updated = item.copy(
            isCompleted = !item.isCompleted,
            modifiedDate = Instant.now().toString()
        )
        itemDao.upsert(updated)

        if (item.itemId < 0) {
            val ops = pendingOpDao.getByEntity("item", item.itemId)
            val createOp = ops.firstOrNull { it.operationType == "create" }
            if (createOp != null) {
                val request = gson.fromJson(createOp.payload, CreateTodoItemRequest::class.java)
                val newPayload = gson.toJson(
                    CreateTodoItemRequest(
                        request.title, request.notes, request.categoryId,
                        request.dueDate, request.sortOrder
                    )
                )
                pendingOpDao.deleteById(createOp.id)
                pendingOpDao.insert(createOp.copy(id = 0, payload = newPayload))
            }
        } else {
            val payload = gson.toJson(ToggleCompleteRequest(updated.isCompleted))
            pendingOpDao.insert(
                PendingOperationEntity(
                    entityType = "item",
                    operationType = "toggle_complete",
                    entityId = item.itemId,
                    payload = payload,
                    createdDate = Instant.now().toString()
                )
            )
        }
    }

    suspend fun deleteItems(itemIds: List<Long>) {
        for (id in itemIds) {
            itemDao.deleteById(id)
            if (id < 0) {
                pendingOpDao.deleteByEntity("item", id)
            } else {
                pendingOpDao.insert(
                    PendingOperationEntity(
                        entityType = "item",
                        operationType = "delete",
                        entityId = id,
                        createdDate = Instant.now().toString()
                    )
                )
            }
        }
    }

    suspend fun reorder(listId: Long, items: List<TodoItemEntity>) {
        for ((index, item) in items.withIndex()) {
            itemDao.upsert(item.copy(sortOrder = index))
        }

        val entries = items.mapIndexed { index, item ->
            ReorderEntry(item.itemId, index)
        }
        val payload = gson.toJson(ReorderItemsRequest(entries))
        pendingOpDao.insert(
            PendingOperationEntity(
                entityType = "item",
                operationType = "reorder",
                entityId = 0,
                parentId = listId,
                payload = payload,
                createdDate = Instant.now().toString()
            )
        )
    }

    suspend fun getItemsForWidget(listId: Long): List<TodoItemEntity> {
        return itemDao.getItemsForWidget(listId, 50)
    }
}
