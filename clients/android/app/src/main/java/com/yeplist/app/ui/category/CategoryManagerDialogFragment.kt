package com.yeplist.app.ui.category

import android.app.Dialog
import android.os.Bundle
import android.view.View
import androidx.fragment.app.DialogFragment
import androidx.fragment.app.activityViewModels
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.lifecycleScope
import androidx.lifecycle.repeatOnLifecycle
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import com.google.android.material.button.MaterialButton
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import com.yeplist.app.R
import com.yeplist.app.YepListApp
import com.yeplist.app.data.local.entity.CategoryEntity
import com.yeplist.app.ui.MainViewModel
import kotlinx.coroutines.launch

class CategoryManagerDialogFragment : DialogFragment() {

    private val viewModel: MainViewModel by activityViewModels()
    private lateinit var adapter: CategoryAdapter

    override fun onCreateDialog(savedInstanceState: Bundle?): Dialog {
        val view = layoutInflater.inflate(R.layout.dialog_category_manager, null)
        val recyclerView = view.findViewById<RecyclerView>(R.id.categoriesRecyclerView)
        val emptyText = view.findViewById<View>(R.id.emptyCategoriesText)
        val addButton = view.findViewById<MaterialButton>(R.id.addCategoryButton)

        adapter = CategoryAdapter(
            onEdit = { category -> showEditDialog(category) },
            onDelete = { category -> confirmDelete(category) }
        )

        recyclerView.layoutManager = LinearLayoutManager(requireContext())
        recyclerView.adapter = adapter

        addButton.setOnClickListener { showEditDialog(null) }

        lifecycleScope.launch {
            repeatOnLifecycle(Lifecycle.State.STARTED) {
                viewModel.categories.collect { categories ->
                    adapter.submitList(categories)
                    emptyText.visibility = if (categories.isEmpty()) View.VISIBLE else View.GONE
                    recyclerView.visibility = if (categories.isEmpty()) View.GONE else View.VISIBLE
                }
            }
        }

        return MaterialAlertDialogBuilder(requireContext())
            .setTitle(R.string.manage_categories)
            .setView(view)
            .setPositiveButton(android.R.string.ok, null)
            .create()
    }

    private fun showEditDialog(category: CategoryEntity?) {
        CategoryEditDialogFragment.newInstance(
            categoryId = category?.categoryId,
            currentName = category?.name,
            currentColor = category?.color
        ).show(childFragmentManager, "edit_category")
    }

    private fun confirmDelete(category: CategoryEntity) {
        MaterialAlertDialogBuilder(requireContext())
            .setTitle(R.string.delete_category)
            .setMessage(getString(R.string.delete_list_confirm, category.name))
            .setPositiveButton(R.string.delete_category) { _, _ ->
                val container = (requireContext().applicationContext as YepListApp).container
                lifecycleScope.launch {
                    container.categoryRepository.delete(category.categoryId)
                    container.syncManager.pushOnly()
                }
            }
            .setNegativeButton(R.string.cancel, null)
            .show()
    }
}
