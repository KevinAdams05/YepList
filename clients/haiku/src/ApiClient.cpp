/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */


#include "ApiClient.h"

#include <DataIO.h>
#include <HttpFields.h>
#include <HttpRequest.h>
#include <HttpResult.h>
#include <HttpSession.h>
#include <Json.h>
#include <Message.h>
#include <Url.h>

#include <cstdio>

#include "JsonHelper.h"


using namespace BPrivate::Network;


// Shared HTTP session for connection pooling
static BHttpSession sHttpSession;


ApiClient::ApiClient()
	:
	fServerUrl("")
{
}


void
ApiClient::SetServerUrl(const BString& url)
{
	fServerUrl = url;
	// Strip trailing slash
	if (fServerUrl.EndsWith("/"))
		fServerUrl.Truncate(fServerUrl.Length() - 1);
}


// -- Lists --

void
ApiClient::GetLists(BMessenger target)
{
	RequestData* data = new RequestData();
	data->url = _BuildUrl("/api/lists");
	data->method = "GET";
	data->resultWhat = kMsgGetListsDone;
	data->target = target;

	thread_id thread = spawn_thread(_RequestThreadEntry,
		"api:get_lists", B_NORMAL_PRIORITY, data);
	if (thread >= B_OK)
		resume_thread(thread);
	else
		delete data;
}


void
ApiClient::CreateList(const char* name, int32 sortOrder,
	BMessenger target)
{
	BMessage fields;
	fields.AddString("name", name);
	fields.AddInt32("sortOrder", sortOrder);

	RequestData* data = new RequestData();
	data->url = _BuildUrl("/api/lists");
	data->method = "POST";
	data->body = JsonHelper::BuildJsonObject(fields);
	data->resultWhat = kMsgCreateListDone;
	data->target = target;

	thread_id thread = spawn_thread(_RequestThreadEntry,
		"api:create_list", B_NORMAL_PRIORITY, data);
	if (thread >= B_OK)
		resume_thread(thread);
	else
		delete data;
}


void
ApiClient::UpdateList(int64 listId, const char* name, int32 sortOrder,
	BMessenger target)
{
	BMessage fields;
	fields.AddString("name", name);
	fields.AddInt32("sortOrder", sortOrder);

	BString path;
	path.SetToFormat("/api/lists/%" B_PRId64, listId);

	RequestData* data = new RequestData();
	data->url = _BuildUrl(path.String());
	data->method = "PUT";
	data->body = JsonHelper::BuildJsonObject(fields);
	data->resultWhat = kMsgUpdateListDone;
	data->target = target;

	thread_id thread = spawn_thread(_RequestThreadEntry,
		"api:update_list", B_NORMAL_PRIORITY, data);
	if (thread >= B_OK)
		resume_thread(thread);
	else
		delete data;
}


void
ApiClient::DeleteList(int64 listId, BMessenger target)
{
	BString path;
	path.SetToFormat("/api/lists/%" B_PRId64, listId);

	RequestData* data = new RequestData();
	data->url = _BuildUrl(path.String());
	data->method = "DELETE";
	data->resultWhat = kMsgDeleteListDone;
	data->target = target;

	thread_id thread = spawn_thread(_RequestThreadEntry,
		"api:delete_list", B_NORMAL_PRIORITY, data);
	if (thread >= B_OK)
		resume_thread(thread);
	else
		delete data;
}


// -- Categories --

void
ApiClient::GetCategories(BMessenger target)
{
	RequestData* data = new RequestData();
	data->url = _BuildUrl("/api/categories");
	data->method = "GET";
	data->resultWhat = kMsgGetCategoriesDone;
	data->target = target;

	thread_id thread = spawn_thread(_RequestThreadEntry,
		"api:get_categories", B_NORMAL_PRIORITY, data);
	if (thread >= B_OK)
		resume_thread(thread);
	else
		delete data;
}


void
ApiClient::CreateCategory(const char* name, const char* color,
	BMessenger target)
{
	BMessage fields;
	fields.AddString("name", name);
	if (color != NULL && color[0] != '\0')
		fields.AddString("color", color);

	RequestData* data = new RequestData();
	data->url = _BuildUrl("/api/categories");
	data->method = "POST";
	data->body = JsonHelper::BuildJsonObject(fields);
	data->resultWhat = kMsgCreateCategoryDone;
	data->target = target;

	thread_id thread = spawn_thread(_RequestThreadEntry,
		"api:create_category", B_NORMAL_PRIORITY, data);
	if (thread >= B_OK)
		resume_thread(thread);
	else
		delete data;
}


