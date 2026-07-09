package com.yeplist.app.data.remote.dto

data class UpdateTodoItemRequest(
    val title: String,
    val notes: String? = null,
    val categoryId: Long? = null,
    val listId: Long? = null,
    val isCompleted: Boolean = false,
    val dueDate: String? = null,
    val sortOrder: Int = 0,
    // Real user-edit time (ISO-8601 UTC). Newest-edit-wins conflict arbiter.
    val clientModifiedDate: String? = null
)
