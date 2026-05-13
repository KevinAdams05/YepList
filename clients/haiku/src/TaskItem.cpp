/*
 * Copyright 2026, Kevin Adams. All rights reserved.
 * Distributed under the terms of the MIT License.
 */


#include "TaskItem.h"

#include <Font.h>
#include <InterfaceDefs.h>
#include <View.h>


static const float kCheckboxSize = 12.0f;
static const float kPadding = 6.0f;


TaskItem::TaskItem(int64 itemId, const char* title, bool isCompleted,
	const char* categoryName, const char* categoryColor,
	const char* dueDate)
	:
	BListItem(),
	fItemId(itemId),
	fTitle(title),
	fIsCompleted(isCompleted),
	fCategoryName(categoryName),
	fCategoryColor(categoryColor),
	fDueDate(dueDate),
	fBaselineOffset(0.0f),
	fStrikeOffset(0.0f)
{
}


void
TaskItem::DrawItem(BView* owner, BRect frame, bool complete)
{
	rgb_color background;
	if (IsSelected()) {
		background = ui_color(B_LIST_SELECTED_BACKGROUND_COLOR);
	} else {
		background = ui_color(B_LIST_BACKGROUND_COLOR);
	}

	owner->SetHighColor(background);
	owner->FillRect(frame);

	rgb_color textColor;
	if (IsSelected()) {
		textColor = ui_color(B_LIST_SELECTED_ITEM_TEXT_COLOR);
	} else {
		textColor = ui_color(B_LIST_ITEM_TEXT_COLOR);
	}

	// Draw completion indicator
	float circleX = frame.left + kPadding + kCheckboxSize / 2.0f;
	float circleY = frame.top + (frame.Height() / 2.0f);

	if (fIsCompleted) {
		owner->SetHighColor(ui_color(B_CONTROL_MARK_COLOR));
		owner->FillEllipse(BPoint(circleX, circleY),
			kCheckboxSize / 2.0f, kCheckboxSize / 2.0f);
	} else {
		owner->SetHighColor(tint_color(background, B_DARKEN_2_TINT));
		owner->StrokeEllipse(BPoint(circleX, circleY),
			kCheckboxSize / 2.0f, kCheckboxSize / 2.0f);
	}

	// Draw title
	float textX = frame.left + kPadding * 2.0f + kCheckboxSize;
	float textY = frame.top + fBaselineOffset;

	rgb_color titleColor = textColor;
	if (fIsCompleted) {
		// Blend the text color halfway toward the background so completed
		// tasks are visibly de-emphasized but still readable.
		titleColor.red = static_cast<uint8>(
			(textColor.red + background.red) / 2);
		titleColor.green = static_cast<uint8>(
			(textColor.green + background.green) / 2);
		titleColor.blue = static_cast<uint8>(
			(textColor.blue + background.blue) / 2);
	}
	owner->SetHighColor(titleColor);
	owner->DrawString(fTitle.String(), BPoint(textX, textY));

	// Strikethrough completed items
	if (fIsCompleted) {
		float titleWidth = owner->StringWidth(fTitle.String());
		float strikeY = textY - fStrikeOffset;
		owner->StrokeLine(BPoint(textX, strikeY),
			BPoint(textX + titleWidth, strikeY));
	}

	// Draw due date on the right
	if (fDueDate.Length() > 0) {
		float dateWidth = owner->StringWidth(fDueDate.String());
		float dateX = frame.right - dateWidth - kPadding;
		rgb_color dateColor = titleColor;
		// Slightly lighter than the title for visual hierarchy
		dateColor.red = static_cast<uint8>(
			(dateColor.red + background.red) / 2);
		dateColor.green = static_cast<uint8>(
			(dateColor.green + background.green) / 2);
		dateColor.blue = static_cast<uint8>(
			(dateColor.blue + background.blue) / 2);
		owner->SetHighColor(dateColor);
		owner->DrawString(fDueDate.String(), BPoint(dateX, textY));
	}
}


void
TaskItem::Update(BView* owner, const BFont* font)
{
	BListItem::Update(owner, font);

	font_height fontHeight;
	font->GetHeight(&fontHeight);

	float itemHeight = ceilf(fontHeight.ascent + fontHeight.descent
		+ fontHeight.leading) + kPadding * 2.0f;

	if (itemHeight < kCheckboxSize + kPadding * 2.0f)
		itemHeight = kCheckboxSize + kPadding * 2.0f;

	SetHeight(itemHeight);
	fBaselineOffset = kPadding + fontHeight.ascent;
	// Position strikethrough about a third of the way up the ascent
	fStrikeOffset = fontHeight.ascent / 3.0f;
}


void
TaskItem::SetCompleted(bool completed)
{
	fIsCompleted = completed;
}
