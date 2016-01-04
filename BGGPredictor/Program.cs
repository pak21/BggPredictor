using System;
using System.Collections.Generic;
using System.Data.Entity.SqlServer;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BGGAPI;
using BGGPredictor.DB;

namespace BGGPredictor
{
    class Program
    {
        private const string Username = "speshul";

        private const int ItemsBatchSize = 50;

        private const string PrimaryNameType = "primary";

        public static void Main(string[] args)
        {
            var now = DateTime.UtcNow;

            var client = new BGGClient();

            var db = new BggDbContext();

            var task = FetchCollection(client, db, now, Username, true);
            if (task.Result == null)
            {
                return;
            }
            var collection = task.Result.Items;
            var collectionIds = collection.Select(game => game.Id).ToList();

            FetchGames(collectionIds, db, now, client);

            StoreRatings(db, collection, Username);
        }

        private async static Task<BGGCollection> FetchCollection(BGGClient client, BggDbContext db, DateTime now, string username, bool retry)
        {
            var existingUser = db.Users.SingleOrDefault(user => user.Username == username);

            if (existingUser != null && now - existingUser.CollectionLastFetched <= TimeSpan.FromDays(7))
            {
                return null;
            }

            var request = new BGGCollectionRequest {Username = username, ExcludeSubtype = BGGSubtype.BoardGameExpansion, Stats = true};
            var task = client.GetCollectionAsync(request);
            var response = await task;

            if (response.StatusCode == HttpStatusCode.Accepted)
            {
                if (retry)
                {
                    Console.WriteLine("Waiting to get collection for {0}", username);
                    Thread.Sleep(TimeSpan.FromSeconds(30));
                    return await FetchCollection(client, db, now, username, false);
                }
                else
                {
                    Console.WriteLine("Failed to fetch collection at second attempt for {0}", username);
                    throw new InvalidOperationException("Couldn't fetch collection");
                }
            }

            if (existingUser == null)
            {
                db.Users.Add(new User {Username = username, CollectionLastFetched = now});
            }
            else
            {
                existingUser.CollectionLastFetched = now;
            }

            db.SaveChanges();

            return response.Data;
        }

        private static void FetchGames(ICollection<int> collectionIds, BggDbContext db, DateTime now, BGGClient client)
        {
            var newGames = collectionIds.Except(db.Games.Select(game => game.Id));
            var oldGames =
                db.Games.Where(
                    game => collectionIds.Contains(game.Id) && SqlFunctions.DateDiff("day", game.LastUpdate, now) > 7)
                    .Select(game => game.Id);

            var gamesToFetch = newGames.Concat(oldGames).ToList();

            for (var start = 0; start < gamesToFetch.Count; start += ItemsBatchSize)
            {
                var itemsToFetch = gamesToFetch.Skip(start).Take(ItemsBatchSize).ToList();

                var itemsRequest = new BGGThingsRequest { Id = itemsToFetch, Stats = true };
                var items = client.GetThingsAsync(itemsRequest).Result.Data.Items;

                var games =
                    items.Select(
                        item =>
                            new Game
                            {
                                Id = item.Id,
                                LastUpdate = now,
                                Name = item.Names.Single(name => name.Type == PrimaryNameType).Value,
                                Rating = item.AverageRating,
                                BayesRating = item.BayesAverageRating,
                                Weight = item.AverageWeight,
                                MinimumPlayers = item.MinimumPlayers,
                                MaximumPlayers = item.MaximumPlayers,
                                PlayingTime = item.PlayingTime,
                                MinimumAge = item.MinimumAge
                            });

                foreach (var game in games)
                {
                    var oldGame = db.Games.FirstOrDefault(old => old.Id == game.Id);
                    if (oldGame == null)
                    {
                        db.Games.Add(game);
                    }
                    else
                    {
                        db.Entry(oldGame).CurrentValues.SetValues(game);
                    }
                }

                db.SaveChanges();
            }
        }

        private static void StoreRatings(BggDbContext db, IEnumerable<BGGCollection.Item> collection, string username)
        {
            var user = db.Users.Single(dbUser => dbUser.Username == username);

            foreach (var item in collection.Where(item => item.RatingFromThisUser.HasValue))
            {
                var game = db.Games.Single(dbGame => dbGame.Id == item.Id);

                var existingItem = db.CollectionItems.SingleOrDefault(dbItem => dbItem.User.Id == user.Id && dbItem.Game.Id == game.Id);

                if (existingItem == null)
                {
                    db.CollectionItems.Add(new CollectionItem
                    {
                        User = user,
                        Game = game,
                        Rating = item.RatingFromThisUser.Value
                    });
                }
                else
                {
                    existingItem.Rating = item.RatingFromThisUser.Value;
                }
            }

            db.SaveChanges();
        }
    }
}
