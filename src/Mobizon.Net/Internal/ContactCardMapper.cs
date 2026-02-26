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
            Mobile      = d.Fields?.Mobile?.Value,
            MobileType  = d.Fields?.Mobile?.Type,
            Email       = d.Fields?.Email,
            Viber       = d.Fields?.Viber,
            Whatsapp    = d.Fields?.Whatsapp,
            Landline    = d.Fields?.Landline,
            Skype       = d.Fields?.Skype,
            Telegram    = d.Fields?.Telegram,
            BirthDate   = d.Fields?.BirthDate,
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
                MobileValue = e.Mobile,
                MobileType  = e.MobileType,
                Email       = e.Email,
                Viber       = e.Viber,
                Whatsapp    = e.Whatsapp,
                Landline    = e.Landline,
                Skype       = e.Skype,
                Telegram    = e.Telegram,
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
                MobileValue = e.Mobile,
                MobileType  = e.MobileType,
                Email       = e.Email,
                Viber       = e.Viber,
                Whatsapp    = e.Whatsapp,
                Landline    = e.Landline,
                Skype       = e.Skype,
                Telegram    = e.Telegram,
                BirthDate   = e.BirthDate,
                Gender      = e.Gender,
                CompanyName = e.CompanyName,
                CompanyUrl  = e.CompanyUrl,
                Info        = e.Info
            };
    }
}
