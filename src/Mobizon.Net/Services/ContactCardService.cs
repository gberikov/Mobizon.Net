using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mobizon.Contracts;
using Mobizon.Net.Internal;

namespace Mobizon.Net.Services
{
    internal class ContactCardService
    {
        private const string ModuleName = "contactcard";
        private readonly MobizonApiClient _apiClient;

        public ContactCardService(MobizonApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<MobizonListResult<ContactCardData>> ListAsync(
            ContactCardListRequest? request = null,
            CancellationToken cancellationToken = default)
        {
            Dictionary<string, string>? parameters = null;

            if (request != null)
            {
                parameters = new Dictionary<string, string>();

                if (request.Criteria != null)
                    for (var i = 0; i < request.Criteria.Count; i++)
                    {
                        var c = request.Criteria[i];
                        parameters[$"criteria[{i}][field]"] = c.Field;
                        parameters[$"criteria[{i}][operator]"] = c.Operator;
                        parameters[$"criteria[{i}][value]"] = c.Value;
                    }

                if (request.Pagination != null)
                {
                    parameters["pagination[currentPage]"] = ApiFormat.Int(request.Pagination.CurrentPage);
                    parameters["pagination[pageSize]"] = ApiFormat.Int(request.Pagination.PageSize);
                }

                if (request.Sort != null)
                    parameters[$"sort[{request.Sort.Field}]"] = ApiFormat.Sort(request.Sort.Direction);
            }

            return (await _apiClient.SendAsync<MobizonListResult<ContactCardData>>(
                ModuleName, "list", parameters, cancellationToken).ConfigureAwait(false)).Data!;
        }

        public async Task<ContactCardData> GetAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"] = id
            };

            return (await _apiClient.SendAsync<ContactCardData>(
                ModuleName, "get", parameters, cancellationToken).ConfigureAwait(false)).Data!;
        }

        public async Task<long> CreateAsync(
            CreateContactCardRequest request,
            CancellationToken cancellationToken = default)
        {
            var fields = BuildCardFields(request.Title, request.Name, request.Surname,
                request.MobileValue, request.MobileType, request.Email,
                request.Viber, request.WhatsApp, request.Landline,
                request.Skype, request.Telegram, request.Address, request.BirthDate,
                request.Gender, request.CompanyName, request.CompanyUrl, request.Info);

            return (await _apiClient.SendMultipartAsync<long>(
                ModuleName, "create", fields,
                request.Photo, request.PhotoFileName,
                cancellationToken).ConfigureAwait(false)).Data;
        }

        public async Task UpdateAsync(
            UpdateContactCardRequest request,
            CancellationToken cancellationToken = default)
        {
            var fields = BuildCardFields(request.Title, request.Name, request.Surname,
                request.MobileValue, request.MobileType, request.Email,
                request.Viber, request.WhatsApp, request.Landline,
                request.Skype, request.Telegram, request.Address, request.BirthDate,
                request.Gender, request.CompanyName, request.CompanyUrl, request.Info);

            fields["id"] = request.Id;

            await _apiClient.SendMultipartAsync<bool>(
                ModuleName, "update", fields,
                request.Photo, request.PhotoFileName,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task SetGroupsAsync(
            string id,
            IReadOnlyList<long> groupIds,
            CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"] = id
            };

            for (var i = 0; i < groupIds.Count; i++)
                parameters[$"groupIds[{i}]"] = ApiFormat.Int(groupIds[i]);

            await _apiClient.SendAsync<bool>(
                ModuleName, "setgroups", parameters, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<ContactGroupRef>> GetGroupsAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string>
            {
                ["id"] = id
            };

            return (await _apiClient.SendAsync<IReadOnlyList<ContactGroupRef>>(
                ModuleName, "getgroups", parameters, cancellationToken).ConfigureAwait(false)).Data!;
        }

        public async Task RemoveAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            var parameters = new Dictionary<string, string> { ["id"] = id };
            await _apiClient.SendAsync<bool>(
                ModuleName, "delete", parameters, cancellationToken).ConfigureAwait(false);
        }

        private static Dictionary<string, string> BuildCardFields(
            string? title, string? name, string? surname,
            string? mobileValue, ContactType? mobileType,
            string? email, string? viber, string? whatsapp, string? landline,
            string? skype, string? telegram, AddressFieldInfo? address,
            DateTime? birthDate, Gender? gender, string? companyName, string? companyUrl,
            string? info)
        {
            var fields = new Dictionary<string, string>
            {
                ["data[title]"]           = title       ?? string.Empty,
                ["data[name]"]            = name        ?? string.Empty,
                ["data[surname]"]         = surname     ?? string.Empty,
                ["data[mobile][value]"]   = mobileValue ?? string.Empty,
                ["data[mobile][type]"]    = mobileType?.ToString().ToUpperInvariant() ?? string.Empty,
                ["data[email]"]           = email       ?? string.Empty,
                ["data[viber]"]           = viber       ?? string.Empty,
                ["data[whatsapp]"]        = whatsapp    ?? string.Empty,
                ["data[landline]"]        = landline    ?? string.Empty,
                ["data[skype]"]           = skype       ?? string.Empty,
                ["data[telegram]"]        = telegram    ?? string.Empty,
                ["data[birth_date]"]      = birthDate.HasValue ? ApiFormat.Date(birthDate.Value) : string.Empty,
                ["data[gender]"]          = ApiFormat.Gender(gender),
                ["data[company_name]"]    = companyName ?? string.Empty,
                ["data[company_url]"]     = companyUrl  ?? string.Empty,
                ["data[info]"]            = info        ?? string.Empty,
            };

            // Address is only sent when provided, so callers that don't touch it
            // (e.g. update) don't accidentally clear an existing address.
            if (address != null)
            {
                fields["data[address][countryA2]"]  = address.CountryA2  ?? string.Empty;
                fields["data[address][country]"]    = address.Country    ?? string.Empty;
                fields["data[address][regionId]"]   = address.RegionId   ?? string.Empty;
                fields["data[address][region]"]     = address.Region     ?? string.Empty;
                fields["data[address][cityId]"]     = address.CityId     ?? string.Empty;
                fields["data[address][city]"]       = address.City       ?? string.Empty;
                fields["data[address][postalcode]"] = address.PostalCode ?? string.Empty;
                fields["data[address][street]"]     = address.Street     ?? string.Empty;
                fields["data[address][building]"]   = address.Building   ?? string.Empty;
                fields["data[address][other]"]      = address.Other      ?? string.Empty;
            }

            return fields;
        }
    }
}
