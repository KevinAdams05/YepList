package com.yeplist.app.util

import android.annotation.SuppressLint
import android.content.Context
import android.content.Intent
import android.net.Uri
import android.os.PowerManager
import android.provider.Settings

/**
 * Helpers for the battery-optimization exemption. On aggressive OEM skins
 * (notably newer One UI / Android 16), background work — the sync worker that
 * also refreshes home-screen widgets — gets killed unless the app is exempt
 * from battery optimization and excluded from "Sleeping apps". This lets us
 * detect that state and send the user to grant the exemption.
 */
object BatteryOptimizationHelper {

    fun isIgnoringBatteryOptimizations(context: Context): Boolean {
        val pm = context.getSystemService(Context.POWER_SERVICE) as PowerManager
        return pm.isIgnoringBatteryOptimizations(context.packageName)
    }

    // BatteryLife: this is a personal/sideloaded app where reliable background
    // sync is the whole point, so the direct request prompt is appropriate.
    @SuppressLint("BatteryLife")
    fun requestIgnore(context: Context): Boolean {
        val direct = Intent(
            Settings.ACTION_REQUEST_IGNORE_BATTERY_OPTIMIZATIONS,
            Uri.parse("package:${context.packageName}")
        ).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
        if (tryStart(context, direct)) {
            return true
        }

        // Fall back to the full battery-optimization settings list.
        val list = Intent(Settings.ACTION_IGNORE_BATTERY_OPTIMIZATION_SETTINGS)
            .addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
        return tryStart(context, list)
    }

    private fun tryStart(context: Context, intent: Intent): Boolean {
        return try {
            context.startActivity(intent)
            true
        } catch (e: Exception) {
            false
        }
    }
}
