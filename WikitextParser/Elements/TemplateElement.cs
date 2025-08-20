using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace WikitextParser.Elements
{
    /// <summary>
    /// Template element starts with {{template template-name
    /// </summary>
    public class TemplateElement : WikitextElement
    {
        private string _infoboxType;
        private IEnumerable<WikitextElement> _plainListElements;
        private IEnumerable<TemplateParameterElement> _parameters;

        public TemplateElement(string sourceText) : base(WikitextElementType.Template, sourceText)
        {
        }

        public string TemplateName { get; set; }

        public IEnumerable<TemplateParameterElement> Parameters
        {
            get
            {
                if (_parameters == null)
                {
                    _parameters = TemplateParser.ParseTemplateParameters(this).ToImmutableList();
                }
                return _parameters;
            }
        }

        public bool IsPlainlist => TemplateName.StartsWith("Plainlist|");

        public bool IsTransclusion => TemplateName.StartsWith(":");

        public bool IsInfobox => TemplateName.StartsWith("Infobox");

        public bool IsInfoboxOfType(string type) => IsInfobox && InfoboxType == type;

        public string InfoboxType
        {
            get
            {
                if (_infoboxType == null)
                {
                    _infoboxType = IsInfobox ? TemplateName.Split(new char[] { ' ' }, 2).Last() : null;
                }
                return _infoboxType;
            }
        }

        public IEnumerable<WikitextElement> GetPlainListElements()
        {
            if (!IsPlainlist)
            {
                return new WikitextElement[0];
            }

            if (_plainListElements == null)
            {
                _plainListElements = TemplateParser.ParsePlainList(this).Select(e => e).ToImmutableList();
            }
            return _plainListElements;
        }

        public override string ConvertToHtml()
        {
            if (IsPlainlist)
            {
                var items = GetPlainListElements()
                    .Select(e => $"<li>{e.ConvertToHtml()}</li>");
                return $"<ul>{string.Concat(items)}</ul>";
            }
            return "";
        }

        public override string ConvertToText()
        {
            if (IsPlainlist)
            {
                return string.Concat(GetPlainListElements().Select(e => $"\n* {e.ConvertToText()}"));
            }
            return "";
        }

        protected internal override string ToDebugString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append($"Template: {TemplateName}");
            foreach (TemplateParameterElement param in Parameters)
            {
                sb.Append($"    {param.ToDebugString()}");
            }

            return sb.ToString();
        }
    }
}