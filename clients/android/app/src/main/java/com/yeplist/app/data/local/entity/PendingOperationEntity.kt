package com.yeplist.app.data.local.entity

import androidx.room.Entity
import androidx.room.PrimaryKey

@Entity(tableName = "pending_operations")
data class PendingOperationEntity(
    @PrimaryKey(autoGenerate = true) val id: Long = 0,
    val entityType: String,
    val operationType: String,
    val entityId: Long,
    val parentId: Long? = null,
    val payload: String? = null,
    val createdDate: String = ""
)
