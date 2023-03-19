namespace NyaProxy.API.Config
{
    public class ConfigComment
    {
        public ConfigComment()
        {

        }

        public ConfigComment(string preceding)
        {
            Preceding = preceding;
        }

        public ConfigComment(string preceding, string inline)
        {
            Preceding = preceding;
            Inline = inline;
        }

        public string Preceding { get; set; }
        public string Inline { get; set; }
    }
}
