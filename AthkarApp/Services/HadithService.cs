using System.Net.Http.Json;
using AthkarApp.Models;

namespace AthkarApp.Services
{
    public class HadithService
    {
        private readonly HttpClient _httpClient = new();
        private const string BaseUrl = "https://hadeethenc.com/api/v1";

        public async Task<List<Hadith>> GetHadithsAsync(int page = 1, int perPage = 20, string categoryId = "5")
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<HadeethListResponse>($"{BaseUrl}/hadeeths/list/?language=ar&category_id={categoryId}&per_page={perPage}&page={page}");
                
                var hadiths = new List<Hadith>();
                if (response?.Data != null)
                {
                    foreach (var item in response.Data)
                    {
                        hadiths.Add(new Hadith
                        {
                            Id = item.Id,
                            Title = item.Title
                        });
                    }
                }
                return hadiths;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching hadiths: {ex.Message}");
                return new List<Hadith>();
            }
        }

        public async Task<Hadith> GetHadithDetailAsync(string id)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<HadeethDetailResponse>($"{BaseUrl}/hadeeths/one/?language=ar&id={id}");
                if (response != null)
                {
                    return new Hadith
                    {
                        Id = response.Id,
                        Title = response.Title,
                        Text = response.Hadeeth,
                        Explanation = response.Explanation,
                        Attribution = response.Attribution,
                        Grade = response.Grade,
                        Reference = response.Reference
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching hadith detail: {ex.Message}");
            }
            return null;
        }
    }

    public class HadeethListResponse
    {
        public List<HadeethListItem> Data { get; set; }
    }

    public class HadeethListItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
    }

    public class HadeethDetailResponse
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Hadeeth { get; set; }
        public string Attribution { get; set; }
        public string Grade { get; set; }
        public string Explanation { get; set; }
        public string Reference { get; set; }
    }
}
