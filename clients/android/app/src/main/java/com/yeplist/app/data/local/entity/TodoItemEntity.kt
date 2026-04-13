package com.yeplist.app.data.local.entity

import androidx.room.Entity
import androidx.room.Index
import androidx.room.PrimaryKey

@Entity(
    tableName = "todo_items",
    indices = [
        Index("listId"),
        Index("categoryId")
    ]
)
data class TodoItemEntity(
    @PrimaryKey val itemId: Long,
    val listId: Long,
    val categoryId: Long? = null,
    val title: String,
    val notes: String? = null,
    val isCompleted: Boolean = false,
    val dueDate: String? = null,
    val sortOrder: Int = 0,
    val createdDate: String = "",
    val modifiedDate: String = ""
)
