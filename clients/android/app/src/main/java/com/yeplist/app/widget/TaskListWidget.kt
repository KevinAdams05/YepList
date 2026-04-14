package com.yeplist.app.widget

import android.content.Context
import android.content.Intent
import androidx.compose.runtime.Composable
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.datastore.preferences.core.Preferences
import androidx.glance.GlanceId
import androidx.glance.GlanceModifier
import androidx.glance.GlanceTheme
import androidx.glance.action.clickable
import androidx.glance.appwidget.action.actionStartActivity
import androidx.glance.appwidget.GlanceAppWidget
import androidx.glance.appwidget.appWidgetBackground
import androidx.glance.appwidget.lazy.LazyColumn
import androidx.glance.appwidget.lazy.items
import androidx.glance.appwidget.provideContent
import androidx.glance.background
import androidx.glance.currentState
import androidx.glance.layout.Alignment
import androidx.glance.layout.Box
import androidx.glance.layout.Column
import androidx.glance.layout.Row
import androidx.glance.layout.fillMaxSize
import androidx.glance.layout.fillMaxWidth
import androidx.glance.layout.padding
import androidx.glance.text.FontWeight
import androidx.glance.text.Text
import androidx.glance.text.TextStyle
import com.yeplist.app.YepListApp
import com.yeplist.app.data.local.entity.TodoItemEntity
import androidx.glance.appwidget.GlanceAppWidgetManager
import com.yeplist.app.debug.RemoteLogger
import com.yeplist.app.ui.MainActivity
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

class TaskListWidget : GlanceAppWidget() {

    override suspend fun provideGlance(context: Context, id: GlanceId) {
        RemoteLogger.d(TAG, "provideGlance called: glanceId=$id")

        // Pre-load data on IO thread; the listId will come from Glance state in provideContent
        val container = (context.applicationContext as YepListApp).container
        val allLists = withContext(Dispatchers.IO) {
            container.todoListDao.getAllSync()
        }

        provideContent {
            GlanceTheme {
                val prefs = currentState<Preferences>()
                val listId = prefs[WidgetConfigActivity.PREF_KEY_LIST_ID] ?: -1L
                RemoteLogger.d(TAG, "provideContent: listId from Glance state = $listId")

                val list = allLists.find { it.listId == listId }
                val listName = list?.name ?: ""

                if (listId <= 0 || listName.isEmpty()) {
                    ListNotFoundContent(context)
                } else {
                    // Load items synchronously since we're already in composition
                    val items = kotlinx.coroutines.runBlocking {
                        container.todoItemRepository.getItemsForWidget(listId)
                    }
                    RemoteLogger.d(TAG, "provideContent: listName='$listName', items=${items.size}")
                    TaskListContent(context, listName, listId, items)
                }
            }
        }
    }

    @Composable
    private fun ListNotFoundContent(context: Context) {
        Box(
            modifier = GlanceModifier
                .fillMaxSize()
                .appWidgetBackground()
                .background(GlanceTheme.colors.widgetBackground)
                .clickable(androidx.glance.action.actionStartActivity<WidgetConfigActivity>()),
            contentAlignment = Alignment.Center
        ) {
            Text(
                text = context.getString(com.yeplist.app.R.string.widget_list_not_found),
                style = TextStyle(
                    color = GlanceTheme.colors.onSurface,
                    fontSize = 14.sp
                )
            )
        }
    }

    private fun openListIntent(context: Context, listId: Long): Intent {
        return Intent(context, MainActivity::class.java).apply {
            putExtra(MainActivity.EXTRA_LIST_ID, listId)
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TOP
        }
    }

    @Composable
    private fun TaskListContent(
        context: Context,
        listName: String,
        listId: Long,
        items: List<TodoItemEntity>
    ) {
        Column(
            modifier = GlanceModifier
                .fillMaxSize()
                .appWidgetBackground()
                .background(GlanceTheme.colors.widgetBackground)
                .clickable(actionStartActivity(openListIntent(context, listId)))
        ) {
            // Header
            Text(
                text = listName,
                modifier = GlanceModifier.padding(horizontal = 16.dp, vertical = 12.dp),
                style = TextStyle(
                    color = GlanceTheme.colors.onSurface,
                    fontSize = 16.sp,
                    fontWeight = FontWeight.Bold
                )
            )

            // Task list
            LazyColumn(modifier = GlanceModifier.fillMaxSize()) {
                items(items, itemId = { it.itemId }) { item ->
                    TaskRow(item)
                }
            }
        }
    }

    @Composable
    private fun TaskRow(item: TodoItemEntity) {
        Row(
            modifier = GlanceModifier
                .fillMaxWidth()
                .padding(start = 16.dp, end = 4.dp, top = 6.dp, bottom = 6.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            // Checkbox + title area (clickable to toggle complete)
            Row(
                modifier = GlanceModifier
                    .defaultWeight()
                    .clickable(ToggleCompleteAction.action(item.itemId)),
                verticalAlignment = Alignment.CenterVertically
            ) {
                val checkmark = if (item.isCompleted) "\u2611" else "\u2610"
                Text(
                    text = checkmark,
                    style = TextStyle(
                        color = GlanceTheme.colors.onSurface,
                        fontSize = 18.sp
                    ),
                    modifier = GlanceModifier.padding(end = 8.dp)
                )
                Text(
                    text = item.title,
                    style = TextStyle(
                        color = if (item.isCompleted) {
                            GlanceTheme.colors.outline
                        } else {
                            GlanceTheme.colors.onSurface
                        },
                        fontSize = 14.sp
                    )
                )
            }

            // Delete button
            Box(
                modifier = GlanceModifier
                    .padding(horizontal = 8.dp, vertical = 2.dp)
                    .clickable(DeleteTaskAction.action(item.itemId)),
                contentAlignment = Alignment.Center
            ) {
                Text(
                    text = "\u00D7",
                    style = TextStyle(
                        color = GlanceTheme.colors.outline,
                        fontSize = 18.sp
                    )
                )
            }
        }
    }

    companion object {
        private const val TAG = "TaskListWidget"

        /**
         * Update all widget instances. Called after sync completes
         * so widgets reflect the latest data.
         */
        suspend fun updateAll(context: Context) {
            try {
                val manager = GlanceAppWidgetManager(context)
                val glanceIds = manager.getGlanceIds(TaskListWidget::class.java)
                val widget = TaskListWidget()
                for (id in glanceIds) {
                    widget.update(context, id)
                }
                if (glanceIds.isNotEmpty()) {
                    RemoteLogger.d(TAG, "Updated ${glanceIds.size} widget(s) after sync")
                }
            } catch (e: Exception) {
                RemoteLogger.e(TAG, "Failed to update widgets", e)
            }
        }
    }
}
