package com.yeplist.app.data.local.dao

import androidx.room.Dao
import androidx.room.Query
import androidx.room.Upsert
import com.yeplist.app.data.local.entity.SyncMetadataEntity

@Dao
interface SyncMetadataDao {

    @Query("SELECT * FROM sync_metadata WHERE id = 1")
    suspend fun get(): SyncMetadataEntity?

    @Upsert
    suspend fun upsert(entity: SyncMetadataEntity)
}
