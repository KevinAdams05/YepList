package com.yeplist.app.data.local.dao

import androidx.room.Dao
import androidx.room.Insert
import androidx.room.Query
import com.yeplist.app.data.local.entity.PendingOperationEntity

@Dao
interface PendingOperationDao {

    @Query("SELECT * FROM pending_operations ORDER BY createdDate, id")
    suspend fun getAllOrdered(): List<PendingOperationEntity>

    @Insert
    suspend fun insert(entity: PendingOperationEntity): Long

    @Query("DELETE FROM pending_operations WHERE id = :id")
    suspend fun deleteById(id: Long)

    @Query("DELETE FROM pending_operations WHERE entityType = :entityType AND entityId = :entityId")
    suspend fun deleteByEntity(entityType: String, entityId: Long)

    @Query("SELECT * FROM pending_operations WHERE entityType = :entityType AND entityId = :entityId ORDER BY createdDate, id")
    suspend fun getByEntity(entityType: String, entityId: Long): List<PendingOperationEntity>

    @Query("UPDATE pending_operations SET entityId = :newId WHERE entityId = :oldId AND entityType = :entityType")
    suspend fun updateEntityId(oldId: Long, newId: Long, entityType: String)

    @Query("UPDATE pending_operations SET parentId = :newId WHERE parentId = :oldId")
    suspend fun updateParentId(oldId: Long, newId: Long)

    @Query("DELETE FROM pending_operations")
    suspend fun deleteAll()

    @Query("SELECT COUNT(*) FROM pending_operations")
    suspend fun getCount(): Int
}
