using System.Net;
using System.Text;
using System.Text.Json;

namespace InternalRequest_Management.Services
{
    // Gọi Gemini API. Mọi lỗi đều được gói lại thành thông báo, không làm sập chức năng khác.
    public class GeminiService
    {
        private readonly HttpClient _http;
        private readonly string? _apiKey;
        private readonly string _model;

        public GeminiService(HttpClient http, IConfiguration cfg)
        {
            _http = http;
            _apiKey = cfg["Gemini:ApiKey"];
            _model = cfg["Gemini:Model"] ?? "gemini-3.5-flash";
        }

        public bool DaCauHinh => !string.IsNullOrWhiteSpace(_apiKey);

        // Trả về (văn bản, lỗi). Khi lỗi khác null nghĩa là không dùng được gợi ý AI.
        public async Task<(string? Text, string? Loi)> HoiAsync(string prompt, CancellationToken ct = default)
        {
            if (!DaCauHinh)
                return (null, "Chưa cấu hình khóa API cho gợi ý AI.");

            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent";
                var body = new
                {
                    contents = new[] { new { role = "user", parts = new[] { new { text = prompt } } } },
                    generationConfig = new { temperature = 0.3, maxOutputTokens = 2000 }
                };

                using var req = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
                };
                req.Headers.Add("x-goog-api-key", _apiKey);

                using var res = await _http.SendAsync(req, ct);
                var json = await res.Content.ReadAsStringAsync(ct);

                if (res.StatusCode == HttpStatusCode.TooManyRequests)
                    return (null, "Đã vượt hạn mức gọi AI, vui lòng thử lại sau ít phút.");
                if (res.StatusCode == HttpStatusCode.NotFound)
                    return (null, "Không tìm thấy mô hình AI, hãy kiểm tra lại tên mô hình trong cấu hình.");
                if (res.StatusCode == HttpStatusCode.BadRequest || res.StatusCode == HttpStatusCode.Unauthorized
                    || res.StatusCode == HttpStatusCode.Forbidden)
                    return (null, "Yêu cầu bị từ chối. Hãy kiểm tra khóa API và tên mô hình.");
                if (!res.IsSuccessStatusCode)
                    return (null, "Dịch vụ AI đang gặp sự cố, vui lòng thử lại sau.");

                using var doc = JsonDocument.Parse(json);
                var sb = new StringBuilder();
                if (doc.RootElement.TryGetProperty("candidates", out var cands) && cands.GetArrayLength() > 0
                    && cands[0].TryGetProperty("content", out var content)
                    && content.TryGetProperty("parts", out var parts))
                {
                    foreach (var p in parts.EnumerateArray())
                        if (p.TryGetProperty("text", out var t)) sb.Append(t.GetString());
                }

                var text = sb.ToString().Trim();
                return text.Length == 0 ? (null, "AI không trả về nội dung.") : (text, null);
            }
            catch (TaskCanceledException)
            {
                return (null, "AI phản hồi quá lâu, vui lòng thử lại.");
            }
            catch (Exception)
            {
                return (null, "Không kết nối được dịch vụ AI.");
            }
        }
    }
}