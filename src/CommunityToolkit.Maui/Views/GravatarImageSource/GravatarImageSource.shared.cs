using System;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Views
{
	public class GravatarImageSource : ImageSource
	{
		public static readonly BindableProperty EmailProperty =
			BindableProperty.Create(nameof(Email), typeof(string), typeof(GravatarImageSource), null, propertyChanged: OnGravatarPropertyChanged);

		public static readonly BindableProperty SizeProperty =
			BindableProperty.Create(nameof(Size), typeof(int), typeof(GravatarImageSource), 40, propertyChanged: OnGravatarPropertyChanged);

		public static readonly BindableProperty DefaultProperty =
			BindableProperty.Create(nameof(Default), typeof(DefaultGravatar), typeof(GravatarImageSource), DefaultGravatar.MysteryPerson, propertyChanged: OnGravatarPropertyChanged);

		public static readonly BindableProperty CachingEnabledProperty =
			BindableProperty.Create(nameof(CachingEnabled), typeof(bool), typeof(GravatarImageSource), true, propertyChanged: OnGravatarPropertyChanged);

		public static readonly BindableProperty CacheValidityProperty =
			BindableProperty.Create(nameof(CacheValidity), typeof(TimeSpan), typeof(GravatarImageSource), TimeSpan.FromDays(7), propertyChanged: OnGravatarPropertyChanged);

		static void OnGravatarPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			if (bindable is GravatarImageSource gis)
				gis.OnSourceChanged();
		}

		public string? Email
		{
			get => (string?)GetValue(EmailProperty);
			set => SetValue(EmailProperty, value);
		}

		public int Size
		{
			get => (int)GetValue(SizeProperty);
			set => SetValue(SizeProperty, value);
		}

		public DefaultGravatar Default
		{
			get => (DefaultGravatar)GetValue(DefaultProperty);
			set => SetValue(DefaultProperty, value);
		}

		public bool CachingEnabled
		{
			get => (bool)GetValue(CachingEnabledProperty);
			set => SetValue(CachingEnabledProperty, value);
		}

		public TimeSpan CacheValidity
		{
			get => (TimeSpan)GetValue(CacheValidityProperty);
			set => SetValue(CacheValidityProperty, value);
		}
	}
}