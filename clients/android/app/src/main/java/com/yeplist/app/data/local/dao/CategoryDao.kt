package com.yeplist.app.data.local.dao

import androidx.room.Dao
import androidx.room.Query
import androidx.room.Upsert
import com.yeplist.app.data.local.entity.CategoryEntity
import kotlinx.coroutines.flow.Flow

@Dao
interface CategoryDao {

    @Query("SELECT * FROM categories ORDER BY name")
    fun getAll(): Flow<List<CategoryEntity>>

    @Query("SELECT * FROM categories ORDER BY name")
    suspend fun getAllSync(): List<CategoryEntity>

    @Query("SELECT * FROM categories WHERE categoryId = :id")
    suspend fun getById(id: Long): CategoryEntity?

    @Query("SELECT MIN(categoryId) FROM categories")
    suspend fun getMinId(): Long?

    @Upsert
    suspend fun upsert(entity: CategoryEntity)

    @Upsert
    suspend fun upsertAll(entities: List<CategoryEntity>)

    @Query("DELETE FROM categories WHERE categoryId = :id")
    suspend fun deleteById(id: Long)

    @Query("DELETE FROM categories WHERE categoryId IN (:ids)")
    suspend fun deleteByIds(ids: List<Long>)
}
