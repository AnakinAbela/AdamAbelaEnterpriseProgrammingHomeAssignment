using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Interfaces;
using Domain.Models;
using Newtonsoft.Json.Linq;

namespace Presentation.Factory
{
    public class ImportItemFactory
    {
        public List<IItemValidating> BuildList(string json)
        {
            var token = JToken.Parse(json);
            var restaurants = new Dictionary<string, Restaurant>(StringComparer.OrdinalIgnoreCase);
            var menuItems = new List<MenuItem>();

            IEnumerable<JToken> GetTokens(JToken t)
            {
                if (t is JArray array) return array;
                if (t is JObject obj && obj["restaurants"] is JArray nested) return nested;
                return new List<JToken> { t };
            }

            foreach (var t in GetTokens(token))
            {
                var type = ((string?)t["type"])?.ToLowerInvariant();
                var looksLikeRestaurant = type == "restaurant" || t["menuItems"] != null || t["ownerEmailAddress"] != null;
                if (looksLikeRestaurant)
                {
                    var restaurant = t.ToObject<Restaurant>() ?? new Restaurant();
                    PrepareRestaurant(restaurant);
                    restaurants[restaurant.Id] = restaurant;

                    if (t["menuItems"] is JArray nested)
                    {
                        foreach (var menuToken in nested.OfType<JObject>())
                        {
                            var menuItem = menuToken.ToObject<MenuItem>() ?? new MenuItem();
                            PrepareMenuItem(menuItem, restaurant.Id, menuToken);
                            menuItems.Add(menuItem);
                        }
                    }
                    continue;
                }

                var looksLikeMenuItem = type == "menuitem" || t["restaurantId"] != null;
                if (looksLikeMenuItem)
                {
                    var menuItem = t.ToObject<MenuItem>() ?? new MenuItem();
                    PrepareMenuItem(menuItem, menuItem.RestaurantId, t);
                    menuItems.Add(menuItem);
                }
            }

            foreach (var item in menuItems)
            {
                var restaurantKey = string.IsNullOrWhiteSpace(item.RestaurantId)
                    ? Guid.NewGuid().ToString("N")
                    : item.RestaurantId;
                item.RestaurantId = restaurantKey;

                if (!restaurants.TryGetValue(restaurantKey, out var restaurant))
                {
                    restaurant = new Restaurant
                    {
                        Id = restaurantKey,
                        Name = "Unknown Restaurant",
                        OwnerEmailAddress = string.Empty,
                        Status = false
                    };
                    restaurants[restaurant.Id] = restaurant;
                }

                item.Restaurant = restaurant;
                restaurant.MenuItems.Add(item);
            }

            var combined = new List<IItemValidating>();
            combined.AddRange(restaurants.Values);
            combined.AddRange(menuItems);
            return combined;
        }

        private static void PrepareRestaurant(Restaurant restaurant)
        {
            restaurant.Id = string.IsNullOrWhiteSpace(restaurant.Id) ? Guid.NewGuid().ToString("N") : restaurant.Id;
            restaurant.Status = false;
            restaurant.MenuItems ??= new List<MenuItem>();
        }

        private static void PrepareMenuItem(MenuItem menuItem, string? restaurantId, JToken sourceToken)
        {
            menuItem.Id = string.IsNullOrWhiteSpace(menuItem.Id) ? Guid.NewGuid().ToString("N") : menuItem.Id;
            var targetRestaurantId = !string.IsNullOrWhiteSpace(restaurantId)
                ? restaurantId
                : TryReadRestaurantId(sourceToken, menuItem.RestaurantId);
            menuItem.RestaurantId = string.IsNullOrWhiteSpace(targetRestaurantId)
                ? Guid.NewGuid().ToString("N")
                : targetRestaurantId!;
            menuItem.Status = false;
            menuItem.ImagePath = null;
        }

        private static string? TryReadRestaurantId(JToken token, string? fallback)
        {
            if (!string.IsNullOrWhiteSpace(fallback)) return fallback;

            if (token is JObject obj)
            {
                foreach (var prop in obj.Properties())
                {
                    var normalized = prop.Name.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase);
                    if (string.Equals(normalized, "restaurantid", StringComparison.OrdinalIgnoreCase))
                    {
                        return prop.Value.Value<string>();
                    }
                }
            }

            return null;
        }
    }
}