void
ApiClient::UpdateCategory(int64 categoryId, const char* name,
	const char* color, BMessenger target)
{
	BMessage fields;
	fields.AddString("name", name);
	if (color != NULL && color[0] != '\0')
		fields.AddString("color", color);

	BString path;
	path.SetToFormat("/api/categories/%" B_PRId64, categoryId);

	RequestData* data = new RequestData();
	data->url = _BuildUrl(path.String());
	data->method = "PUT";
	data->body = JsonHelper::BuildJsonObject(fields);
	data->resultWhat = kMsgUpdateCategoryDone;
	data->target = target;

	thread_id thread = spawn_thread(_RequestThreadEntry,
		"api:update_category", B_NORMAL_PRIORITY, data);
	if (thread >= B_OK)
		resume_thread(thread);
	else
		delete data;
}


void
ApiClient::DeleteCategory(int64 categoryId, BMessenger target)
{
	BString path;
	path.SetToFormat("/api/categories/%" B_PRId64, categoryId);

	RequestData* data = new RequestData();
	data->url = _BuildUrl(path.String());
	data->method = "DELETE";
	data->resultWhat = kMsgDeleteCategoryDone;
	data->target = target;

	thread_id thread = spawn_thread(_RequestThreadEntry,
		"api:delete_category", B_NORMAL_PRIORITY, data);
	if (thread >= B_OK)
		resume_thread(thread);
	else
		delete data;
}


// -- Items --

void
ApiClient::GetItemsByList(int64 listId, BMessenger target)
{
	BString path;
	path.SetToFormat("/api/lists/%" B_PRId64 "/items", listId);

	RequestData* data = new RequestData();
	data->url = _BuildUrl(path.String());
	data->method = "GET";
	data->resultWhat = kMsgGetItemsDone;
	data->target = target;

	thread_id thread = spawn_thread(_RequestThreadEntry,
		"api:get_items", B_NORMAL_PRIORITY, data);
	if (thread >= B_OK)
		resume_thread(thread);
	else
		delete data;
}


void
ApiClient::CreateItem(int64 listId, const char* title,
	const char* notes, int64 categoryId, const char* dueDate,
	int32 sortOrder, BMessenger target)
{
	BMessage fields;
	fields.AddString("title", title);
	if (notes != NULL && notes[0] != '\0')
		fields.AddString("notes", notes);
	if (categoryId > 0)
		fields.AddInt64("categoryId", categoryId);
	if (dueDate != NULL && dueDate[0] != '\0')
		fields.AddString("dueDate", dueDate);
	fields.AddInt32("sortOrder", sortOrder);

	BString path;
	path.SetToFormat("/api/lists/%" B_PRId64 "/items", listId);

	RequestData* data = new RequestData();
	data->url = _BuildUrl(path.String());
	data->method = "POST";
	data->body = JsonHelper::BuildJsonObject(fields);
	data->resultWhat = kMsgCreateItemDone;
	data->target = target;

	thread_id thread = spawn_thread(_RequestThreadEntry,
		"api:create_item", B_NORMAL_PRIORITY, data);
	if (thread >= B_OK)
		resume_thread(thread);
	else
		delete data;
}


void
ApiClient::UpdateItem(int64 itemId, const char* title,
	const char* notes, int64 categoryId, int64 listId,
	bool isCompleted, const char* dueDate, int32 sortOrder,
	BMessenger target)
{
	BMessage fields;
	fields.AddString("title", title);
	if (notes != NULL && notes[0] != '\0')
		fields.AddString("notes", notes);
	if (categoryId > 0)
		fields.AddInt64("categoryId", categoryId);
	if (listId > 0)
		fields.AddInt64("listId", listId);
	fields.AddBool("isCompleted", isCompleted);
	if (dueDate != NULL && dueDate[0] != '\0')
		fields.AddString("dueDate", dueDate);
	fields.AddInt32("sortOrder", sortOrder);

	BString path;
	path.SetToFormat("/api/items/%" B_PRId64, itemId);

	RequestData* data = new RequestData();
	data->url = _BuildUrl(path.String());
	data->method = "PUT";
	data->body = JsonHelper::BuildJsonObject(fields);
	data->resultWhat = kMsgUpdateItemDone;
	data->target = target;

	thread_id thread = spawn_thread(_RequestThreadEntry,
		"api:update_item", B_NORMAL_PRIORITY, data);
	if (thread >= B_OK)
		resume_thread(thread);
	else
		delete data;
}


void
ApiClient::DeleteItem(int64 itemId, BMessenger target)
{
	BString path;
	path.SetToFormat("/api/items/%" B_PRId64, itemId);

	RequestData* data = new RequestData();
	data->url = _BuildUrl(path.String());
	data->method = "DELETE";
	data->resultWhat = kMsgDeleteItemDone;
	data->target = target;

	thread_id thread = spawn_thread(_RequestThreadEntry,
		"api:delete_item", B_NORMAL_PRIORITY, data);
	if (thread >= B_OK)
		resume_thread(thread);
	else
		delete data;
}


