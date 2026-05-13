/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */


#include "App.h"

#include "AboutWindow.h"
#include "MainWindow.h"


static const char* kAppSignature = "application/x-vnd.YepList-ToDoList";


YepListApp::YepListApp()
	:
	BApplication(kAppSignature),
	fMainWindow(NULL)
{
}


void
YepListApp::ReadyToRun()
{
	fMainWindow = new MainWindow();
	fMainWindow->Show();
}


void
YepListApp::AboutRequested()
{
	AboutWindow* window = new AboutWindow();
	window->Show();
}


int
main()
{
	YepListApp* app = new YepListApp();
	app->Run();
	delete app;
	return 0;
}
