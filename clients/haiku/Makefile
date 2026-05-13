## Haiku Generic Makefile v2.6 ##

NAME = YepList
TYPE = APP
APP_MIME_SIG = application/x-vnd.YepList-ToDoList

SRCS = \
	src/App.cpp \
	src/MainWindow.cpp \
	src/ListSidebar.cpp \
	src/TaskListView.cpp \
	src/TaskItem.cpp \
	src/ListItem.cpp \
	src/TaskEditWindow.cpp \
	src/ListEditWindow.cpp \
	src/CategoryWindow.cpp \
	src/CategoryEditWindow.cpp \
	src/SettingsWindow.cpp \
	src/AboutWindow.cpp \
	src/ApiClient.cpp \
	src/JsonHelper.cpp \
	src/Models.cpp \
	src/Settings.cpp

RDEFS = resources/YepList.rdef

LIBS = be shared bnetapi localestub $(STDCPPLIBS)

SYSTEM_INCLUDE_PATHS = \
	$(shell findpaths -e B_FIND_PATH_HEADERS_DIRECTORY private/shared) \
	$(shell findpaths -e B_FIND_PATH_HEADERS_DIRECTORY private/netservices2)

OPTIMIZE := FULL

COMPILER_FLAGS = -std=c++17

include $(shell findpaths -r "makefile-engine" B_FIND_PATH_DEVELOP_DIRECTORY)
