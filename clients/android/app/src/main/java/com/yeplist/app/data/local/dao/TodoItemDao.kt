package com.yeplist.app.data.local.dao

import androidx.room.Dao
import androidx.room.Query
import androidx.room.Upsert
import com.yeplist.app.data.local.entity.TodoItemEntity
import kotlinx.coroutines.flow.Flow

@Dao
interface TodoItemDao {

    @Query("SELECT * FROM todo_items WHERE listId = :listId ORDER BY sortOrder, title")
    fun getByListId(listId: Long): Flow<List<TodoItemEntity>>

    @Query("SELECT * FROM todo_items WHERE itemId = :id")
    suspend fun getById(id: Long): TodoItemEntity?

    @Query("SELECT MIN(itemId) FROM todo_items")
    suspend fun getMinId(): Long?

    @Upsert
    suspend fun upsert(entity: TodoItemEntity)

    @Upsert
    suspend fun upsertAll(entities: List<TodoItemEntity>)

    @Query("DELETE FROM todo_items WHERE itemId = :id")
    suspend fun deleteById(id: Long)

    @Query("DELETE FROM todo_items WHERE itemId IN (:ids)")
    suspend fun deleteByIds(ids: List<Long>)

    @Query("UPDATE todo_items SET listId = :newListId WHERE listId = :oldListId")
    suspend fun updateListId(oldListId: Long, newListId: Long)

    @Query("UPDATE todo_items SET categoryId = :newCategoryId WHERE categoryId = :oldCategoryId")
    suspend fun updateCategoryId(oldCategoryId: Long, newCategoryId: Long)

    @Query("SELECT * FROM todo_items WHERE listId = :listId ORDER BY sortOrder, title LIMIT :limit")
    suspend fun getItemsForWidget(listId: Long, limit: Int = 50): List<TodoItemEntity>
}
