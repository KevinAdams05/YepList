package com.yeplist.app.data.remote.dto

data class UpdateTodoItemRequest(
    val title: String,
    val notes: String? = null,
    val categoryId: Long? = null,
    val listId: Long? = null,
    val isCompleted: Boolean = false,
    val dueDate: String? = null,
    val sortOrder: Int = 0
)
