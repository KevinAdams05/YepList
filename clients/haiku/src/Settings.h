/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */
#ifndef SETTINGS_H
#define SETTINGS_H


#include <String.h>


class Settings {
public:
	static Settings				Load();
			status_t			Save() const;

			const BString&		ServerUrl() const { return fServerUrl; }
			void				SetServerUrl(const BString& url);

			int64				DefaultListId() const
									{ return fDefaultListId; }
			void				SetDefaultListId(int64 id);

private:
	static status_t				_SettingsPath(BString& path);

			BString				fServerUrl;
			int64				fDefaultListId;
};


#endif	// SETTINGS_H
