/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */


#include "JsonHelper.h"

#include <DataIO.h>
#include <Json.h>
#include <JsonTextWriter.h>
#include <Message.h>

#include <cstdio>


using BPrivate::BJson;
using BPrivate::BJsonTextWriter;


BString
JsonHelper::GetString(const BMessage& message, const char* name,
	const char* defaultValue)
{
	const char* value;
	if (message.FindString(name, &value) == B_OK)
		return BString(value);
	return BString(defaultValue);
}


int64
JsonHelper::GetInt64(const BMessage& message, const char* name,
	int64 defaultValue)
{
	int64 value;
	if (message.FindInt64(name, &value) == B_OK)
		return value;
	// BJson parses all numbers as doubles
	double doubleValue;
	if (message.FindDouble(name, &doubleValue) == B_OK)
		return static_cast<int64>(doubleValue);
	return defaultValue;
}


int32
JsonHelper::GetInt32(const BMessage& message, const char* name,
	int32 defaultValue)
{
	int32 value;
	if (message.FindInt32(name, &value) == B_OK)
		return value;
	double doubleValue;
	if (message.FindDouble(name, &doubleValue) == B_OK)
		return static_cast<int32>(doubleValue);
	return defaultValue;
}


bool
JsonHelper::GetBool(const BMessage& message, const char* name,
	bool defaultValue)
{
	bool value;
	if (message.FindBool(name, &value) == B_OK)
		return value;
	return defaultValue;
}


status_t
JsonHelper::GetMessage(const BMessage& message, const char* name,
	BMessage& result)
{
	return message.FindMessage(name, &result);
}


std::vector<BMessage>
JsonHelper::GetMessageArray(const BMessage& message, const char* name)
{
	std::vector<BMessage> result;

	BMessage arrayMsg;
	if (message.FindMessage(name, &arrayMsg) != B_OK)
		return result;

	// BJson arrays store items with keys "0", "1", "2", ...
	for (int32 i = 0; ; i++) {
		char key[16];
		snprintf(key, sizeof(key), "%" B_PRId32, i);

		BMessage itemMsg;
		if (arrayMsg.FindMessage(key, &itemMsg) != B_OK)
			break;

		result.push_back(itemMsg);
	}

	return result;
}


std::vector<int64>
JsonHelper::GetInt64Array(const BMessage& message, const char* name)
{
	std::vector<int64> result;

	BMessage arrayMsg;
	if (message.FindMessage(name, &arrayMsg) != B_OK)
		return result;

	for (int32 i = 0; ; i++) {
		char key[16];
		snprintf(key, sizeof(key), "%" B_PRId32, i);

		double value;
		if (arrayMsg.FindDouble(key, &value) == B_OK) {
			result.push_back(static_cast<int64>(value));
		} else {
			break;
		}
	}

	return result;
}


BString
JsonHelper::BuildJsonObject(const BMessage& fields)
{
	BMallocIO buffer;
	BJsonTextWriter writer(&buffer);

	writer.WriteObjectStart();

	char* name;
	type_code type;
	int32 count;
	for (int32 i = 0;
		fields.GetInfo(B_ANY_TYPE, i, &name, &type, &count) == B_OK;
		i++) {

		writer.WriteObjectName(name);

		switch (type) {
			case B_STRING_TYPE:
			{
				const char* value;
				if (fields.FindString(name, &value) == B_OK)
					writer.WriteString(value);
				else
					writer.WriteNull();
				break;
			}

			case B_INT64_TYPE:
			{
				int64 value;
				if (fields.FindInt64(name, &value) == B_OK)
					writer.WriteInteger(value);
				else
					writer.WriteNull();
				break;
			}

			case B_INT32_TYPE:
			{
				int32 value;
				if (fields.FindInt32(name, &value) == B_OK)
					writer.WriteInteger(value);
				else
					writer.WriteNull();
				break;
			}

			case B_BOOL_TYPE:
			{
				bool value;
				if (fields.FindBool(name, &value) == B_OK)
					writer.WriteBoolean(value);
				else
					writer.WriteNull();
				break;
			}

			case B_DOUBLE_TYPE:
			{
				double value;
				if (fields.FindDouble(name, &value) == B_OK)
					writer.WriteDouble(value);
				else
					writer.WriteNull();
				break;
			}

			default:
				writer.WriteNull();
				break;
		}
	}

	writer.WriteObjectEnd();
	writer.Complete();

	BString result;
	result.SetTo(static_cast<const char*>(buffer.Buffer()),
		buffer.BufferLength());
	return result;
}


status_t
JsonHelper::Parse(const BString& json, BMessage& result)
{
	return BJson::Parse(json, result);
}
