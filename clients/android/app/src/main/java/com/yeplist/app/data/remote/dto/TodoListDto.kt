package com.yeplist.app.data.remote.dto

import com.yeplist.app.data.local.entity.TodoListEntity

data class TodoListDto(
    val listId: Long,
    val name: String,
    val sortOrder: Int,
    val createdDate: String,
    val modifiedDate: String
) {
    fun toEntity(): TodoListEntity = TodoListEntity(
        listId = listId,
        name = name,
        sortOrder = sortOrder,
        createdDate = createdDate,
        modifiedDate = modifiedDate
    )
}
