package com.yeplist.app.ui.settings

import android.app.Dialog
import android.os.Bundle
import android.view.WindowManager
import androidx.fragment.app.DialogFragment
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import com.google.android.material.textfield.TextInputEditText
import com.yeplist.app.R
import com.yeplist.app.YepListApp
import com.yeplist.app.di.AppContainer

class SettingsDialogFragment : DialogFragment() {

    override fun onCreateDialog(savedInstanceState: Bundle?): Dialog {
        val view = layoutInflater.inflate(R.layout.dialog_settings, null)
        val serverUrlEdit = view.findViewById<TextInputEditText>(R.id.serverUrlEditText)

        val container = (requireContext().applicationContext as YepListApp).container
        serverUrlEdit.setText(container.serverUrl)

        val dialog = MaterialAlertDialogBuilder(requireContext())
            .setTitle(R.string.settings)
            .setView(view)
            .setPositiveButton(R.string.save) { _, _ ->
                val url = serverUrlEdit.text.toString().trim()
                if (url.isNotEmpty()) {
                    container.prefs.edit()
                        .putString(AppContainer.PREF_SERVER_URL, url)
                        .apply()
                }
            }
            .setNegativeButton(R.string.cancel, null)
            .create()

        dialog.window?.setSoftInputMode(WindowManager.LayoutParams.SOFT_INPUT_STATE_VISIBLE)
        return dialog
    }
}
