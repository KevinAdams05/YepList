package com.yeplist.app.data.local

import android.content.Context
import androidx.room.Database
import androidx.room.Room
import androidx.room.RoomDatabase
import com.yeplist.app.data.local.dao.CategoryDao
import com.yeplist.app.data.local.dao.PendingOperationDao
import com.yeplist.app.data.local.dao.SyncMetadataDao
import com.yeplist.app.data.local.dao.TodoItemDao
import com.yeplist.app.data.local.dao.TodoListDao
import com.yeplist.app.data.local.entity.CategoryEntity
import com.yeplist.app.data.local.entity.PendingOperationEntity
import com.yeplist.app.data.local.entity.SyncMetadataEntity
import com.yeplist.app.data.local.entity.TodoItemEntity
import com.yeplist.app.data.local.entity.TodoListEntity

@Database(
    entities = [
        TodoListEntity::class,
        TodoItemEntity::class,
        CategoryEntity::class,
        PendingOperationEntity::class,
        SyncMetadataEntity::class
    ],
    version = 1,
    exportSchema = true
)
abstract class YepListDatabase : RoomDatabase() {

    abstract fun todoListDao(): TodoListDao
    abstract fun todoItemDao(): TodoItemDao
    abstract fun categoryDao(): CategoryDao
    abstract fun pendingOperationDao(): PendingOperationDao
    abstract fun syncMetadataDao(): SyncMetadataDao

    companion object {
        @Volatile
        private var instance: YepListDatabase? = null

        fun getInstance(context: Context): YepListDatabase {
            return instance ?: synchronized(this) {
                instance ?: Room.databaseBuilder(
                    context.applicationContext,
                    YepListDatabase::class.java,
                    "yeplist.db"
                ).build().also { instance = it }
            }
        }
    }
}
