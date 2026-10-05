using FluentValidation.Results;
using ST.LiquorTNT.Business.Common.Exceptions;

namespace ST.LiquorTNT.Business.Common;

public static class ValidationExtensions
{
    /// <summary>
    /// Turns a failed FluentValidation result into a 400 <see cref="ValidationException"/>,
    /// keyed by camelCase field name so the frontend can place each message on its form field.
    /// </summary>
    public static void EnsureValid(this ValidationResult result)
    {
        if (result.IsValid)
        {
            return;
        }

        var errors = result.Errors
            .GroupBy(e => ToCamelCase(e.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        throw new ValidationException(errors);
    }

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
