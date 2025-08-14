namespace NetRoll.Models
{
    public record MenuItem(
        string Title,
        string Icon,
        string Url,
        List<MenuItem>? Children = null
    );
}