void
ApiClient::ToggleComplete(int64 itemId, bool isCompleted,
	BMessenger target)
{
	BMessage fields;
	fields.AddBool("isCompleted", isCompleted);

	BString path;
	path.SetToFormat("/api/items/%" B_PRId64 "/complete", itemId);

	RequestData* data = new RequestData();
	data->url = _BuildUrl(path.String());
	data->method = "PATCH";
	data->body = JsonHelper::BuildJsonObject(fields);
	data->resultWhat = kMsgToggleCompleteDone;
	data->target = target;

	thread_id thread = spawn_thread(_RequestThreadEntry,
		"api:toggle_complete", B_NORMAL_PRIORITY, data);
	if (thread >= B_OK)
		resume_thread(thread);
	else
		delete data;
}


void
ApiClient::ReorderItems(int64 listId,
	const std::vector<std::pair<int64, int32>>& items,
	BMessenger target)
{
	// Build JSON manually for the array structure
	BString body = "{\"items\":[";
	for (size_t i = 0; i < items.size(); i++) {
		if (i > 0)
			body += ",";
		BString entry;
		entry.SetToFormat("{\"itemId\":%" B_PRId64 ",\"sortOrder\":%"
			B_PRId32 "}", items[i].first, items[i].second);
		body += entry;
	}
	body += "]}";

	BString path;
	path.SetToFormat("/api/lists/%" B_PRId64 "/items/reorder", listId);

	RequestData* data = new RequestData();
	data->url = _BuildUrl(path.String());
	data->method = "PUT";
	data->body = body;
	data->resultWhat = kMsgReorderItemsDone;
	data->target = target;

	thread_id thread = spawn_thread(_RequestThreadEntry,
		"api:reorder", B_NORMAL_PRIORITY, data);
	if (thread >= B_OK)
		resume_thread(thread);
	else
		delete data;
}


// -- Sync --

void
ApiClient::Sync(const char* since, BMessenger target)
{
	BString path("/api/sync");
	if (since != NULL && since[0] != '\0') {
		path += "?since=";
		path += since;
	}

	RequestData* data = new RequestData();
	data->url = _BuildUrl(path.String());
	data->method = "GET";
	data->resultWhat = kMsgSyncDone;
	data->target = target;

	thread_id thread = spawn_thread(_RequestThreadEntry,
		"api:sync", B_NORMAL_PRIORITY, data);
	if (thread >= B_OK)
		resume_thread(thread);
	else
		delete data;
}


// -- Internal --

int32
ApiClient::_RequestThreadEntry(void* data)
{
	RequestData* requestData = static_cast<RequestData*>(data);
	_DoRequest(requestData);
	delete requestData;
	return 0;
}


void
ApiClient::_DoRequest(RequestData* data)
{
	BUrl url(data->url.String(), true);

	BHttpRequest request;
	request.SetUrl(url);

	// Set HTTP method
	if (data->method == "GET") {
		request.SetMethod(BHttpMethod::Get);
	} else if (data->method == "POST") {
		request.SetMethod(BHttpMethod::Post);
	} else if (data->method == "PUT") {
		request.SetMethod(BHttpMethod::Put);
	} else if (data->method == "DELETE") {
		request.SetMethod(BHttpMethod::Delete);
	} else if (data->method == "PATCH") {
		request.SetMethod(BHttpMethod(std::string_view("PATCH")));
	}

	// Set headers — Content-Type is reserved and added automatically
	// by SetRequestBody; adding it here would make SetFields() throw.
	BHttpFields fields;
	fields.AddField("Accept", "application/json");
	request.SetFields(fields);

	// Set request body if present
	if (data->body.Length() > 0) {
		auto bodyData = std::make_unique<BMallocIO>();
		bodyData->Write(data->body.String(), data->body.Length());
		bodyData->Seek(0, SEEK_SET);
		request.SetRequestBody(std::move(bodyData),
			BString("application/json"),
			std::optional<off_t>(data->body.Length()));
	}

	try {
		BHttpResult result = sHttpSession.Execute(
			std::move(request));

		// Block until response is complete
		const BHttpStatus& status = result.Status();
		const BHttpBody& responseBody = result.Body();

		BMessage reply(data->resultWhat);
		reply.AddInt32("status_code", status.code);

		if (status.code >= 200 && status.code < 300) {
			reply.AddBool("success", true);

			// Parse JSON response body if present
			if (responseBody.text.has_value()
				&& responseBody.text.value().Length() > 0) {
				BMessage jsonMsg;
				status_t parseStatus = BPrivate::BJson::Parse(
					responseBody.text.value(), jsonMsg);
				if (parseStatus == B_OK) {
					reply.AddMessage("data", &jsonMsg);
				}
			}
		} else {
			reply.AddBool("success", false);
			BString errorText;
			errorText.SetToFormat("HTTP %d: %s",
				status.code, status.text.String());
			reply.AddString("error", errorText.String());
		}

		data->target.SendMessage(&reply);

	} catch (...) {
		BMessage errorMsg(data->resultWhat);
		errorMsg.AddBool("success", false);
		errorMsg.AddString("error", "Network request failed");
		data->target.SendMessage(&errorMsg);
	}
}


BString
ApiClient::_BuildUrl(const char* path) const
{
	BString url(fServerUrl);
	url += path;
	return url;
}
