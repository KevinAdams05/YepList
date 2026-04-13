package com.yeplist.app

import android.app.Application
import com.google.android.material.color.DynamicColors
import com.yeplist.app.debug.RemoteLogger
import com.yeplist.app.di.AppContainer

class YepListApp : Application() {

    lateinit var container: AppContainer
        private set

    override fun onCreate() {
        super.onCreate()
        DynamicColors.applyToActivitiesIfAvailable(this)
        container = AppContainer(this)
        RemoteLogger.init(container.serverUrl)
        RemoteLogger.i("App", "YepList started, server=${container.serverUrl}")
    }
}
