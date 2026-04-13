package com.yeplist.app.ui.task

import android.graphics.Color
import android.graphics.Paint
import android.graphics.drawable.GradientDrawable
import android.view.LayoutInflater
import android.view.MotionEvent
import android.view.View
import android.view.ViewGroup
import android.widget.CheckBox
import android.widget.ImageView
import android.widget.LinearLayout
import android.widget.TextView
import androidx.recyclerview.widget.DiffUtil
import androidx.recyclerview.widget.ListAdapter
import androidx.recyclerview.widget.RecyclerView
import com.yeplist.app.R
import com.yeplist.app.data.local.entity.CategoryEntity
import com.yeplist.app.data.local.entity.TodoItemEntity
import java.time.LocalDate
import java.time.format.DateTimeFormatter
import java.time.format.FormatStyle

class TaskAdapter(
    private val onToggleComplete: (TodoItemEntity) -> Unit,
    private val onClick: (TodoItemEntity) -> Unit,
    private val onDragStarted: (RecyclerView.ViewHolder) -> Unit,
    private val onLongClick: ((TodoItemEntity) -> Unit)? = null
) : ListAdapter<TodoItemEntity, TaskAdapter.ViewHolder>(DiffCallback) {

    var categories: Map<Long, CategoryEntity> = emptyMap()
        set(value) {
            field = value
            notifyDataSetChanged()
        }

    var selectedIds: Set<Long> = emptySet()
        set(value) {
            field = value
            notifyDataSetChanged()
        }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
        val view = LayoutInflater.from(parent.context).inflate(R.layout.item_task, parent, false)
        return ViewHolder(view)
    }

    override fun onBindViewHolder(holder: ViewHolder, position: Int) {
        val item = getItem(position)
        holder.bind(item)
    }

    inner class ViewHolder(itemView: View) : RecyclerView.ViewHolder(itemView) {
        private val dragHandle: ImageView = itemView.findViewById(R.id.dragHandle)
        private val checkBox: CheckBox = itemView.findViewById(R.id.completedCheckBox)
        private val titleText: TextView = itemView.findViewById(R.id.titleText)
        private val detailsRow: LinearLayout = itemView.findViewById(R.id.detailsRow)
        private val categoryChip: TextView = itemView.findViewById(R.id.categoryChip)
        private val dueDateText: TextView = itemView.findViewById(R.id.dueDateText)

        fun bind(item: TodoItemEntity) {
            checkBox.setOnCheckedChangeListener(null)
            checkBox.isChecked = item.isCompleted
            checkBox.setOnCheckedChangeListener { _, _ -> onToggleComplete(item) }

            titleText.text = item.title
            if (item.isCompleted) {
                titleText.paintFlags = titleText.paintFlags or Paint.STRIKE_THRU_TEXT_FLAG
                titleText.alpha = 0.5f
            } else {
                titleText.paintFlags = titleText.paintFlags and Paint.STRIKE_THRU_TEXT_FLAG.inv()
                titleText.alpha = 1.0f
            }

            // Category chip
            val category = item.categoryId?.let { categories[it] }
            if (category != null) {
                categoryChip.text = category.name
                categoryChip.visibility = View.VISIBLE
                if (category.color != null) {
                    try {
                        val color = Color.parseColor(category.color)
                        val bg = categoryChip.background as? GradientDrawable
                            ?: GradientDrawable().also { categoryChip.background = it }
                        bg.setColor(color)
                        bg.cornerRadius = 12f * itemView.resources.displayMetrics.density
                        categoryChip.setTextColor(if (isColorDark(color)) Color.WHITE else Color.BLACK)
                    } catch (e: IllegalArgumentException) {
                        // Invalid color, use default
                    }
                }
            } else {
                categoryChip.visibility = View.GONE
            }

            // Due date
            if (item.dueDate != null) {
                try {
                    val date = LocalDate.parse(item.dueDate)
                    dueDateText.text = date.format(DateTimeFormatter.ofLocalizedDate(FormatStyle.MEDIUM))
                    dueDateText.visibility = View.VISIBLE
                } catch (e: Exception) {
                    dueDateText.visibility = View.GONE
                }
            } else {
                dueDateText.visibility = View.GONE
            }

            detailsRow.visibility =
                if (categoryChip.visibility == View.VISIBLE || dueDateText.visibility == View.VISIBLE) {
                    View.VISIBLE
                } else {
                    View.GONE
                }

            // Selection state
            itemView.isActivated = item.itemId in selectedIds

            itemView.setOnClickListener { onClick(item) }
            itemView.setOnLongClickListener {
                onLongClick?.invoke(item)
                true
            }

            dragHandle.setOnTouchListener { _, event ->
                if (event.actionMasked == MotionEvent.ACTION_DOWN) {
                    onDragStarted(this)
                }
                false
            }
        }

        private fun isColorDark(color: Int): Boolean {
            val r = Color.red(color) / 255.0
            val g = Color.green(color) / 255.0
            val b = Color.blue(color) / 255.0
            val luminance = 0.299 * r + 0.587 * g + 0.114 * b
            return luminance < 0.5
        }
    }

    private object DiffCallback : DiffUtil.ItemCallback<TodoItemEntity>() {
        override fun areItemsTheSame(oldItem: TodoItemEntity, newItem: TodoItemEntity): Boolean {
            return oldItem.itemId == newItem.itemId
        }

        override fun areContentsTheSame(oldItem: TodoItemEntity, newItem: TodoItemEntity): Boolean {
            return oldItem == newItem
        }
    }
}
