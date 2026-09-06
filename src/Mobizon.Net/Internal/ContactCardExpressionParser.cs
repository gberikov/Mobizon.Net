using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Mobizon.Contracts;

namespace Mobizon.Net.Internal
{
    internal static class ContactCardExpressionParser
    {
        public static IReadOnlyList<ContactCardCriteria> Parse(
            Expression<Func<ContactCardFilterSpec, bool>> predicate)
        {
            var result = new List<ContactCardCriteria>();
            Visit(predicate.Body, result);
            return result;
        }

        private static void Visit(Expression expr, List<ContactCardCriteria> result)
        {
            // x => A && B && C  →  recurse into each branch
            if (expr is BinaryExpression { NodeType: ExpressionType.AndAlso } and_)
            {
                Visit(and_.Left, result);
                Visit(and_.Right, result);
                return;
            }

            result.Add(ParseSingle(expr));
        }

        private static ContactCardCriteria ParseSingle(Expression expr)
        {
            // contain:  x.Surname.Contains("Smith")
            if (expr is MethodCallExpression call
                && call.Method.Name == "Contains"
                && call.Object is MemberExpression containMember)
            {
                return Build(containMember, "contain", Evaluate(call.Arguments[0]));
            }

            if (expr is BinaryExpression bin)
            {
                switch (bin.NodeType)
                {
                    case ExpressionType.GreaterThanOrEqual:
                        return Build(bin.Left, "from", Evaluate(bin.Right));

                    case ExpressionType.LessThanOrEqual:
                        return Build(bin.Left, "to", Evaluate(bin.Right));

                    case ExpressionType.Equal:
                        var value = Evaluate(bin.Right);
                        // empty:  x.GroupId == null
                        if (value is null)
                            return Build(bin.Left, "empty", null);
                        // equal:  x.GroupId == 33  or  x.Mobile.Type == PhoneType.Main
                        return Build(bin.Left, "equal", value);

                    case ExpressionType.NotEqual:
                        var neValue = Evaluate(bin.Right);
                        // The API has no "not empty" operator, so `!= null` cannot be expressed.
                        if (neValue is null)
                            throw new NotSupportedException(
                                $"Unsupported expression '{expr}': the API has no \"not empty\" " +
                                "operator. Use '== null' for the \"empty\" operator.");
                        // not_equal:  x.GroupId != 33
                        return Build(bin.Left, "not_equal", neValue);
                }
            }

            throw new NotSupportedException(
                $"Unsupported expression '{expr.NodeType}': {expr}. " +
                "Supported: == (equal/empty), != (not_equal), >=, <=, .Contains(), &&.");
        }

        private static ContactCardCriteria Build(Expression memberExpr, string op, object? value)
        {
            // Unwrap Convert node that appears for nullable value types
            if (memberExpr is UnaryExpression { NodeType: ExpressionType.Convert } u)
                memberExpr = u.Operand;

            if (!(memberExpr is MemberExpression member))
                throw new NotSupportedException(
                    $"Unsupported expression '{memberExpr}': the left-hand side of a filter must be " +
                    "a ContactCardFilterSpec member, e.g. x.Surname == \"Smith\".");

            var path  = GetMemberPath(member);
            var field = GetApiFieldName(path);

            // The C# compiler sometimes folds enum constants to their underlying integer in
            // expression trees (e.g. ContactType.Main becomes (int)0). Restore the enum so
            // that the serialisation below produces "MAIN" instead of "0".
            if (!(value is Enum) && value != null)
            {
                var memberType     = member.Type;
                var underlyingType = Nullable.GetUnderlyingType(memberType) ?? memberType;
                if (underlyingType.IsEnum)
                    value = Enum.ToObject(underlyingType, value);
            }

            string apiValue;
            if (value is DateTime dt)
                // birth_date is a date-only field; every other date field carries a time component.
                apiValue = field == "birth_date" ? ApiFormat.Date(dt) : ApiFormat.DateTime(dt);
            else if (value is Enum e)
            {
                // (1) Gender.Undefined (and any future "Undefined" enum value) → empty operator.
                // Only `== Undefined` can be expressed: the API has no "not empty" operator, and
                // silently sending "empty" for `!=` would invert the caller's intent.
                if (e.ToString() == nameof(Gender.Undefined))
                {
                    if (op != "equal")
                        throw new NotSupportedException(
                            $"Unsupported expression: operator '{op}' against an undefined enum value. " +
                            "Only '== Gender.Undefined' is supported (the \"empty\" operator).");

                    return new ContactCardCriteria { Field = field, Operator = "empty", Value = string.Empty };
                }

                // Gender is lowercase on the wire everywhere else (ApiFormat.Gender on the write path
                // and the API's own responses), unlike ContactType which the API spells uppercase.
                // Invariant casing so the wire value matches regardless of the current culture
                // (e.g. tr-TR would otherwise turn "Additional" into "ADDİTİONAL").
                apiValue = e is Gender g
                    ? ApiFormat.Gender(g)                     // Gender.Male → "male"
                    : e.ToString().ToUpperInvariant();        // ContactType.Main → "MAIN"
            }
            else if (value is long l)
                apiValue = ApiFormat.Int(l);
            else if (value is int i)
                apiValue = ApiFormat.Int(i);
            else
                apiValue = value?.ToString() ?? string.Empty;

            return new ContactCardCriteria
            {
                Field    = field,
                Operator = op,
                Value    = apiValue
            };
        }

