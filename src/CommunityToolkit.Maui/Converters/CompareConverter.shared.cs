﻿using System;
using System.Globalization;
using CommunityToolkit.Maui.Extensions.Internals;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts an object that implements IComparable to an object or a boolean based on a comparison.
/// </summary>
public sealed class CompareConverter : CompareConverter<object>
{
}

/// <summary>
/// Converts an object that implements IComparable to an object or a boolean based on a comparison.
/// </summary>
public abstract class CompareConverter<TObject> : ValueConverterExtension, IValueConverter
{
    [Flags]
    public enum OperatorType
    {
        NotEqual = 0,
        Smaller = 1 << 0,
        SmallerOrEqual = 1 << 1,
        Equal = 1 << 2,
        Greater = 1 << 3,
        GreaterOrEqual = 1 << 4,
    }

    enum Modes
    {
        Boolean,
        Object
    }

    Modes mode;

    /// <summary>
    /// The comparing value.
    /// </summary>
    public IComparable? ComparingValue { get; set; }

    /// <summary>
    /// The comparison operator.
    /// </summary>
    public OperatorType ComparisonOperator { get; set; }

    /// <summary>
    /// The object that corresponds to True value.
    /// </summary>
    public TObject? TrueObject { get; set; }

    /// <summary>
    /// The object that corresponds to False value.
    /// </summary>
    public TObject? FalseObject { get; set; }

    /// <summary>
    /// Converts an object that implements IComparable to a specified object or a boolean based on a comparaison result.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="targetType">The type of the binding target property. This is not implemented.</param>
    /// <param name="parameter">Additional parameter for the converter to handle. This is not implemented.</param>
    /// <param name="culture">The culture to use in the converter.  This is not implemented.</param>
    /// <returns>The object assigned to <see cref="TrueObject"/> if (value <see cref="ComparisonOperator"/> <see cref="ComparingValue"/>) equals True and <see cref="TrueObject"/> is not null, if <see cref="TrueObject"/> is null it returns true, otherwise the value assigned to <see cref="FalseObject"/>, if no value is assigned then it returns false.</returns>
    public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (ComparingValue == null)
        {
            throw new ArgumentNullException(nameof(ComparingValue), $"{nameof(ComparingValue)} and {nameof(ComparisonOperator)} parameters shouldn't be null");
        }

        if (value is not IComparable)
        {
            throw new ArgumentException("is expected to implement IComparable interface.", nameof(value));
        }

        if (!Enum.IsDefined(typeof(OperatorType), ComparisonOperator))
        {
            throw new ArgumentOutOfRangeException($"is expected to be of type {nameof(OperatorType)}", nameof(ComparisonOperator));
        }

        if (!(TrueObject == null ^ FalseObject != null))
        {
            throw new ArgumentNullException(nameof(TrueObject), $"{nameof(TrueObject)} and {nameof(FalseObject)} should be either defined both or omitted both.");
        }

        if (TrueObject != null)
        {
            mode = Modes.Object;
        }

        var valueIComparable = (IComparable)value;
        var result = valueIComparable.CompareTo(ComparingValue);

        return ComparisonOperator switch
        {
            OperatorType.Smaller => EvaluateCondition(result < 0),
            OperatorType.SmallerOrEqual => EvaluateCondition(result <= 0),
            OperatorType.Equal => EvaluateCondition(result == 0),
            OperatorType.NotEqual => EvaluateCondition(result != 0),
            OperatorType.GreaterOrEqual => EvaluateCondition(result >= 0),
            OperatorType.Greater => EvaluateCondition(result > 0),
            _ => throw new ArgumentNullException(nameof(ComparisonOperator), $"\"{ComparisonOperator}\" is not supported."),
        };
    }

    object EvaluateCondition(bool comparaisonResult)
    {
        if (comparaisonResult)
        {
            return mode == Modes.Object ? TrueObject! : true;
        }
        else if (mode == Modes.Object)
        {
            return FalseObject!;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// This method is not implemented and will throw a <see cref="NotImplementedException"/>.
    /// </summary>
    /// <param name="value">N/A</param>
    /// <param name="targetType">N/A</param>
    /// <param name="parameter">N/A</param>
    /// <param name="culture">N/A</param>
    /// <returns>N/A</returns>
    public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}