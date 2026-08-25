namespace Project_Nook.Models
{
    /// <summary>
    /// What the notch shows while expanded. The Shelf proved this pattern with a single bool; widgets that
    /// need more room than a 64x64 tile claim a page of their own the same way.
    /// </summary>
    public enum ExpandedPage
    {
        Dashboard,
        Shelf,
        Notes,
        Clipboard
    }
}
