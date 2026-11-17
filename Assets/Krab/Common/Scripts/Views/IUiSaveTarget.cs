namespace Krab.Common.Views
{
    /// <summary>A UI target with its own data to save; UiBuilder calls Save after Prefs on the save shortcut.</summary>
    public interface IUiSaveTarget
    {
        public void Save();
    }
}