        // Walks up the MemberExpression chain and returns the property path.
        // x.Mobile.Type  →  ["Mobile", "Type"]
        // x.GroupId      →  ["GroupId"]
        private static string[] GetMemberPath(MemberExpression expr)
        {
            var path = new List<string>();
            Expression current = expr;
            while (current is MemberExpression m)
            {
                path.Insert(0, m.Member.Name);
                current = m.Expression!;
            }
            return path.ToArray();
        }

        // Evaluates any constant or closed-over variable expression. Literals and captured locals are read
        // straight from the tree (no per-query IL compilation); anything else falls back to Compile().
        // Nothing is cached: a captured variable may change between two executions of the same query.
        private static object? Evaluate(Expression expr)
        {
            if (TryReadDirect(expr, out var direct))
                return direct;

            try
            {
                return Expression.Lambda(expr).Compile().DynamicInvoke();
            }
            catch (InvalidOperationException ex)
            {
                // The expression references the lambda parameter, so it is not a value at all:
                // `100604 == x.GroupId` (member on the right) or `ids.Contains(x.GroupId)`.
                // Kept as the inner exception: a caller's own IOE (e.g. `ids.Single()` on an
                // empty list) lands here too and must stay diagnosable.
                throw new NotSupportedException(
                    $"Unsupported expression '{expr}': a filter value must be a constant or a " +
                    "captured variable, and the contact-card member must be on the left-hand side. " +
                    "Supported: == (equal/empty), != (not_equal), >=, <=, .Contains(), &&.", ex);
            }
        }

        // Constant, closure field/property (`x.GroupId == id`), or either wrapped in a Convert node
        // (nullable / widening casts). The conversion itself is skipped: Build formats by runtime type
        // and restores enums from the member type, so int-vs-long or int-vs-enum makes no difference.
        private static bool TryReadDirect(Expression expr, out object? value)
        {
            switch (expr)
            {
                case ConstantExpression c:
                    value = c.Value;
                    return true;

                case MemberExpression { Expression: ConstantExpression closure } m:
                    switch (m.Member)
                    {
                        case System.Reflection.FieldInfo f:
                            value = f.GetValue(closure.Value);
                            return true;
                        case System.Reflection.PropertyInfo p when p.GetIndexParameters().Length == 0:
                            value = p.GetValue(closure.Value);
                            return true;
                    }
                    break;

                case UnaryExpression { NodeType: ExpressionType.Convert } u:
                    return TryReadDirect(u.Operand, out value);
            }

            value = null;
            return false;
        }

        internal static string GetApiFieldName(string[] path) =>
            string.Join(".", path) switch
            {
                "GroupId"              => "groupId",
                "Title"                => "title",
                "Name"                 => "name",
                "Surname"              => "surname",
                "Mobile"               => "mobile",
                "Mobile.Type"          => "mobile.type",
                "Mobile.Value"         => "mobile.value",

                "Email"                => "email",
                "Email.Type"           => "email.type",
                "Email.Value"          => "email.value",

                "Viber"                => "viber",
                "Viber.Type"           => "viber.type",
                "Viber.Value"          => "viber.value",

                "WhatsApp"             => "whatsapp",
                "WhatsApp.Type"        => "whatsapp.type",
                "WhatsApp.Value"       => "whatsapp.value",

                "Landline"             => "landline",
                "Landline.Type"        => "landline.type",
                "Landline.Value"       => "landline.value",

                "Skype"                => "skype",
                "Skype.Type"           => "skype.type",
                "Skype.Value"          => "skype.value",

                "Telegram"             => "telegram",
                "Telegram.Type"        => "telegram.type",
                "Telegram.Value"       => "telegram.value",

                "Address.CountryA2"    => "address.countryA2",
                "Address.Country"      => "address.country",
                "Address.RegionId"     => "address.regionId",
                "Address.Region"       => "address.region",
                "Address.CityId"       => "address.cityId",
                "Address.City"         => "address.city",
                "Address.PostalCode"   => "address.postalcode",
                "Address.Street"       => "address.street",
                "Address.Building"     => "address.building",
                "Address.Other"        => "address.other",

                "BirthDate"            => "birth_date",
                "Gender"               => "gender",
                "CompanyName"          => "company_name",
                "CompanyUrl"           => "company_url",
                "Info"                 => "info",

                "CreatedAt"            => "createTs",
                var p                  => throw new NotSupportedException($"Unknown filter/sort field: {p}")
            };

        // Overload for single-segment paths (used by ContactCardQuery for OrderBy)
        internal static string GetApiFieldName(string propertyName) =>
            GetApiFieldName(new[] { propertyName });
    }
}
