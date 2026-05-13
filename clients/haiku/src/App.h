/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */
#ifndef APP_H
#define APP_H


#include <Application.h>


class MainWindow;


class YepListApp : public BApplication {
public:
								YepListApp();

	virtual void				ReadyToRun();
	virtual void				AboutRequested();

private:
			MainWindow*			fMainWindow;
};


#endif	// APP_H
