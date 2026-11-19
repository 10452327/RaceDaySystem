using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RaceDay.Mvc.Models;

namespace RaceDay.Mvc.Services
{
    public class ApiClientService
    {
        private static readonly ConcurrentDictionary<string, CookieContainer> _cookieContainers = new();
        private readonly string _baseUrl;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public ApiClientService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            var configured = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5066/";
            _baseUrl = configured.EndsWith("/") ? configured : configured + "/";
        }

        private HttpClient CreateClient()
        {
            var sessionId = _httpContextAccessor.HttpContext?.Session.Id ?? "default";
            var container = _cookieContainers.GetOrAdd(sessionId, _ => new CookieContainer());
            var handler = new HttpClientHandler
            {
                CookieContainer = container,
                UseCookies = true
            };

            return new HttpClient(handler)
            {
                BaseAddress = new Uri(_baseUrl)
            };
        }

        public async Task<(bool Ok, string Message)> RegisterAsync(RegisterViewModel model)
        {
            using var client = CreateClient();
            var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var res = await client.PostAsync("api/auth/register", content);
            var body = await res.Content.ReadAsStringAsync();
            return (res.IsSuccessStatusCode, res.IsSuccessStatusCode ? "Registration successful. Please log in." : ExtractMessage(body, "Registration failed."));
        }

        public async Task<(bool Ok, string Message, LoginApiResponse? Data)> LoginAsync(LoginViewModel model)
        {
            using var client = CreateClient();
            var content = new StringContent(JsonSerializer.Serialize(model), Encoding.UTF8, "application/json");
            var res = await client.PostAsync("api/auth/login", content);
            var body = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
            {
                return (false, ExtractMessage(body, "Invalid email or password."), null);
            }

            var data = JsonSerializer.Deserialize<LoginApiResponse>(body, _jsonOptions);
            return (true, "Logged in successfully.", data);
        }

        public async Task LogoutAsync()
        {
            using var client = CreateClient();
            await client.PostAsync("api/auth/logout", null);
            var sessionId = _httpContextAccessor.HttpContext?.Session.Id;
            if (!string.IsNullOrEmpty(sessionId))
            {
                _cookieContainers.TryRemove(sessionId, out _);
            }
        }

        public async Task<List<EventItem>> GetEventsAsync(string? search = null, string? eventType = null, string? location = null)
        {
            using var client = CreateClient();
            var queryParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(search)) queryParts.Add($"search={Uri.EscapeDataString(search)}");
            if (!string.IsNullOrWhiteSpace(eventType)) queryParts.Add($"eventType={Uri.EscapeDataString(eventType)}");
            if (!string.IsNullOrWhiteSpace(location)) queryParts.Add($"location={Uri.EscapeDataString(location)}");
            var url = queryParts.Count > 0 ? $"api/events?{string.Join("&", queryParts)}" : "api/events";

