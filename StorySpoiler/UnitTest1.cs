using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;


namespace StorySpoiler

{
    [TestFixture]
    public class StorySpoilTests
    {
        private RestClient client;
        private static string createStoryId;
        private const string baseUrl = "https://d3s5nxhwblsjbi.cloudfront.net";


        [OneTimeSetUp]
        public void Setup()
        {
            string token = GetJwtToken("lora1", "123456");

            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };
            client = new RestClient(options);

        }
        private string GetJwtToken(string username, string password)
        {
            var loginClient = new RestClient(baseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);

            request.AddJsonBody(new { username, password });

            var response = loginClient.Execute(request);

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            return json.GetProperty("accessToken").GetString() ?? string.Empty;

        }

        [Test, Order(1)]
        public void CreateStory_ShouldReturnCreated()
        {
            var story = new
            {
                title = "New story",
                description = "This is my first story",
                url = ""

            };
            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(story);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            createStoryId = json.GetProperty("storyId").GetString() ?? string.Empty;

            Assert.That(createStoryId, Is.Not.Null.And.Not.Empty, "Story Id should not be null or empty");
            Assert.That(response.Content, Does.Contain("Successfully created!"));
        }

        [Test, Order(2)]

        public void EditCreatedStory_ShouldReturnOk()
        {
            var changes = new 
            {
                title = "New story edited", 
                description = "Edited my first story", 
                url = ""
            };

            var request = new RestRequest($"/api/Story/Edit/{createStoryId}", Method.Put);
            request.AddJsonBody(changes);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Successfully edited"));
        }
        [Test, Order(3)]

        public void GetAllStory_ShouldReturnList()
        {
            var request = new RestRequest("/api/Story/All", Method.Get);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var stories = JsonSerializer.Deserialize<List<object>>(response.Content);

            Assert.That(stories, Is.Not.Empty);
        }
        [Test, Order(4)]

        public void DeleteStory_ShouldReturnOk()
        {
            var request = new RestRequest($"/api/Story/Delete/{createStoryId}", Method.Delete);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Deleted successfully!"));

        }
        [Test, Order(5)]

        public void CreateStory_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var story = new
            {
                title = "",
                description = "",
                url = ""

            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(story);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo((HttpStatusCode)HttpStatusCode.BadRequest));

        }
        [Test, Order(6)]

        public void EditNonExistingStory_ShouldReturnNotFound()
        {
            string fakeID = "123";
            var changes = new
            {
                title = "new title",
                description = "new description",
                url = ""
            };
            var request = new RestRequest($"/api/Story/Edit/{fakeID}", Method.Put);
            request.AddJsonBody(changes);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(response.Content, Does.Contain("No spoilers..."));
        }
        [Test, Order(7)]

        public void DeleteNonExistingStory_ShouldReturnBadRequest()
        {
            string fakeID = "123";

            var request = new RestRequest($"/api/Story/Delete/{fakeID}", Method.Delete);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("Unable to delete this story spoiler!"));
        }



        [OneTimeTearDown]
        public void Cleanup()
        {
            client?.Dispose();
        }
    }
}