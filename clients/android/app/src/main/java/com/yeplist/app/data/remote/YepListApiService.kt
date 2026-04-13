package com.yeplist.app.data.remote

import com.yeplist.app.data.remote.dto.CategoryDto
import com.yeplist.app.data.remote.dto.CreateCategoryRequest
import com.yeplist.app.data.remote.dto.CreateTodoItemRequest
import com.yeplist.app.data.remote.dto.CreateTodoListRequest
import com.yeplist.app.data.remote.dto.ReorderItemsRequest
import com.yeplist.app.data.remote.dto.SyncResponseDto
import com.yeplist.app.data.remote.dto.TodoItemDto
import com.yeplist.app.data.remote.dto.TodoListDto
import com.yeplist.app.data.remote.dto.ToggleCompleteRequest
import com.yeplist.app.data.remote.dto.UpdateTodoItemRequest
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.PATCH
import retrofit2.http.POST
import retrofit2.http.PUT
import retrofit2.http.Path
import retrofit2.http.Query

interface YepListApiService {

    // Lists
    @GET("api/lists")
    suspend fun getLists(): List<TodoListDto>

    @POST("api/lists")
    suspend fun createList(@Body request: CreateTodoListRequest): TodoListDto

    @PUT("api/lists/{id}")
    suspend fun updateList(@Path("id") id: Long, @Body request: CreateTodoListRequest): TodoListDto

    @DELETE("api/lists/{id}")
    suspend fun deleteList(@Path("id") id: Long): Response<Unit>

    // Categories
    @GET("api/categories")
    suspend fun getCategories(): List<CategoryDto>

    @POST("api/categories")
    suspend fun createCategory(@Body request: CreateCategoryRequest): CategoryDto

    @PUT("api/categories/{id}")
    suspend fun updateCategory(@Path("id") id: Long, @Body request: CreateCategoryRequest): CategoryDto

    @DELETE("api/categories/{id}")
    suspend fun deleteCategory(@Path("id") id: Long): Response<Unit>

    // Items
    @GET("api/lists/{listId}/items")
    suspend fun getItemsByList(@Path("listId") listId: Long): List<TodoItemDto>

    @POST("api/lists/{listId}/items")
    suspend fun createItem(@Path("listId") listId: Long, @Body request: CreateTodoItemRequest): TodoItemDto

    @PUT("api/items/{id}")
    suspend fun updateItem(@Path("id") id: Long, @Body request: UpdateTodoItemRequest): TodoItemDto

    @PATCH("api/items/{id}/complete")
    suspend fun toggleComplete(@Path("id") id: Long, @Body request: ToggleCompleteRequest): TodoItemDto

    @DELETE("api/items/{id}")
    suspend fun deleteItem(@Path("id") id: Long): Response<Unit>

    @PUT("api/lists/{listId}/items/reorder")
    suspend fun reorderItems(@Path("listId") listId: Long, @Body request: ReorderItemsRequest): Response<Unit>

    // Sync
    @GET("api/sync")
    suspend fun sync(@Query("since") since: String? = null): SyncResponseDto
}
