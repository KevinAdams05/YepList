package com.yeplist.app.data.local.entity

import androidx.room.Entity
import androidx.room.PrimaryKey

@Entity(tableName = "todo_lists")
data class TodoListEntity(
    @PrimaryKey val listId: Long,
    val name: String,
    val sortOrder: Int = 0,
    val createdDate: String = "",
    val modifiedDate: String = ""
)
