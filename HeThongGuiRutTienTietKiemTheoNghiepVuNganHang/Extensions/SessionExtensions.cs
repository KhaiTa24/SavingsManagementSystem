using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace HeThongGuiRutTienTietKiemTheoNghiepVuNganHang.Extensions
{
    public static class SessionExtensions
    {
        public static void SetBool(this ISession session, string key, bool value)
        {
            session.SetString(key, value.ToString());
        }

        public static bool? GetBool(this ISession session, string key)
        {
            var value = session.GetString(key);
            if (bool.TryParse(value, out bool result))
            {
                return result;
            }
            return null;
        }

        public static void SetObject<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        public static T GetObject<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default(T) : JsonSerializer.Deserialize<T>(value);
        }
    }
}