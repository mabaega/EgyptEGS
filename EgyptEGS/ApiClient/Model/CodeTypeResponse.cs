namespace EgyptEGS.ApiClient.Model
{
    public class CodeTypeResponse
    {
        public List<CodeTypeItem> Result { get; set; } = new();
    }

    public class CodeTypeItem
    {
        public string ItemCode { get; set; }
        public string CodeNamePrimaryLang { get; set; }
    }
}