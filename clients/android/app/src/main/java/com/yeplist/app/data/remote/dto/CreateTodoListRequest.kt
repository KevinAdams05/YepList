package com.yeplist.app.data.remote.dto

data class CreateTodoListRequest(
    val name: String,
    val sortOrder: Int = 0
)
