package com.yeplist.app.data.remote.dto

import com.yeplist.app.data.local.entity.CategoryEntity

data class CategoryDto(
    val categoryId: Long,
    val name: String,
    val color: String?,
    val createdDate: String,
    val modifiedDate: String
) {
    fun toEntity(): CategoryEntity = CategoryEntity(
        categoryId = categoryId,
        name = name,
        color = color,
        createdDate = createdDate,
        modifiedDate = modifiedDate
    )
}
