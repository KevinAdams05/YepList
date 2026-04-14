package com.yeplist.app.ui

import android.content.Intent
import android.content.res.Configuration
import android.os.Bundle
import android.view.Menu
import android.view.MenuItem
import android.graphics.Typeface
import android.text.SpannableStringBuilder
import android.text.Spanned
import android.text.style.RelativeSizeSpan
import android.text.style.StyleSpan
import android.view.View
import android.widget.ImageView
import android.widget.TextView
import androidx.activity.enableEdgeToEdge
import androidx.activity.viewModels
import androidx.appcompat.app.ActionBarDrawerToggle
import androidx.appcompat.app.AppCompatActivity
import androidx.core.view.ViewCompat
import androidx.core.view.WindowInsetsCompat
import androidx.core.view.updatePadding
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

    companion object {
        const val EXTRA_LIST_ID = "extra_list_id"
    }

    private lateinit var binding: ActivityMainBinding
    private val viewModel: MainViewModel by viewModels()

    private var sidebarFragment: ListSidebarFragment? = null
    private var taskListFragment: TaskListFragment? = null

    override fun onCreate(savedInstanceState: Bundle?) {
        enableEdgeToEdge()
        super.onCreate(savedInstanceState)
        binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)

        setSupportActionBar(binding.toolbar)

        // Handle edge-to-edge insets: pad toolbar for status bar, drawer for system bars
        ViewCompat.setOnApplyWindowInsetsListener(binding.toolbar) { view, insets ->
            val systemBars = insets.getInsets(WindowInsetsCompat.Type.systemBars())
            view.updatePadding(top = systemBars.top)
            insets
        }
        ViewCompat.setOnApplyWindowInsetsListener(binding.drawerContainer) { view, insets ->
            val systemBars = insets.getInsets(WindowInsetsCompat.Type.systemBars())
            view.updatePadding(top = systemBars.top, bottom = systemBars.bottom)
            insets
        }

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

        // Handle deep-link from widget
        handleWidgetIntent(intent)

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

    override fun onNewIntent(intent: Intent) {
        super.onNewIntent(intent)
        handleWidgetIntent(intent)
    }

    private fun handleWidgetIntent(intent: Intent?) {
        val listId = intent?.getLongExtra(EXTRA_LIST_ID, -1) ?: -1
        if (listId > 0) {
            viewModel.selectList(listId)
        }
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

        // Load changelog from assets
        val changelogText = view.findViewById<TextView>(R.id.changelogText)
        try {
            val changelog = assets.open("CHANGELOG.md").bufferedReader().readText()
            changelogText.text = parseMarkdown(changelog)
        } catch (_: Exception) {
            changelogText.text = "Changelog not found."
        }

        // Tab switching
        val librariesContent = view.findViewById<View>(R.id.librariesContent)
        val changelogContent = view.findViewById<View>(R.id.changelogContent)
        val tabLayout = view.findViewById<com.google.android.material.tabs.TabLayout>(R.id.aboutTabs)

        tabLayout.addTab(tabLayout.newTab().setText("Libraries"))
        tabLayout.addTab(tabLayout.newTab().setText("Changelog"))

        tabLayout.addOnTabSelectedListener(object : com.google.android.material.tabs.TabLayout.OnTabSelectedListener {
            override fun onTabSelected(tab: com.google.android.material.tabs.TabLayout.Tab) {
                when (tab.position) {
                    0 -> {
                        librariesContent.visibility = View.VISIBLE
                        changelogContent.visibility = View.GONE
                    }
                    1 -> {
                        librariesContent.visibility = View.GONE
                        changelogContent.visibility = View.VISIBLE
                    }
                }
            }
            override fun onTabUnselected(tab: com.google.android.material.tabs.TabLayout.Tab) {}
            override fun onTabReselected(tab: com.google.android.material.tabs.TabLayout.Tab) {}
        })

        MaterialAlertDialogBuilder(this)
            .setView(view)
            .setPositiveButton(android.R.string.ok, null)
            .show()
    }

    private fun parseMarkdown(markdown: String): SpannableStringBuilder {
        val sb = SpannableStringBuilder()

        for (line in markdown.lines()) {
            when {
                line.startsWith("### ") -> {
                    val start = sb.length
                    sb.append(line.removePrefix("### "))
                    sb.setSpan(StyleSpan(Typeface.BOLD), start, sb.length, Spanned.SPAN_EXCLUSIVE_EXCLUSIVE)
                    sb.setSpan(RelativeSizeSpan(1.1f), start, sb.length, Spanned.SPAN_EXCLUSIVE_EXCLUSIVE)
                    sb.append("\n")
                }
                line.startsWith("## ") -> {
                    val start = sb.length
                    sb.append(line.removePrefix("## "))
                    sb.setSpan(StyleSpan(Typeface.BOLD), start, sb.length, Spanned.SPAN_EXCLUSIVE_EXCLUSIVE)
                    sb.setSpan(RelativeSizeSpan(1.3f), start, sb.length, Spanned.SPAN_EXCLUSIVE_EXCLUSIVE)
                    sb.append("\n")
                }
                line.startsWith("# ") -> {
                    val start = sb.length
                    sb.append(line.removePrefix("# "))
                    sb.setSpan(StyleSpan(Typeface.BOLD), start, sb.length, Spanned.SPAN_EXCLUSIVE_EXCLUSIVE)
                    sb.setSpan(RelativeSizeSpan(1.5f), start, sb.length, Spanned.SPAN_EXCLUSIVE_EXCLUSIVE)
                    sb.append("\n")
                }
                line.startsWith("- ") -> {
                    sb.append("  \u2022 ")
                    appendWithInlineBold(sb, line.removePrefix("- "))
                    sb.append("\n")
                }
                else -> {
                    sb.append(line)
                    sb.append("\n")
                }
            }
        }

        return sb
    }

    private fun appendWithInlineBold(sb: SpannableStringBuilder, text: String) {
        var i = 0
        while (i < text.length) {
            val boldStart = text.indexOf("**", i)
            if (boldStart == -1) {
                sb.append(text.substring(i))
                break
            }
            if (boldStart > i) {
                sb.append(text.substring(i, boldStart))
            }
            val boldEnd = text.indexOf("**", boldStart + 2)
            if (boldEnd == -1) {
                sb.append(text.substring(boldStart))
                break
            }
            val start = sb.length
            sb.append(text.substring(boldStart + 2, boldEnd))
            sb.setSpan(StyleSpan(Typeface.BOLD), start, sb.length, Spanned.SPAN_EXCLUSIVE_EXCLUSIVE)
            i = boldEnd + 2
        }
    }
}
