namespace WikitextParser.Elements
{
    public class BehaviorSwitchElement : WikitextElement
    {
        public string Name { get; }
        public BehaviorSwitchElement(string sourceText) : base(WikitextElementType.BehaviorSwitch, sourceText)
        { Name = sourceText.Substring(2, sourceText.Length - 4); }
        public override string ConvertToHtml() => "";
        public override string ConvertToText() => "";
        protected internal override string ToDebugString() => "Behavior switch: " + Name;
    }
}
