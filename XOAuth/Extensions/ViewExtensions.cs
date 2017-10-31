using System;
using AppKit;

namespace XOAuth.Extensions
{
	public static class ViewExtensions
	{
		public static void AddSubviewToCenter(this NSView view, NSView subview)
		{
			view.AddSubview(subview);
			view.AddConstraint(NSLayoutConstraint.Create(subview,
														 NSLayoutAttribute.CenterX,
														 NSLayoutRelation.Equal,
														 view,
														 NSLayoutAttribute.CenterX,
														 1f, 0f));
			view.AddConstraint(NSLayoutConstraint.Create(subview,
														 NSLayoutAttribute.CenterY,
														 NSLayoutRelation.Equal,
														 view,
														 NSLayoutAttribute.CenterY,
														 1f, 0f));
		}

		public static void AddSubviewFullSize(this NSView view, NSView subview)
		{
			view.AddSubview(subview);
			view.AddConstraint(NSLayoutConstraint.Create(subview,
														 NSLayoutAttribute.Top,
														 NSLayoutRelation.Equal,
														 view,
														 NSLayoutAttribute.Top,
														 1f, 0f));
			view.AddConstraint(NSLayoutConstraint.Create(subview,
														 NSLayoutAttribute.Bottom,
														 NSLayoutRelation.Equal,
														 view,
														 NSLayoutAttribute.Bottom,
														 1f, 0f));
			view.AddConstraint(NSLayoutConstraint.Create(subview,
														 NSLayoutAttribute.Left,
														 NSLayoutRelation.Equal,
														 view,
														 NSLayoutAttribute.Left,
														 1f, 0f));
			view.AddConstraint(NSLayoutConstraint.Create(subview,
														 NSLayoutAttribute.Right,
														 NSLayoutRelation.Equal,
														 view,
														 NSLayoutAttribute.Right,
														 1f, 0f));
		}
	}
}
