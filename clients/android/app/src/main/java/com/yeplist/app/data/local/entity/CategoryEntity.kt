package com.yeplist.app.data.local.entity

import androidx.room.Entity
import androidx.room.PrimaryKey

@Entity(tableName = "categories")
data class CategoryEntity(
    @PrimaryKey val categoryId: Long,
    val name: String,
    val color: String? = null,
    val createdDate: String = "",
    val modifiedDate: String = ""
)
