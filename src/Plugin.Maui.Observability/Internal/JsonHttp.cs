using System.Net.Http.Headers;
using System.Text.Json.Nodes;

namespace Plugin.Maui.Observability;

static class JsonHttp
{
    public static StringContent Content(string json)
    {
        var content = new StringContent(json, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
        return content;
    }

    public static StringContent Content(JsonNode node) => Content(node.ToJsonString());
}
