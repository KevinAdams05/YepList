/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */


#include "Settings.h"

#include <Directory.h>
#include <File.h>
#include <FindDirectory.h>
#include <Message.h>
#include <Path.h>


static const char* kSettingsFileName = "YepList/settings";
static const char* kDefaultServerUrl = "http://192.168.74.122:5000";


Settings
Settings::Load()
{
	Settings settings;
	settings.fServerUrl = kDefaultServerUrl;
	settings.fDefaultListId = -1;

	BString path;
	if (_SettingsPath(path) != B_OK)
		return settings;

	BFile file(path.String(), B_READ_ONLY);
	if (file.InitCheck() != B_OK)
		return settings;

	BMessage message;
	if (message.Unflatten(&file) != B_OK)
		return settings;

	const char* url;
	if (message.FindString("server_url", &url) == B_OK)
		settings.fServerUrl = url;

	message.FindInt64("default_list_id", &settings.fDefaultListId);

	BRect frame;
	if (message.FindRect("window_frame", &frame) == B_OK)
		settings.fWindowFrame = frame;

	return settings;
}


status_t
Settings::Save() const
{
	BString path;
	status_t status = _SettingsPath(path);
	if (status != B_OK)
		return status;

	// Ensure parent directory exists
	BPath dirPath(path.String());
	dirPath.GetParent(&dirPath);
	create_directory(dirPath.Path(), 0755);

	BFile file(path.String(),
		B_WRITE_ONLY | B_CREATE_FILE | B_ERASE_FILE);
	if (file.InitCheck() != B_OK)
		return file.InitCheck();

	BMessage message('yplS');
	message.AddString("server_url", fServerUrl.String());
	message.AddInt64("default_list_id", fDefaultListId);
	if (fWindowFrame.IsValid())
		message.AddRect("window_frame", fWindowFrame);

	return message.Flatten(&file);
}


void
Settings::SetServerUrl(const BString& url)
{
	fServerUrl = url;
}


void
Settings::SetDefaultListId(int64 id)
{
	fDefaultListId = id;
}


void
Settings::SetWindowFrame(BRect frame)
{
	fWindowFrame = frame;
}


status_t
Settings::_SettingsPath(BString& path)
{
	BPath settingsPath;
	status_t status = find_directory(B_USER_SETTINGS_DIRECTORY,
		&settingsPath);
	if (status != B_OK)
		return status;

	settingsPath.Append(kSettingsFileName);
	path = settingsPath.Path();
	return B_OK;
}
