package com.yeplist.app.ui

import android.content.res.Configuration
import android.os.Bundle
import android.view.Menu
import android.view.MenuItem
import android.widget.ImageView
import android.widget.TextView
import androidx.activity.viewModels
import androidx.appcompat.app.ActionBarDrawerToggle
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.lifecycleScope
import androidx.lifecycle.repeatOnLifecycle
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import com.yeplist.app.R
import com.yeplist.app.databinding.ActivityMainBinding
import com.yeplist.app.ui.category.CategoryManagerDialogFragment
import com.yeplist.app.ui.list.ListSidebarFragment
import com.yeplist.app.ui.settings.SettingsDialogFragment
import com.yeplist.app.ui.task.TaskListFragment
import kotlinx.coroutines.launch

class MainActivity : AppCompatActivity() {

    private lateinit var binding: ActivityMainBinding
    private val viewModel: MainViewModel by viewModels()

    private var sidebarFragment: ListSidebarFragment? = null
    private var taskListFragment: TaskListFragment? = null

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)

        setSupportActionBar(binding.toolbar)

        // Drawer toggle
        val toggle = ActionBarDrawerToggle(
            this, binding.drawerLayout, binding.toolbar,
            R.string.app_name, R.string.app_name
        )
        binding.drawerLayout.addDrawerListener(toggle)
        toggle.syncState()

        // Set up fragments
        if (savedInstanceState == null) {
            sidebarFragment = ListSidebarFragment().apply {
                onListSelected = { _ -> binding.drawerLayout.closeDrawers() }
                onManageCategoriesClicked = {
                    CategoryManagerDialogFragment().show(supportFragmentManager, "category_manager")
                }
            }
            taskListFragment = TaskListFragment()

            supportFragmentManager.beginTransaction()
                .replace(R.id.drawerContainer, sidebarFragment!!)
                .replace(R.id.fragmentContainer, taskListFragment!!)
                .commit()
        } else {
            sidebarFragment = supportFragmentManager.findFragmentById(R.id.drawerContainer) as? ListSidebarFragment
            taskListFragment = supportFragmentManager.findFragmentById(R.id.fragmentContainer) as? TaskListFragment
        }

        // Update toolbar title when selected list changes
        lifecycleScope.launch {
            repeatOnLifecycle(Lifecycle.State.STARTED) {
                launch {
                    viewModel.selectedListId.collect { listId ->
                        val lists = viewModel.lists.value
                        val selectedList = lists.firstOrNull { it.listId == listId }
                        supportActionBar?.title = selectedList?.name ?: getString(R.string.app_name)
                    }
                }
                launch {
                    viewModel.lists.collect { lists ->
                        val selectedList = lists.firstOrNull { it.listId == viewModel.selectedListId.value }
                        supportActionBar?.title = selectedList?.name ?: getString(R.string.app_name)
                    }
                }
            }
        }

        // Restart sync loop when settings are saved
        supportFragmentManager.setFragmentResultListener(
            SettingsDialogFragment.RESULT_KEY, this
        ) { _, _ ->
            viewModel.restartForegroundSync()
        }

        // Trigger initial sync
        viewModel.sync()
    }

    override fun onCreateOptionsMenu(menu: Menu): Boolean {
        menuInflater.inflate(R.menu.menu_main, menu)
        return true
    }

    override fun onOptionsItemSelected(item: MenuItem): Boolean {
        return when (item.itemId) {
            R.id.action_refresh -> {
                viewModel.fullRefresh()
                true
            }
            R.id.action_settings -> {
                SettingsDialogFragment().show(supportFragmentManager, "settings")
                true
            }
            R.id.action_about -> {
                showAboutDialog()
                true
            }
            else -> super.onOptionsItemSelected(item)
        }
    }

    private fun showAboutDialog() {
        val view = layoutInflater.inflate(R.layout.dialog_about, null)

        // Set logo based on dark/light mode
        val logoView = view.findViewById<ImageView>(R.id.aboutLogo)
        val isDark = (resources.configuration.uiMode and Configuration.UI_MODE_NIGHT_MASK) == Configuration.UI_MODE_NIGHT_YES
        logoView.setImageResource(if (isDark) R.drawable.logo_light else R.drawable.logo_dark)

        // Set version
        val versionText = view.findViewById<TextView>(R.id.aboutVersion)
        val versionName = packageManager.getPackageInfo(packageName, 0).versionName
        versionText.text = "Version $versionName"

        MaterialAlertDialogBuilder(this)
            .setView(view)
            .setPositiveButton(android.R.string.ok, null)
            .show()
    }
}
