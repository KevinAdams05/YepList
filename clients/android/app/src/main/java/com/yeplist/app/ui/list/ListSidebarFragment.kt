package com.yeplist.app.ui.list

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.PopupMenu
import androidx.fragment.app.Fragment
import androidx.fragment.app.activityViewModels
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.lifecycleScope
import androidx.lifecycle.repeatOnLifecycle
import androidx.recyclerview.widget.LinearLayoutManager
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import com.yeplist.app.R
import com.yeplist.app.data.local.entity.TodoListEntity
import com.yeplist.app.databinding.FragmentListSidebarBinding
import com.yeplist.app.ui.MainViewModel
import kotlinx.coroutines.launch

class ListSidebarFragment : Fragment() {

    private var _binding: FragmentListSidebarBinding? = null
    private val binding get() = _binding!!

    private val viewModel: MainViewModel by activityViewModels()
    private lateinit var adapter: ListSidebarAdapter

    var onListSelected: ((Long) -> Unit)? = null
    var onManageCategoriesClicked: (() -> Unit)? = null

    override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?): View {
        _binding = FragmentListSidebarBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        adapter = ListSidebarAdapter(
            onClick = { list ->
                viewModel.selectList(list.listId)
                onListSelected?.invoke(list.listId)
            },
            onLongClick = { list, anchor -> showContextMenu(list, anchor) }
        )

        binding.listsRecyclerView.layoutManager = LinearLayoutManager(requireContext())
        binding.listsRecyclerView.adapter = adapter

        binding.newListButton.setOnClickListener { showNewListDialog() }
        binding.manageCategoriesButton.setOnClickListener { onManageCategoriesClicked?.invoke() }

        viewLifecycleOwner.lifecycleScope.launch {
            viewLifecycleOwner.repeatOnLifecycle(Lifecycle.State.STARTED) {
                launch {
                    viewModel.lists.collect { lists ->
                        val sorted = lists.sortedWith(compareBy({ it.sortOrder }, { it.name }))
                        adapter.submitList(sorted)

                        // Auto-select first list if nothing selected
                        if (viewModel.selectedListId.value == null && sorted.isNotEmpty()) {
                            val defaultId = viewModel.getDefaultListId()
                            val target = sorted.firstOrNull { it.listId == defaultId } ?: sorted.first()
                            viewModel.selectList(target.listId)
                        }
                    }
                }
                launch {
                    viewModel.selectedListId.collect { id ->
                        adapter.selectedListId = id
                    }
                }
            }
        }

        adapter.defaultListId = viewModel.getDefaultListId()
    }

    private fun showContextMenu(list: TodoListEntity, anchor: View) {
        val popup = PopupMenu(requireContext(), anchor)
        popup.menuInflater.inflate(R.menu.menu_list_context, popup.menu)

        val isDefault = list.listId == viewModel.getDefaultListId()
        popup.menu.findItem(R.id.action_set_default)?.isVisible = !isDefault
        popup.menu.findItem(R.id.action_clear_default)?.isVisible = isDefault

        popup.setOnMenuItemClickListener { item ->
            when (item.itemId) {
                R.id.action_rename -> {
                    showRenameDialog(list)
                    true
                }
                R.id.action_delete -> {
                    showDeleteConfirmation(list)
                    true
                }
                R.id.action_set_default -> {
                    viewModel.setDefaultListId(list.listId)
                    adapter.defaultListId = list.listId
                    true
                }
                R.id.action_clear_default -> {
                    viewModel.clearDefaultListId()
                    adapter.defaultListId = -1
                    true
                }
                else -> false
            }
        }
        popup.show()
    }

    private fun showNewListDialog() {
        ListDialogFragment.newInstance(null, null).show(childFragmentManager, "new_list")
    }

    private fun showRenameDialog(list: TodoListEntity) {
        ListDialogFragment.newInstance(list.listId, list.name).show(childFragmentManager, "rename_list")
    }

    private fun showDeleteConfirmation(list: TodoListEntity) {
        MaterialAlertDialogBuilder(requireContext())
            .setMessage(getString(R.string.delete_list_confirm, list.name))
            .setPositiveButton(R.string.delete_list) { _, _ ->
                viewLifecycleOwner.lifecycleScope.launch {
                    viewModel.deleteList(list.listId)
                }
            }
            .setNegativeButton(R.string.cancel, null)
            .show()
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}
