package com.yeplist.app.data.remote.dto

data class ToggleCompleteRequest(
    val isCompleted: Boolean,
    // Real user-edit time (ISO-8601 UTC). Newest-edit-wins conflict arbiter.
    val clientModifiedDate: String? = null
)
