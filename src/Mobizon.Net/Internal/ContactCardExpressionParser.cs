using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Mobizon.Contracts.Models.ContactCards;

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
            // contain:  x.Surname.Contains("Петр")
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
                }
            }

            throw new NotSupportedException(
                $"Unsupported expression '{expr.NodeType}': {expr}. " +
                "Supported: == (equal/empty), >=, <=, .Contains(), &&.");
        }

        private static ContactCardCriteria Build(Expression memberExpr, string op, object? value)
        {
            // Unwrap Convert node that appears for nullable value types
            if (memberExpr is UnaryExpression { NodeType: ExpressionType.Convert } u)
                memberExpr = u.Operand;

            var path  = GetMemberPath((MemberExpression)memberExpr);
            var field = GetApiFieldName(path);

            // The C# compiler sometimes folds enum constants to their underlying integer in
            // expression trees (e.g. ContactType.Main becomes (int)0). Restore the enum so
            // that the serialisation below produces "MAIN" instead of "0".
            if (!(value is Enum) && value != null)
            {
                var memberType     = ((MemberExpression)memberExpr).Type;
                var underlyingType = Nullable.GetUnderlyingType(memberType) ?? memberType;
                if (underlyingType.IsEnum)
                    value = Enum.ToObject(underlyingType, value);
            }

            string apiValue;
            if (value is DateTime dt)
                apiValue = dt.ToString("yyyy-MM-dd HH:mm:ss");
            else if (value is Enum e)
            {
                // Gender.Undefined (and any future "Undefined" enum value) → empty operator
                if (e.ToString() == nameof(Gender.Undefined))
                    return new ContactCardCriteria { Field = field, Operator = "empty", Value = string.Empty };

                apiValue = e.ToString().ToUpper();   // ContactType.Main → "MAIN", Gender.Male → "MALE"
            }
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

        // Evaluates any constant or closed-over variable expression
        private static object? Evaluate(Expression expr) =>
            Expression.Lambda(expr).Compile().DynamicInvoke();

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
                "Address.PostalCode"   => "address.postalCode",
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
