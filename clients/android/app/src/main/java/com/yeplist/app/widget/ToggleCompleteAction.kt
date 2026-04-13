package com.yeplist.app.widget

import android.content.Context
import androidx.glance.GlanceId
import androidx.glance.action.Action
import androidx.glance.action.ActionParameters
import androidx.glance.action.actionParametersOf
import androidx.glance.appwidget.action.ActionCallback
import androidx.glance.appwidget.action.actionRunCallback
import com.yeplist.app.YepListApp
import com.yeplist.app.debug.RemoteLogger

class ToggleCompleteAction : ActionCallback {

    override suspend fun onAction(context: Context, glanceId: GlanceId, parameters: ActionParameters) {
        val itemId = parameters[PARAM_ITEM_ID] ?: return
        val container = (context.applicationContext as YepListApp).container

        RemoteLogger.d("WidgetToggle", "Toggling item $itemId")
        val item = container.todoItemDao.getById(itemId) ?: return
        container.todoItemRepository.toggleComplete(item)
        container.syncManager.pushOnly()

        // Update widget
        TaskListWidget().update(context, glanceId)
    }

    companion object {
        private val PARAM_ITEM_ID = ActionParameters.Key<Long>("item_id")

        fun action(itemId: Long): Action {
            return actionRunCallback<ToggleCompleteAction>(
                actionParametersOf(PARAM_ITEM_ID to itemId)
            )
        }
    }
}
