using System;

namespace Krab.Common.Views
{
    /// <summary>Declares a settings path, title and display order independently of UiBuilder.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class UiTargetAttribute : Attribute
    {
        public string CategoryPath { get; }
        public string Title { get; }
        public int Order { get; }

        // '/' separates categories. The first segment becomes a top-level tab.
        public UiTargetAttribute(string categoryPath, string title, int order = 0)
        {
            CategoryPath = categoryPath;
            Title = title;
            Order = order;
        }
    }
}
