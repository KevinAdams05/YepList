package com.yeplist.app.ui.task

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.yeplist.app.YepListApp
import com.yeplist.app.data.local.entity.TodoItemEntity
import com.yeplist.app.di.AppContainer
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.flatMapLatest
import kotlinx.coroutines.flow.flowOf
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.launch

class TaskListViewModel(application: Application) : AndroidViewModel(application) {

    private val container: AppContainer = (application as YepListApp).container

    private val _listId = MutableStateFlow<Long?>(null)

    @OptIn(ExperimentalCoroutinesApi::class)
    val items: StateFlow<List<TodoItemEntity>> = _listId
        .flatMapLatest { id ->
            if (id != null) {
                container.todoItemRepository.getByListId(id)
            } else {
                flowOf(emptyList())
            }
        }
        .stateIn(viewModelScope, SharingStarted.WhileSubscribed(5000), emptyList())

    fun setListId(listId: Long?) {
        _listId.value = listId
    }

    fun quickAdd(title: String) {
        val listId = _listId.value ?: return
        viewModelScope.launch {
            val currentItems = items.value
            val maxOrder = currentItems.maxOfOrNull { it.sortOrder } ?: -1
            container.todoItemRepository.create(
                listId = listId,
                title = title,
                sortOrder = maxOrder + 1
            )
            pushIfOnline()
        }
    }

    fun toggleComplete(item: TodoItemEntity) {
        viewModelScope.launch {
            container.todoItemRepository.toggleComplete(item)
            pushIfOnline()
        }
    }

    fun deleteItems(itemIds: List<Long>) {
        viewModelScope.launch {
            container.todoItemRepository.deleteItems(itemIds)
            pushIfOnline()
        }
    }

    fun reorder(items: List<TodoItemEntity>) {
        val listId = _listId.value ?: return
        viewModelScope.launch {
            container.todoItemRepository.reorder(listId, items)
            pushIfOnline()
        }
    }

    suspend fun createItem(
        title: String,
        notes: String?,
        categoryId: Long?,
        dueDate: String?,
        sortOrder: Int
    ): TodoItemEntity? {
        val listId = _listId.value ?: return null
        val entity = container.todoItemRepository.create(
            listId = listId,
            title = title,
            notes = notes,
            categoryId = categoryId,
            dueDate = dueDate,
            sortOrder = sortOrder
        )
        pushIfOnline()
        return entity
    }

    suspend fun updateItem(
        itemId: Long,
        title: String,
        notes: String?,
        categoryId: Long?,
        isCompleted: Boolean,
        dueDate: String?,
        sortOrder: Int,
        listId: Long? = null
    ) {
        container.todoItemRepository.update(itemId, title, notes, categoryId, isCompleted, dueDate, sortOrder, listId)
        pushIfOnline()
    }

    private fun pushIfOnline() {
        viewModelScope.launch {
            container.syncManager.pushOnly()
        }
    }
}
