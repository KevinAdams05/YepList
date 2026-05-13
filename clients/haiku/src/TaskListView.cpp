/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */


#include "TaskListView.h"

#include <LayoutBuilder.h>
#include <ListView.h>
#include <MenuItem.h>
#include <Messenger.h>
#include <PopUpMenu.h>
#include <ScrollView.h>
#include <StringView.h>
#include <TextControl.h>
#include <Window.h>

#include "MainWindow.h"
#include "TaskItem.h"


static const uint32 kMsgTaskInvoked		= 'tinv';
static const uint32 kMsgQuickAddEnter	= 'qaen';
static const uint32 kMsgTaskRightClick	= 'trck';
static const uint32 kMsgContextEdit		= 'ctxe';
static const uint32 kMsgContextDelete	= 'ctxd';


// Custom BListView that forwards right-clicks to the parent so we can
// show a context menu.
class TaskContextListView : public BListView {
public:
	TaskContextListView(const char* name)
		:
		BListView(name, B_SINGLE_SELECTION_LIST)
	{
	}

	virtual void MouseDown(BPoint where)
	{
		BMessage* message = Window()->CurrentMessage();
		int32 buttons = 0;
		if (message != NULL)
			message->FindInt32("buttons", &buttons);

		if (buttons & B_SECONDARY_MOUSE_BUTTON) {
			int32 index = IndexOf(where);
			if (index >= 0) {
				Select(index);
				BMessage rightClick(kMsgTaskRightClick);
				rightClick.AddPoint("screen_where",
					ConvertToScreen(where));
				BMessenger(Parent()).SendMessage(&rightClick);
			}
			return;
		}

		BListView::MouseDown(where);
	}
};


TaskListView::TaskListView()
	:
	BView("task_list_view", B_WILL_DRAW),
	fHeaderView(NULL),
	fListView(NULL),
	fQuickAddField(NULL)
{
	fHeaderView = new BStringView("header", "Select a list");
	BFont headerFont(be_bold_font);
	headerFont.SetSize(headerFont.Size() * 1.2f);
	fHeaderView->SetFont(&headerFont);

	fListView = new TaskContextListView("task_view");
	fListView->SetInvocationMessage(new BMessage(kMsgTaskInvoked));
	BScrollView* scrollView = new BScrollView("task_scroll",
		fListView, 0, false, true);

	fQuickAddField = new BTextControl("quick_add", NULL,
		"", new BMessage(kMsgQuickAddEnter));
	fQuickAddField->SetModificationMessage(NULL);

	BLayoutBuilder::Group<>(this, B_VERTICAL, 0.0f)
		.AddGroup(B_HORIZONTAL)
			.Add(fHeaderView)
			.SetInsets(B_USE_SMALL_SPACING)
		.End()
		.Add(scrollView, 1.0f)
		.AddGroup(B_HORIZONTAL)
			.Add(fQuickAddField)
			.SetInsets(B_USE_SMALL_SPACING)
		.End()
	;
}


void
TaskListView::AttachedToWindow()
{
	BView::AttachedToWindow();
	fQuickAddField->SetTarget(this);
	fListView->SetTarget(this);
}


void
TaskListView::MessageReceived(BMessage* message)
{
	switch (message->what) {
		case kMsgTaskInvoked:
		{
			// Double-click on a task — toggle completion
			int32 index = fListView->CurrentSelection();
			if (index < 0)
				break;

			TaskItem* item = dynamic_cast<TaskItem*>(
				fListView->ItemAt(index));
			if (item == NULL)
				break;

			BMessage toggleMsg(kMsgToggleComplete);
			toggleMsg.AddInt64("item_id", item->ItemId());
			toggleMsg.AddBool("completed", !item->IsCompleted());
			Window()->PostMessage(&toggleMsg);
			break;
		}

		case kMsgQuickAddEnter:
		{
			const char* text = fQuickAddField->Text();
			if (text == NULL || text[0] == '\0')
				break;

			BMessage quickAddMsg(kMsgQuickAdd);
			quickAddMsg.AddString("title", text);
			Window()->PostMessage(&quickAddMsg);

			fQuickAddField->SetText("");
			break;
		}

		case kMsgTaskRightClick:
		{
			BPoint screenWhere;
			if (message->FindPoint("screen_where",
				&screenWhere) != B_OK) {
				break;
			}

			int32 index = fListView->CurrentSelection();
			if (index < 0)
				break;

			BPopUpMenu* menu = new BPopUpMenu(
				"task_context", false, false);
			menu->AddItem(new BMenuItem("Edit Task" B_UTF8_ELLIPSIS,
				new BMessage(kMsgContextEdit)));
			menu->AddItem(new BMenuItem("Delete Task",
				new BMessage(kMsgContextDelete)));
			menu->SetTargetForItems(this);
			menu->Go(screenWhere, true, true, true);
			break;
		}

		case kMsgContextEdit:
			Window()->PostMessage(kMsgEditTask);
			break;

		case kMsgContextDelete:
			Window()->PostMessage(kMsgDeleteTask);
			break;

		default:
			BView::MessageReceived(message);
			break;
	}
}


void
TaskListView::UpdateItems(const std::vector<TodoItem>& items,
	const std::vector<Category>& categories)
{
	fListView->MakeEmpty();

	// Sort completed items to the bottom, then by sort_order.
	// Insertion sort is fine for the small list sizes we expect.
	std::vector<TodoItem> sorted(items);
	for (size_t i = 1; i < sorted.size(); i++) {
		TodoItem item = sorted[i];
		size_t j = i;
		while (j > 0) {
			const TodoItem& prev = sorted[j - 1];
			bool outOfOrder;
			if (prev.isCompleted != item.isCompleted)
				outOfOrder = prev.isCompleted;
			else
				outOfOrder = prev.sortOrder > item.sortOrder;
			if (!outOfOrder)
				break;
			sorted[j] = sorted[j - 1];
			j--;
		}
		sorted[j] = item;
	}

	for (const TodoItem& item : sorted) {
		// Look up category name and color
		BString categoryName;
		BString categoryColor;
		if (item.categoryId > 0) {
			for (const Category& cat : categories) {
				if (cat.categoryId == item.categoryId) {
					categoryName = cat.name;
					categoryColor = cat.color;
					break;
				}
			}
		}

		fListView->AddItem(new TaskItem(item.itemId,
			item.title.String(), item.isCompleted,
			categoryName.String(), categoryColor.String(),
			item.dueDate.String()));
	}
}


void
TaskListView::SetHeaderText(const char* text)
{
	if (fHeaderView != NULL)
		fHeaderView->SetText(text);
}


int32
TaskListView::SelectedIndex() const
{
	return fListView->CurrentSelection();
}


int64
TaskListView::SelectedItemId() const
{
	int32 index = fListView->CurrentSelection();
	if (index < 0)
		return -1;

	TaskItem* item = dynamic_cast<TaskItem*>(fListView->ItemAt(index));
	if (item == NULL)
		return -1;

	return item->ItemId();
}
