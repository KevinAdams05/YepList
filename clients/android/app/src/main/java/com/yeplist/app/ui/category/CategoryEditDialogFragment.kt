package com.yeplist.app.ui.category

import android.app.Dialog
import android.graphics.Color
import android.graphics.drawable.GradientDrawable
import android.os.Bundle
import android.view.View
import android.view.WindowManager
import android.widget.GridLayout
import androidx.core.widget.addTextChangedListener
import androidx.fragment.app.DialogFragment
import androidx.lifecycle.lifecycleScope
import com.google.android.material.dialog.MaterialAlertDialogBuilder
import com.google.android.material.textfield.TextInputEditText
import com.yeplist.app.R
import com.yeplist.app.YepListApp
import kotlinx.coroutines.launch

class CategoryEditDialogFragment : DialogFragment() {

    private var selectedColor: String? = null
    private var selectedSwatchView: View? = null

    private val presetColors = listOf(
        "#F44336", "#E91E63", "#9C27B0", "#673AB7", "#3F51B5", "#2196F3",
        "#03A9F4", "#00BCD4", "#009688", "#4CAF50", "#8BC34A", "#CDDC39",
        "#FFEB3B", "#FFC107", "#FF9800", "#FF5722", "#795548", "#607D8B"
    )

    override fun onCreateDialog(savedInstanceState: Bundle?): Dialog {
        val categoryId = arguments?.getLong(ARG_CATEGORY_ID, -1)?.takeIf { it > 0 }
        val currentName = arguments?.getString(ARG_CURRENT_NAME) ?: ""
        val currentColor = arguments?.getString(ARG_CURRENT_COLOR) ?: ""
        val isEdit = categoryId != null

        selectedColor = currentColor.takeIf { it.isNotEmpty() }

        val view = layoutInflater.inflate(R.layout.dialog_category_edit, null)
        val nameEdit = view.findViewById<TextInputEditText>(R.id.categoryNameEditText)
        val colorGrid = view.findViewById<GridLayout>(R.id.colorGrid)
        val customColorEdit = view.findViewById<TextInputEditText>(R.id.customColorEditText)

        nameEdit.setText(currentName)

        // Build color swatches
        val swatchSize = (40 * resources.displayMetrics.density).toInt()
        val margin = (4 * resources.displayMetrics.density).toInt()

        for (hex in presetColors) {
            val swatch = View(requireContext())
            val params = GridLayout.LayoutParams().apply {
                width = swatchSize
                height = swatchSize
                setMargins(margin, margin, margin, margin)
            }
            swatch.layoutParams = params

            val bg = GradientDrawable()
            bg.shape = GradientDrawable.OVAL
            try {
                bg.setColor(Color.parseColor(hex))
            } catch (e: Exception) {
                bg.setColor(Color.GRAY)
            }
            bg.setStroke((2 * resources.displayMetrics.density).toInt(), Color.TRANSPARENT)
            swatch.background = bg

            if (selectedColor?.equals(hex, ignoreCase = true) == true) {
                highlightSwatch(swatch, bg)
                selectedSwatchView = swatch
            }

            swatch.setOnClickListener {
                // Clear previous selection
                selectedSwatchView?.let { prev ->
                    val prevBg = prev.background as? GradientDrawable
                    prevBg?.setStroke((2 * resources.displayMetrics.density).toInt(), Color.TRANSPARENT)
                }
                // Highlight new
                highlightSwatch(swatch, bg)
                selectedSwatchView = swatch
                selectedColor = hex
                customColorEdit.setText("")
            }

            colorGrid.addView(swatch)
        }

        // Custom color input
        if (currentColor.isNotEmpty() && presetColors.none { it.equals(currentColor, ignoreCase = true) }) {
            customColorEdit.setText(currentColor)
        }

        customColorEdit.addTextChangedListener { text ->
            val custom = text.toString().trim()
            if (custom.isNotEmpty()) {
                selectedColor = custom
                // Clear swatch selection
                selectedSwatchView?.let { prev ->
                    val prevBg = prev.background as? GradientDrawable
                    prevBg?.setStroke((2 * resources.displayMetrics.density).toInt(), Color.TRANSPARENT)
                }
                selectedSwatchView = null
            }
        }

        val title = if (isEdit) R.string.edit_category else R.string.add_category

        val dialog = MaterialAlertDialogBuilder(requireContext())
            .setTitle(title)
            .setView(view)
            .setPositiveButton(R.string.save) { _, _ ->
                val name = nameEdit.text.toString().trim()
                val color = selectedColor?.takeIf { it.isNotEmpty() }
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

    private fun highlightSwatch(view: View, bg: GradientDrawable) {
        bg.setStroke((3 * resources.displayMetrics.density).toInt(), Color.WHITE)
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
