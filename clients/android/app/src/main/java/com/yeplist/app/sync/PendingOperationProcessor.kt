package com.yeplist.app.sync

import android.util.Log
import com.google.gson.Gson
import com.yeplist.app.data.local.dao.PendingOperationDao
import com.yeplist.app.data.local.entity.PendingOperationEntity
import com.yeplist.app.data.remote.YepListApiService
import com.yeplist.app.data.remote.dto.CreateCategoryRequest
import com.yeplist.app.data.remote.dto.CreateTodoItemRequest
import com.yeplist.app.data.remote.dto.CreateTodoListRequest
import com.yeplist.app.data.remote.dto.ReorderItemsRequest
import com.yeplist.app.data.remote.dto.ToggleCompleteRequest
import com.yeplist.app.data.remote.dto.UpdateTodoItemRequest

class PendingOperationProcessor(
    private val apiService: YepListApiService,
    private val pendingOpDao: PendingOperationDao,
    private val idRemapper: IdRemapper,
    private val gson: Gson
) {

    /**
     * Processes all pending operations in FIFO order, running multiple passes
     * to resolve dependency chains (e.g., create list before creating items in it).
     * Returns true if all operations were processed successfully.
     */
    suspend fun processAll(): Boolean {
        var madeProgress = true

        while (madeProgress) {
            val ops = pendingOpDao.getAllOrdered()
            if (ops.isEmpty()) return true

            madeProgress = false
            for (op in ops) {
                val success = processOperation(op)
                if (success) {
                    pendingOpDao.deleteById(op.id)
                    madeProgress = true
                }
            }
        }

        val remaining = pendingOpDao.getCount()
        if (remaining > 0) {
            Log.w(TAG, "$remaining pending operations could not be processed")
        }
        return remaining == 0
    }

    private suspend fun processOperation(op: PendingOperationEntity): Boolean {
        return try {
            when (op.entityType) {
                "list" -> processListOp(op)
                "item" -> processItemOp(op)
                "category" -> processCategoryOp(op)
                else -> {
                    Log.w(TAG, "Unknown entity type: ${op.entityType}")
                    true
                }
            }
        } catch (e: Exception) {
            Log.e(TAG, "Failed to process ${op.entityType}/${op.operationType} id=${op.entityId}", e)
            false
        }
    }

    private suspend fun processListOp(op: PendingOperationEntity): Boolean {
        return when (op.operationType) {
            "create" -> {
                val request = gson.fromJson(op.payload, CreateTodoListRequest::class.java)
                val dto = apiService.createList(request)
                idRemapper.remapListId(op.entityId, dto.listId)
                true
            }
            "update" -> {
                val request = gson.fromJson(op.payload, CreateTodoListRequest::class.java)
                apiService.updateList(op.entityId, request)
                true
            }
            "delete" -> {
                apiService.deleteList(op.entityId)
                true
            }
            else -> true
        }
    }

    private suspend fun processItemOp(op: PendingOperationEntity): Boolean {
        return when (op.operationType) {
            "create" -> {
                val parentId = op.parentId ?: return false
                // Parent list not yet synced — retry after list CREATE completes
                if (parentId < 0) return false

                val request = gson.fromJson(op.payload, CreateTodoItemRequest::class.java)
                // Category not yet synced — retry after category CREATE completes
                if (request.categoryId != null && request.categoryId < 0) return false

                val dto = apiService.createItem(parentId, request)
                idRemapper.remapItemId(op.entityId, dto.itemId)
                true
            }
            "update" -> {
                if (op.entityId < 0) return false
                val request = gson.fromJson(op.payload, UpdateTodoItemRequest::class.java)
                apiService.updateItem(op.entityId, request)
                true
            }
            "toggle_complete" -> {
                if (op.entityId < 0) return false
                val request = gson.fromJson(op.payload, ToggleCompleteRequest::class.java)
                apiService.toggleComplete(op.entityId, request)
                true
            }
            "delete" -> {
                apiService.deleteItem(op.entityId)
                true
            }
            "reorder" -> {
                val parentId = op.parentId ?: return false
                if (parentId < 0) return false
                val request = gson.fromJson(op.payload, ReorderItemsRequest::class.java)
                // All items must have server IDs before reorder can be sent
                if (request.items.any { it.itemId < 0 }) return false
                apiService.reorderItems(parentId, request)
                true
            }
            else -> true
        }
    }

    private suspend fun processCategoryOp(op: PendingOperationEntity): Boolean {
        return when (op.operationType) {
            "create" -> {
                val request = gson.fromJson(op.payload, CreateCategoryRequest::class.java)
                val dto = apiService.createCategory(request)
                idRemapper.remapCategoryId(op.entityId, dto.categoryId)
                true
            }
            "update" -> {
                val request = gson.fromJson(op.payload, CreateCategoryRequest::class.java)
                apiService.updateCategory(op.entityId, request)
                true
            }
            "delete" -> {
                apiService.deleteCategory(op.entityId)
                true
            }
            else -> true
        }
    }

    companion object {
        private const val TAG = "PendingOpProcessor"
    }
}
