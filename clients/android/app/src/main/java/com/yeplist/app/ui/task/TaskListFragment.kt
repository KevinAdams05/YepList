package com.yeplist.app.ui.task

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.view.inputmethod.EditorInfo
import androidx.appcompat.app.AlertDialog
import androidx.core.content.ContextCompat
import androidx.core.view.ViewCompat
import androidx.core.view.WindowInsetsCompat
import androidx.core.view.updatePadding
import androidx.fragment.app.Fragment
import androidx.fragment.app.activityViewModels
import androidx.fragment.app.viewModels
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.lifecycleScope
import androidx.lifecycle.repeatOnLifecycle
import androidx.recyclerview.widget.ItemTouchHelper
import androidx.recyclerview.widget.LinearLayoutManager
import com.google.android.material.snackbar.Snackbar
import com.yeplist.app.R
import com.yeplist.app.data.local.entity.TodoItemEntity
import com.yeplist.app.databinding.FragmentTaskListBinding
import com.yeplist.app.sync.SyncManager
import com.yeplist.app.ui.MainViewModel
import kotlinx.coroutines.launch
import java.util.Collections

class TaskListFragment : Fragment() {

    private var _binding: FragmentTaskListBinding? = null
    private val binding get() = _binding!!

    private val mainViewModel: MainViewModel by activityViewModels()
    private val taskViewModel: TaskListViewModel by viewModels()

    private lateinit var adapter: TaskAdapter
    private var itemTouchHelper: ItemTouchHelper? = null
    private val mutableItems = mutableListOf<TodoItemEntity>()

    override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?): View {
        _binding = FragmentTaskListBinding.inflate(inflater, container, false)
        return binding.root
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        // Handle edge-to-edge insets: pad bottom for nav bar + keyboard
        ViewCompat.setOnApplyWindowInsetsListener(view) { v, insets ->
            val imeInsets = insets.getInsets(WindowInsetsCompat.Type.ime())
            val navInsets = insets.getInsets(WindowInsetsCompat.Type.navigationBars())
            val bottomPadding = maxOf(imeInsets.bottom, navInsets.bottom)
            v.updatePadding(bottom = bottomPadding)
            insets
        }

        adapter = TaskAdapter(
            onToggleComplete = { item -> taskViewModel.toggleComplete(item) },
            onClick = { item -> showEditDialog(item.itemId) },
            onDragStarted = { holder -> itemTouchHelper?.startDrag(holder) },
            onLongClick = { item -> showDeleteConfirmation(item) }
        )

        binding.tasksRecyclerView.layoutManager = LinearLayoutManager(requireContext())
        binding.tasksRecyclerView.adapter = adapter

        // Drag-and-drop + swipe-to-delete
        val deleteIcon = ContextCompat.getDrawable(requireContext(), R.drawable.ic_delete)
        val dragCallback = TaskDragCallback(
            onMoved = { from, to ->
                Collections.swap(mutableItems, from, to)
                adapter.notifyItemMoved(from, to)
            },
            onDropped = {
                taskViewModel.reorder(mutableItems.toList())
            },
            onSwiped = { position ->
                val item = mutableItems[position]
                taskViewModel.deleteItems(listOf(item.itemId))
                Snackbar.make(binding.root, getString(R.string.task_deleted, item.title), Snackbar.LENGTH_SHORT).show()
            },
            deleteIcon = deleteIcon
        )
        itemTouchHelper = ItemTouchHelper(dragCallback)
        itemTouchHelper?.attachToRecyclerView(binding.tasksRecyclerView)

        // Quick-add
        binding.quickAddEditText.setOnEditorActionListener { textView, actionId, _ ->
            if (actionId == EditorInfo.IME_ACTION_DONE) {
                val title = textView.text.toString().trim()
                if (title.isNotEmpty()) {
                    taskViewModel.quickAdd(title)
                    textView.text = ""
                }
                true
            } else {
                false
            }
        }

        // FAB — open new task dialog
        binding.addTaskFab.setOnClickListener { showEditDialog(0) }

        // Observe data
        viewLifecycleOwner.lifecycleScope.launch {
            viewLifecycleOwner.repeatOnLifecycle(Lifecycle.State.STARTED) {
                launch {
                    mainViewModel.selectedListId.collect { listId ->
                        taskViewModel.setListId(listId)
                    }
                }
                launch {
                    taskViewModel.items.collect { items ->
                        val sorted = items.sortedBy { it.sortOrder }
                        mutableItems.clear()
                        mutableItems.addAll(sorted)
                        adapter.submitList(sorted.toList())

                        binding.emptyText.visibility = if (items.isEmpty()) View.VISIBLE else View.GONE
                        binding.tasksRecyclerView.visibility = if (items.isEmpty()) View.GONE else View.VISIBLE
                    }
                }
                launch {
                    mainViewModel.categories.collect { categories ->
                        adapter.categories = categories.associateBy { it.categoryId }
                    }
                }
                launch {
                    mainViewModel.syncState.collect { state ->
                        updateSyncStatus(state)
                    }
                }
            }
        }
    }

    private fun updateSyncStatus(state: SyncManager.SyncState) {
        val statusText = binding.syncStatusText
        when (state) {
            SyncManager.SyncState.IDLE -> statusText.text = ""
            SyncManager.SyncState.SYNCING -> {
                statusText.text = getString(R.string.syncing)
            }
            SyncManager.SyncState.SYNCED -> {
                statusText.text = getString(R.string.connected)
                statusText.postDelayed({ statusText.text = "" }, 3000)
            }
            SyncManager.SyncState.OFFLINE -> {
                statusText.text = getString(R.string.offline)
            }
            SyncManager.SyncState.ERROR -> {
                statusText.text = getString(R.string.offline)
            }
        }
    }

    fun deleteSelectedItems(selectedIds: Set<Long>) {
        taskViewModel.deleteItems(selectedIds.toList())
    }

    private fun showDeleteConfirmation(item: TodoItemEntity) {
        AlertDialog.Builder(requireContext())
            .setTitle(getString(R.string.delete_task))
            .setMessage(getString(R.string.delete_task_confirm_single))
            .setPositiveButton(getString(R.string.delete_task)) { _, _ ->
                taskViewModel.deleteItems(listOf(item.itemId))
            }
            .setNegativeButton(getString(R.string.cancel), null)
            .show()
    }

    private fun showEditDialog(itemId: Long) {
        TaskEditDialogFragment.newInstance(itemId).show(childFragmentManager, "task_edit")
    }

    override fun onDestroyView() {
        super.onDestroyView()
        _binding = null
    }
}
