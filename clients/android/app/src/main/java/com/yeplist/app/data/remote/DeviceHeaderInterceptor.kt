package com.yeplist.app.data.remote

import okhttp3.Interceptor
import okhttp3.Response

/**
 * Adds device-identity headers to every request so the server can attribute
 * sync activity and deletions to this device (see DeviceTrackingMiddleware on
 * the backend). Header values are sanitised to printable ASCII because OkHttp
 * rejects control/non-ASCII characters in header values.
 */
class DeviceHeaderInterceptor(
    deviceId: String,
    deviceName: String,
    platform: String
) : Interceptor {

    private val deviceId = sanitize(deviceId)
    private val deviceName = sanitize(deviceName)
    private val platform = sanitize(platform)

    override fun intercept(chain: Interceptor.Chain): Response {
        val request = chain.request().newBuilder()
            .header("X-Device-Id", deviceId)
            .header("X-Device-Name", deviceName)
            .header("X-Device-Platform", platform)
            .build()
        return chain.proceed(request)
    }

    private fun sanitize(value: String): String {
        val cleaned = value.filter { it.code in 0x20..0x7E }.trim()
        return if (cleaned.isEmpty()) "unknown" else cleaned.take(100)
    }
}
