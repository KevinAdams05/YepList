package com.yeplist.app.data.remote.dto

import com.yeplist.app.data.local.entity.TodoItemEntity

data class TodoItemDto(
    val itemId: Long,
    val listId: Long,
    val categoryId: Long?,
    val title: String,
    val notes: String?,
    val isCompleted: Boolean,
    val dueDate: String?,
    val sortOrder: Int,
    val createdDate: String,
    val modifiedDate: String
) {
    fun toEntity(): TodoItemEntity = TodoItemEntity(
        itemId = itemId,
        listId = listId,
        categoryId = categoryId,
        title = title,
        notes = notes,
        isCompleted = isCompleted,
        dueDate = dueDate,
        sortOrder = sortOrder,
        createdDate = createdDate,
        modifiedDate = modifiedDate
    )
}
