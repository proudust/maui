#nullable enable

using System;
using System.ComponentModel;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using AndroidX.CardView.Widget;
using AndroidX.Core.View;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Graphics;
using AColor = Android.Graphics.Color;
using ARect = Android.Graphics.Rect;
using AView = Android.Views.View;
using Color = Microsoft.Maui.Graphics.Color;

namespace Microsoft.Maui.Controls.Handlers.Compatibility
{
	public class FrameRenderer : CardView, IPlatformViewHandler
	{
		public static IPropertyMapper<Frame, FrameRenderer> Mapper
			= new PropertyMapper<Frame, FrameRenderer>(ViewRenderer.VisualElementRendererMapper)
			{
				[Frame.HasShadowProperty.PropertyName] = (h, _) => h.UpdateShadow(),
				[VisualElement.BackgroundColorProperty.PropertyName] = (h, _) => h.UpdateBackgroundColor(),
				[VisualElement.BackgroundProperty.PropertyName] = (h, _) => h.UpdateBackground(),
				[Frame.CornerRadiusProperty.PropertyName] = (h, _) => h.UpdateCornerRadius(),
				[Frame.BorderColorProperty.PropertyName] = (h, _) => h.UpdateBorderColor(),
				[Microsoft.Maui.Controls.Compatibility.Layout.IsClippedToBoundsProperty.PropertyName] = (h, _) => h.UpdateClippedToBounds(),
				[Frame.ContentProperty.PropertyName] = (h, _) => h.UpdateContent(),

				// TODO NET8. These are all needed because the AndroidBatchMapper doesn't run via the mapper it's a manual call on ViewHandler
				// With NET8 we can move the BatchMapper call to the actual ViewHandler.Mapper. 
				// Because we most likely want to backport these fixes to NET7, I've just opted to add these manually for now on Frame
				[nameof(IView.AutomationId)] = (h, v) => ViewHandler.MapAutomationId(h, v),
				[nameof(IView.IsEnabled)] = (h, v) => ViewRenderer.VisualElementRendererMapper.UpdateProperty(h, v, nameof(IView.IsEnabled)),
				[nameof(IView.Visibility)] = (h, v) => ViewRenderer.VisualElementRendererMapper.UpdateProperty(h, v, nameof(IView.Visibility)),
				[nameof(IView.MinimumHeight)] = (h, v) => ViewRenderer.VisualElementRendererMapper.UpdateProperty(h, v, nameof(IView.MinimumHeight)),
				[nameof(IView.MinimumWidth)] = (h, v) => ViewRenderer.VisualElementRendererMapper.UpdateProperty(h, v, nameof(IView.MinimumWidth)),
				[nameof(IView.Opacity)] = (h, v) => ViewRenderer.VisualElementRendererMapper.UpdateProperty(h, v, nameof(IView.Opacity)),
				[nameof(IView.TranslationX)] = (h, v) => ViewRenderer.VisualElementRendererMapper.UpdateProperty(h, v, nameof(IView.TranslationX)),
				[nameof(IView.TranslationY)] = (h, v) => ViewRenderer.VisualElementRendererMapper.UpdateProperty(h, v, nameof(IView.TranslationY)),
				[nameof(IView.Scale)] = (h, v) => ViewRenderer.VisualElementRendererMapper.UpdateProperty(h, v, nameof(IView.Scale)),
				[nameof(IView.ScaleX)] = (h, v) => ViewRenderer.VisualElementRendererMapper.UpdateProperty(h, v, nameof(IView.ScaleX)),
				[nameof(IView.ScaleY)] = (h, v) => ViewRenderer.VisualElementRendererMapper.UpdateProperty(h, v, nameof(IView.ScaleY)),
				[nameof(IView.Rotation)] = (h, v) => ViewRenderer.VisualElementRendererMapper.UpdateProperty(h, v, nameof(IView.Rotation)),
				[nameof(IView.RotationX)] = (h, v) => ViewRenderer.VisualElementRendererMapper.UpdateProperty(h, v, nameof(IView.RotationX)),
				[nameof(IView.RotationY)] = (h, v) => ViewRenderer.VisualElementRendererMapper.UpdateProperty(h, v, nameof(IView.RotationY)),
				[nameof(IView.AnchorX)] = (h, v) => ViewRenderer.VisualElementRendererMapper.UpdateProperty(h, v, nameof(IView.AnchorX)),
				[nameof(IView.AnchorY)] = (h, v) => ViewRenderer.VisualElementRendererMapper.UpdateProperty(h, v, nameof(IView.AnchorY)),
			};

		public static CommandMapper<Frame, FrameRenderer> CommandMapper
			= new CommandMapper<Frame, FrameRenderer>(ViewRenderer.VisualElementRendererCommandMapper);


