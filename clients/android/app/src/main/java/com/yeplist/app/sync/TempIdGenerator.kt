package com.yeplist.app.sync

import java.util.concurrent.atomic.AtomicLong

object TempIdGenerator {

    private val counter = AtomicLong(0)

    fun next(): Long = counter.decrementAndGet()

    fun resetTo(startBelow: Long) {
        val start = if (startBelow >= 0) 0 else startBelow
        counter.set(start)
    }
}
