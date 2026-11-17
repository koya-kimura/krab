using System.Collections.Generic;
using RosettaUI;

namespace Krab.Common.Views
{
    /// <summary>
    /// A UI target split into several windows. UiBuilder adds one launcher per window, titled "Title - Name",
    /// instead of one window built from CreateElement.
    /// </summary>
    public interface IUiWindowsTarget
    {
        public IReadOnlyList<(string Name, Element Content)> CreateWindows();
    }
}
