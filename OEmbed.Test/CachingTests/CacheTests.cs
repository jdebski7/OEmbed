﻿namespace OEmbed.Test.CachingTests
{
    public class CacheTests
    {
        private readonly ICacheKey _cacheKey;

        private readonly ITestOutputHelper _output;

        public CacheTests(ITestOutputHelper outputHelper)
        {
            _cacheKey = new DefaultCacheKey();
            _output = outputHelper;
        }

        [Fact]
        public async void GetAsyncNullTest()
        {
            var cache = new DefaultCache();
            var key = _cacheKey.CreateKey("https://www.youtube.com/watch?v=D1PvIWdJ8xo");

            var result = await cache.GetAsync<Rich>(key);

            Assert.Null(result);
        }

        [Fact]
        public async void GetSetAsyncValueTest()
        {
            var cache = new DefaultCache();
            var key = _cacheKey.CreateKey("https://www.youtube.com/watch?v=D1PvIWdJ8xo");

            await cache.SetAsync<Rich>(key, new());

            var result = await cache.GetAsync<Rich>(key);

            Assert.NotNull(result);
        }

        [Fact]
        public async void AddOrGetExistingAsyncTest()
        {
            var cache = new DefaultCache();

            string url = "https://www.youtube.com/watch?v=D1PvIWdJ8xo";
            var expected = new Rich { AuthorName = "IU", Title = "blueming" };

            var item = await cache.AddOrGetExistingAsync(url, url => Task.FromResult<Rich?>(expected));

            var key = _cacheKey.CreateKey(url);
            var cachedItem = await cache.GetAsync<Rich>(key);

            Assert.NotNull(item);
            Assert.NotNull(cachedItem);

            Assert.Equal(expected, item);
            Assert.Equal(expected, cachedItem);
        }

        [Fact]
        public async void AddOrGetExistingAsyncNullTest()
        {
            var cache = new DefaultCache();

            string url = "https://www.youtube.com/watch?v=D1PvIWdJ8xo";

            var item = await cache.AddOrGetExistingAsync(url, url => Task.FromResult<Rich?>(null));

            var key = _cacheKey.CreateKey(url);
            var cachedItem = await cache.GetAsync<Rich>(key);

            Assert.Null(item);
            Assert.Null(cachedItem);
        }

        [Fact]
        public async void MultiThreadingTest()
        {
            var cache = new DefaultCache();

            int requestsCounter = 0;

            const string badUrl = "https://www.youtube.com/watch?v=D1PvIWdJ8xo";

            // List of dublicated links
            var urls = Enumerable.Repeat(badUrl, 20).ToList();

            // Url with ending slash
            urls.Add(badUrl + "/");

            urls.Add("https://www.instagram.com/p/1XSKgBAGz-/");
            urls.Add("https://vimeo.com/22439234");

            var results = await Task.WhenAll(urls.Select(async url =>
            {
                return await cache.AddOrGetExistingAsync(url, async (url) =>
                {
                    _output.WriteLine("Request #" + ++requestsCounter);

                    await Task.Delay(3000); // simulate long running request

                    return url.Contains(badUrl) ? null : new Base();
                });
            }));

            Assert.NotNull(results);

            _output.WriteLine("Total results: " + results!.Length.ToString());

            // Should be 3, because we use keylock to prevent race condition
            Assert.Equal(3, requestsCounter);
        }
    }
}