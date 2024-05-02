using Android.Content;
using Android.Database;
using CommunityToolkit.Maui.ApplicationModel;
using Application = Android.App.Application;

namespace CommunityToolkit.Maui.Sample;

class SamsungBadgeProvider : IBadgeProvider
{
	const string contentStringUri = "content://com.sec.badge/apps?notify=true";
	static readonly string[] contentProjection = ["_id", "class"];
	static readonly string[] packageNameArray = new string[1];

	public void SetCount(uint count)
	{
		var contentUri = Android.Net.Uri.Parse(contentStringUri);
		if (contentUri is null)
		{
			return;
		}

		var packageName = Application.Context.PackageName;
		if (packageName is null)
		{
			return;
		}

		var componentName = Application.Context.PackageManager?.GetLaunchIntentForPackage(packageName)?.Component;
		if (componentName is null)
		{
			return;
		}

		var contentResolver = Application.Context.ContentResolver;
		ICursor? cursor = null;
		try
		{
			packageNameArray[0] = packageName;
			cursor = contentResolver?.Query(contentUri, contentProjection, "package=?", packageNameArray, null);
			if (cursor is not null)
			{
				var entryActivityExist = false;
				var selectionArgs = new string[1];
				while (cursor.MoveToNext())
				{
					var id = cursor.GetInt(0);
					selectionArgs[0] = id.ToString();
					var contentValues = GetContentValues(componentName, count, false);
					contentResolver?.Update(contentUri, contentValues, "_id=?", selectionArgs);
					if (componentName.ClassName.Equals(cursor.GetString(cursor.GetColumnIndex("class"))))
					{
						entryActivityExist = true;
					}
				}

				if (!entryActivityExist)
				{
					var contentValues = GetContentValues(componentName, count, true);
					contentResolver?.Insert(contentUri, contentValues);
				}
			}
		}
		finally
		{
			if (cursor?.IsClosed is false)
			{
				cursor.Close();
			}
		}
	}

	static ContentValues GetContentValues(ComponentName componentName, uint badgeCount, bool isInsert)
	{
		var contentValues = new ContentValues();
		if (isInsert)
		{
			contentValues.Put("package", componentName.PackageName);
			contentValues.Put("class", componentName.ClassName);
		}

		contentValues.Put("badgecount", badgeCount);
		return contentValues;
	}
}