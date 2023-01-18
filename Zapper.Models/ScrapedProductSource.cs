
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Zapper.Models
{
	[JsonConverter(typeof(StringEnumConverter))]
    public enum ScrapedProductSource
    {
        TMS,
        KSP,
        Ivory,
        Bug,
        PC_Center,
        SHIRION,
        AA_Computers,
        CompMaster,
        Images
    }

}
