using System;
using System.Windows.Input;
using CommunityToolkit.Maui.Helpers;
using CommunityToolkit.Maui.Views.Internals;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using static System.Math;

namespace CommunityToolkit.Maui.Views
{
	[ContentProperty(nameof(Content))]
	public class Expander : BaseTemplatedView<StackLayout>
	{
		const string expandAnimationName = nameof(expandAnimationName);

		const uint defaultAnimationLength = 250;

		readonly WeakEventManager tappedEventManager = new WeakEventManager();

		public event EventHandler Tapped
		{
			add => tappedEventManager.AddEventHandler(value);
			remove => tappedEventManager.RemoveEventHandler(value);
		}

		ContentView? contentHolder;

		GestureRecognizer? headerTapGestureRecognizer;

		DataTemplate? previousTemplate;

		double lastVisibleSize = -1;

		Size previousSize = new Size(-1, -1);

		bool shouldIgnoreContentSetting;

		readonly object contentSetLocker = new object();

		public static readonly BindableProperty HeaderProperty
			= BindableProperty.Create(nameof(Header), typeof(View), typeof(Expander), propertyChanged: OnHeaderPropertyChanged);

		public static readonly BindableProperty ContentProperty
			= BindableProperty.Create(nameof(Content), typeof(View), typeof(Expander), propertyChanged: OnContentPropertyChanged);

		public static readonly BindableProperty ContentTemplateProperty
			= BindableProperty.Create(nameof(ContentTemplate), typeof(DataTemplate), typeof(Expander), propertyChanged: OnContentTemplatePropertyChanged);

		public static readonly BindableProperty IsExpandedProperty
			= BindableProperty.Create(nameof(IsExpanded), typeof(bool), typeof(Expander), default(bool), BindingMode.TwoWay, propertyChanged: OnIsExpandedPropertyChanged);

		public static readonly BindableProperty DirectionProperty
			= BindableProperty.Create(nameof(Direction), typeof(ExpandDirection), typeof(Expander), default(ExpandDirection), propertyChanged: OnDirectionPropertyChanged);

		public static readonly BindableProperty TouchCaptureViewProperty
			= BindableProperty.Create(nameof(TouchCaptureView), typeof(View), typeof(Expander), propertyChanged: OnTouchCaptureViewPropertyChanged);

		public static readonly BindableProperty AnimationLengthProperty
			= BindableProperty.Create(nameof(AnimationLength), typeof(uint), typeof(Expander), defaultAnimationLength);

		public static readonly BindableProperty ExpandAnimationLengthProperty
			= BindableProperty.Create(nameof(ExpandAnimationLength), typeof(uint), typeof(Expander), uint.MaxValue);

		public static readonly BindableProperty CollapseAnimationLengthProperty
			= BindableProperty.Create(nameof(CollapseAnimationLength), typeof(uint), typeof(Expander), uint.MaxValue);

		public static readonly BindableProperty AnimationEasingProperty
			= BindableProperty.Create(nameof(AnimationEasing), typeof(Easing), typeof(Expander));

		public static readonly BindableProperty ExpandAnimationEasingProperty
			= BindableProperty.Create(nameof(ExpandAnimationEasing), typeof(Easing), typeof(Expander));

		public static readonly BindableProperty CollapseAnimationEasingProperty
			= BindableProperty.Create(nameof(CollapseAnimationEasing), typeof(Easing), typeof(Expander));

		public static readonly BindableProperty StateProperty
			= BindableProperty.Create(nameof(State), typeof(ExpandState), typeof(Expander), ExpandState.Collapsed, BindingMode.OneWayToSource);

		public static readonly BindableProperty CommandParameterProperty
			= BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(Expander));

