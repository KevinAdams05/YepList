package com.yeplist.app.data.remote.dto

data class CreateCategoryRequest(
    val name: String,
    val color: String? = null
)
