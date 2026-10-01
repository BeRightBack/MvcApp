using System.Globalization;
using System.Reflection;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace MvcApp.Infrastructure.Seeding;

internal static class SeedKey
{
    private const char Separator = '\u001F';

    public static IReadOnlyList<IProperty> KeyProperties<TEntity>(DbContext db)
        where TEntity : class
    {
        var entityType = db.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"{typeof(TEntity).Name} is not in the model.");

        var primaryKey = entityType.FindPrimaryKey()
            ?? throw new InvalidOperationException($"{typeof(TEntity).Name} has no primary key.");

        return primaryKey.Properties.ToList();
    }

    public static string TypeName<TEntity>() => typeof(TEntity).FullName ?? typeof(TEntity).Name;

    public static string Write<TEntity>(TEntity entity, IReadOnlyList<IProperty> keyProperties)
        where TEntity : class =>
        string.Join(Separator, keyProperties.Select(p => Format(p.GetGetter().GetClrValue(entity))));

    public static IReadOnlyList<string> Split(string key) => key.Split(Separator);

    public static Expression<Func<TEntity, bool>> Predicate<TEntity>(
        IReadOnlyList<IProperty> keyProperties,
        IReadOnlyList<string> keyValues)
        where TEntity : class
    {
        if (keyProperties.Count != keyValues.Count)
        {
            throw new InvalidOperationException(
                $"Seed key arity mismatch: {keyProperties.Count} key properties but {keyValues.Count} values.");
        }

        var parameter = Expression.Parameter(typeof(TEntity), "e");
        Expression? body = null;

        for (var i = 0; i < keyProperties.Count; i++)
        {
            var property = keyProperties[i];
            var value = Parse(property.ClrType, keyValues[i]);

            var comparison = Expression.Equal(
                Expression.MakeMemberAccess(parameter, Member(property)),
                Expression.Constant(value, property.ClrType));

            body = body is null ? comparison : Expression.AndAlso(body, comparison);
        }

        return Expression.Lambda<Func<TEntity, bool>>(body!, parameter);
    }

    private static MemberInfo Member(IProperty property) =>
        (MemberInfo?)property.PropertyInfo ?? property.FieldInfo
        ?? throw new InvalidOperationException(
            $"Property '{property.Name}' has no accessible member.");

    private static string Format(object? value) => value switch
    {
        null => string.Empty,
        byte[] bytes => Convert.ToBase64String(bytes),
        DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
    };

    public static object? Parse(Type clrType, string text)
    {
        var underlying = Nullable.GetUnderlyingType(clrType) ?? clrType;

        if (underlying == typeof(string))
            return text;

        if (underlying == typeof(Guid))
            return Guid.Parse(text);

        if (underlying == typeof(DateTime))
            return DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        if (underlying == typeof(DateTimeOffset))
            return DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        if (underlying == typeof(byte[]))
            return Convert.FromBase64String(text);

        if (underlying.IsEnum)
            return Enum.Parse(underlying, text);

        return Convert.ChangeType(text, underlying, CultureInfo.InvariantCulture);
    }
}
