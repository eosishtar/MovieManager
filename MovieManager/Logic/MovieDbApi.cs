using MovieManager.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace MovieManager.Logic
{
    public class MovieDbApi
    {
        private readonly Settings _settings;
        private HttpClient _httpClient;

        public MovieDbApi(Settings settings)
        {
            this._settings = settings;
           // this._httpClient = GetHttpClient();

        }


        public async void GetDetail()
        {

            _httpClient.BaseAddress = new Uri("https://localhost:44379/");

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("", "login")
            });

            HttpResponseMessage result = await _httpClient.PostAsync("/api/Membership/exists", content);
            string resultContent = await result.Content.ReadAsStringAsync();

            Console.WriteLine(result);
        }


        private System.Net.Http.HttpClient GetHttpClient()
        {
            var serverUrl = ConfigurationManager.AppSettings["MovieDbApi:ServerUrl"].ToString() ?? null;
            //var user = ConfigurationManager.AppSettings["UserDetail"].ToString();

            //if (serverUrl == null) { throw new ArgumentNullException(nameof(serverUrl)); }
            //if (user == null) { throw new ArgumentNullException(nameof(user)); }

            var client = new System.Net.Http.HttpClient();

            client.BaseAddress = new Uri(serverUrl);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(user)));

            return client;
        }

        public async System.Threading.Tasks.Task<bool> TestMovieDbApiAsync()
        {
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Get;
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                string apiKey = _settings.MovieDbApiKey ?? throw new ArgumentNullException(nameof(apiKey));

                var client = new System.Net.Http.HttpClient();

                client.BaseAddress = new Uri(string.Format("https://api.themoviedb.org/3/movie/550?api_key={0}", apiKey)); 
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage httpResponseMessage = await _httpClient.SendAsync(request);

                return (HttpStatusCode.OK == httpResponseMessage.StatusCode);
            }
        }

    }
}
