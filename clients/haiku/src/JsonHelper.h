/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */
#ifndef JSON_HELPER_H
#define JSON_HELPER_H


#include <String.h>

#include <vector>


class BMessage;


class JsonHelper {
public:
	// Extract fields from a BMessage produced by BJson::Parse
	static BString			GetString(const BMessage& message,
								const char* name,
								const char* defaultValue = "");
	static int64			GetInt64(const BMessage& message,
								const char* name,
								int64 defaultValue = 0);
	static int32			GetInt32(const BMessage& message,
								const char* name,
								int32 defaultValue = 0);
	static bool				GetBool(const BMessage& message,
								const char* name,
								bool defaultValue = false);

	// Get a sub-message (nested object)
	static status_t			GetMessage(const BMessage& message,
								const char* name,
								BMessage& result);

	// Get array of sub-messages (nested array of objects)
	static std::vector<BMessage>
							GetMessageArray(const BMessage& message,
								const char* name);

	// Get array of int64 values
	static std::vector<int64>
							GetInt64Array(const BMessage& message,
								const char* name);

	// Build JSON string from key-value pairs in a BMessage
	static BString			BuildJsonObject(const BMessage& fields);

	// Parse a JSON string into a BMessage
	static status_t			Parse(const BString& json,
								BMessage& result);
};


#endif	// JSON_HELPER_H
