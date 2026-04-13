package com.yeplist.app.data.local.dao

import androidx.room.Dao
import androidx.room.Query
import androidx.room.Upsert
import com.yeplist.app.data.local.entity.TodoListEntity
import kotlinx.coroutines.flow.Flow

@Dao
interface TodoListDao {

    @Query("SELECT * FROM todo_lists ORDER BY sortOrder, name")
    fun getAll(): Flow<List<TodoListEntity>>

    @Query("SELECT * FROM todo_lists ORDER BY sortOrder, name")
    suspend fun getAllSync(): List<TodoListEntity>

    @Query("SELECT * FROM todo_lists WHERE listId = :id")
    suspend fun getById(id: Long): TodoListEntity?

    @Query("SELECT MIN(listId) FROM todo_lists")
    suspend fun getMinId(): Long?

    @Upsert
    suspend fun upsert(entity: TodoListEntity)

    @Upsert
    suspend fun upsertAll(entities: List<TodoListEntity>)

    @Query("DELETE FROM todo_lists WHERE listId = :id")
    suspend fun deleteById(id: Long)

    @Query("DELETE FROM todo_lists WHERE listId IN (:ids)")
    suspend fun deleteByIds(ids: List<Long>)
}
