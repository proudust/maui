﻿using System.Collections.Generic;
using AView = Android.Views.View;
using AViewGroup = Android.Views.ViewGroup;

namespace Microsoft.Maui
{
	public static class ViewGroupExtensions
	{
		public static IEnumerable<T> GetChildrenOfType<T>(this AViewGroup viewGroup) where T : AView
		{
			for (var i = 0; i < viewGroup.ChildCount; i++)
			{
				AView? child = viewGroup.GetChildAt(i);

				if (child is T typedChild)
					yield return typedChild;

				if (child is AViewGroup)
				{
					IEnumerable<T>? myChildren = (child as AViewGroup)?.GetChildrenOfType<T>();
					if (myChildren != null)
						foreach (T nextChild in myChildren)
							yield return nextChild;
				}
			}
		}

		public static T? GetFirstChildOfType<T>(this AViewGroup viewGroup) where T : AView
		{
			for (var i = 0; i < viewGroup.ChildCount; i++)
			{
				AView? child = viewGroup.GetChildAt(i);

				if (child is T typedChild)
					return typedChild;

				if (child is AViewGroup vg)
				{
					var descendant = vg.GetFirstChildOfType<T>();
					if (descendant != null)
					{
						return descendant;
					}
				}
			}

			return null;
		}
	}
}