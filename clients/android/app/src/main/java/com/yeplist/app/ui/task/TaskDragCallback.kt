package com.yeplist.app.ui.task

import android.graphics.Canvas
import android.graphics.Color
import android.graphics.Paint
import android.graphics.RectF
import android.graphics.drawable.Drawable
import androidx.recyclerview.widget.ItemTouchHelper
import androidx.recyclerview.widget.RecyclerView

class TaskDragCallback(
    private val onMoved: (fromPos: Int, toPos: Int) -> Unit,
    private val onDropped: () -> Unit,
    private val onSwiped: (position: Int) -> Unit,
    private val deleteIcon: Drawable? = null
) : ItemTouchHelper.Callback() {

    private val deletePaint = Paint().apply {
        color = Color.parseColor("#D32F2F")
    }

    override fun getMovementFlags(recyclerView: RecyclerView, viewHolder: RecyclerView.ViewHolder): Int {
        val dragFlags = ItemTouchHelper.UP or ItemTouchHelper.DOWN
        val swipeFlags = ItemTouchHelper.LEFT
        return makeMovementFlags(dragFlags, swipeFlags)
    }

    override fun onMove(
        recyclerView: RecyclerView,
        viewHolder: RecyclerView.ViewHolder,
        target: RecyclerView.ViewHolder
    ): Boolean {
        onMoved(viewHolder.bindingAdapterPosition, target.bindingAdapterPosition)
        return true
    }

    override fun onSwiped(viewHolder: RecyclerView.ViewHolder, direction: Int) {
        onSwiped(viewHolder.bindingAdapterPosition)
    }

    override fun clearView(recyclerView: RecyclerView, viewHolder: RecyclerView.ViewHolder) {
        super.clearView(recyclerView, viewHolder)
        onDropped()
    }

    override fun isLongPressDragEnabled(): Boolean = false

    override fun onChildDraw(
        c: Canvas,
        recyclerView: RecyclerView,
        viewHolder: RecyclerView.ViewHolder,
        dX: Float,
        dY: Float,
        actionState: Int,
        isCurrentlyActive: Boolean
    ) {
        if (actionState == ItemTouchHelper.ACTION_STATE_SWIPE && dX < 0) {
            val itemView = viewHolder.itemView
            val backgroundRect = RectF(
                itemView.right + dX,
                itemView.top.toFloat(),
                itemView.right.toFloat(),
                itemView.bottom.toFloat()
            )
            c.drawRect(backgroundRect, deletePaint)

            // Draw delete icon
            if (deleteIcon != null) {
                val iconSize = 24 * itemView.resources.displayMetrics.density
                val iconMargin = 16 * itemView.resources.displayMetrics.density
                val iconTop = (itemView.top + (itemView.height - iconSize) / 2).toInt()
                val iconLeft = (itemView.right - iconMargin - iconSize).toInt()
                val iconRight = (itemView.right - iconMargin).toInt()
                val iconBottom = (iconTop + iconSize).toInt()
                deleteIcon.setBounds(iconLeft, iconTop, iconRight, iconBottom)
                deleteIcon.setTint(Color.WHITE)
                deleteIcon.draw(c)
            }
        }

        super.onChildDraw(c, recyclerView, viewHolder, dX, dY, actionState, isCurrentlyActive)
    }
}
