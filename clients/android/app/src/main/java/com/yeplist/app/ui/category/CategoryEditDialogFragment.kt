package com.yeplist.app.ui.category

import android.app.Dialog
import android.os.Bundle
import android.view.WindowManager
import android.widget.EditText
import android.widget.LinearLayout
import androidx.fragment.app.DialogFragment
import androidx.lifecycle.lifecycleScope
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import com.yeplist.app.R
import com.yeplist.app.YepListApp
import kotlinx.coroutines.launch

class CategoryEditDialogFragment : DialogFragment() {

    override fun onCreateDialog(savedInstanceState: Bundle?): Dialog {
        val categoryId = arguments?.getLong(ARG_CATEGORY_ID, -1)?.takeIf { it > 0 }
        val currentName = arguments?.getString(ARG_CURRENT_NAME) ?: ""
        val currentColor = arguments?.getString(ARG_CURRENT_COLOR) ?: ""
        val isEdit = categoryId != null

        val container = LinearLayout(requireContext()).apply {
            orientation = LinearLayout.VERTICAL
            setPadding(64, 32, 64, 0)
        }

        val nameEdit = EditText(requireContext()).apply {
            hint = getString(R.string.category_name)
            setText(currentName)
            setSingleLine()
        }
        container.addView(nameEdit)

        val colorEdit = EditText(requireContext()).apply {
            hint = getString(R.string.color) + " (#FF5733)"
            setText(currentColor)
            setSingleLine()
        }
        container.addView(colorEdit)

        val title = if (isEdit) R.string.edit_category else R.string.add_category

        val dialog = MaterialAlertDialogBuilder(requireContext())
            .setTitle(title)
            .setView(container)
            .setPositiveButton(R.string.save) { _, _ ->
                val name = nameEdit.text.toString().trim()
                val color = colorEdit.text.toString().trim().takeIf { it.isNotEmpty() }
                if (name.isNotEmpty()) {
                    val appContainer = (requireContext().applicationContext as YepListApp).container
                    lifecycleScope.launch {
                        if (isEdit) {
                            appContainer.categoryRepository.update(categoryId!!, name, color)
                        } else {
                            appContainer.categoryRepository.create(name, color)
                        }
                        appContainer.syncManager.pushOnly()
                    }
                }
            }
            .setNegativeButton(R.string.cancel, null)
            .create()

        dialog.window?.setSoftInputMode(WindowManager.LayoutParams.SOFT_INPUT_STATE_VISIBLE)
        return dialog
    }

    companion object {
        private const val ARG_CATEGORY_ID = "category_id"
        private const val ARG_CURRENT_NAME = "current_name"
        private const val ARG_CURRENT_COLOR = "current_color"

        fun newInstance(categoryId: Long?, currentName: String?, currentColor: String?): CategoryEditDialogFragment {
            return CategoryEditDialogFragment().apply {
                arguments = Bundle().apply {
                    if (categoryId != null) putLong(ARG_CATEGORY_ID, categoryId)
                    if (currentName != null) putString(ARG_CURRENT_NAME, currentName)
                    if (currentColor != null) putString(ARG_CURRENT_COLOR, currentColor)
                }
            }
        }
    }
}
