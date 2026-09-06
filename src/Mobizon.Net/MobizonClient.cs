using System;
using System.Net.Http;
using Mobizon.Contracts;
using Mobizon.Net.Internal;
using Mobizon.Net.Services;


namespace Mobizon.Net
{
    /// <summary>
    /// The default implementation of <see cref="IMobizonClient"/> that communicates with the Mobizon REST API.
    /// </summary>
    /// <remarks>
    /// When constructed with <see cref="MobizonClient(MobizonClientOptions)"/>, the client owns the underlying
    /// <see cref="HttpClient"/> and will dispose it when <see cref="Dispose"/> is called.
    /// When constructed with <see cref="MobizonClient(HttpClient, MobizonClientOptions)"/>, the caller is
    /// responsible for the lifetime of the <see cref="HttpClient"/>.
    /// </remarks>
    public class MobizonClient : IMobizonClient, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly bool _ownsHttpClient;

        /// <summary>
        /// Gets the service for sending and managing SMS messages.
        /// </summary>
        public IMessageService Messages { get; }

        /// <summary>
        /// Gets the service for creating and managing bulk SMS campaigns.
        /// </summary>
        public ICampaignService Campaigns { get; }

        /// <summary>
        /// Gets the service for creating and managing short links.
        /// </summary>
        public ILinkService Links { get; }

        /// <summary>
        /// Gets the service for retrieving account information such as balance.
        /// </summary>
        public IUserService User { get; }

        /// <summary>
        /// Gets the service for querying the status of background tasks.
        /// </summary>
        public ITaskQueueService TaskQueue { get; }

        /// <summary>
        /// Gets the service for managing contact groups.
        /// </summary>
        public IContactGroupService ContactGroups { get; }

        /// <summary>
        /// Gets the set for managing contact cards.
        /// </summary>
        public IContactCardSet ContactCards { get; }

        /// <summary>
        /// Gets the service for managing the number stop-list.
        /// </summary>
        public INumberStopListService NumberStopList { get; }

        /// <summary>
        /// Gets the service for listing registered sender IDs (alphanames).
        /// </summary>
        public IAlphanameService Alphanames { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="MobizonClient"/> using a privately managed <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="options">The configuration options including API key, URL, version, and timeout.</param>
        public MobizonClient(MobizonClientOptions options)
            : this(new HttpClient(), options, ownsHttpClient: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MobizonClient"/> using an externally provided <see cref="HttpClient"/>.
        /// The caller retains ownership of <paramref name="httpClient"/> and is responsible for disposing it.
        /// </summary>
        /// <param name="httpClient">The <see cref="HttpClient"/> instance to use for all API requests.</param>
        /// <param name="options">
        /// The configuration options including API key, URL, and version. <see cref="MobizonClientOptions.Timeout"/>
        /// is <em>not</em> applied by this overload — the caller owns <paramref name="httpClient"/>, so it is the
        /// caller's responsibility to set <see cref="HttpClient.Timeout"/> directly (or to configure it on the
        /// named client when registering via dependency injection). Only the overload that owns the
        /// <see cref="HttpClient"/> (<see cref="MobizonClient(MobizonClientOptions)"/>) honors this option.
        /// </param>
        public MobizonClient(HttpClient httpClient, MobizonClientOptions options)
            : this(httpClient, options, ownsHttpClient: false)
        {
        }

        /// <summary>
        /// Upper bound on a buffered API response body, applied to an <see cref="HttpClient"/> the SDK owns
        /// (this constructor or the DI registration). The largest documented responses (100-item list pages,
        /// link statistics) are well under 1 MB; a larger body is a protocol error, not data.
        /// </summary>
        public const long MaxResponseContentBufferSize = 16 * 1024 * 1024;

        private MobizonClient(HttpClient httpClient, MobizonClientOptions options, bool ownsHttpClient)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException(nameof(httpClient));
            }

            if (options == null)
            {
                if (ownsHttpClient) httpClient.Dispose();
                throw new ArgumentNullException(nameof(options));
            }

            _httpClient = httpClient;
            _ownsHttpClient = ownsHttpClient;

            MobizonApiClient apiClient;
            try
            {
                apiClient = new MobizonApiClient(httpClient, options); // validates options
            }
            catch
            {
                if (ownsHttpClient) httpClient.Dispose();
                throw;
            }

            // A caller-supplied HttpClient keeps its own timeout and buffer limit: its lifetime and settings
            // belong to the caller (see the DI package for the named-client equivalent).
            if (ownsHttpClient)
            {
                _httpClient.Timeout = options.Timeout;
                _httpClient.MaxResponseContentBufferSize = MaxResponseContentBufferSize;
            }

            Messages = new MessageService(apiClient);
            Campaigns = new CampaignService(apiClient);
            Links = new LinkService(apiClient);
            User = new UserService(apiClient);
            TaskQueue = new TaskQueueService(apiClient);
            ContactGroups = new ContactGroupService(apiClient);
            ContactCards = new ContactCardSet(new ContactCardService(apiClient));
            NumberStopList = new NumberStopListService(apiClient);
            Alphanames = new AlphanameService(apiClient);
        }

        /// <summary>
        /// Releases the underlying <see cref="HttpClient"/> if it was created by this instance.
        /// </summary>
        public void Dispose()
        {
            if (_ownsHttpClient)
                _httpClient.Dispose();
        }
    }
}
