package com.yeplist.app.data.repository

import com.google.gson.Gson
import com.yeplist.app.data.local.dao.PendingOperationDao
import com.yeplist.app.data.local.dao.TodoListDao
import com.yeplist.app.data.local.entity.PendingOperationEntity
import com.yeplist.app.data.local.entity.TodoListEntity
import com.yeplist.app.data.remote.dto.CreateTodoListRequest
import com.yeplist.app.sync.TempIdGenerator
import kotlinx.coroutines.flow.Flow
import java.time.Instant

class TodoListRepository(
    private val listDao: TodoListDao,
    private val pendingOpDao: PendingOperationDao,
    private val gson: Gson
) {

    fun getAll(): Flow<List<TodoListEntity>> = listDao.getAll()

    suspend fun getAllSync(): List<TodoListEntity> = listDao.getAllSync()

    suspend fun create(name: String, sortOrder: Int = 0): TodoListEntity {
        val tempId = TempIdGenerator.next()
        val now = Instant.now().toString()
        val entity = TodoListEntity(
            listId = tempId,
            name = name,
            sortOrder = sortOrder,
            createdDate = now,
            modifiedDate = now
        )
        listDao.upsert(entity)

        val payload = gson.toJson(CreateTodoListRequest(name, sortOrder, clientModifiedDate = now))
        pendingOpDao.insert(
            PendingOperationEntity(
                entityType = "list",
                operationType = "create",
                entityId = tempId,
                payload = payload,
                createdDate = now
            )
        )

        return entity
    }

    suspend fun update(listId: Long, name: String, sortOrder: Int) {
        val existing = listDao.getById(listId) ?: return
        val now = Instant.now().toString()
        val updated = existing.copy(
            name = name,
            sortOrder = sortOrder,
            modifiedDate = now
        )
        listDao.upsert(updated)

        val payload = gson.toJson(CreateTodoListRequest(name, sortOrder, clientModifiedDate = now))
        if (listId < 0) {
            // Not yet synced — update the pending create's payload
            val ops = pendingOpDao.getByEntity("list", listId)
            val createOp = ops.firstOrNull { it.operationType == "create" }
            if (createOp != null) {
                pendingOpDao.deleteById(createOp.id)
                pendingOpDao.insert(createOp.copy(id = 0, payload = payload))
            }
        } else {
            pendingOpDao.insert(
                PendingOperationEntity(
                    entityType = "list",
                    operationType = "update",
                    entityId = listId,
                    payload = payload,
                    createdDate = now
                )
            )
        }
    }

    suspend fun delete(listId: Long) {
        listDao.deleteById(listId)
        if (listId < 0) {
            pendingOpDao.deleteByEntity("list", listId)
        } else {
            pendingOpDao.insert(
                PendingOperationEntity(
                    entityType = "list",
                    operationType = "delete",
                    entityId = listId,
                    createdDate = Instant.now().toString()
                )
            )
        }
    }
}
