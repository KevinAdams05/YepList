package com.yeplist.app.widget

import android.appwidget.AppWidgetManager
import android.content.ComponentName
import android.content.Context
import android.content.Intent
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import androidx.datastore.preferences.core.longPreferencesKey
import androidx.glance.appwidget.GlanceAppWidgetManager
import androidx.glance.appwidget.state.updateAppWidgetState
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import kotlinx.coroutines.launch
import androidx.recyclerview.widget.RecyclerView
import com.yeplist.app.R
import com.yeplist.app.YepListApp
import com.yeplist.app.data.local.entity.TodoListEntity
import com.yeplist.app.debug.RemoteLogger

class WidgetConfigActivity : AppCompatActivity() {

    private var appWidgetId = AppWidgetManager.INVALID_APPWIDGET_ID

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        // Set result to CANCELED in case the user backs out
        setResult(RESULT_CANCELED)

        appWidgetId = intent?.extras?.getInt(
            AppWidgetManager.EXTRA_APPWIDGET_ID,
            AppWidgetManager.INVALID_APPWIDGET_ID
        ) ?: AppWidgetManager.INVALID_APPWIDGET_ID

        RemoteLogger.d(TAG, "onCreate: appWidgetId=$appWidgetId")

        if (appWidgetId == AppWidgetManager.INVALID_APPWIDGET_ID) {
            RemoteLogger.w(TAG, "Invalid appWidgetId, finishing")
            finish()
            return
        }

        setContentView(R.layout.activity_widget_config)

        val container = (application as YepListApp).container
        val recyclerView = findViewById<RecyclerView>(R.id.listRecyclerView)
        recyclerView.layoutManager = LinearLayoutManager(this)

        // Always sync to get the latest lists from the server
        lifecycleScope.launch {
            try {
                RemoteLogger.d(TAG, "Syncing to get latest lists")
                container.syncManager.sync()
            } catch (e: Exception) {
                RemoteLogger.e(TAG, "Sync failed in widget config", e)
            }

            val lists = container.todoListRepository.getAllSync()
            RemoteLogger.d(TAG, "Lists count: ${lists.size}")

            recyclerView.adapter = ListPickerAdapter(lists) { selectedList ->
                saveWidgetConfig(selectedList.listId)
            }
        }
    }

    private fun saveWidgetConfig(listId: Long) {
        RemoteLogger.d(TAG, "saveWidgetConfig: appWidgetId=$appWidgetId, listId=$listId")

        // Store in Glance's own state so it knows data changed and will re-render
        lifecycleScope.launch {
            try {
                val glanceId = GlanceAppWidgetManager(this@WidgetConfigActivity)
                    .getGlanceIdBy(appWidgetId)
                RemoteLogger.d(TAG, "saveWidgetConfig: glanceId=$glanceId")

                // Update Glance state — this is what triggers provideGlance on next update()
                updateAppWidgetState(this@WidgetConfigActivity, glanceId) { prefs ->
                    prefs[PREF_KEY_LIST_ID] = listId
                }
                RemoteLogger.d(TAG, "saveWidgetConfig: Glance state updated with listId=$listId")

                // Now update() will see the state changed and call provideGlance
                TaskListWidget().update(this@WidgetConfigActivity, glanceId)
                RemoteLogger.d(TAG, "saveWidgetConfig: widget update() complete")
            } catch (e: Exception) {
                RemoteLogger.e(TAG, "saveWidgetConfig: failed", e)
            }

            val resultValue = Intent().putExtra(AppWidgetManager.EXTRA_APPWIDGET_ID, appWidgetId)
            setResult(RESULT_OK, resultValue)
            finish()
        }
    }

    private class ListPickerAdapter(
        private val lists: List<TodoListEntity>,
        private val onPick: (TodoListEntity) -> Unit
    ) : RecyclerView.Adapter<ListPickerAdapter.ViewHolder>() {

        override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
            val view = LayoutInflater.from(parent.context)
                .inflate(R.layout.item_list, parent, false)
            return ViewHolder(view)
        }

        override fun onBindViewHolder(holder: ViewHolder, position: Int) {
            val list = lists[position]
            holder.nameText.text = list.name
            holder.itemView.setOnClickListener { onPick(list) }
        }

        override fun getItemCount(): Int = lists.size

        class ViewHolder(view: View) : RecyclerView.ViewHolder(view) {
            val nameText: TextView = view.findViewById(R.id.listNameText)
        }
    }

    companion object {
        private const val TAG = "WidgetConfig"
        val PREF_KEY_LIST_ID = longPreferencesKey("widget_list_id")
    }
}
