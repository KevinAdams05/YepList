package com.yeplist.app.data.remote.dto

data class CreateTodoItemRequest(
    val title: String,
    val notes: String? = null,
    val categoryId: Long? = null,
    val dueDate: String? = null,
    val sortOrder: Int = 0
)