		float _defaultElevation = -1f;
		float _defaultCornerRadius = -1f;

		readonly ARect _clipRect = new();
		int _height;
		int _width;
		readonly Controls.Compatibility.Platform.Android.MotionEventHelper2 _motionEventHelper = new Controls.Compatibility.Platform.Android.MotionEventHelper2();
		bool _disposed;
		GradientDrawable? _backgroundDrawable;
		private IMauiContext? _mauiContext;
		ViewHandlerDelegator<Frame> _viewHandlerWrapper;
		Frame? _element;
		public event EventHandler<VisualElementChangedEventArgs>? ElementChanged;
		public event EventHandler<PropertyChangedEventArgs>? ElementPropertyChanged;

		public FrameRenderer(Context context) : this(context, Mapper)
		{
		}

		public FrameRenderer(Context context, IPropertyMapper mapper)
			: this(context, mapper, CommandMapper)
		{
		}

		public FrameRenderer(Context context, IPropertyMapper mapper, CommandMapper commandMapper) : base(context)
		{
			_viewHandlerWrapper = new ViewHandlerDelegator<Frame>(mapper, commandMapper, this);
		}

		protected CardView Control => this;

		protected Frame? Element
		{
			get { return _viewHandlerWrapper.Element ?? _element; }
			set
			{
				if (value != null)
					(this as IPlatformViewHandler).SetVirtualView(value);

				_element = value;
			}
		}

		Size IViewHandler.GetDesiredSize(double widthMeasureSpec, double heightMeasureSpec)
		{
			double minWidth = 20;
			if (Primitives.Dimension.IsExplicitSet(widthMeasureSpec) && !double.IsInfinity(widthMeasureSpec))
				minWidth = widthMeasureSpec;

			double minHeight = 20;
			if (Primitives.Dimension.IsExplicitSet(widthMeasureSpec) && !double.IsInfinity(heightMeasureSpec))
				minHeight = heightMeasureSpec;

			return VisualElementRenderer<Frame>.GetDesiredSize(this, widthMeasureSpec, heightMeasureSpec,
				new Size(minWidth, minHeight));
		}

		protected override void Dispose(bool disposing)
		{
			if (_disposed)
				return;

			_disposed = true;

			if (disposing)
			{
				if (Element != null)
				{
					Element.PropertyChanged -= OnElementPropertyChanged;
				}

				if (_backgroundDrawable != null)
				{
					_backgroundDrawable.Dispose();
					_backgroundDrawable = null;
				}

				while (ChildCount > 0)
				{
					var child = GetChildAt(0);
					child?.RemoveFromParent();
					child?.Dispose();
				}

				Element?.Handler?.DisconnectHandler();
			}

			base.Dispose(disposing);
		}

		protected virtual void OnElementChanged(ElementChangedEventArgs<Frame> e)
		{
			ElementChanged?.Invoke(this, new VisualElementChangedEventArgs(e.OldElement, e.NewElement));

			if (e.NewElement != null)
			{
				this.EnsureId();
				_backgroundDrawable = new GradientDrawable();
				_backgroundDrawable.SetShape(ShapeType.Rectangle);
				this.SetBackground(_backgroundDrawable);

				ElevationHelper.SetElevation(this, e.NewElement);
			}
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			if (Element?.Handler is IPlatformViewHandler pvh &&
				Element is IContentView cv)
			{
				var size = pvh.MeasureVirtualView(widthMeasureSpec, heightMeasureSpec, cv.CrossPlatformMeasure);
				SetMeasuredDimension((int)size.Width, (int)size.Height);
			}
			else
				base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
		}

		protected override void OnLayout(bool changed, int l, int t, int r, int b)
		{
			if (Element == null)
				return;


			if (Element.Handler is IPlatformViewHandler pvh &&
				Element is IContentView cv)
			{
				pvh.LayoutVirtualView(l, t, r, b, cv.CrossPlatformArrange);
			}
			else
				base.OnLayout(changed, l, t, r, b);

			if (Element.IsClippedToBounds)
			{
				_clipRect.Right = r - l;
				_clipRect.Bottom = b - t;
				ClipBounds = _clipRect;
			}
			else
			{
				ClipBounds = null;
			}
		}

		public override void Draw(Canvas? canvas)
		{
			Controls.Compatibility.Platform.Android.CanvasExtensions.ClipShape(canvas, Context, Element);

			base.Draw(canvas);
		}

		public override bool OnTouchEvent(MotionEvent? e)
		{
			if (base.OnTouchEvent(e))
			{
				return true;
			}

			return _motionEventHelper.HandleMotionEvent(Parent, e);
		}

		protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
		{
			base.OnSizeChanged(w, h, oldw, oldh);

			if (w != _width || h != _height)
				UpdateBackground();
		}

		protected virtual void OnElementPropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (this.IsDisposed())
			{
				return;
			}

			if (Element != null && e.PropertyName != null)
				_viewHandlerWrapper.UpdateProperty(e.PropertyName);

			ElementPropertyChanged?.Invoke(this, e);
			_motionEventHelper.UpdateElement(Element);
		}

		void UpdateClippedToBounds()
		{
			if (Element == null)
				return;

			var shouldClip = Element.IsClippedToBoundsSet(Element.CornerRadius > 0f);

			this.SetClipToOutline(shouldClip);
		}

		void UpdateBackgroundColor()
		{
			if (_disposed || Element == null || _backgroundDrawable == null)
				return;

			Color bgColor = Element.BackgroundColor;
			_backgroundDrawable.SetColor(bgColor?.ToPlatform() ?? AColor.White);
		}

		void UpdateBackground()
		{
			if (_disposed || Element == null)
				return;

			Brush background = Element.Background;

			if (Brush.IsNullOrEmpty(background))
			{
				if (_backgroundDrawable.UseGradients())
				{
					_backgroundDrawable?.Dispose();
					_backgroundDrawable = null;
					this.SetBackground(null);

					_backgroundDrawable = new GradientDrawable();
					_backgroundDrawable.SetShape(ShapeType.Rectangle);
					this.SetBackground(_backgroundDrawable);
				}

				UpdateBackgroundColor();
			}
			else
			{
				_height = Control.Height;
				_width = Control.Width;
				_backgroundDrawable.UpdateBackground(background, _height, _width);
			}
		}

		void UpdateBorderColor()
		{
			if (_disposed || Element == null || _backgroundDrawable == null)
				return;

			Color borderColor = Element.BorderColor;

			if (borderColor == null)
				_backgroundDrawable.SetStroke(0, AColor.Transparent);
			else
				_backgroundDrawable.SetStroke(3, borderColor.ToPlatform());
		}

		void UpdateShadow()
		{
			if (_disposed || Element == null)
				return;

			float elevation = _defaultElevation;

			if (elevation == -1f)
				_defaultElevation = elevation = CardElevation;

			if (Element.HasShadow)
				CardElevation = elevation;
			else
				CardElevation = 0f;
		}

		void UpdateCornerRadius()
		{
			if (_disposed || Element == null || _backgroundDrawable == null)
				return;

			if (_defaultCornerRadius == -1f)
			{
				_defaultCornerRadius = Radius;
			}

			float cornerRadius = Element.CornerRadius;

			if (cornerRadius == -1f)
				cornerRadius = _defaultCornerRadius;
			else
				cornerRadius = Context.ToPixels(cornerRadius);

			_backgroundDrawable.SetCornerRadius(cornerRadius);

			UpdateClippedToBounds();
		}

		void UpdateContent()
		{
			var content = Element?.Content;

			if (content == null || _mauiContext == null)
			{
				if (ChildCount == 1)
					RemoveViewAt(0);

				return;
			}

			var platformView = content.ToPlatform(_mauiContext);
			AddView(platformView);
		}

		#region IPlatformViewHandler
		bool IViewHandler.HasContainer { get => false; set { } }

		object? IViewHandler.ContainerView => null;

		IView? IViewHandler.VirtualView => Element;

		object IElementHandler.PlatformView => this;

		Maui.IElement? IElementHandler.VirtualView => Element;

		IMauiContext? IElementHandler.MauiContext => _mauiContext;

		AView IPlatformViewHandler.PlatformView => this;

		AView? IPlatformViewHandler.ContainerView => this;

		void IViewHandler.PlatformArrange(Graphics.Rect rect) =>
			this.PlatformArrangeHandler(rect);

		void IElementHandler.SetMauiContext(IMauiContext mauiContext) =>
			_mauiContext = mauiContext;

		void IElementHandler.SetVirtualView(Maui.IElement view)
		{
			_viewHandlerWrapper.SetVirtualView(view, OnElementChanged, false);
			_element = view as Frame;
		}

		void IElementHandler.UpdateValue(string property)
		{
			if (Element != null)
			{
				OnElementPropertyChanged(Element, new PropertyChangedEventArgs(property));
			}
		}

		void IElementHandler.Invoke(string command, object? args)
		{
			_viewHandlerWrapper.Invoke(command, args);
		}

		void IElementHandler.DisconnectHandler()
		{
			_viewHandlerWrapper.DisconnectHandler();
		}
		#endregion
	}
}
