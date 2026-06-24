using System;
using System.Globalization;
using Mobizon.Contracts.Models.ContactCards;

namespace Mobizon.Net.Internal
{
    internal static class ContactCardMapper
    {
        internal static ContactCard ToEntity(ContactCardData d) => new ContactCard
        {
            Id          = d.Id,
            UserId      = d.UserId,
            IsDeleted   = d.IsDeleted,
            IsAvailable = d.IsAvailable,
            Groups      = d.Groups,
            Title       = d.Fields?.Title,
            Name        = d.Fields?.Name,
            Surname     = d.Fields?.Surname,
            Mobile      = d.Fields?.Mobile,
            Email       = d.Fields?.Email,
            Viber       = d.Fields?.Viber,
            WhatsApp    = d.Fields?.WhatsApp,
            Landline    = d.Fields?.Landline,
            Skype       = d.Fields?.Skype,
            Telegram    = d.Fields?.Telegram,
            Address     = d.Fields?.Address,
            BirthDate   = ParseDate(d.Fields?.BirthDate),
            Gender      = d.Fields?.Gender,
            CompanyName = d.Fields?.CompanyName,
            CompanyUrl  = d.Fields?.CompanyUrl,
            Info        = d.Fields?.Info
        };

        internal static CreateContactCardRequest ToCreateRequest(ContactCard e) =>
            new CreateContactCardRequest
            {
                Title       = e.Title,
                Name        = e.Name,
                Surname     = e.Surname,
                MobileValue = e.Mobile?.Value,
                MobileType  = e.Mobile?.Type,
                Email       = e.Email?.Value,
                Viber       = e.Viber?.Value,
                WhatsApp    = e.WhatsApp?.Value,
                Landline    = e.Landline?.Value,
                Skype       = e.Skype?.Value,
                Telegram    = e.Telegram?.Value,
                Address     = e.Address,
                BirthDate   = e.BirthDate,
                Gender      = e.Gender,
                CompanyName = e.CompanyName,
                CompanyUrl  = e.CompanyUrl,
                Info        = e.Info
            };

        internal static UpdateContactCardRequest ToUpdateRequest(ContactCard e) =>
            new UpdateContactCardRequest
            {
                Id          = e.Id!.Value.ToString(),
                Title       = e.Title,
                Name        = e.Name,
                Surname     = e.Surname,
                MobileValue = e.Mobile?.Value,
                MobileType  = e.Mobile?.Type,
                Email       = e.Email?.Value,
                Viber       = e.Viber?.Value,
                WhatsApp    = e.WhatsApp?.Value,
                Landline    = e.Landline?.Value,
                Skype       = e.Skype?.Value,
                Telegram    = e.Telegram?.Value,
                Address     = e.Address,
                BirthDate   = e.BirthDate,
                Gender      = e.Gender,
                CompanyName = e.CompanyName,
                CompanyUrl  = e.CompanyUrl,
                Info        = e.Info
            };

        private static DateTime? ParseDate(string? s) =>
            string.IsNullOrWhiteSpace(s) ? (DateTime?)null
            : DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
                ? dt : (DateTime?)null;
    }
}
