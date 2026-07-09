package com.yeplist.app.data.repository

import com.google.gson.Gson
import com.yeplist.app.data.local.dao.CategoryDao
import com.yeplist.app.data.local.dao.PendingOperationDao
import com.yeplist.app.data.local.entity.CategoryEntity
import com.yeplist.app.data.local.entity.PendingOperationEntity
import com.yeplist.app.data.remote.dto.CreateCategoryRequest
import com.yeplist.app.sync.TempIdGenerator
import kotlinx.coroutines.flow.Flow
import java.time.Instant

class CategoryRepository(
    private val categoryDao: CategoryDao,
    private val pendingOpDao: PendingOperationDao,
    private val gson: Gson
) {

    fun getAll(): Flow<List<CategoryEntity>> = categoryDao.getAll()

    suspend fun getAllSync(): List<CategoryEntity> = categoryDao.getAllSync()

    suspend fun create(name: String, color: String? = null): CategoryEntity {
        val tempId = TempIdGenerator.next()
        val now = Instant.now().toString()
        val entity = CategoryEntity(
            categoryId = tempId,
            name = name,
            color = color,
            createdDate = now,
            modifiedDate = now
        )
        categoryDao.upsert(entity)

        val payload = gson.toJson(CreateCategoryRequest(name, color, clientModifiedDate = now))
        pendingOpDao.insert(
            PendingOperationEntity(
                entityType = "category",
                operationType = "create",
                entityId = tempId,
                payload = payload,
                createdDate = now
            )
        )

        return entity
    }

    suspend fun update(categoryId: Long, name: String, color: String?) {
        val existing = categoryDao.getById(categoryId) ?: return
        val now = Instant.now().toString()
        val updated = existing.copy(
            name = name,
            color = color,
            modifiedDate = now
        )
        categoryDao.upsert(updated)

        val payload = gson.toJson(CreateCategoryRequest(name, color, clientModifiedDate = now))
        if (categoryId < 0) {
            val ops = pendingOpDao.getByEntity("category", categoryId)
            val createOp = ops.firstOrNull { it.operationType == "create" }
            if (createOp != null) {
                pendingOpDao.deleteById(createOp.id)
                pendingOpDao.insert(createOp.copy(id = 0, payload = payload))
            }
        } else {
            pendingOpDao.insert(
                PendingOperationEntity(
                    entityType = "category",
                    operationType = "update",
                    entityId = categoryId,
                    payload = payload,
                    createdDate = now
                )
            )
        }
    }

    suspend fun delete(categoryId: Long) {
        categoryDao.deleteById(categoryId)
        if (categoryId < 0) {
            pendingOpDao.deleteByEntity("category", categoryId)
        } else {
            pendingOpDao.insert(
                PendingOperationEntity(
                    entityType = "category",
                    operationType = "delete",
                    entityId = categoryId,
                    createdDate = Instant.now().toString()
                )
            )
        }
    }
}
