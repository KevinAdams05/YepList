package com.yeplist.app.ui.task

import android.app.Dialog
import android.os.Bundle
import android.view.WindowManager
import android.widget.ArrayAdapter
import android.widget.AutoCompleteTextView
import androidx.fragment.app.DialogFragment
import androidx.fragment.app.activityViewModels
import androidx.fragment.app.viewModels
import androidx.lifecycle.lifecycleScope
import com.google.android.material.datepicker.MaterialDatePicker
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import com.google.android.material.textfield.TextInputEditText
import com.yeplist.app.R
import com.yeplist.app.data.local.entity.CategoryEntity
import com.yeplist.app.ui.MainViewModel
import kotlinx.coroutines.launch
import java.time.Instant
import java.time.LocalDate
import java.time.ZoneOffset
import java.time.format.DateTimeFormatter
import java.time.format.FormatStyle

class TaskEditDialogFragment : DialogFragment() {

    private val mainViewModel: MainViewModel by activityViewModels()
    private val taskViewModel: TaskListViewModel by viewModels({ requireParentFragment() })

    private var selectedCategoryId: Long? = null
    private var selectedDueDate: String? = null
    private var categories: List<CategoryEntity> = emptyList()

    override fun onCreateDialog(savedInstanceState: Bundle?): Dialog {
        val itemId = arguments?.getLong(ARG_ITEM_ID, 0) ?: 0
        val isEdit = itemId != 0L

        val view = layoutInflater.inflate(R.layout.dialog_task_edit, null)
        val titleEdit = view.findViewById<TextInputEditText>(R.id.titleEditText)
        val notesEdit = view.findViewById<TextInputEditText>(R.id.notesEditText)
        val categoryDropdown = view.findViewById<AutoCompleteTextView>(R.id.categoryDropdown)
        val dueDateEdit = view.findViewById<TextInputEditText>(R.id.dueDateEditText)

        categories = mainViewModel.categories.value

        // Populate category dropdown
        val categoryNames = mutableListOf(getString(R.string.none))
        categoryNames.addAll(categories.map { it.name })
        val categoryAdapter = ArrayAdapter(requireContext(), android.R.layout.simple_dropdown_item_1line, categoryNames)
        categoryDropdown.setAdapter(categoryAdapter)
        categoryDropdown.setOnItemClickListener { _, _, position, _ ->
            selectedCategoryId = if (position == 0) null else categories[position - 1].categoryId
        }

        // Due date picker
        dueDateEdit.setOnClickListener {
            val picker = MaterialDatePicker.Builder.datePicker()
                .setTitleText(getString(R.string.due_date))
                .apply {
                    if (selectedDueDate != null) {
                        val millis = LocalDate.parse(selectedDueDate)
                            .atStartOfDay(ZoneOffset.UTC)
                            .toInstant()
                            .toEpochMilli()
                        setSelection(millis)
                    }
                }
                .build()
            picker.addOnPositiveButtonClickListener { millis ->
                val date = Instant.ofEpochMilli(millis).atZone(ZoneOffset.UTC).toLocalDate()
                selectedDueDate = date.toString()
                dueDateEdit.setText(date.format(DateTimeFormatter.ofLocalizedDate(FormatStyle.MEDIUM)))
            }
            picker.show(childFragmentManager, "date_picker")
        }

        // Populate fields for edit mode
        if (isEdit) {
            val item = taskViewModel.items.value.firstOrNull { it.itemId == itemId }
            if (item != null) {
                titleEdit.setText(item.title)
                notesEdit.setText(item.notes ?: "")
                selectedCategoryId = item.categoryId
                selectedDueDate = item.dueDate

                val catIndex = categories.indexOfFirst { it.categoryId == item.categoryId }
                if (catIndex >= 0) {
                    categoryDropdown.setText(categories[catIndex].name, false)
                } else {
                    categoryDropdown.setText(getString(R.string.none), false)
                }

                if (item.dueDate != null) {
                    try {
                        val date = LocalDate.parse(item.dueDate)
                        dueDateEdit.setText(date.format(DateTimeFormatter.ofLocalizedDate(FormatStyle.MEDIUM)))
                    } catch (e: Exception) {
                        // Invalid date format
                    }
                }
            }
        }

        val dialogTitle = if (isEdit) R.string.edit_task else R.string.add_task

        val dialog = MaterialAlertDialogBuilder(requireContext())
            .setTitle(dialogTitle)
            .setView(view)
            .setPositiveButton(R.string.save) { _, _ ->
                val title = titleEdit.text.toString().trim()
                if (title.isNotEmpty()) {
                    lifecycleScope.launch {
                        if (isEdit) {
                            val item = taskViewModel.items.value.firstOrNull { it.itemId == itemId }
                            if (item != null) {
                                taskViewModel.updateItem(
                                    itemId = itemId,
                                    title = title,
                                    notes = notesEdit.text.toString().takeIf { it.isNotBlank() },
                                    categoryId = selectedCategoryId,
                                    isCompleted = item.isCompleted,
                                    dueDate = selectedDueDate,
                                    sortOrder = item.sortOrder
                                )
                            }
                        } else {
                            val maxOrder = taskViewModel.items.value.maxOfOrNull { it.sortOrder } ?: -1
                            taskViewModel.createItem(
                                title = title,
                                notes = notesEdit.text.toString().takeIf { it.isNotBlank() },
                                categoryId = selectedCategoryId,
                                dueDate = selectedDueDate,
                                sortOrder = maxOrder + 1
                            )
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
        private const val ARG_ITEM_ID = "item_id"

        fun newInstance(itemId: Long): TaskEditDialogFragment {
            return TaskEditDialogFragment().apply {
                arguments = Bundle().apply {
                    putLong(ARG_ITEM_ID, itemId)
                }
            }
        }
    }
}
