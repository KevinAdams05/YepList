package com.yeplist.app.data.remote.dto

data class SyncResponseDto(
    val serverTime: String,
    val lists: List<TodoListDto> = emptyList(),
    val categories: List<CategoryDto> = emptyList(),
    val items: List<TodoItemDto> = emptyList(),
    val deletedItemIds: List<Long> = emptyList(),
    val deletedListIds: List<Long> = emptyList(),
    val deletedCategoryIds: List<Long> = emptyList()
)
