package com.yeplist.app.data.remote.dto

data class CreateCategoryRequest(
    val name: String,
    val color: String? = null,
    // Real user-edit time (ISO-8601 UTC). Newest-edit-wins conflict arbiter.
    val clientModifiedDate: String? = null
)
