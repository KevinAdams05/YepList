package com.yeplist.app.data.remote.dto

data class ReorderItemsRequest(
    val items: List<ReorderEntry>
)

data class ReorderEntry(
    val itemId: Long,
    val sortOrder: Int
)
