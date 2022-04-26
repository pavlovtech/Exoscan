using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using WebReaper.Abstractions.Parsers;
using WebReaper.Domain.Schema;

namespace WebReaper.Parser
{
    public class ContentParser : IContentParser
    {
        public JObject Parse(string html, SchemaElement[] schema)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            return GetJson(doc, schema);
        }

        private JObject GetJson(HtmlDocument doc, SchemaElement[] schema)
        {
            var output = new JObject();

            foreach (var item in schema)
            {
                var result = FillOutput(output, doc, item);
            }

            return output;
        }

        private HtmlNode QuerySelector(HtmlDocument doc, string selector)
        {
            return doc.DocumentNode.QuerySelector(selector);
        }

        private JObject FillOutput(JObject result, HtmlDocument doc, SchemaElement item)
        {
            HtmlNode? node = null;

            if(item.ContentType != ContentType.Nested) {
                ArgumentNullException.ThrowIfNull(item.Selector);

                node = QuerySelector(doc, item.Selector);

                if(node == null) {
                    throw new Exception($"No element found that matche the selector {item.Selector}.");
                }
            }

            bool ok = false;

            switch (item.ContentType)
            {
                case ContentType.Text:
                    var str = node?.InnerText;

                    ok = !string.IsNullOrWhiteSpace(str);

                    if(ok) {
                        result[item.Field] = item.Transform(str);
                    } else {
                        throw new Exception($"Cannot find image link in {node?.OuterHtml}.");
                    }
                    break;
                case ContentType.Image:
                    var value = node?.GetAttributeValue("title", "");

                    ok = !string.IsNullOrWhiteSpace(value);

                    if(ok) {
                        result[item.Field] = item.Transform(value);
                    } else {
                        throw new Exception($"Cannot find image link in {node?.OuterHtml}.");
                    }
                    break;
                case ContentType.Html:
                    var html = node?.InnerHtml;

                    ok = !string.IsNullOrWhiteSpace(html);

                    if(ok) {
                        result[item.Field] = item.Transform(html);
                    } else {
                        throw new Exception($"No html found in convert {html}.");
                    }
                    break;
                case ContentType.Url:
                    var url = node?.GetAttributeValue("href", "");

                    ok = !string.IsNullOrWhiteSpace(url);

                    if(ok) {
                        result[item.Field] = item.Transform(url);
                    } else {
                        throw new Exception($"No href attribute found in {node}.");
                    }
                    break;
                case ContentType.Nested: 

                    if(item.Children == null) {
                        throw new Exception("ContentType is incorrect, node has no children");
                    }

                    var obj = new JObject();
                    
                    foreach(var el in item.Children) {
                        var child = FillOutput(obj, doc, el);
                    }
                    result[item.Field] = obj;
                    break;
            }

            return result;
        }
    }
}