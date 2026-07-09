package com.yeplist.app.data.remote.dto

data class CreateTodoListRequest(
    val name: String,
    val sortOrder: Int = 0,
    // Real user-edit time (ISO-8601 UTC). Newest-edit-wins conflict arbiter.
    val clientModifiedDate: String? = null
)
