namespace AdminPanelDB.Abstractions
{
    public interface ICurrentMandantProvider
    {
        int MandantId { get; }
    }
}
