package com.yeplist.app.ui.list

import android.app.Dialog
import android.os.Bundle
import android.view.WindowManager
import android.widget.EditText
import androidx.fragment.app.DialogFragment
import androidx.fragment.app.activityViewModels
import androidx.lifecycle.lifecycleScope
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import com.yeplist.app.R
import com.yeplist.app.ui.MainViewModel
import kotlinx.coroutines.launch

class ListDialogFragment : DialogFragment() {

    private val viewModel: MainViewModel by activityViewModels()

    override fun onCreateDialog(savedInstanceState: Bundle?): Dialog {
        val listId = arguments?.getLong(ARG_LIST_ID, -1)?.takeIf { it > 0 }
        val currentName = arguments?.getString(ARG_CURRENT_NAME) ?: ""
        val isEdit = listId != null

        val editText = EditText(requireContext()).apply {
            hint = getString(R.string.list_name)
            setText(currentName)
            setSingleLine()
            setPadding(64, 32, 64, 16)
        }

        val title = if (isEdit) R.string.rename_list else R.string.new_list

        val dialog = MaterialAlertDialogBuilder(requireContext())
            .setTitle(title)
            .setView(editText)
            .setPositiveButton(R.string.save) { _, _ ->
                val name = editText.text.toString().trim()
                if (name.isNotEmpty()) {
                    lifecycleScope.launch {
                        if (isEdit) {
                            val list = viewModel.lists.value.firstOrNull { it.listId == listId }
                            if (list != null) {
                                viewModel.updateList(listId!!, name, list.sortOrder)
                            }
                        } else {
                            val maxOrder = viewModel.lists.value.maxOfOrNull { it.sortOrder } ?: -1
                            val newList = viewModel.createList(name)
                            viewModel.selectList(newList.listId)
                        }
                    }
                }
            }
            .setNegativeButton(R.string.cancel, null)
            .create()

        dialog.window?.setSoftInputMode(WindowManager.LayoutParams.SOFT_INPUT_STATE_VISIBLE)
        return dialog
    }

    companion object {
        private const val ARG_LIST_ID = "list_id"
        private const val ARG_CURRENT_NAME = "current_name"

        fun newInstance(listId: Long?, currentName: String?): ListDialogFragment {
            return ListDialogFragment().apply {
                arguments = Bundle().apply {
                    if (listId != null) putLong(ARG_LIST_ID, listId)
                    if (currentName != null) putString(ARG_CURRENT_NAME, currentName)
                }
            }
        }
    }
}
