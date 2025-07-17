using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using BFormDomain.CommonCode.Platform.Authorization;

namespace BFormDomain.CommonCode.Platform.Tables;

public class TableViewModel
{
    public string Name { get; set; } = null!;
    public List<string> Tags { get; set; } = new();
    public string? Title { get; set; } = null!;
    public string? Description { get; set; }

    public bool IsVisibleToUsers { get; set; } = true;
    public bool IsUserEditAllowed { get; set; }
    public bool IsUserDeleteAllowed { get; set; }
    public bool IsUserAddAllowed { get; set; }

    public bool DisplayMasterDetail { get; set; }
    public string? DetailFormTemplate { get; set; }

    public string? IconClass { get; set; }

    public List<ColDef> Columns { get; set; } = new();
    public string? AgGridColumnDefsJson { get; set; }

    public List<JObject> Data { get; set; } = new();//Could be this aswell this would be easier to fix

    [JsonIgnore]
    public List<TableRowData> RawData { get; set; } = new();//It could be this causing the issue not sure how to side step this

    public DataSet MakeDataSet(string tzId)
    {
        var localTz = TimeZoneInfo.FromSerializedString(tzId);

        var ds = new DataSet();
        var dt = new DataTable();

        foreach(var cd in Columns)
        {
            switch(cd.Type)
            {
                case JTokenType.Integer:
                    dt.Columns.Add(cd.Field, typeof(int));
                    break;
                case JTokenType.Float:
                    dt.Columns.Add(cd.Field, typeof(string)); // to apply formatting
                    break;
                case JTokenType.String:
                    dt.Columns.Add(cd.Field, typeof(string));  
                    break;
                case JTokenType.Boolean:
                    dt.Columns.Add(cd.Field, typeof(bool));
                    break;
                case JTokenType.Date:
                    dt.Columns.Add(cd.Field, typeof(string)); // for formatting
                    break;
                case JTokenType.Uri:
                    dt.Columns.Add(cd.Field, typeof(string));
                    break;
            }
        }


        foreach(var sourceRow in Data)
        {
            var row = dt.NewRow();

            foreach(var cd in Columns)
            {
                switch(cd.Type)
                {
                    case JTokenType.Integer:
                        row[cd.Field] = (int)sourceRow[cd.Field]!;
                        break;
                    case JTokenType.Float:
                        var fltData = (float)sourceRow[cd.Field]!;
                        row[cd.Field] = fltData.ToString("0.00");
                        break;
                    case JTokenType.String:
                        row[cd.Field] = (string)sourceRow[cd.Field]!;
                        break;
                    case JTokenType.Boolean:
                        row[cd.Field] = (bool)sourceRow[cd.Field]!;
                        break;
                    case JTokenType.Date:
                        var dtData = (DateTime)sourceRow[cd.Field]!;
                        var local = TimeZoneInfo.ConvertTimeFromUtc(dtData, localTz);
                        row[cd.Field] = $"{local.ToShortDateString()} {local.ToShortTimeString()}";
                        break;
                    case JTokenType.Uri:
                        var uriData = (Uri) sourceRow[cd.Field]!;
                        var cvt = uriData.ToString();
                        row[cd.Field] = cvt;
                        break;
                }
            }

            dt.Rows.Add(row);
        }

        dt.AcceptChanges();
        ds.Tables.Add(dt);
        return ds;

    }


    public static TableViewModel Create(
        IEnumerable<TableRowData> data,
        TableTemplate template)
    {
        return new TableViewModel
        {
            Name = template.Name,
            Tags = template.Tags,
            Title = template.Title,
            Description = template.Description,
            IsVisibleToUsers = template.IsVisibleToUsers,
            IsUserEditAllowed = template.IsUserEditAllowed,
            IsUserDeleteAllowed = template.IsUserDeleteAllowed,
            IsUserAddAllowed = template.IsUserAddAllowed,
            DisplayMasterDetail = template.DisplayMasterDetail,
            DetailFormTemplate = template.DetailFormTemplate,
            IconClass = template.IconClass,
            Columns = template.Columns.ToList(),
            AgGridColumnDefsJson = template.AgGridColumnDefs?.Json?.ToString(),
            RawData = data.ToList(),
            Data = data.Select(tr=>TableRowConverter.Create(tr, template)).ToList()
        };
    }

}
