package com.yeplist.app.ui.category

import android.graphics.Color
import android.graphics.drawable.GradientDrawable
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.ImageView
import android.widget.TextView
import androidx.recyclerview.widget.DiffUtil
import androidx.recyclerview.widget.ListAdapter
import androidx.recyclerview.widget.RecyclerView
import com.yeplist.app.R
import com.yeplist.app.data.local.entity.CategoryEntity

class CategoryAdapter(
    private val onEdit: (CategoryEntity) -> Unit,
    private val onDelete: (CategoryEntity) -> Unit
) : ListAdapter<CategoryEntity, CategoryAdapter.ViewHolder>(DiffCallback) {

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ViewHolder {
        val view = LayoutInflater.from(parent.context).inflate(R.layout.item_category, parent, false)
        return ViewHolder(view)
    }

    override fun onBindViewHolder(holder: ViewHolder, position: Int) {
        holder.bind(getItem(position))
    }

    inner class ViewHolder(itemView: View) : RecyclerView.ViewHolder(itemView) {
        private val colorSwatch: View = itemView.findViewById(R.id.colorSwatch)
        private val nameText: TextView = itemView.findViewById(R.id.categoryNameText)
        private val editButton: ImageView = itemView.findViewById(R.id.editButton)
        private val deleteButton: ImageView = itemView.findViewById(R.id.deleteButton)

        fun bind(category: CategoryEntity) {
            nameText.text = category.name

            if (category.color != null) {
                try {
                    val color = Color.parseColor(category.color)
                    val bg = colorSwatch.background as? GradientDrawable
                        ?: GradientDrawable().also { colorSwatch.background = it }
                    bg.shape = GradientDrawable.OVAL
                    bg.setColor(color)
                } catch (e: IllegalArgumentException) {
                    // Invalid color
                }
            }

            editButton.setOnClickListener { onEdit(category) }
            deleteButton.setOnClickListener { onDelete(category) }
        }
    }

    private object DiffCallback : DiffUtil.ItemCallback<CategoryEntity>() {
        override fun areItemsTheSame(oldItem: CategoryEntity, newItem: CategoryEntity): Boolean {
            return oldItem.categoryId == newItem.categoryId
        }

        override fun areContentsTheSame(oldItem: CategoryEntity, newItem: CategoryEntity): Boolean {
            return oldItem == newItem
        }
    }
}
