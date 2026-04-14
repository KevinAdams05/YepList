package com.yeplist.app.ui.settings

import android.app.Dialog
import android.os.Bundle
import android.view.WindowManager
import android.widget.ArrayAdapter
import android.widget.AutoCompleteTextView
import androidx.core.os.bundleOf
import androidx.fragment.app.DialogFragment
import androidx.fragment.app.setFragmentResult
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import com.google.android.material.textfield.TextInputEditText
import com.yeplist.app.R
import com.yeplist.app.YepListApp
import com.yeplist.app.di.AppContainer

class SettingsDialogFragment : DialogFragment() {

    companion object {
        const val RESULT_KEY = "settings_saved"
    }

    private data class SyncOption(val label: String, val seconds: Long)

    private val syncOptions = listOf(
        SyncOption("15 seconds", 15),
        SyncOption("30 seconds", 30),
        SyncOption("1 minute", 60),
        SyncOption("5 minutes", 300),
        SyncOption("15 minutes", 900)
    )

    override fun onCreateDialog(savedInstanceState: Bundle?): Dialog {
        val view = layoutInflater.inflate(R.layout.dialog_settings, null)
        val serverUrlEdit = view.findViewById<TextInputEditText>(R.id.serverUrlEditText)
        val syncIntervalDropdown = view.findViewById<AutoCompleteTextView>(R.id.syncIntervalDropdown)

        val container = (requireContext().applicationContext as YepListApp).container
        serverUrlEdit.setText(container.serverUrl)

        // Set up sync interval dropdown
        val labels = syncOptions.map { it.label }
        val adapter = ArrayAdapter(requireContext(), android.R.layout.simple_dropdown_item_1line, labels)
        syncIntervalDropdown.setAdapter(adapter)

        val currentInterval = container.prefs.getLong(
            AppContainer.PREF_SYNC_INTERVAL, AppContainer.DEFAULT_SYNC_INTERVAL
        )
        val currentOption = syncOptions.find { it.seconds == currentInterval } ?: syncOptions[1]
        syncIntervalDropdown.setText(currentOption.label, false)

        var selectedSeconds = currentOption.seconds
        syncIntervalDropdown.setOnItemClickListener { _, _, position, _ ->
            selectedSeconds = syncOptions[position].seconds
        }

        val dialog = MaterialAlertDialogBuilder(requireContext())
            .setTitle(R.string.settings)
            .setView(view)
            .setPositiveButton(R.string.save) { _, _ ->
                val url = serverUrlEdit.text.toString().trim()
                val editor = container.prefs.edit()
                if (url.isNotEmpty()) {
                    editor.putString(AppContainer.PREF_SERVER_URL, url)
                }
                editor.putLong(AppContainer.PREF_SYNC_INTERVAL, selectedSeconds)
                editor.apply()
                setFragmentResult(RESULT_KEY, bundleOf())
            }
            .setNegativeButton(R.string.cancel, null)
            .create()

        dialog.window?.setSoftInputMode(WindowManager.LayoutParams.SOFT_INPUT_STATE_VISIBLE)
        return dialog
    }
}
