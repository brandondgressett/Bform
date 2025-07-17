using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Content;

public class SatelliteJson
{
    public SatelliteJson(IContentType host, string name)
    {
        _host = host;
        _name = name.ToLowerInvariant();
    }

    private IContentType _host;
    private string _name;
    private JObject? _json = null!;

    public JObject? Json 
    {
        get 
        {
            var satellites = _host.SatelliteData!;
            var present = satellites.ContainsKey(_name);

            if (_json is null && present)
            {
                var text = satellites[_name];
                _json = JObject.Parse(text);
            }

            return _json;
        }
    }

}
