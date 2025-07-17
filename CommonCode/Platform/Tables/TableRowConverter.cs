using BFormDomain.CommonCode.Utility;
using BFormDomain.Validation;
using Newtonsoft.Json.Linq;

namespace BFormDomain.CommonCode.Platform.Tables;

public static class TableRowConverter
{
    
    public static JObject Create(
        TableRowData row,
        TableTemplate tableTemplate)
    {

        row.PropertyBag.Requires().IsNotNull();
        tableTemplate.Requires().IsNotNull();
        
        var propertyBagJson = row.PropertyBag!.ToJsonString();
        var propertyBag = JObject.Parse(propertyBagJson);
        var data = new JObject();

        foreach(var colDef in tableTemplate.Columns)
        {
            var fieldName = colDef.Field;
            var jToken = propertyBag.SelectToken(fieldName);
            if(jToken is not null)
                data.Add(fieldName, jToken);
        }

        data.Add(nameof(TableRowData.Id), row.Id);
        data.Add(nameof(TableRowData.Tags), new JArray(row.Tags));

        return data;
    }

    

}