		public static readonly BindableProperty CommandProperty
			= BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(Expander));

		public static readonly BindableProperty ForceUpdateSizeCommandProperty
			= BindableProperty.Create(nameof(ForceUpdateSizeCommand), typeof(ICommand), typeof(Expander), null, BindingMode.OneWayToSource, defaultValueCreator: GetDefaultForceUpdateSizeCommand);

		double Size => Direction.IsVertical()
			? Height
			: Width;

		double ContentSize => Direction.IsVertical()
			? contentHolder?.Height ?? throw new NullReferenceException()
			: contentHolder?.Width ?? throw new NullReferenceException();

		double ContentSizeRequest
		{
			get
			{
				var sizeRequest = Direction.IsVertical()
					? Content.HeightRequest
					: Content.WidthRequest;

				if (sizeRequest < 0 || Content is not Layout layout)
					return sizeRequest;

				return sizeRequest + (Direction.IsVertical()
					? layout.Padding.VerticalThickness
					: layout.Padding.HorizontalThickness);
			}

			set
			{
				_ = contentHolder ?? throw new NullReferenceException();

				if (Direction.IsVertical())
				{
					contentHolder.HeightRequest = value;
					return;
				}
				contentHolder.WidthRequest = value;
			}
		}

		double MeasuredContentSize => Direction.IsVertical()
			? contentHolder?.Measure(Width, double.PositiveInfinity).Request.Height ?? throw new NullReferenceException()
			: contentHolder?.Measure(double.PositiveInfinity, Height).Request.Width ?? throw new NullReferenceException();

		public View? Header
		{
			get => (View?)GetValue(HeaderProperty);
			set => SetValue(HeaderProperty, value);
		}

		public View Content
		{
			get => (View)GetValue(ContentProperty);
			set => SetValue(ContentProperty, value);
		}

		public DataTemplate? ContentTemplate
		{
			get => (DataTemplate?)GetValue(ContentTemplateProperty);
			set => SetValue(ContentTemplateProperty, value);
		}

		public bool IsExpanded
		{
			get => (bool)GetValue(IsExpandedProperty);
			set => SetValue(IsExpandedProperty, value);
		}

		public ExpandDirection Direction
		{
			get => (ExpandDirection)GetValue(DirectionProperty);
			set => SetValue(DirectionProperty, value);
		}

		public View? TouchCaptureView
		{
			get => (View?)GetValue(TouchCaptureViewProperty);
			set => SetValue(TouchCaptureViewProperty, value);
		}

		public uint AnimationLength
		{
			get => (uint)GetValue(AnimationLengthProperty);
			set => SetValue(AnimationLengthProperty, value);
		}

		public uint ExpandAnimationLength
		{
			get => (uint)GetValue(ExpandAnimationLengthProperty);
			set => SetValue(ExpandAnimationLengthProperty, value);
		}

		public uint CollapseAnimationLength
		{
			get => (uint)GetValue(CollapseAnimationLengthProperty);
			set => SetValue(CollapseAnimationLengthProperty, value);
		}

		public Easing AnimationEasing
		{
			get => (Easing)GetValue(AnimationEasingProperty);
			set => SetValue(AnimationEasingProperty, value);
		}

		public Easing ExpandAnimationEasing
		{
			get => (Easing)GetValue(ExpandAnimationEasingProperty);
			set => SetValue(ExpandAnimationEasingProperty, value);
		}

		public Easing CollapseAnimationEasing
		{
			get => (Easing)GetValue(CollapseAnimationEasingProperty);
			set => SetValue(CollapseAnimationEasingProperty, value);
		}

		public ExpandState State
		{
			get => (ExpandState)GetValue(StateProperty);
			set => SetValue(StateProperty, value);
		}

		public object? CommandParameter
		{
			get => GetValue(CommandParameterProperty);
			set => SetValue(CommandParameterProperty, value);
		}

		public ICommand? Command
		{
			get => (ICommand?)GetValue(CommandProperty);
			set => SetValue(CommandProperty, value);
		}

		public ICommand ForceUpdateSizeCommand
		{
			get => (ICommand)GetValue(ForceUpdateSizeCommandProperty);
			set => SetValue(ForceUpdateSizeCommandProperty, value);
		}

		public void ForceUpdateSize()
		{
			lastVisibleSize = -1;
			OnIsExpandedChanged();
		}

		protected override void OnControlInitialized(StackLayout control)
		{
			ForceUpdateSizeCommand = new Command(ForceUpdateSize);
			headerTapGestureRecognizer = new TapGestureRecognizer
			{
				CommandParameter = this,
				Command = new Command(parameter =>
				{
					var parent = ((View)parameter).Parent;
					while (parent != null && !(parent is Page))
					{
						if (parent is Expander ancestorExpander)
							ancestorExpander.ContentSizeRequest = -1;

						parent = parent.Parent;
					}
					IsExpanded = !IsExpanded;
					Command?.Execute(CommandParameter);
					OnTapped();
				})
			};
			control.Spacing = 0;
		}

		protected override void OnBindingContextChanged()
		{
			base.OnBindingContextChanged();
			lastVisibleSize = -1;
			SetContent(true, true);
		}

		protected override void OnSizeAllocated(double width, double height)
		{
			base.OnSizeAllocated(width, height);
			if ((Abs(width - previousSize.Width) >= double.Epsilon && Direction.IsVertical()) ||
				(Abs(height - previousSize.Height) >= double.Epsilon && !Direction.IsVertical()))
				ForceUpdateSize();

			previousSize = new Size(width, height);
		}

		static void OnHeaderPropertyChanged(BindableObject bindable, object oldValue, object newValue)
			=> ((Expander)bindable).OnHeaderPropertyChanged((View)oldValue);

		static void OnContentPropertyChanged(BindableObject bindable, object oldValue, object newValue)
			=> ((Expander)bindable).OnContentPropertyChanged();

		static void OnContentTemplatePropertyChanged(BindableObject bindable, object oldValue, object newValue)
			=> ((Expander)bindable).OnContentTemplatePropertyChanged();

		static void OnIsExpandedPropertyChanged(BindableObject bindable, object oldValue, object newValue)
			=> ((Expander)bindable).OnIsExpandedPropertyChanged();

		static void OnDirectionPropertyChanged(BindableObject bindable, object oldValue, object newValue)
			=> ((Expander)bindable).OnDirectionPropertyChanged((ExpandDirection)oldValue);

		static void OnTouchCaptureViewPropertyChanged(BindableObject bindable, object oldValue, object newValue)
			=> ((Expander)bindable).OnTouchCaptureViewPropertyChanged((View?)oldValue);

		static object GetDefaultForceUpdateSizeCommand(BindableObject bindable)
			=> new Command(((Expander)bindable).ForceUpdateSize);

		void OnHeaderPropertyChanged(View oldView)
			=> SetHeader(oldView);

		void OnContentPropertyChanged()
			=> SetContent();

		void OnContentTemplatePropertyChanged()
			=> SetContent(true);

		void OnIsExpandedPropertyChanged()
			=> SetContent(false);

		void OnDirectionPropertyChanged(ExpandDirection oldDirection)
			=> SetDirection(oldDirection);

		void OnTouchCaptureViewPropertyChanged(View? oldView)
			=> SetTouchCaptureView(oldView);

		void OnIsExpandedChanged(bool shouldIgnoreAnimation = false)
		{
			if (contentHolder == null || (!IsExpanded && !contentHolder.IsVisible))
				return;

			var isAnimationRunning = contentHolder.AnimationIsRunning(expandAnimationName);
			contentHolder.AbortAnimation(expandAnimationName);

			var startSize = contentHolder.IsVisible
				? Max(ContentSize, 0)
				: 0;

			if (IsExpanded)
				contentHolder.IsVisible = true;

			var endSize = ContentSizeRequest >= 0
				? ContentSizeRequest
				: lastVisibleSize;

			if (IsExpanded)
			{
				if (endSize <= 0)
				{
					ContentSizeRequest = -1;
					endSize = MeasuredContentSize;
					ContentSizeRequest = 0;
				}
			}
			else
			{
				lastVisibleSize = startSize = ContentSizeRequest >= 0
						? ContentSizeRequest
						: !isAnimationRunning
							? ContentSize
							: lastVisibleSize;
				endSize = 0;
			}

			InvokeAnimation(startSize, endSize, shouldIgnoreAnimation);
		}

		void SetHeader(View? oldHeader)
		{
			if (oldHeader != null)
			{
				Control?.Children.Remove(oldHeader);
			}

			if (Header != null)
			{
				if (Direction.IsRegularOrder())
					Control?.Children.Insert(0, Header);
				else
					Control?.Children.Add(Header);
			}

			SetTouchCaptureView(oldHeader);
		}

		void SetContent(bool isForceUpdate, bool shouldIgnoreAnimation = false, bool isForceContentReset = false)
		{
			if (IsExpanded && (Content == null || isForceUpdate || isForceContentReset))
			{
				lock (contentSetLocker)
				{
					shouldIgnoreContentSetting = true;

					var contentFromTemplate = CreateContent();
					if (contentFromTemplate != null)
						Content = contentFromTemplate;
					else if (isForceContentReset)
						SetContent();

					shouldIgnoreContentSetting = false;
				}
			}
			OnIsExpandedChanged(shouldIgnoreAnimation);
		}

		void SetContent()
		{
			if (contentHolder != null)
			{
				contentHolder.AbortAnimation(expandAnimationName);
				Control?.Children.Remove(contentHolder);
				contentHolder = null;
			}
			if (Content != null)
			{
				contentHolder = new ContentView
				{
					IsClippedToBounds = true,
					IsVisible = false,
					Content = Content
				};
				ContentSizeRequest = 0;

				if (Direction.IsRegularOrder())
					Control?.Children.Add(contentHolder);
				else
					Control?.Children.Insert(0, contentHolder);
			}

			if (!shouldIgnoreContentSetting)
				SetContent(true);
		}

		View? CreateContent()
		{
			var template = ContentTemplate;
			while (template is DataTemplateSelector selector)
				template = selector.SelectTemplate(BindingContext, this);

			if (template == previousTemplate && Content != null)
				return null;

			previousTemplate = template;
			return (View?)template?.CreateContent();
		}

		void SetDirection(ExpandDirection oldDirection)
		{
			if (oldDirection.IsVertical() == Direction.IsVertical())
			{
				SetHeader(Header);
				return;
			}

			if (Control != null)
			{
				Control.Orientation = Direction.IsVertical()
					? StackOrientation.Vertical
					: StackOrientation.Horizontal;
			}

			lastVisibleSize = -1;
			SetHeader(Header);
			SetContent(true, true, true);
		}

		void SetTouchCaptureView(View? oldView)
		{
			oldView?.GestureRecognizers.Remove(headerTapGestureRecognizer);
			TouchCaptureView?.GestureRecognizers?.Remove(headerTapGestureRecognizer);
			Header?.GestureRecognizers.Remove(headerTapGestureRecognizer);
			(TouchCaptureView ?? Header)?.GestureRecognizers.Add(headerTapGestureRecognizer);
		}

		void InvokeAnimation(double startSize, double endSize, bool shouldIgnoreAnimation)
		{
			State = IsExpanded
				? ExpandState.Expanding
				: ExpandState.Collapsing;

			if (shouldIgnoreAnimation || Size < 0)
			{
				State = IsExpanded
					? ExpandState.Expanded
					: ExpandState.Collapsed;
				ContentSizeRequest = endSize;

				_ = contentHolder ?? throw new NullReferenceException();
				contentHolder.IsVisible = IsExpanded;
				return;
			}

			var length = CollapseAnimationLength;
			var easing = CollapseAnimationEasing;
			if (IsExpanded)
			{
				length = ExpandAnimationLength;
				easing = ExpandAnimationEasing;
			}

			if (length == uint.MaxValue)
				length = AnimationLength;

			easing ??= AnimationEasing;

			if (lastVisibleSize > 0)
				length = (uint)(length * (Abs(endSize - startSize) / lastVisibleSize));

			length = Max(length, 1);

			new Animation(v => ContentSizeRequest = v, startSize, endSize)
				.Commit(contentHolder, expandAnimationName, 16, length, easing, (value, isInterrupted) =>
				{
					if (isInterrupted)
						return;

					if (!IsExpanded)
					{
						_ = contentHolder ?? throw new NullReferenceException();
						contentHolder.IsVisible = false;
						State = ExpandState.Collapsed;
						return;
					}
					State = ExpandState.Expanded;
				});
		}

		void OnTapped() => tappedEventManager.RaiseEvent(this, EventArgs.Empty, nameof(Tapped));
	}
}