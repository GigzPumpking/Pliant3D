/// <summary>
/// Implemented by any object that can be picked up and tracked by a FetchObjective.
/// </summary>
public interface IFetchable
{
    bool isFetched { get; }
    void SetFetchedSilently();
}
