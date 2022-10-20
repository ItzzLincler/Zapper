namespace Zapper.Api.Data.Scrapers.Ksp
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public partial class GetProductsResult
    {
        [JsonProperty("result")]
        public Result Result { get; set; }
    }

    public partial class Result
    {
        [JsonProperty("items")]
        public Item[] Items { get; set; }

        [JsonProperty("timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        [JsonProperty("redis")]
        public string Redis { get; set; }

        [JsonProperty("filter")]
        public Dictionary<string, Filter> Filter { get; set; }

        [JsonProperty("subTypes")]
        public object[] SubTypes { get; set; }

        [JsonProperty("types")]
        public Dictionary<string, Brand> Types { get; set; }

        [JsonProperty("brands")]
        public Dictionary<string, Brand> Brands { get; set; }

        [JsonProperty("banner")]
        public Banner Banner { get; set; }

        [JsonProperty("brand")]
        public bool Brand { get; set; }

        [JsonProperty("minMax")]
        public MinMax MinMax { get; set; }

        [JsonProperty("next")]
        public long Next { get; set; }
    }

    public partial class Banner
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("image")]
        public Uri Image { get; set; }
    }

    public partial class Brand
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("img")]
        public Uri Img { get; set; }

        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("choose")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Choose { get; set; }

        [JsonProperty("upUin", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(ParseStringConverter))]
        public long? UpUin { get; set; }
    }

    public partial class Filter
    {
        [JsonProperty("catName")]
        public string CatName { get; set; }

        [JsonProperty("tags")]
        public Dictionary<string, Tag> Tags { get; set; }
    }

    public partial class Tag
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("c")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long C { get; set; }

        [JsonProperty("hide", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Hide { get; set; }

        [JsonProperty("action")]
        public string Action { get; set; }
    }

    public partial class Item
    {
        [JsonProperty("uin")]
        public long Uin { get; set; }

        [JsonProperty("uinsql")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Uinsql { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("img")]
        public Uri Img { get; set; }

        [JsonProperty("price")]
        public long Price { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("pricePerUnit")]
        public object PricePerUnit { get; set; }

        [JsonProperty("tags")]
        public Tags Tags { get; set; }

        [JsonProperty("eilatPrice")]
        public long EilatPrice { get; set; }

        [JsonProperty("redMsg")]
        public object[] RedMsg { get; set; }

        [JsonProperty("kg")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Kg { get; set; }

        [JsonProperty("hool")]
        public bool Hool { get; set; }

        [JsonProperty("pp")]
        public long Pp { get; set; }

        [JsonProperty("labels")]
        public object[] Labels { get; set; }

        [JsonProperty("brandImg")]
        public Uri BrandImg { get; set; }

        [JsonProperty("brandName")]
        public string BrandName { get; set; }

        [JsonProperty("brandTag")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long BrandTag { get; set; }

        [JsonProperty("addToCart")]
        public bool AddToCart { get; set; }

        [JsonProperty("isPickup")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long IsPickup { get; set; }
    }

    public partial class Tags
    {
        [JsonProperty("תת קטגוריה")]
        public תתקטגוריה תתקטגוריה { get; set; }

        [JsonProperty("סוג")]
        public סוג סוג { get; set; }

        [JsonProperty("יצרן")]
        public string יצרן { get; set; }

        [JsonProperty("כרטיס מסך", NullValueHandling = NullValueHandling.Ignore)]
        public string כרטיסמסך { get; set; }

        [JsonProperty("זיכרון כרטיס מסך", NullValueHandling = NullValueHandling.Ignore)]
        public גודלזכרון? זיכרוןכרטיסמסך { get; set; }

        [JsonProperty("ערכת שבבים", NullValueHandling = NullValueHandling.Ignore)]
        public ערכתשבבים? ערכתשבבים { get; set; }

        [JsonProperty("חיבורים", NullValueHandling = NullValueHandling.Ignore)]
        public string חיבורים { get; set; }

        [JsonProperty("משך אחריות", NullValueHandling = NullValueHandling.Ignore)]
        public משךאחריות? משךאחריות { get; set; }

        [JsonProperty("יבואן", NullValueHandling = NullValueHandling.Ignore)]
        public יבואן? יבואן { get; set; }

        [JsonProperty("גודל זכרון", NullValueHandling = NullValueHandling.Ignore)]
        public גודלזכרון? גודלזכרון { get; set; }
    }

    public partial class MinMax
    {
        [JsonProperty("min")]
        public long Min { get; set; }

        [JsonProperty("max")]
        public long Max { get; set; }
    }

    public enum גודלזכרון { The2Gb, The4Gb };

    public enum יבואן { יבואןמקביל, יבואןרשמי };

    public enum משךאחריות { The3שנים, שנתיים };

    public enum סוג { Amd, NVidia, מחזיקכרטיסמסך };

    public enum ערכתשבבים { GeForce, Nvidia, Radeon };

    public enum תתקטגוריה { כרטיסימסך, כרטיסימסךמארזיםעיצובותכנוןמארזים };

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                גודלזכרון_Converter.Singleton,
                יבואן_Converter.Singleton,
                משךאחריות_Converter.Singleton,
                סוג_Converter.Singleton,
                ערכתשבבים_Converter.Singleton,
                תתקטגוריה_Converter.Singleton,
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }

    internal class ParseStringConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(long) || t == typeof(long?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            long l;
            if (Int64.TryParse(value, out l))
            {
                return l;
            }
            throw new Exception("Cannot unmarshal type long");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (long)untypedValue;
            serializer.Serialize(writer, value.ToString());
            return;
        }

        public static readonly ParseStringConverter Singleton = new ParseStringConverter();
    }

    internal class גודלזכרון_Converter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(גודלזכרון) || t == typeof(גודלזכרון?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            switch (value)
            {
                case "2GB":
                    return גודלזכרון.The2Gb;
                case "4GB":
                    return גודלזכרון.The4Gb;
            }
            throw new Exception("Cannot unmarshal type גודלזכרון");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (גודלזכרון)untypedValue;
            switch (value)
            {
                case גודלזכרון.The2Gb:
                    serializer.Serialize(writer, "2GB");
                    return;
                case גודלזכרון.The4Gb:
                    serializer.Serialize(writer, "4GB");
                    return;
            }
            throw new Exception("Cannot marshal type גודלזכרון");
        }

        public static readonly גודלזכרון_Converter Singleton = new גודלזכרון_Converter();
    }

    internal class יבואן_Converter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(יבואן) || t == typeof(יבואן?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            switch (value)
            {
                case "יבואן מקביל":
                    return יבואן.יבואןמקביל;
                case "יבואן רשמי":
                    return יבואן.יבואןרשמי;
            }
            throw new Exception("Cannot unmarshal type יבואן");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (יבואן)untypedValue;
            switch (value)
            {
                case יבואן.יבואןמקביל:
                    serializer.Serialize(writer, "יבואן מקביל");
                    return;
                case יבואן.יבואןרשמי:
                    serializer.Serialize(writer, "יבואן רשמי");
                    return;
            }
            throw new Exception("Cannot marshal type יבואן");
        }

        public static readonly יבואן_Converter Singleton = new יבואן_Converter();
    }

    internal class משךאחריות_Converter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(משךאחריות) || t == typeof(משךאחריות?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            switch (value)
            {
                case "3 שנים":
                    return משךאחריות.The3שנים;
                case "שנתיים":
                    return משךאחריות.שנתיים;
            }
            throw new Exception("Cannot unmarshal type משךאחריות");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (משךאחריות)untypedValue;
            switch (value)
            {
                case משךאחריות.The3שנים:
                    serializer.Serialize(writer, "3 שנים");
                    return;
                case משךאחריות.שנתיים:
                    serializer.Serialize(writer, "שנתיים");
                    return;
            }
            throw new Exception("Cannot marshal type משךאחריות");
        }

        public static readonly משךאחריות_Converter Singleton = new משךאחריות_Converter();
    }

    internal class סוג_Converter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(סוג) || t == typeof(סוג?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            switch (value)
            {
                case "AMD":
                    return סוג.Amd;
                case "nVidia":
                    return סוג.NVidia;
                case "מחזיק כרטיס מסך":
                    return סוג.מחזיקכרטיסמסך;
            }
            throw new Exception("Cannot unmarshal type סוג");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (סוג)untypedValue;
            switch (value)
            {
                case סוג.Amd:
                    serializer.Serialize(writer, "AMD");
                    return;
                case סוג.NVidia:
                    serializer.Serialize(writer, "nVidia");
                    return;
                case סוג.מחזיקכרטיסמסך:
                    serializer.Serialize(writer, "מחזיק כרטיס מסך");
                    return;
            }
            throw new Exception("Cannot marshal type סוג");
        }

        public static readonly סוג_Converter Singleton = new סוג_Converter();
    }

    internal class ערכתשבבים_Converter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(ערכתשבבים) || t == typeof(ערכתשבבים?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            switch (value)
            {
                case "GeForce":
                    return ערכתשבבים.GeForce;
                case "Nvidia":
                    return ערכתשבבים.Nvidia;
                case "Radeon":
                    return ערכתשבבים.Radeon;
            }
            throw new Exception("Cannot unmarshal type ערכתשבבים");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (ערכתשבבים)untypedValue;
            switch (value)
            {
                case ערכתשבבים.GeForce:
                    serializer.Serialize(writer, "GeForce");
                    return;
                case ערכתשבבים.Nvidia:
                    serializer.Serialize(writer, "Nvidia");
                    return;
                case ערכתשבבים.Radeon:
                    serializer.Serialize(writer, "Radeon");
                    return;
            }
            throw new Exception("Cannot marshal type ערכתשבבים");
        }

        public static readonly ערכתשבבים_Converter Singleton = new ערכתשבבים_Converter();
    }

    internal class תתקטגוריה_Converter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(תתקטגוריה) || t == typeof(תתקטגוריה?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            switch (value)
            {
                case "כרטיסי מסך":
                    return תתקטגוריה.כרטיסימסך;
                case "כרטיסי מסך, מארזים, עיצוב ותכנון מארזים":
                    return תתקטגוריה.כרטיסימסךמארזיםעיצובותכנוןמארזים;
            }
            throw new Exception("Cannot unmarshal type תתקטגוריה");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (תתקטגוריה)untypedValue;
            switch (value)
            {
                case תתקטגוריה.כרטיסימסך:
                    serializer.Serialize(writer, "כרטיסי מסך");
                    return;
                case תתקטגוריה.כרטיסימסךמארזיםעיצובותכנוןמארזים:
                    serializer.Serialize(writer, "כרטיסי מסך, מארזים, עיצוב ותכנון מארזים");
                    return;
            }
            throw new Exception("Cannot marshal type תתקטגוריה");
        }

        public static readonly תתקטגוריה_Converter Singleton = new תתקטגוריה_Converter();
    }
}