            var res = await client.GetAsync(url);
            if (!res.IsSuccessStatusCode) return new List<EventItem>();
            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<EventItem>>(json, _jsonOptions) ?? new List<EventItem>();
        }

        public async Task<EventItem?> GetEventByIdAsync(int id)
        {
            using var client = CreateClient();
            var res = await client.GetAsync($"api/events/{id}");
            if (!res.IsSuccessStatusCode) return null;
            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<EventItem>(json, _jsonOptions);
        }

        public async Task<(bool Ok, string Message, int? EventId)> CreateEventAsync(EventFormViewModel model)
        {
            using var client = CreateClient();
            var payload = new
            {
                model.Name,
                model.Description,
                model.Date,
                model.Location,
                model.Distance,
                model.EventType
            };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var res = await client.PostAsync("api/events", content);
            var body = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode) return (false, ExtractMessage(body, "Could not create event."), null);

            using var doc = JsonDocument.Parse(body);
            int? eventId = doc.RootElement.TryGetProperty("eventId", out var idProp) && idProp.TryGetInt32(out var idVal) ? idVal : null;
            return (true, "Event created successfully.", eventId);
        }

        public async Task<(bool Ok, string Message)> UpdateEventAsync(int id, EventFormViewModel model)
        {
            using var client = CreateClient();
            var payload = new
            {
                model.Name,
                model.Description,
                model.Date,
                model.Location,
                model.Distance,
                model.EventType,
                model.BannerImageUrl
            };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var res = await client.PutAsync($"api/events/{id}", content);
            var body = await res.Content.ReadAsStringAsync();
            return (res.IsSuccessStatusCode, res.IsSuccessStatusCode ? "Event updated." : ExtractMessage(body, "Failed to update event."));
        }

        public async Task<(bool Ok, string Message)> DeleteEventAsync(int id)
        {
            using var client = CreateClient();
            var res = await client.DeleteAsync($"api/events/{id}");
            var body = await res.Content.ReadAsStringAsync();
            return (res.IsSuccessStatusCode, res.IsSuccessStatusCode ? "Event deleted." : ExtractMessage(body, "Failed to delete event."));
        }

        public async Task<(bool Ok, string Message)> AddCategoryAsync(int eventId, string categoryName)
        {
            using var client = CreateClient();
            var content = new StringContent(JsonSerializer.Serialize(new { Name = categoryName }), Encoding.UTF8, "application/json");
            var res = await client.PostAsync($"api/events/{eventId}/categories", content);
            var body = await res.Content.ReadAsStringAsync();
            return (res.IsSuccessStatusCode, res.IsSuccessStatusCode ? "Category added." : ExtractMessage(body, "Failed to add category."));
        }

        public async Task<(bool Ok, string Message)> UploadEventBannerAsync(int eventId, IFormFile file)
        {
            using var client = CreateClient();
            using var form = new MultipartFormDataContent();
            await using var stream = file.OpenReadStream();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
            form.Add(fileContent, "file", file.FileName);

            var res = await client.PostAsync($"api/events/{eventId}/banner", form);
            var body = await res.Content.ReadAsStringAsync();
            return (res.IsSuccessStatusCode, res.IsSuccessStatusCode ? "Banner uploaded." : ExtractMessage(body, "Banner upload failed."));
        }

        public async Task<(bool Ok, string Message)> EnrolAsync(int eventId, int categoryId)
        {
            using var client = CreateClient();
            var content = new StringContent(JsonSerializer.Serialize(new { EventId = eventId, CategoryId = categoryId }), Encoding.UTF8, "application/json");
            var res = await client.PostAsync("api/enrolments", content);
            var body = await res.Content.ReadAsStringAsync();
            return (res.IsSuccessStatusCode, res.IsSuccessStatusCode ? "Enrolment confirmed!" : ExtractMessage(body, "Could not complete enrolment."));
        }

        public async Task<List<EnrolmentItem>> GetMyEnrolmentsAsync()
        {
            using var client = CreateClient();
            var res = await client.GetAsync("api/enrolments/my");
            if (!res.IsSuccessStatusCode) return new List<EnrolmentItem>();
            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<EnrolmentItem>>(json, _jsonOptions) ?? new List<EnrolmentItem>();
        }

        public async Task<List<EnrolmentItem>> GetEventEnrolmentsAsync(int eventId)
        {
            using var client = CreateClient();
            var res = await client.GetAsync($"api/enrolments/event/{eventId}");
            if (!res.IsSuccessStatusCode) return new List<EnrolmentItem>();
            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<EnrolmentItem>>(json, _jsonOptions) ?? new List<EnrolmentItem>();
        }

        public async Task<(bool Ok, string Message)> UpdateEnrolmentStatusAsync(int enrolmentId, string status)
        {
            using var client = CreateClient();
            var content = new StringContent(JsonSerializer.Serialize(status), Encoding.UTF8, "application/json");
            var res = await client.PatchAsync($"api/enrolments/{enrolmentId}/status", content);
            var body = await res.Content.ReadAsStringAsync();
            return (res.IsSuccessStatusCode, res.IsSuccessStatusCode ? "Status updated." : ExtractMessage(body, "Failed to update status."));
        }

        public async Task<(bool Ok, string Message)> RecordResultAsync(int enrolmentId, TimeSpan finishTime, int position)
        {
            using var client = CreateClient();
            var payload = new { EnrolmentId = enrolmentId, FinishTime = finishTime, Position = position };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var res = await client.PostAsync("api/results", content);
            var body = await res.Content.ReadAsStringAsync();
            return (res.IsSuccessStatusCode, res.IsSuccessStatusCode ? "Result published!" : ExtractMessage(body, "Failed to save result."));
        }

        public async Task<List<ResultItem>> GetMyResultsAsync()
        {
            using var client = CreateClient();
            var res = await client.GetAsync("api/results/my");
            if (!res.IsSuccessStatusCode) return new List<ResultItem>();
            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<ResultItem>>(json, _jsonOptions) ?? new List<ResultItem>();
        }

        public async Task<ProfileViewModel?> GetProfileAsync()
        {
            using var client = CreateClient();
            var res = await client.GetAsync("api/profile");
            if (!res.IsSuccessStatusCode) return null;
            var json = await res.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ProfileViewModel>(json, _jsonOptions);
        }

        public async Task<(bool Ok, string Message)> UpdateProfileAsync(string fullName, string email)
        {
            using var client = CreateClient();
            var content = new StringContent(JsonSerializer.Serialize(new { FullName = fullName, Email = email }), Encoding.UTF8, "application/json");
            var res = await client.PutAsync("api/profile", content);
            var body = await res.Content.ReadAsStringAsync();
            return (res.IsSuccessStatusCode, res.IsSuccessStatusCode ? "Profile updated." : ExtractMessage(body, "Failed to update profile."));
        }

        public async Task<(bool Ok, string Message)> UploadProfilePictureAsync(IFormFile file)
        {
            using var client = CreateClient();
            using var form = new MultipartFormDataContent();
            await using var stream = file.OpenReadStream();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
            form.Add(fileContent, "file", file.FileName);

            var res = await client.PostAsync("api/profile/picture", form);
            var body = await res.Content.ReadAsStringAsync();
            return (res.IsSuccessStatusCode, res.IsSuccessStatusCode ? "Profile picture uploaded." : ExtractMessage(body, "Failed to upload picture."));
        }

        private static string ExtractMessage(string body, string fallback)
        {
            if (string.IsNullOrWhiteSpace(body)) return fallback;
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("message", out var msg))
                {
                    return msg.GetString() ?? fallback;
                }
            }
            catch
            {
                // Ignore non-JSON bodies
            }
            return fallback;
        }
    }
}