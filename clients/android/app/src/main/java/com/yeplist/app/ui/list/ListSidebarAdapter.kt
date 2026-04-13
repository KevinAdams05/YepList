package com.yeplist.app.ui.list

import android.graphics.Typeface
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.ImageView
import android.widget.TextView
import androidx.recyclerview.widget.DiffUtil
import androidx.recyclerview.widget.ListAdapter
import androidx.recyclerview.widget.RecyclerView
import com.yeplist.app.R
import com.yeplist.app.data.local.entity.TodoListEntity

class ListSidebarAdapter(
    private val onClick: (TodoListEntity) -> Unit,
    private val onLongClick: (TodoListEntity, View) -> Unit
) : ListAdapter<TodoListEntity, ListSidebarAdapter.ViewHolder>(DiffCallback) {

    var selectedListId: Long? = null
        set(value) {
            field = value
            notifyDataSetChanged()
        }

    var defaultListId: Long = -1
        set(value) {
            field = value
            notifyDataSetChanged()
        }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
        val view = LayoutInflater.from(parent.context).inflate(R.layout.item_list, parent, false)
        return ViewHolder(view)
    }

    override fun onBindViewHolder(holder: ViewHolder, position: Int) {
        val item = getItem(position)
        holder.bind(item)
    }

    inner class ViewHolder(itemView: View) : RecyclerView.ViewHolder(itemView) {
        private val nameText: TextView = itemView.findViewById(R.id.listNameText)
        private val starIcon: ImageView = itemView.findViewById(R.id.defaultStarIcon)

        fun bind(entity: TodoListEntity) {
            nameText.text = entity.name

            val isSelected = entity.listId == selectedListId
            nameText.setTypeface(null, if (isSelected) Typeface.BOLD else Typeface.NORMAL)
            itemView.isActivated = isSelected

            starIcon.visibility = if (entity.listId == defaultListId) View.VISIBLE else View.GONE

            itemView.setOnClickListener { onClick(entity) }
            itemView.setOnLongClickListener { view ->
                onLongClick(entity, view)
                true
            }
        }
    }

    private object DiffCallback : DiffUtil.ItemCallback<TodoListEntity>() {
        override fun areItemsTheSame(oldItem: TodoListEntity, newItem: TodoListEntity): Boolean {
            return oldItem.listId == newItem.listId
        }

        override fun areContentsTheSame(oldItem: TodoListEntity, newItem: TodoListEntity): Boolean {
            return oldItem == newItem
        }
    }
}
